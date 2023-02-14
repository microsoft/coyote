// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// The context of the execution implemented as an acyclic directed graph where nodes
    /// are controlled operations and steps are transitions between operations.
    /// </summary>
    internal sealed class ExecutionContext : IEnumerable, IEnumerable<ExecutionContext.Node>
    {
        /// <summary>
        /// The nodes in this execution context.
        /// </summary>
        private readonly List<Node> Nodes;

        /// <summary>
        /// Map from operation states to operation group ids.
        /// </summary>
        internal readonly Dictionary<ulong, Guid> OperationGroupStateMap;

        /// <summary>
        /// Map from operation group ids to their concurrent creation counters.
        /// </summary>
        internal readonly Dictionary<Guid, ulong> DegreeConcurrentCreations;

        /// <summary>
        /// Cache mapping operation ids to their last node occurrence in the current execution.
        /// </summary>
        private readonly Dictionary<ulong, Node> LastNodeForOperationCache;

        /// <summary>
        /// Set of operation groups that perform at least one write access during their lifetimes.
        /// </summary>
        internal readonly HashSet<Guid> WriteAccessOperationGroups;

        /// <summary>
        /// Map from operation states to their visited frequencies in the current execution.
        /// </summary>
        internal readonly Dictionary<ulong, ulong> OperationStateFrequencies;

        /// <summary>
        /// Map containing coverage information across executions. It maps call site transitions.
        /// </summary>
        internal readonly Dictionary<string, HashSet<string>> CoverageMap;

        /// <summary>
        /// The current node in the execution.
        /// </summary>
        private Node CurrentNode;

        /// <summary>
        /// The number of nodes in the context.
        /// </summary>
        internal int Length
        {
            get { return this.Nodes.Count; }
        }

        /// <summary>
        /// Indexes the context.
        /// </summary>
        internal Node this[int index]
        {
            get { return this.Nodes[index]; }
            set { this.Nodes[index] = value; }
        }

        // TODO: add logic that assigns a new operation group id per created operation, and then does pruning from accumulated knowledge.

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionContext"/> class.
        /// </summary>
        private ExecutionContext()
        {
            this.Nodes = new List<Node>();
            this.OperationGroupStateMap = new Dictionary<ulong, Guid>();
            this.LastNodeForOperationCache = new Dictionary<ulong, Node>();
            this.WriteAccessOperationGroups = new HashSet<Guid>();
            this.OperationStateFrequencies = new Dictionary<ulong, ulong>();
            this.DegreeConcurrentCreations = new Dictionary<Guid, ulong>();
            this.CoverageMap = new Dictionary<string, HashSet<string>>();
            this.CurrentNode = null;
        }

        /// <summary>
        /// Creates a new <see cref="ExecutionContext"/>.
        /// </summary>
        internal static ExecutionContext Create() => new ExecutionContext();

        /// <summary>
        /// Applies a creation transition to the specified operation in the execution context.
        /// </summary>
        internal void ApplyCreation(ControlledOperation operation, ulong explicitState, ulong totalState) =>
            this.Apply(operation, explicitState, totalState, StepCategory.Creation);

        /// <summary>
        /// Applies an invocation transition to the specified operation in the execution context.
        /// </summary>
        internal void ApplyInvocation(ControlledOperation operation, ulong explicitState, ulong totalState) =>
            this.Apply(operation, explicitState, totalState, StepCategory.Invocation);

        /// <summary>
        /// Applies a context switch transition to the specified operation in the execution context.
        /// </summary>
        internal void ApplyContextSwitch(ControlledOperation operation, ulong explicitState, ulong totalState) =>
            this.Apply(operation, explicitState, totalState, StepCategory.ContextSwitch);

        /// <summary>
        /// Applies a context switch transition to the specified operation in the execution context.
        /// </summary>
        private void Apply(ControlledOperation operation, ulong explicitState, ulong totalState, StepCategory category)
        {
            // Check if this is a newly created operation.
            bool isNewOperation = operation.Status is OperationStatus.None;

            // Check if the node already exists from a previous test iteration.
            // operation.Runtime.LogWriter.LogImportant($">>> APPLY NODE FOR {operation.DebugInfo} (IN:{category} | STATE:{operation.LastHashedState})");
            Node node = this.FindNode(operation);
            if (node is null)
            {
                // This is a new node, so first, find the predecessor node in the execution, if one exists.
                // This is either the node corresponding to the last call site of the same operation, or the
                // node corresponding to its parent operation.
                Node predecessorNode = this.Length > 0 ? this.GetPredecessorNode(operation) : null;

                // Next, create a node corresponding to the latest call site invoked by the operation.
                // Else, if it has no known call sites so far and this is the root operation, assign
                // a dummy 'Test' call site, else assign the call site of its parent operation.
                string callSite = operation.CallSiteSequence.Count > 0 ? operation.LastCallSite :
                    predecessorNode is null ? "Test" : predecessorNode.CallSite;
                node = new Node(this, operation, predecessorNode, callSite, explicitState, totalState);

                // Next, connect the new node with the predecessor node, if a predecessor exists.
                predecessorNode?.AddStep(node, category);
                this.Nodes.Add(node);
            }

            if (this.CurrentNode is null || !isNewOperation)
            {
                // This is the first node or the step category is not 'Creation', so set it as the current node.
                this.CurrentNode = node;
            }

            this.LastNodeForOperationCache[operation.Id] = node;
        }

        /// <summary>
        /// Assigns an operation group for the specified operation.
        /// </summary>
        internal OperationGroup AssignOperationGroup(ControlledOperation operation)
        {
            OperationGroup group;
            if (this.Length is 0)
            {
                // The context is empty, so assign a new operation group.
                group = OperationGroup.Create(Guid.NewGuid());
            }
            else if (operation.Id is 0)
            {
                // TODO: make it so that it maps to pre-existing nodes.
                // This is the first operation, so assign the operation group of the root node in the context.
                group = OperationGroup.Create(this.Nodes[0].GroupId);
            }
            else if (this.OperationGroupStateMap.TryGetValue(this.CurrentNode.OperationState, out Guid id))
            {
                // A new group operation has been created at this operation state before, so retrieve it.
                group = OperationGroup.Create(id);
            }
            else
            {
                group = OperationGroup.Create(Guid.NewGuid());
            }

            if (this.CurrentNode != null && !this.OperationGroupStateMap.ContainsKey(this.CurrentNode.OperationState))
            {
                this.OperationGroupStateMap.Add(this.CurrentNode.OperationState, group.Id);
            }

            // Assign the operation as a member of its group.
            group.AddMember(operation);
            return group;
        }

        /// <summary>
        /// Finds a node in the context that matches the specified operation, if it exists.
        /// </summary>
        private Node FindNode(ControlledOperation operation)
        {
            Node result = null;
            if (operation.IsRoot && operation.Status is OperationStatus.None && this.Length > 0)
            {
                // If we are at the root operation at the moment it is created, then just return that node.
                result = this.Nodes[0];
            }
            else
            {
                // Else, find the last occurrence of the operation, if such a predecessor node is available, and check
                // if a previously seen edge exists from that node that leads to the current state of this operation.
                Node predecessorNode = this.GetPredecessorNode(operation);
                if (predecessorNode != null)
                {
                    // operation.Runtime.LogWriter.LogImportant($"   [-] id:{predecessorNode.GroupId} | out:{predecessorNode.OutSteps.Count} | state:{predecessorNode.OperationState}");
                    // If the current node is not the last node in the context, then check if we are replaying a known node.
                    foreach (var step in predecessorNode.OutSteps)
                    {
                        // operation.Runtime.LogWriter.LogImportant($"      [-] step:{step.Category} | id:{step.Target.GroupId} | state:{step.Target.OperationState}");
                        if (step.Target.OperationState == operation.LastHashedState)
                        {
                            result = step.Target;
                            break;
                        }
                    }
                }
            }

            // operation.Runtime.LogWriter.LogImportant($">>> FOUND NODE FOR {operation.DebugInfo} (STATE:{operation.LastHashedState}): {result != null}");
            return result;
        }

        /// <summary>
        /// Returns the predecessor node for the specified operation, if it exists.
        /// </summary>
        /// <remarks>
        /// This is either the node corresponding to the last call site of the same operation,
        /// or the node corresponding to its parent operation.
        /// </remarks>
        private Node GetPredecessorNode(ControlledOperation operation) =>
            operation.Status is OperationStatus.None ? this.CurrentNode : this.GetLastNodeForOperation(operation);

        internal void PrettyPrint()
        {
            foreach (var node in this.Nodes)
            {
                // Console.WriteLine($" >>> NODE:{node.Index} | group:{node.GroupId} | DEPTH:{node.Depth} | OP-STATE:{node.OperationState} | STATE:{node.ProgramState} | READ:{node.IsReadOnly} | IN:{node.InStep?.Category} | STEPS:{node.OutSteps.Count} | @{node.CallSite}");
            }
        }

        /// <summary>
        /// Returns the last node corresponding to the specified operation id.
        /// </summary>
        internal Node GetLastNodeForOperation(ControlledOperation operation) =>
            this.LastNodeForOperationCache.TryGetValue(operation.Id, out Node node) ? node : null;

        /// <summary>
        /// Returns the frequency of visiting the current state of the specified operation.
        /// </summary>
        internal ulong GetStateFrequencyOfOperation(ControlledOperation operation) =>
            this.OperationStateFrequencies[operation.GetGroupAgnosticLatestHashedState()];

        /// <summary>
        /// Returns true if the specified operation group is read-only, else false.
        /// </summary>
        internal bool IsOperationGroupReadOnly(OperationGroup group) => !this.WriteAccessOperationGroups.Contains(group.Id);

        /// <summary>
        /// Returns an enumerator.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Nodes.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator.
        /// </summary>
        IEnumerator<Node> IEnumerable<Node>.GetEnumerator()
        {
            return this.Nodes.GetEnumerator();
        }

        /// <summary>
        /// Clears the context.
        /// </summary>
        internal void Clear()
        {
            this.LastNodeForOperationCache.Clear();
            this.OperationStateFrequencies.Clear();
            this.CurrentNode = null;
        }

        /// <summary>
        /// Represents a node in the execution context.
        /// </summary>
        internal sealed class Node : IEquatable<Node>, IComparable<Node>
        {
            /// <summary>
            /// The execution context containing this node.
            /// </summary>
            internal readonly ExecutionContext Context;

            /// <summary>
            /// The unique index of this execution node.
            /// </summary>
            internal readonly int Index;

            /// <summary>
            /// The id of the operation group associated with this node.
            /// </summary>
            internal readonly Guid GroupId;

            /// <summary>
            /// The call site associated with this node.
            /// </summary>
            internal readonly string CallSite;

            /// <summary>
            /// A hash value that represents the operation state associated with this node.
            /// </summary>
            internal readonly ulong OperationState;

            /// <summary>
            /// A hash value that represents the explicit program state associated with this node.
            /// </summary>
            internal readonly ulong ExplicitProgramState;

            /// <summary>
            /// A hash value that represents the total program state associated with this node.
            /// </summary>
            internal readonly ulong ProgramState;

            /// <summary>
            /// True if this node modified the explicit program state.
            /// </summary>
            internal readonly bool IsReadOnly;

            /// <summary>
            /// A value representing the depth of this node from the root.
            /// </summary>
            internal int Depth => this.InStep?.Depth ?? 0;

            /// <summary>
            /// The ingoing steps of this node.
            /// </summary>
            internal Step InStep { get; private set; }

            /// <summary>
            /// The outgoing steps of this node.
            /// </summary>
            internal readonly List<Step> OutSteps;

            /// <summary>
            /// Initializes a new instance of the <see cref="Node"/> class.
            /// </summary>
            internal Node(ExecutionContext context, ControlledOperation operation, Node predecessor,
                string callSite, ulong explicitState, ulong totalState)
            {
                this.Context = context;
                this.Index = context.Length;
                this.GroupId = operation.Group.Id;
                this.CallSite = callSite;
                this.OperationState = operation.LastHashedState;
                this.ExplicitProgramState = explicitState;
                this.ProgramState = totalState;
                this.InStep = null;
                this.OutSteps = new List<Step>();

                // Check if the node is read-only or not in regards to explicit program state.
                this.IsReadOnly = predecessor is null ? false : predecessor.ExplicitProgramState == explicitState;
                if (!this.IsReadOnly)
                {
                    context.WriteAccessOperationGroups.Add(this.GroupId);
                }

                if (operation.Runtime.Configuration.IsExecutionTraceCycleReductionEnabled)
                {
                    ulong groupAgnosticState = operation.GetGroupAgnosticLatestHashedState();
                    if (this.Context.OperationStateFrequencies.TryGetValue(groupAgnosticState, out ulong frequency))
                    {
                        this.Context.OperationStateFrequencies[groupAgnosticState] = frequency + 1;
                    }
                    else
                    {
                        this.Context.OperationStateFrequencies.Add(groupAgnosticState, 1);
                    }
                }
            }

            /// <summary>
            /// Adds a new step to the target node.
            /// </summary>
            internal void AddStep(Node target, StepCategory category)
            {
                Step step = new Step(this, target, category);
                this.OutSteps.Add(step);
                step.Target.InStep = step;

                // Cache the new step to track coverage.
                if (step.Category is StepCategory.Creation || step.Category is StepCategory.Invocation ||
                    this.CallSite != step.Target.CallSite)
                {
                    if (this.Context.CoverageMap.TryGetValue(this.CallSite, out var targetMap))
                    {
                        targetMap.Add(step.Target.CallSite);
                    }
                    else
                    {
                        this.Context.CoverageMap.Add(this.CallSite, new HashSet<string> { step.Target.CallSite });
                    }
                }
            }

            /// <inheritdoc/>
            public override int GetHashCode() => this.Index.GetHashCode();

            /// <summary>
            /// Indicates whether the specified <see cref="Node"/> is equal
            /// to the current <see cref="Node"/>.
            /// </summary>
            internal bool Equals(Node other) => other is Node node ?
                this.Index == node.Index :
                false;

            /// <inheritdoc/>
            public override bool Equals(object obj) => this.Equals(obj as Node);

            /// <summary>
            /// Indicates whether the specified <see cref="Node"/> is equal
            /// to the current <see cref="Node"/>.
            /// </summary>
            bool IEquatable<Node>.Equals(Node other) => this.Equals(other);

            /// <summary>
            /// Compares the specified <see cref="Node"/> with the current
            /// <see cref="Node"/> for ordering or sorting purposes.
            /// </summary>
            int IComparable<Node>.CompareTo(Node other) => this.Index - other.Index;
        }

        /// <summary>
        /// Represents a step in the execution context.
        /// </summary>
        internal sealed class Step
        {
            /// <summary>
            /// The source execution node.
            /// </summary>
            internal readonly Node Source;

            /// <summary>
            /// The target execution node.
            /// </summary>
            internal readonly Node Target;

            /// <summary>
            /// The step category.
            /// </summary>
            internal readonly StepCategory Category;

            /// <summary>
            /// A value representing the depth of this step from the root.
            /// </summary>
            internal readonly int Depth;

            /// <summary>
            /// The execution context containing this step.
            /// </summary>
            internal ExecutionContext Context => this.Source.Context;

            /// <summary>
            /// Initializes a new instance of the <see cref="Step"/> class.
            /// </summary>
            internal Step(Node source, Node target, StepCategory category)
            {
                this.Source = source;
                this.Target = target;
                this.Category = category;

                // Calculate the depth of the step by incrementing the depth of the source node.
                this.Depth = this.Source.Depth + 1;
            }
        }

        /// <summary>
        /// The step category.
        /// </summary>
        internal enum StepCategory
        {
            Creation = 0,
            Invocation,
            ContextSwitch
        }
    }
}
