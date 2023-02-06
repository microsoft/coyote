// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// An execution graph where nodes represent executing operations and edges represent
    /// execution steps by each operation.
    /// </summary>
    internal sealed class ExecutionGraph : IEnumerable, IEnumerable<ExecutionGraph.Node>
    {
        /// <summary>
        /// The nodes in this execution graph.
        /// </summary>
        private readonly List<Node> Nodes;

        /// <summary>
        /// First node index per operation id.
        /// </summary>
        private readonly Dictionary<ulong, Node> FirstNodeForOperation;

        /// <summary>
        /// Last node index per operation id.
        /// </summary>
        private readonly Dictionary<ulong, Node> LastNodeForOperation;

        /// <summary>
        /// Last visited call site index per operation id.
        /// </summary>
        private readonly Dictionary<ulong, int> LastVisitedCallSiteIndexForOperation;

        /// <summary>
        /// Map containing the frequencies of executing each call site per operation id.
        /// </summary>
        private readonly Dictionary<ulong, Dictionary<string, ulong>> CallSiteFrequenciesForOperation;

        /// <summary>
        /// Map containing the ranks of each call site per operation id.
        /// </summary>
        private readonly Dictionary<ulong, Dictionary<string, ulong>> CallSiteRanksForOperation;

        /// <summary>
        /// The number of nodes in the graph.
        /// </summary>
        internal int Length
        {
            get { return this.Nodes.Count; }
        }

        /// <summary>
        /// Indexes the graph.
        /// </summary>
        internal Node this[int index]
        {
            get { return this.Nodes[index]; }
            set { this.Nodes[index] = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionGraph"/> class.
        /// </summary>
        private ExecutionGraph()
        {
            this.Nodes = new List<Node>();
            this.FirstNodeForOperation = new Dictionary<ulong, Node>();
            this.LastNodeForOperation = new Dictionary<ulong, Node>();
            this.LastVisitedCallSiteIndexForOperation = new Dictionary<ulong, int>();
            this.CallSiteFrequenciesForOperation = new Dictionary<ulong, Dictionary<string, ulong>>();
            this.CallSiteRanksForOperation = new Dictionary<ulong, Dictionary<string, ulong>>();
        }

        /// <summary>
        /// Creates a new <see cref="ExecutionGraph"/>.
        /// </summary>
        internal static ExecutionGraph Create() => new ExecutionGraph();

        /// <summary>
        /// Adds the execution step associated with the specified operation to the graph.
        /// </summary>
        internal void Add(ControlledOperation operation)
        {
            var writer = CoyoteRuntime.Current.LogWriter;

            // Create the call site frequency & rank map if this is the first time executing this operation.
            if (!this.CallSiteFrequenciesForOperation.TryGetValue(operation.Id, out var callSiteFrequencies) ||
                !this.CallSiteRanksForOperation.TryGetValue(operation.Id, out var callSiteRanks))
            {
                callSiteFrequencies = new Dictionary<string, ulong>();
                callSiteRanks = new Dictionary<string, ulong>();
                this.CallSiteFrequenciesForOperation.Add(operation.Id, callSiteFrequencies);
                this.CallSiteRanksForOperation.Add(operation.Id, callSiteRanks);
            }

            // Create sequence of nodes corresponding to call sites invoked by the operation since its previous node occurrence.
            var nodes = new List<Node>();
            this.LastVisitedCallSiteIndexForOperation.TryGetValue(operation.Id, out int lastVisitedCallSiteIndex);
            foreach (var callSite in operation.VisitedCallSites.Skip(lastVisitedCallSiteIndex))
            {
                Node previousNode = nodes.LastOrDefault();
                Node callSiteNode = new Node(this, operation.Id, operation.SequenceId, callSite, operation.LastHashedProgramState);
                nodes.Add(callSiteNode);
                if (previousNode != null)
                {
                    Edge edge = new InvocationEdge(previousNode, callSiteNode);
                    previousNode.AddEdge(edge);
                }

                // Cache the frequency and rank of the call site.
                if (callSiteFrequencies.TryGetValue(callSite, out ulong callSiteFrequency) &&
                    callSiteRanks.TryGetValue(callSite, out ulong callSiteRank))
                {
                    callSiteFrequencies[callSite] = callSiteFrequency + 1;
                    callSiteRanks[callSite] = (ulong)operation.VisitedCallSites.Count;
                }
                else
                {
                    callSiteFrequencies.Add(callSite, 1);
                    callSiteRanks.Add(callSite, (ulong)lastVisitedCallSiteIndex);
                }

                lastVisitedCallSiteIndex++;
            }

            if (nodes.Count is 0)
            {
                // If there are no new call sites invoked by the operation,
                // then create a new node from the last visited call site.
                string callSite;
                if (operation.VisitedCallSites.Count > 0)
                {
                    callSite = operation.VisitedCallSites[lastVisitedCallSiteIndex - 1];
                }
                else if (operation.IsRoot)
                {
                    // This is the root operation, and has no known call sites so far, so assign a dummy 'Test' call site.
                    callSite = "Test";
                }
                else
                {
                    // This operation has not visited any call sites yet, so use the call site of its parent operation.
                    Node parentNode = this.LastNodeForOperation[operation.ParentId];
                    callSite = parentNode.CallSite;
                }

                Node callSiteNode = new Node(this, operation.Id, operation.SequenceId, callSite, operation.LastHashedProgramState);
                nodes.Add(callSiteNode);
            }
            else
            {
                this.LastVisitedCallSiteIndexForOperation[operation.Id] = operation.VisitedCallSites.Count;
            }

            if (this.Length > 0)
            {
                // Get the first node in the sequence of nodes corresponding to the operation.
                Node node = nodes.First();

                // Only add an edge if there is at least one node in the graph.
                if (!this.LastNodeForOperation.ContainsKey(operation.Id))
                {
                    // This operation is new, so connect it with its parent operation.
                    Node parentNode = this.LastNodeForOperation[operation.ParentId];
                    Edge edge = new CreationEdge(parentNode, node);
                    parentNode.AddEdge(edge);
                }
                else
                {
                    // This operation is not new, so connect it to the previous node corresponding to the same operation.
                    Node previousNode = this.LastNodeForOperation[operation.Id];
                    Edge edge = new StepEdge(previousNode, node);
                    previousNode.AddEdge(edge);
                }
            }

            this.Nodes.AddRange(nodes);
            this.LastNodeForOperation[operation.Id] = nodes.Last();
            if (!this.FirstNodeForOperation.ContainsKey(operation.Id))
            {
                // Cache the first node index for this operation.
                this.FirstNodeForOperation.Add(operation.Id, nodes.First());
            }
        }

        /// <summary>
        /// Adds any missing invocation edges associated with the specified operation to the graph.
        /// </summary>
        private void AddInvocationEdges(ControlledOperation operation)
        {
            if (this.Length > 0)
            {
                // Only add an edge if there is at least one node in the graph.
                if (!this.LastNodeForOperation.ContainsKey(operation.Id))
                {
                    // This operation is new, so connect it with its parent operation.
                    Node parentNode = this.LastNodeForOperation[operation.ParentId];
                    Node node = new Node(this, operation.Id, operation.SequenceId, operation.LastCallSite, operation.LastHashedProgramState);
                    Edge edge = new CreationEdge(parentNode, node);
                    parentNode.AddEdge(edge);
                    this.Nodes.Add(node);
                    this.LastNodeForOperation[operation.Id] = node;
                }
                else
                {
                    // This operation is not new, so connect it to the previous node corresponding to the same operation.
                    Node previousNode = this.LastNodeForOperation[operation.Id];
                    Node node = new Node(this, operation.Id, operation.SequenceId, operation.LastCallSite, operation.LastHashedProgramState);
                    Edge edge = new StepEdge(previousNode, node);
                    previousNode.AddEdge(edge);
                    this.Nodes.Add(node);
                    this.LastNodeForOperation[operation.Id] = node;
                }
            }
        }

        /// <summary>
        /// Returns the first node corresponding to the specified operation id.
        /// </summary>
        internal Node GetFirstNodeForOperation(ulong operationId) =>
            this.FirstNodeForOperation.TryGetValue(operationId, out Node node) ? node : null;

        /// <summary>
        /// Returns the last node corresponding to the specified operation id.
        /// </summary>
        internal Node GetLastNodeForOperation(ulong operationId) =>
            this.LastNodeForOperation.TryGetValue(operationId, out Node node) ? node : null;

        /// <summary>
        /// Returns the frequency for the specified call site invoked by the operation with the given id.
        /// </summary>
        internal ulong GetCallSiteFrequency(ulong operationId, string callSite) =>
            this.CallSiteFrequenciesForOperation.TryGetValue(operationId, out var callSiteFrequencies) ?
            callSiteFrequencies.TryGetValue(callSite, out ulong rank) ?
            rank : 0 : 0;

        /// <summary>
        /// Returns the lowest frequency call site invoked by the operation with the specified id.
        /// </summary>
        internal string GetLowestCallSiteFrequencyForOperation(ulong operationId) =>
            this.CallSiteFrequenciesForOperation.TryGetValue(operationId, out var callSiteFrequencies) ?
            callSiteFrequencies.Aggregate((l, r) => l.Value < r.Value ? l : r).Key :
            null;

        /// <summary>
        /// Returns the highest frequency call site invoked by the operation with the specified id.
        /// </summary>
        internal string GetHighestCallSiteFrequencyForOperation(ulong operationId) =>
            this.CallSiteFrequenciesForOperation.TryGetValue(operationId, out var callSiteFrequencies) ?
            callSiteFrequencies.Aggregate((l, r) => l.Value > r.Value ? l : r).Key :
            null;

        /// <summary>
        /// Returns the rank for the specified call site invoked by the operation with the given id.
        /// </summary>
        internal ulong GetCallSiteRank(ulong operationId, string callSite) =>
            this.CallSiteRanksForOperation.TryGetValue(operationId, out var callSiteRanks) ?
            callSiteRanks.TryGetValue(callSite, out ulong rank) ?
            rank : 0 : 0;

        /// <summary>
        /// Returns the lowest rank call site invoked by the operation with the specified id.
        /// </summary>
        internal string GetLowestCallSiteRankForOperation(ulong operationId) =>
            this.CallSiteRanksForOperation.TryGetValue(operationId, out var callSiteRanks) ?
            callSiteRanks.Aggregate((l, r) => l.Value < r.Value ? l : r).Key :
            null;

        /// <summary>
        /// Returns the highest rank call site invoked by the operation with the specified id.
        /// </summary>
        internal string GetHighestCallSiteRankForOperation(ulong operationId) =>
            this.CallSiteRanksForOperation.TryGetValue(operationId, out var callSiteRanks) ?
            callSiteRanks.Aggregate((l, r) => l.Value > r.Value ? l : r).Key :
            null;

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
        /// Clears the graph.
        /// </summary>
        internal void Clear()
        {
            this.Nodes.Clear();
            this.FirstNodeForOperation.Clear();
            this.LastNodeForOperation.Clear();
            this.LastVisitedCallSiteIndexForOperation.Clear();
            this.CallSiteFrequenciesForOperation.Clear();
            this.CallSiteRanksForOperation.Clear();
        }

        /// <summary>
        /// Represents a node in the execution graph.
        /// </summary>
        internal sealed class Node : IEquatable<Node>, IComparable<Node>
        {
            /// <summary>
            /// The execution graph containing this node.
            /// </summary>
            internal readonly ExecutionGraph Graph;

            /// <summary>
            /// The unique index of this execution node.
            /// </summary>
            internal readonly int Index;

            /// <summary>
            /// The id of the operation executing in this node.
            /// </summary>
            internal readonly ulong Operation;

            /// <summary>
            /// The creation sequence id of the executing operation.
            /// </summary>
            internal readonly ulong SequenceId;

            /// <summary>
            /// The call site associated with this node.
            /// </summary>
            internal readonly string CallSite;

            /// <summary>
            /// A value that represents the hashed program state associated with this node.
            /// </summary>
            internal readonly int HashedProgramState;

            /// <summary>
            /// The ingoing edges of this node.
            /// </summary>
            internal Edge InEdge { get; private set; }

            /// <summary>
            /// The outgoing edges of this node.
            /// </summary>
            internal readonly List<Edge> OutEdges;

            /// <summary>
            /// Initializes a new instance of the <see cref="Node"/> class.
            /// </summary>
            internal Node(ExecutionGraph graph, ulong op, ulong seqId, string callSite, int state)
            {
                this.Graph = graph;
                this.Index = graph.Length;
                this.Operation = op;
                this.SequenceId = seqId;
                this.CallSite = callSite;
                this.HashedProgramState = state;
                this.InEdge = null;
                this.OutEdges = new List<Edge>();
            }

            /// <summary>
            /// Adds a new edge to the target node.
            /// </summary>
            internal void AddEdge(Edge edge)
            {
                this.OutEdges.Add(edge);
                edge.Target.InEdge = edge;
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
        /// Represents an edge in the execution graph.
        /// </summary>
        internal abstract class Edge
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
            /// The execution graph containing this edge.
            /// </summary>
            internal ExecutionGraph Graph => this.Source.Graph;

            /// <summary>
            /// Initializes a new instance of the <see cref="Edge"/> class.
            /// </summary>
            protected Edge(Node source, Node target)
            {
                this.Source = source;
                this.Target = target;
            }
        }

        /// <summary>
        /// Edge representing an execution step where a scheduling or nondeterministic decision is taken.
        /// </summary>
        internal sealed class StepEdge : Edge
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="StepEdge"/> class.
            /// </summary>
            internal StepEdge(Node source, Node target)
                : base(source, target)
            {
            }
        }

        /// <summary>
        /// Edge representing a method invocation.
        /// </summary>
        internal sealed class InvocationEdge : Edge
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="InvocationEdge"/> class.
            /// </summary>
            internal InvocationEdge(Node source, Node target)
                : base(source, target)
            {
            }
        }

        /// <summary>
        /// Edge representing the creation of an operation.
        /// </summary>
        internal sealed class CreationEdge : Edge
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="CreationEdge"/> class.
            /// </summary>
            internal CreationEdge(Node source, Node target)
                : base(source, target)
            {
            }
        }
    }
}
