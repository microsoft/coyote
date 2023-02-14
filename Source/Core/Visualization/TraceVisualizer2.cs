// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Coyote.Runtime;
using StepCategory = Microsoft.Coyote.Runtime.ExecutionContext.StepCategory;

namespace Microsoft.Coyote.Visualization
{
    /// <summary>
    /// Provides the capability to visualize an <see cref="ExecutionContext"/> in DGML format.
    /// </summary>
    internal sealed class TraceVisualizer2
    {
        /// <summary>
        /// Visualizes the specified <see cref="ExecutionContext"/> in the requested layout.
        /// </summary>
        internal static string Visualize(ExecutionContext context, Layout layout)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<DirectedGraph xmlns='http://schemas.microsoft.com/vs/2009/dgml'>");
            sb.AppendLine("  <Nodes>");

            // Links between clustered nodes in the DGML format.
            var clusteringLinks = new List<string>();

            // Visualize the nodes in the specified layout.
            if (layout is Layout.Coverage)
            {
                AppendNodesInCoverageLayout(sb, context, clusteringLinks);
            }
            else if (layout is Layout.Trace)
            {
                AppendNodesInTraceLayout(sb, context, clusteringLinks);
            }

            sb.AppendLine("  </Nodes>");
            sb.AppendLine("  <Links>");

            // Visualize the links between clustered nodes.
            foreach (string link in clusteringLinks)
            {
                sb.AppendLine($"    <Link {link} Category='Contains'/>");
            }

            // Visualize the links across clustered nodes in the specified layout.
            if (layout is Layout.Coverage)
            {
                AppendEdgesInCoverageLayout(sb, context);
            }
            else if (layout is Layout.Trace)
            {
                AppendLinksInTraceLayout(sb, context);
            }

            sb.AppendLine("  </Links>");
            sb.AppendLine("  <Styles>");
            sb.AppendLine("  </Styles>");
            sb.AppendLine("</DirectedGraph>");
            return sb.ToString();
        }

        /// <summary>
        /// Append the nodes in the coverage layout.
        /// </summary>
        private static void AppendNodesInCoverageLayout(StringBuilder sb, ExecutionContext context, List<string> clusteringLinks)
        {
            // Build the cache of method and type names.
            var methodNameCache = new Dictionary<string, (string, string)>();
            foreach (var source in context.CoverageMap)
            {
                GetMethodNameAndType(source.Key, methodNameCache);
                foreach (var target in source.Value)
                {
                    GetMethodNameAndType(target, methodNameCache);
                }
            }

            var typeEntries = methodNameCache.Values.Select(v => v.Item2).Where(t => !string.IsNullOrEmpty(t)).Distinct();
            foreach (string typeEntry in typeEntries)
            {
                string type = FormatToken(typeEntry);
                sb.AppendLine($"    <Node Id='{type}' Category='Type' Group='Expanded'/>");

                foreach (var kvp in methodNameCache)
                {
                    if (kvp.Value.Item2 == typeEntry)
                    {
                        string method = FormatToken(kvp.Value.Item1);
                        sb.AppendLine($"    <Node Id='{type}.{method}' Label='{method}'/>");
                        clusteringLinks.Add($"Source='{type}' Target='{type}.{method}'");
                    }
                }
            }
        }

        /// <summary>
        /// Append the nodes in the trace layout.
        /// </summary>
        private static void AppendNodesInTraceLayout(StringBuilder sb, ExecutionContext context, List<string> clusteringLinks)
        {
            // The trace layout has groups clusters.
            var groups = context.Select(n => n.GroupId).Distinct().OrderBy(v => v);
            foreach (Guid group in groups)
            {
                string operationName = $"Operation({group})";
                sb.AppendLine($"    <Node Id='{operationName}' Category='Operation' Group='Expanded'/>");

                var nodeCache = new HashSet<string>();
                var nodes = context.Where(n => n.GroupId == group).OrderBy(n => n.Index);
                foreach (var node in nodes)
                {
                    if (!nodeCache.Contains(node.CallSite))
                    {
                        // Cache the node to avoid repeating it.
                        nodeCache.Add(node.CallSite);

                        string callSite = FormatToken(node.CallSite);
                        sb.AppendLine($"    <Node Id='{operationName}.{callSite}' Label='{callSite}'/>");
                        clusteringLinks.Add($"Source='{operationName}' Target='{operationName}.{callSite}'");
                    }
                }
            }
        }

        /// <summary>
        /// Append the edges in the coverage layout.
        /// </summary>
        private static void AppendEdgesInCoverageLayout(StringBuilder sb, ExecutionContext context)
        {
            foreach (var source in context.CoverageMap)
            {
                foreach (var target in source.Value)
                {
                    string link = $"Source='{source.Key}' Target='{target}'";

                    var attributes = new List<string>();
                    attributes.Add("Index='0'");

                    string attributesStmt = string.Join(" ", attributes);
                    sb.AppendLine($"    <Link {FormatToken(link)} {attributesStmt}/>");
                }
            }
        }

        /// <summary>
        /// Append the links in the trace layout.
        /// </summary>
        private static void AppendLinksInTraceLayout(StringBuilder sb, ExecutionContext context)
        {
            var linkCache = new HashSet<string>();
            foreach (var node in context)
            {
                foreach (var step in node.OutSteps)
                {
                    string source = $"Operation({step.Source.GroupId}).{step.Source.CallSite}";
                    string target = $"Operation({step.Target.GroupId}).{step.Target.CallSite}";
                    string link = $"Source='{source}' Target='{target}'";
                    if (linkCache.Contains(link))
                    {
                        continue;
                    }

                    // Cache the link to avoid repeating it.
                    linkCache.Add(link);

                    if (step.Category is StepCategory.ContextSwitch && source == target)
                    {
                        // Ignore steps that go to the same call site.
                        continue;
                    }

                    var attributes = new List<string>();
                    attributes.Add("Index='0'");

                    string attributesStmt = string.Join(" ", attributes);
                    sb.AppendLine($"    <Link {FormatToken(link)} {attributesStmt}/>");
                }
            }
        }

        /// <summary>
        /// Splits the specified fully qualified method into its type and method name components.
        /// </summary>
        private static (string, string) GetMethodNameAndType(string callSite, Dictionary<string, (string, string)> cache)
        {
            if (!cache.TryGetValue(callSite, out (string, string) result))
            {
                string methodName;
                string typeName;
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
                cache.Add(callSite, (methodName, typeName));
            }

            return result;
        }

        /// <summary>
        /// Formats the specified token.
        /// </summary>
        private static string FormatToken(string token) => token.Replace("<", "&lt;").Replace(">", "&gt;");

        /// <summary>
        /// The layout to apply to the visualization.
        /// </summary>
        internal enum Layout
        {
            Coverage = 0,
            Trace
        }
    }
}
