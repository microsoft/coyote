// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
                // The first operation is always the main thread running the test method.
                string firstOp = $"op(0)";
                string firstDescription = runtime.OperationMap[0].Description;

                string next = string.Empty;

                var completedOps = new HashSet<ulong>();
                var chainMap = new Dictionary<ulong, HashSet<ulong>>();
                var aliasMap = new Dictionary<string, HashSet<ulong>>();

                int stepCount = 0;
                for (int idx = 0; idx < trace.Length; idx++)
                {
                    if (trace[idx] is ExecutionTrace.SchedulingStep step)
                    {
                        string op = $"op({step.Current})";
                        string name = runtime.OperationMap[step.Current].Description;
                        string source = $"{name}({op})";

                        if (!chainMap.TryGetValue(step.Current, out HashSet<ulong> opChain))
                        {
                            opChain = new HashSet<ulong>();
                            chainMap.Add(step.Current, opChain);
                        }

                        if (!aliasMap.TryGetValue(name, out HashSet<ulong> opAliases))
                        {
                            opAliases = new HashSet<ulong>();
                            aliasMap.Add(name, opAliases);
                        }

                        opChain.Add(step.Current);
                        opAliases.Add(step.Current);

                        string nextOp = $"op({step.Value})";
                        string nextName = runtime.OperationMap[step.Value].Description;
                        next = $"{nextName}({nextOp})";

                        if (step.SchedulingPoint is SchedulingPointType.Create)
                        {
                            string targetOp = $"op({step.Target})";
                            string targetName = runtime.OperationMap[step.Target].Description;
                            string target = $"{targetName}({targetOp})";
                            graph.Append($"  \"{source}\" -> \"{target}\"");
                            graph.AppendLine($" [label=\" {step.SchedulingPoint} [{stepCount}] \" color=\"#3868ceff\"];");
                            stepCount++;
                        }
                        else if (step.SchedulingPoint is SchedulingPointType.ContinueWith)
                        {
                            opChain.Add(step.Target);
                            if (!chainMap.ContainsKey(step.Target))
                            {
                                chainMap.Add(step.Target, opChain);
                            }
                        }
                        else if (step.SchedulingPoint is SchedulingPointType.Complete)
                        {
                            completedOps.Add(step.Current);
                        }
                        else if (step.Current != step.Target || opAliases.Count > 0)
                        {
                            graph.Append($"  \"{source}\" -> \"{next}\"");
                            graph.AppendLine($" [label=\" {step.SchedulingPoint} [{stepCount}] \",style=dashed];");
                            stepCount++;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

                // Write the error state.
                graph.Append($"  \"{next}\" -> error");
                graph.AppendLine($" [label=\" [{stepCount}] \",color=\"#ce3838ff\"];");

                // Write the style options.
                graph.AppendLine();
                graph.AppendLine($"  \"{firstDescription}\"[shape=box];");
                graph.AppendLine("  error[shape=Mdiamond,color=\"#ce3838ff\"];");
            }
            else
            {
                // Write the empty graph.
                graph.AppendLine("  \"N/A\"[shape=Mdiamond];");
            }

            graph.AppendLine("}");
            return graph.ToString();
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
