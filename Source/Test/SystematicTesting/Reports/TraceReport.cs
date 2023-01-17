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
        /// The controlled decisions of this trace.
        /// </summary>
        public List<string> Decisions { get; set; }

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

                foreach (var decision in report.Decisions)
                {
                    string[] tokens = decision.Split(',');
                    string kindToken = tokens[0];
                    string spToken = tokens[1];

#if NET || NETCOREAPP3_1
                    SchedulingPointType sp = Enum.Parse<SchedulingPointType>(spToken.Substring(3, spToken.Length - 4));
#else
                    SchedulingPointType sp = (SchedulingPointType)Enum.Parse(typeof(SchedulingPointType), spToken.Substring(3, spToken.Length - 4));
#endif
                    if (kindToken.StartsWith("op("))
                    {
                        ulong id = ulong.Parse(kindToken.Substring(3, kindToken.Length - 4));
                        trace.AddSchedulingChoice(id, sp);
                    }
                    else if (kindToken.StartsWith("bool("))
                    {
                        bool value = bool.Parse(kindToken.Substring(5, kindToken.Length - 6));
                        trace.AddNondeterministicBooleanChoice(value, sp);
                    }
                    else if (kindToken.StartsWith("int("))
                    {
                        int value = int.Parse(kindToken.Substring(4, kindToken.Length - 5));
                        trace.AddNondeterministicIntegerChoice(value, sp);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unexpected decision '{decision}'.");
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
            this.Decisions = new List<string>();
            for (int idx = 0; idx < trace.Length; idx++)
            {
                ExecutionTrace.Step step = trace[idx];
                if (step.Kind == ExecutionTrace.DecisionKind.SchedulingChoice)
                {
                    this.Decisions.Add($"op({step.ScheduledOperationId}),sp({step.SchedulingPoint})");
                }
                else if (step.BooleanChoice != null)
                {
                    this.Decisions.Add($"bool({step.BooleanChoice.Value}),sp({step.SchedulingPoint})");
                }
                else
                {
                    this.Decisions.Add($"int({step.IntegerChoice.Value}),sp({step.SchedulingPoint})");
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
