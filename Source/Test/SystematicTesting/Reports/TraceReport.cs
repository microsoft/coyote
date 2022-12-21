// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Testing;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Report containing the execution trace from a test run.
    /// </summary>
    internal sealed class TraceReport
    {
        /// <summary>
        /// The name of the test corresponding to this trace.
        /// </summary>
        public string TestName { get; set; }

        /// <summary>
        /// The version of Coyote used during testing.
        /// </summary>
        public string CoyoteVersion { get; set; }

        /// <summary>
        /// The settings that were used during testing.
        /// </summary>
        public TestSettings Settings { get; set; }

        /// <summary>
        /// The operations executing in this trace.
        /// </summary>
        public Dictionary<string, string> Operations { get; set; }

        /// <summary>
        /// The execution steps in this trace.
        /// </summary>
        public List<string> Steps { get; set; }

        /// <summary>
        /// Constructs a <see cref="TraceReport"/> from the specified <see cref="OperationScheduler"/>
        /// and <see cref="Configuration"/>, and returns it in JSON format.
        /// </summary>
        internal static string GetJson(CoyoteRuntime runtime, OperationScheduler scheduler, Configuration configuration)
        {
            var report = new TraceReport();
            report.TestName = configuration.TestMethodName;
            report.CoyoteVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            report.Settings = new TestSettings();
            report.Settings.Strategy = scheduler.GetStrategyName();
            report.Settings.StrategyBound = configuration.StrategyBound;

            if (configuration.RandomGeneratorSeed.HasValue)
            {
                report.Settings.Seed = configuration.RandomGeneratorSeed.Value;
            }

            report.Settings.MaxFairSchedulingSteps = configuration.MaxFairSchedulingSteps;
            report.Settings.MaxUnfairSchedulingSteps = configuration.MaxUnfairSchedulingSteps;
            report.Settings.TimeoutDelay = configuration.TimeoutDelay;
            report.Settings.DeadlockTimeout = configuration.DeadlockTimeout;
            report.Settings.PortfolioMode = configuration.PortfolioMode.ToString().ToLower();
            report.Settings.IsLivenessCheckingEnabled = configuration.IsLivenessCheckingEnabled;
            report.Settings.LivenessTemperatureThreshold = configuration.LivenessTemperatureThreshold;
            report.Settings.IsLockAccessRaceCheckingEnabled = configuration.IsLockAccessRaceCheckingEnabled;
            report.Settings.IsPartiallyControlledConcurrencyAllowed = configuration.IsPartiallyControlledConcurrencyAllowed;
            report.Settings.IsPartiallyControlledDataNondeterminismAllowed = configuration.IsPartiallyControlledDataNondeterminismAllowed;
            report.Settings.UncontrolledConcurrencyResolutionAttempts = configuration.UncontrolledConcurrencyResolutionAttempts;
            report.Settings.UncontrolledConcurrencyResolutionDelay = configuration.UncontrolledConcurrencyResolutionDelay;

            report.ReportOperations(runtime.OperationMap);
            report.ReportTrace(scheduler.Trace);
            return report.ToJson();
        }

        /// <summary>
        /// Returns a <see cref="ExecutionTrace"/> from the specified JSON trace and also updates
        /// the configuration with any values explicitly set in the trace report.
        /// </summary>
        internal static ExecutionTrace FromJson(Configuration configuration)
        {
            var trace = ExecutionTrace.Create();
            if (configuration.ReproducibleTrace.Length > 0)
            {
                var report = JsonSerializer.Deserialize<TraceReport>(configuration.ReproducibleTrace, new JsonSerializerOptions()
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                });

                configuration.TestMethodName = report.TestName;
                configuration.ExplorationStrategy = ExplorationStrategyExtensions.FromName(report.Settings.Strategy);
                configuration.StrategyBound = report.Settings.StrategyBound;
                if (report.Settings.Seed.HasValue)
                {
                    configuration.RandomGeneratorSeed = report.Settings.Seed.Value;
                }

                configuration.MaxFairSchedulingSteps = report.Settings.MaxFairSchedulingSteps;
                configuration.MaxUnfairSchedulingSteps = report.Settings.MaxUnfairSchedulingSteps;
                configuration.TimeoutDelay = report.Settings.TimeoutDelay;
                configuration.DeadlockTimeout = report.Settings.DeadlockTimeout;
                configuration.PortfolioMode = PortfolioModeExtensions.FromString(report.Settings.PortfolioMode);
                configuration.IsLivenessCheckingEnabled = report.Settings.IsLivenessCheckingEnabled;
                configuration.LivenessTemperatureThreshold = report.Settings.LivenessTemperatureThreshold;
                configuration.IsLockAccessRaceCheckingEnabled = report.Settings.IsLockAccessRaceCheckingEnabled;
                configuration.IsPartiallyControlledConcurrencyAllowed = report.Settings.IsPartiallyControlledConcurrencyAllowed;
                configuration.IsPartiallyControlledDataNondeterminismAllowed = report.Settings.IsPartiallyControlledDataNondeterminismAllowed;
                configuration.UncontrolledConcurrencyResolutionAttempts = report.Settings.UncontrolledConcurrencyResolutionAttempts;
                configuration.UncontrolledConcurrencyResolutionDelay = report.Settings.UncontrolledConcurrencyResolutionDelay;

                for (int idx = 0; idx < report.Steps.Count; idx++)
                {
                    string[] tokens = report.Steps[idx].Split(',');
                    string opToken = tokens[0];
                    string decisionToken = tokens[1];

                    ulong op = ulong.Parse(opToken.Substring(3, opToken.Length - 4));
                    if (decisionToken.StartsWith("sp("))
                    {
#if NET || NETCOREAPP3_1
                        SchedulingPointType sp = Enum.Parse<SchedulingPointType>(decisionToken.Substring(
                            3, decisionToken.Length - 4));
#else
                        SchedulingPointType sp = (SchedulingPointType)Enum.Parse(typeof(SchedulingPointType),
                            decisionToken.Substring(3, decisionToken.Length - 4));
#endif

                        string targetToken = tokens[2];
                        string nextToken = tokens[3];

                        ulong target = ulong.Parse(targetToken.Substring(7, targetToken.Length - 8));
                        ulong next = ulong.Parse(nextToken.Substring(5, nextToken.Length - 6));
                        trace.AddSchedulingDecision(op, sp, target, next);
                    }
                    else if (decisionToken.StartsWith("bool("))
                    {
                        bool value = bool.Parse(decisionToken.Substring(5, decisionToken.Length - 6));
                        trace.AddNondeterministicBooleanDecision(op, value);
                    }
                    else if (decisionToken.StartsWith("int("))
                    {
                        int value = int.Parse(decisionToken.Substring(4, decisionToken.Length - 5));
                        trace.AddNondeterministicIntegerDecision(op, value);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unexpected execution step '{report.Steps[idx]}'.");
                    }
                }
            }

            return trace;
        }

        /// <summary>
        /// Constructs a visualization graph from the specified parameters, and returns it in DOT format.
        /// </summary>
        internal static string GetGraph(CoyoteRuntime runtime, ExecutionTrace trace)
        {
            StringBuilder graph = new StringBuilder();
            graph.AppendLine("digraph trace {");

            if (trace.Length > 1)
            {
                var aliases = new Dictionary<string, HashSet<ulong>>();
                var dictionary = new Dictionary<string, (string, string)>();
                var edges = new Dictionary<string, Dictionary<string, Dictionary<SchedulingPointType, ulong>>>();

                string next = string.Empty;
                for (int idx = 0; idx < trace.Length; idx++)
                {
                    if (trace[idx] is ExecutionTrace.SchedulingStep step)
                    {
                        string source = GetOperationAlias(runtime, step.Current, dictionary);
                        var sourceChain = UpdateOperationChain(source, step.Current, aliases);
                        next = step.Current == step.Value ? source : GetOperationAlias(runtime, step.Value, dictionary);

                        string target = null;
                        if (step.SchedulingPoint is SchedulingPointType.Create)
                        {
                            target = GetOperationAlias(runtime, step.Target, dictionary);
                            if (target == source)
                            {
                                sourceChain.Add(step.Target);
                            }
                            else
                            {
                                UpdateOperationChain(target, step.Target, aliases);
                            }
                        }
                        else if (step.SchedulingPoint is SchedulingPointType.ContinueWith)
                        {
                            target = GetOperationAlias(runtime, step.Target, dictionary);
                            sourceChain.Add(step.Target);
                        }

                        if (target != null)
                        {
                            AddEdge(source, target, step.SchedulingPoint, edges);
                        }

                        AddEdge(source, next, SchedulingPointType.Default, edges);
                    }
                }

                int typesCounter = 0;
                var typeEntries = dictionary.Values.Select(v => v.Item1).Where(t => !string.IsNullOrEmpty(t)).Distinct();
                foreach (var typeEntry in typeEntries)
                {
                    graph.AppendLine($"  subgraph cluster_{typesCounter} {{");
                    graph.AppendLine($"    label=\"{typeEntry}\";");
                    graph.AppendLine("    node [style=filled];");
                    graph.AppendLine("    color=purple;");
                    graph.AppendLine();
                    foreach (var kvp in dictionary)
                    {
                        if (kvp.Value.Item1 == typeEntry)
                        {
                            graph.AppendLine($"    \"{kvp.Key}\" [label=\"{kvp.Value.Item2}\"];");
                        }
                    }

                    graph.AppendLine("  }");
                    typesCounter++;
                }

                foreach (var source in edges)
                {
                    foreach (var destination in source.Value)
                    {
                        string edge = $"  \"{source.Key}\" -> \"{destination.Key}\"";
                        foreach (var sp in destination.Value)
                        {
                            if (sp.Key is SchedulingPointType.Create ||
                                sp.Key is SchedulingPointType.ContinueWith)
                            {
                                string text;
                                if (sp.Key is SchedulingPointType.Create)
                                {
                                    text = $"{edge} [color=\"#3868ceff\"];";
                                }
                                else
                                {
                                    text = $"{edge};";
                                }

                                bool summarize = sp.Value > 10;
                                ulong loops = summarize ? 10 : sp.Value;
                                for (ulong idx = 0; idx < loops; idx++)
                                {
                                    if (summarize && idx == loops - 1)
                                    {
                                        text = text.Insert(text.LastIndexOf(';'), $"[style=dotted]");
                                    }

                                    graph.AppendLine(text);
                                }
                            }
                            else if (source.Key != destination.Key)
                            {
                                string text = $"{edge} [color=\"lightgrey\",style=dashed];";
                                graph.AppendLine(text);
                            }
                        }
                    }
                }

                string start = GetOperationAlias(runtime, 0, dictionary);

                // Write the error state.
                graph.Append($"  \"{next}\" -> End");
                graph.AppendLine($" [color=\"#ce3838ff\"];");

                // Write the style options.
                graph.AppendLine();
                graph.AppendLine($"  \"{start}\"[shape=Mdiamond];");
                graph.AppendLine("  End[shape=Msquare,color=\"#ce3838ff\"];");
            }
            else
            {
                // Write the empty graph.
                graph.AppendLine("  \"N/A\"[shape=Msquare];");
            }

            graph.AppendLine("  subgraph cluster_01 {");
            graph.AppendLine("    label = \"Legend\";");
            graph.AppendLine("    node [style=filled];");
            graph.AppendLine();
            graph.AppendLine("    \"_COYOTE__START\" [label=\"\"];");
            graph.AppendLine("    \"_COYOTE__END\" [label=\"\"];");
            graph.AppendLine("    \"_COYOTE__START\" -> \"_COYOTE__END\" [label=\" StartOp \",color=\"#3868ceff\"];");
            graph.AppendLine("    \"_COYOTE__START\" -> \"_COYOTE__END\" [label=\" StartOp(>10) \",color=\"#3868ceff\",style=dashed];");
            graph.AppendLine("    \"_COYOTE__START\" -> \"_COYOTE__END\" [label=\" ContinueOp \"];");
            graph.AppendLine("    \"_COYOTE__START\" -> \"_COYOTE__END\" [label=\" ContinueOp(>10) \",style=dotted];");
            graph.AppendLine("    \"_COYOTE__START\" -> \"_COYOTE__END\" [label=\" ContextSwitch \",color=\"lightgrey\",style=dashed];");
            graph.AppendLine("    \"_COYOTE__START\" -> \"_COYOTE__END\" [label=\" Error \",color=\"#ce3838ff\"];");
            graph.AppendLine("  }");

            graph.AppendLine("}");
            return graph.ToString();
        }

        /// <summary>
        /// Returns the alias for the specified operation.
        /// </summary>
        private static string GetOperationAlias(CoyoteRuntime runtime, ulong id, Dictionary<string, (string, string)> dictionary)
        {
            string alias = runtime.OperationMap[id].Description;
            if (!dictionary.ContainsKey(alias))
            {
                string typeName;
                string methodName;
                if (alias.EndsWith(".MoveNext"))
                {
                    string formattedAlias = alias.Substring(0, alias.Length - ".MoveNext".Length);
                    int splitIndex = formattedAlias.LastIndexOf('.');
                    typeName = formattedAlias.Substring(0, splitIndex);
                    methodName = formattedAlias.Substring(splitIndex + 1);

                    // Format the method name to human readable form.
                    int methodStartIndex = methodName.IndexOf('<');
                    int methodEndIndex = methodName.IndexOf('>');
                    if (methodStartIndex >= 0 && methodEndIndex >= 0)
                    {
                        methodName = methodName.Substring(methodStartIndex + 1, methodEndIndex - methodStartIndex - 1);
                    }
                }
                else
                {
                    int splitIndex = alias.LastIndexOf('.');
                    if (splitIndex >= 0)
                    {
                        typeName = alias.Substring(0, splitIndex);
                        methodName = alias.Substring(splitIndex + 1);
                    }
                    else
                    {
                        typeName = string.Empty;
                        methodName = alias;
                    }
                }

                dictionary.Add(alias, (typeName, methodName));
            }

            if (string.IsNullOrEmpty(alias))
            {
                alias = $"op({id})";
            }

            return alias;
        }

        /// <summary>
        /// Adds a new edge for the specified source and destination operations.
        /// </summary>
        private static void AddEdge(string source, string destination, SchedulingPointType sp,
            Dictionary<string, Dictionary<string, Dictionary<SchedulingPointType, ulong>>> edges)
        {
            if (!edges.TryGetValue(source, out Dictionary<string, Dictionary<SchedulingPointType, ulong>> destinations))
            {
                destinations = new Dictionary<string, Dictionary<SchedulingPointType, ulong>>();
                edges.Add(source, destinations);
            }

            if (!destinations.TryGetValue(destination, out Dictionary<SchedulingPointType, ulong> count))
            {
                count = new Dictionary<SchedulingPointType, ulong>();
                destinations.Add(destination, count);
            }

            if (count.TryGetValue(sp, out ulong value))
            {
                count[sp] = value + 1;
            }
            else
            {
                count.Add(sp, 1);
            }
        }

        /// <summary>
        /// Returns the chain for the specified operation.
        /// </summary>
        private static HashSet<ulong> UpdateOperationChain(string alias, ulong id, Dictionary<string, HashSet<ulong>> aliases)
        {
            if (!aliases.TryGetValue(alias, out HashSet<ulong> chain))
            {
                chain = new HashSet<ulong> { id };
                aliases.Add(alias, chain);
            }
            else
            {
                chain.Add(id);
            }

            return chain;
        }

        /// <summary>
        /// Adds the specified entity information to the report.
        /// </summary>
        internal void ReportOperations(Dictionary<ulong, ControlledOperation> operations)
        {
            this.Operations = new Dictionary<string, string>();
            foreach (var op in operations.Values)
            {
                this.Operations.Add($"op({op.Id})", op.Description);
            }
        }

        /// <summary>
        /// Adds the specified trace to the report.
        /// </summary>
        internal void ReportTrace(ExecutionTrace trace)
        {
            this.Steps = new List<string>();
            if (trace.Length > 0)
            {
                for (int idx = 0; idx < trace.Length; idx++)
                {
                    if (trace[idx] is ExecutionTrace.SchedulingStep step)
                    {
                        this.Steps.Add($"op({step.Current}),sp({step.SchedulingPoint}),target({step.Target}),next({step.Value})");
                    }
                    else if (trace[idx] is ExecutionTrace.BooleanChoiceStep boolChoiceStep)
                    {
                        this.Steps.Add($"op({boolChoiceStep.Current}),bool({boolChoiceStep.Value})");
                    }
                    else if (trace[idx] is ExecutionTrace.IntegerChoiceStep intChoiceStep)
                    {
                        this.Steps.Add($"op({intChoiceStep.Current}),int({intChoiceStep.Value})");
                    }
                }
            }
        }

        /// <summary>
        /// Returns the trace report in JSON format.
        /// </summary>
        internal string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            });

        /// <summary>
        /// The settings that were used during testing.
        /// </summary>
        public sealed class TestSettings
        {
            /// <summary>
            /// The name of the strategy used in this test.
            /// </summary>
            public string Strategy { get; set; }

            /// <summary>
            /// A strategy-specific bound.
            /// </summary>
            public int StrategyBound { get; set; }

            /// <summary>
            /// The random seed.
            /// </summary>
            public uint? Seed { get; set; }

            /// <summary>
            /// The maximum scheduling steps to explore for fair schedulers.
            /// </summary>
            public int MaxFairSchedulingSteps { get; set; }

            /// <summary>
            /// The maximum scheduling steps to explore for unfair schedulers.
            /// </summary>
            public int MaxUnfairSchedulingSteps { get; set; }

            /// <summary>
            /// Value that controls the probability of triggering a timeout during systematic testing.
            /// </summary>
            public uint TimeoutDelay { get; set; }

            /// <summary>
            /// Value that controls how much time the deadlock monitor should wait during
            /// concurrency testing before reporting a potential deadlock.
            /// </summary>
            public uint DeadlockTimeout { get; set; }

            /// <summary>
            /// The enabled exploration strategy portfolio mode.
            /// </summary>
            public string PortfolioMode { get; set; }

            /// <summary>
            /// Value specifying if liveness checking is enabled or not.
            /// </summary>
            public bool IsLivenessCheckingEnabled { get; set; }

            /// <summary>
            /// The liveness temperature threshold.
            /// </summary>
            public int LivenessTemperatureThreshold { get; set; }

            /// <summary>
            /// Value specifying if checking races during lock accesses is enabled or not.
            /// </summary>
            public bool IsLockAccessRaceCheckingEnabled { get; set; }

            /// <summary>
            /// Value specifying if partially controlled concurrency is allowed or not.
            /// </summary>
            public bool IsPartiallyControlledConcurrencyAllowed { get; set; }

            /// <summary>
            /// Value specifying if partially controlled data non-determinism is allowed or not.
            /// </summary>
            public bool IsPartiallyControlledDataNondeterminismAllowed { get; set; }

            /// <summary>
            /// Value that controls how many times the runtime can check if each instance
            /// of uncontrolled concurrency has resolved during testing before timing out.
            /// </summary>
            public uint UncontrolledConcurrencyResolutionAttempts { get; set; }

            /// <summary>
            /// Value that controls how much time the runtime should wait between each attempt
            /// to resolve uncontrolled concurrency during testing before timing out.
            /// </summary>
            public uint UncontrolledConcurrencyResolutionDelay { get; set; }
        }
    }
}
