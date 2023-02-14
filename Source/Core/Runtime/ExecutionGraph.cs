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
        /// Map containing coverage information across executions. It maps call site transitions.
        /// </summary>
        internal readonly Dictionary<string, HashSet<string>> CoverageMap;

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
            this.CoverageMap = new Dictionary<string, HashSet<string>>();
        }

        /// <summary>
        /// Creates a new <see cref="ExecutionGraph"/>.
        /// </summary>
        internal static ExecutionGraph Create() => new ExecutionGraph();

        /// <summary>
        /// Adds the execution step associated with the specified operation to the graph.
        /// </summary>
        internal void Add(ControlledOperation operation, ulong explicitState, ulong totalState, bool isInvocation)
        {
            // First, find the predecessor node in the execution, if one exists. This is either the node corresponding
            // to the last call site of the same operation, or the node corresponding to its parent operation.
            Node predecessorNode = this.Length is 0 ? null :
                this.LastNodeForOperation.ContainsKey(operation.Id) ? this.LastNodeForOperation[operation.Id] :
                this.LastNodeForOperation.ContainsKey(operation.ParentId) ? this.LastNodeForOperation[operation.ParentId] :
                null;

            // Next, create a node corresponding to the latest call site invoked by the operation.
            // Else, if it has no known call sites so far and this is the root operation, assign
            // a dummy 'Test' call site, else assign the call site of its parent operation.
            string callSite = operation.CallSiteSequence.Count > 0 ? operation.LastCallSite :
                predecessorNode is null ? "Test" : predecessorNode.CallSite;
            Node node = new Node(this, operation, callSite, explicitState, totalState);

            // Next, connect the new node with the predecessor node, if a predecessor exists.
            if (predecessorNode != null)
            {
                EdgeCategory edgeCategory = node.Operation == operation.Id ?
                    isInvocation ? EdgeCategory.Invocation : EdgeCategory.Step : EdgeCategory.Creation;
                Edge edge = new Edge(predecessorNode, node, edgeCategory);
                predecessorNode.AddEdge(edge);
            }

            this.Nodes.Add(node);
            this.LastNodeForOperation[operation.Id] = node;
            if (!this.FirstNodeForOperation.ContainsKey(operation.Id))
            {
                this.FirstNodeForOperation.Add(operation.Id, node);
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
            internal int Depth => this.InEdge?.Depth ?? 0;

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
            internal Node(ExecutionGraph graph, ControlledOperation operation, string callSite, ulong explicitState, ulong totalState)
            {
                this.Graph = graph;
                this.Index = graph.Length;
                this.Operation = operation.Id;
                this.GroupId = operation.Group.Id;
                this.CallSite = callSite;
                this.OperationState = operation.LastHashedState;
                this.ExplicitProgramState = explicitState;
                this.ProgramState = totalState;
                this.InEdge = null;
                this.OutEdges = new List<Edge>();

                // Check if the node is read-only or not in regards to explicit program state.
                this.IsReadOnly = graph.Length is 0 ? false : graph[this.Index - 1].ExplicitProgramState == explicitState;
            }

            /// <summary>
            /// Adds a new edge to the target node.
            /// </summary>
            internal void AddEdge(Edge edge)
            {
                this.OutEdges.Add(edge);
                edge.Target.InEdge = edge;

                // Cache the new edge to track coverage.
                if (edge.Category is EdgeCategory.Creation || edge.Category is EdgeCategory.Invocation ||
                    this.CallSite != edge.Target.CallSite)
                {
                    if (this.Graph.CoverageMap.TryGetValue(this.CallSite, out var targetMap))
                    {
                        targetMap.Add(edge.Target.CallSite);
                    }
                    else
                    {
                        this.Graph.CoverageMap.Add(this.CallSite, new HashSet<string> { edge.Target.CallSite });
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
        /// Represents an edge in the execution graph.
        /// </summary>
        internal sealed class Edge
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
            /// The edge category.
            /// </summary>
            internal readonly EdgeCategory Category;

            /// <summary>
            /// A value representing the depth of this edge from the root.
            /// </summary>
            internal readonly int Depth;

            /// <summary>
            /// The execution graph containing this edge.
            /// </summary>
            internal ExecutionGraph Graph => this.Source.Graph;

            /// <summary>
            /// Initializes a new instance of the <see cref="Edge"/> class.
            /// </summary>
            internal Edge(Node source, Node target, EdgeCategory category)
            {
                this.Source = source;
                this.Target = target;
                this.Category = category;

                // Calculate the depth of the edge by incrementing the depth of the source node.
                this.Depth = this.Source.Depth + 1;
            }
        }

        /// <summary>
        /// The edge category.
        /// </summary>
        internal enum EdgeCategory
        {
            Creation = 0,
            Invocation,
            Step
        }
    }
}
