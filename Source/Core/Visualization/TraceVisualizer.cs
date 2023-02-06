// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Coyote.Runtime;
using Edge = Microsoft.Coyote.Runtime.ExecutionGraph.Edge;
using Node = Microsoft.Coyote.Runtime.ExecutionGraph.Node;

namespace Microsoft.Coyote.Visualization
{
    /// <summary>
    /// Provides the capability to visualize an <see cref="ExecutionGraph"/>.
    /// </summary>
    internal sealed class TraceVisualizer
    {
        /// <summary>
        /// Visualizes the specified <see cref="ExecutionGraph"/> in the requested format.
        /// </summary>
        internal static string Visualize(ExecutionGraph graph, TraceFormat format, Layout layout)
        {
            StringBuilder sb = new StringBuilder();
            if (format is TraceFormat.DGML)
            {
                sb.AppendLine("<DirectedGraph xmlns='http://schemas.microsoft.com/vs/2009/dgml'>");
                sb.AppendLine("  <Nodes>");
            }
            else if (format is TraceFormat.GraphViz)
            {
                sb.AppendLine("digraph trace {");
                sb.AppendLine("  newrank=true;");
                sb.AppendLine("  compound=true;");
                sb.AppendLine();
            }

            // Cache all seen edges to avoid repetition.
            var edgeCache = new HashSet<string>();

            // Visualize nodes in clusters as appropriate for the specified layout.
            if (layout is Layout.Trace)
            {
                ClusterCallSitesPerOperation(sb, graph, edgeCache, format);
            }
            else
            {
                ClusterCallSitesPerType(sb, graph, format);
            }

            // Visualize the edges as appropriate for the specified layout.
            foreach (var node in graph)
            {
                foreach (var edge in node.OutEdges)
                {
                    if (TryCreateEdgeStatement(edge, format, layout, edgeCache, out string edgeStmt))
                    {
                        string suffix = format is TraceFormat.DGML ? "  " : string.Empty;
                        sb.AppendLine($"  {suffix}{edgeStmt};");
                    }
                }
            }

            if (format is TraceFormat.DGML)
            {
                sb.AppendLine("  </Links>");
                sb.AppendLine("  <Styles>");
                sb.AppendLine("  </Styles>");
                sb.AppendLine("</DirectedGraph>");
            }
            else if (format is TraceFormat.GraphViz)
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Cluster call sites per operation in the visualization trace.
        /// </summary>
        private static void ClusterCallSitesPerOperation(StringBuilder sb, ExecutionGraph graph, HashSet<string> edgeCache, TraceFormat format)
        {
            // Clustering links for the DGML format.
            var dgmlClusteringLinks = new List<string>();

            // The trace layout has operation clusters with nested type clusters.
            var operations = graph.Select(n => n.Operation).Distinct().OrderBy(v => v);
            foreach (ulong operation in operations)
            {
                string operationName = $"Operation({operation})";
                if (format is TraceFormat.DGML)
                {
                    sb.AppendLine($"    <Node Id='{operationName}' Category='Operation' Group='Expanded'/>");
                }
                else if (format is TraceFormat.GraphViz)
                {
                    sb.AppendLine($"  subgraph cluster_{operation} {{");
                    sb.AppendLine("    style=rounded;");
                    sb.AppendLine("    bgcolor=\"#303030\";");
                    sb.AppendLine("    color=purple;");
                    sb.AppendLine("    fontcolor=\"#ffee00\";");
                    sb.AppendLine("    fontsize=\"30pt\";");
                    sb.AppendLine("    penwidth=5;");
                    sb.AppendLine($"    label=\"{operationName}\";");
                }

                var nodeCache = new HashSet<string>();
                var nodes = graph.Where(n => n.Operation == operation).OrderBy(n => n.Index);
                foreach (var node in nodes)
                {
                    if (!nodeCache.Contains(node.CallSite))
                    {
                        // Cache the node to avoid repeating it.
                        nodeCache.Add(node.CallSite);

                        string callSite = FormatToken(node.CallSite, format);
                        if (format is TraceFormat.DGML)
                        {
                            dgmlClusteringLinks.Add($"Source='{operationName}' Target='{operationName}.{callSite}'");
                            sb.AppendLine($"    <Node Id='{operationName}.{callSite}' Label='{callSite}'/>");
                        }
                        else if (format is TraceFormat.GraphViz)
                        {
                            string nodeName = $"{callSite}({operation})";
                            string attributes = $"style=filled,color=white,shape=rect";
                            sb.AppendLine($"    \"{nodeName}\" [{attributes},label=\"{callSite}\"];");
                        }
                    }
                }

                if (format is TraceFormat.GraphViz)
                {
                    foreach (var node in nodes)
                    {
                        foreach (var edge in node.OutEdges.Where(e => e.Target.Operation == operation))
                        {
                            if (TryCreateEdgeStatement(edge, format, Layout.Trace, edgeCache, out string edgeStmt))
                            {
                                sb.AppendLine($"    {edgeStmt};");
                            }
                        }
                    }

                    sb.AppendLine("  }");
                    sb.AppendLine();
                }
            }

            if (format is TraceFormat.DGML)
            {
                sb.AppendLine("  </Nodes>");
                sb.AppendLine("  <Links>");
                foreach (string link in dgmlClusteringLinks)
                {
                    sb.AppendLine($"    <Link {link} Category='Contains'/>");
                }
            }
        }

        /// <summary>
        /// Cluster call sites per type in the visualization trace.
        /// </summary>
        private static void ClusterCallSitesPerType(StringBuilder sb, ExecutionGraph graph, TraceFormat format)
        {
            // Build the method name cache.
            var methodNameCache = new Dictionary<string, (string, string)>();
            foreach (var node in graph)
            {
                foreach (var edge in node.OutEdges)
                {
                    GetMethodNameAndType(edge.Source, methodNameCache);
                    GetMethodNameAndType(edge.Target, methodNameCache);
                }
            }

            int typesCounter = 0;
            var typeEntries = methodNameCache.Values.Select(v => v.Item2).Where(t => !string.IsNullOrEmpty(t)).Distinct();
            foreach (var typeEntry in typeEntries)
            {
                sb.AppendLine($"  subgraph cluster_{typesCounter} {{");
                sb.AppendLine("    rank=same;");
                sb.AppendLine("    style=filled;");
                sb.AppendLine("    color=lightgrey;");
                sb.AppendLine($"    label=\"{typeEntry}\";");
                sb.AppendLine("    node [style=filled][color=white];");
                sb.AppendLine();
                foreach (var kvp in methodNameCache)
                {
                    if (kvp.Value.Item2 == typeEntry)
                    {
                        sb.AppendLine($"    \"{kvp.Key}\" [label=\"{kvp.Value.Item1}\"];");
                    }
                }

                sb.AppendLine("  }");
                sb.AppendLine();
                typesCounter++;
            }

            if (format is TraceFormat.DGML)
            {
                sb.AppendLine("  </Nodes>");
                sb.AppendLine("  <Links>");
            }
        }

        /// <summary>
        /// Tries to return an edge statement for the specified edge.
        /// </summary>
        private static bool TryCreateEdgeStatement(Edge edge, TraceFormat format, Layout layout, HashSet<string> cache, out string result)
        {
            string source = edge.Source.CallSite;
            string target = edge.Target.CallSite;
            if (layout is Layout.Trace)
            {
                if (edge.Source.Operation != edge.Target.Operation)
                {
                    // We want to assign the highest rank source node, as well as the lowest rank
                    // target node, which will force the outgoing edge go downwards.
                    source = edge.Graph.GetHighestCallSiteRankForOperation(edge.Source.Operation) ?? source;
                    target = edge.Graph.GetFirstNodeForOperation(edge.Target.Operation)?.CallSite ?? target;
                }

                if (format is TraceFormat.DGML)
                {
                    // FIX-FIX!!!
                    source = $"Operation({edge.Source.Operation}).{source}";
                    target = $"Operation({edge.Target.Operation}).{target}";
                }
                else if (format is TraceFormat.GraphViz)
                {
                    source = $"{source}({edge.Source.Operation})";
                    target = $"{target}({edge.Target.Operation})";
                }
            }

            source = FormatToken(source, format);
            target = FormatToken(target, format);

            string edgeStmt = format is TraceFormat.DGML ? $"Source='{source}' Target='{target}'" : $"\"{source}\" -> \"{target}\"";
            if (cache.Contains(edgeStmt))
            {
                result = null;
                return false;
            }

            // Cache the edge to avoid repeating it.
            cache.Add(edgeStmt);

            if (edge is ExecutionGraph.StepEdge && source == target)
            {
                // Ignore step edges that go to the same call site.
                result = null;
                return false;
            }

            var attributes = new List<string>();
            if (format is TraceFormat.DGML)
            {
                attributes.Add("Index='0'");
            }
            else if (format is TraceFormat.GraphViz)
            {
                if (edge is ExecutionGraph.CreationEdge)
                {
                    attributes.Add("color=\"#3868ceff\"");
                    attributes.Add("fontcolor=\"#3868ceff\"");
                    attributes.Add("style=dashed");
                }
                else if (edge is ExecutionGraph.StepEdge)
                {
                    attributes.Add("style=dotted");
                }

                if (layout is Layout.Trace)
                {
                    if (edge.Source.Operation == edge.Target.Operation)
                    {
                        attributes.Add("color=\"#ffee00\"");
                        attributes.Add("weight=2");
                    }
                    else
                    {
                        attributes.Add($"ltail=cluster_{edge.Source.Operation}");
                        attributes.Add($"lhead=cluster_{edge.Target.Operation}");
                        attributes.Add($"label=\"{edge.Source.CallSite}\"");
                    }
                }
            }

            string attributesStmt = string.Join(format is TraceFormat.DGML ? " " : ",", attributes);
            result = format is TraceFormat.DGML ? $"<Link {edgeStmt} {attributesStmt}/>" : $"{edgeStmt} [{attributesStmt}]";
            return true;
        }

        /// <summary>
        /// Splits the specified fully qualified method into its type and method name components.
        /// </summary>
        private static (string, string) GetMethodNameAndType(Node node, Dictionary<string, (string, string)> cache)
        {
            if (!cache.TryGetValue(node.CallSite, out (string, string) result))
            {
                string methodName;
                string typeName;
                string callSite = node.CallSite;
                int splitIndex = callSite.LastIndexOf('.');
                if (splitIndex >= 0)
                {
                    typeName = callSite.Substring(0, splitIndex);
                    methodName = callSite.Substring(splitIndex + 1);
                }
                else
                {
                    typeName = string.Empty;
                    methodName = callSite;
                }

                result = (methodName, typeName);
                cache.Add(node.CallSite, (methodName, typeName));
            }

            return result;
        }

        /// <summary>
        /// Formats the specified token for the given visualization format.
        /// </summary>
        private static string FormatToken(string token, TraceFormat format)
        {
            if (format is TraceFormat.DGML)
            {
                return token.Replace("<", "&lt;").Replace(">", "&gt;");
            }

            return token;
        }

        /// <summary>
        /// The visualization layout to apply.
        /// </summary>
        internal enum Layout
        {
            Compact = 0,
            Trace
        }
    }
}
