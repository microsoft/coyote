// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;
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
        /// The execution steps in this trace.
        /// </summary>
        public List<string> Steps { get; set; }

        /// <summary>
        /// Constructs a <see cref="TraceReport"/> from the specified <see cref="OperationScheduler"/>
        /// and <see cref="Configuration"/>, and returns it in JSON format.
        /// </summary>
        internal static string GetJson(OperationScheduler scheduler, Configuration configuration)
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

                    // The current operation token is of the form 'op(id:seqId)'.
                    string opToken = tokens[0];
                    string[] opTokens = opToken.Substring(3, opToken.Length - 4).Split(':');
                    ulong opId = ulong.Parse(opTokens[0]);
                    ulong opSeqId = ulong.Parse(opTokens[1]);

                    string decisionToken = tokens[1];
                    if (decisionToken.StartsWith("sp("))
                    {
#if NET || NETCOREAPP3_1
                        SchedulingPointType sp = Enum.Parse<SchedulingPointType>(decisionToken.Substring(
                            3, decisionToken.Length - 4));
#else
                        SchedulingPointType sp = (SchedulingPointType)Enum.Parse(typeof(SchedulingPointType),
                            decisionToken.Substring(3, decisionToken.Length - 4));
#endif

                        // The next operation token is of the form 'next(id:seqId)'.
                        string nextToken = tokens[2];
                        string[] nextTokens = nextToken.Substring(5, nextToken.Length - 6).Split(':');
                        ulong nextId = ulong.Parse(nextTokens[0]);
                        ulong nextSeqId = ulong.Parse(nextTokens[1]);
                        trace.AddSchedulingDecision(opId, opSeqId, sp, nextId, nextSeqId);
                    }
                    else if (decisionToken.StartsWith("bool("))
                    {
                        bool value = bool.Parse(decisionToken.Substring(5, decisionToken.Length - 6));
                        trace.AddNondeterministicBooleanDecision(opId, opSeqId, value);
                    }
                    else if (decisionToken.StartsWith("int("))
                    {
                        int value = int.Parse(decisionToken.Substring(4, decisionToken.Length - 5));
                        trace.AddNondeterministicIntegerDecision(opId, opSeqId, value);
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
        /// Adds the specified trace to the report.
        /// </summary>
        internal void ReportTrace(ExecutionTrace trace)
        {
            this.Steps = new List<string>();
            foreach (var step in trace)
            {
                this.Steps.Add(step.ToString());
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
