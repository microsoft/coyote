// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Report containing the reproducible trace from a test run.
    /// </summary>
    internal sealed class TraceReport
    {
        /// <summary>
        /// The name of the test corresponding to this trace.
        /// </summary>
        public string TestName { get; set; }

        /// <summary>
        /// The settings that were used during testing.
        /// </summary>
        public TestSettings Settings { get; set; }

        /// <summary>
        /// The controlled decisions of this trace.
        /// </summary>
        public List<string> Decisions { get; set; }

        /// <summary>
        /// Returns the report from the specified <see cref="ExecutionTrace"/> in JSON format.
        /// </summary>
        internal static string GetJson(ExecutionTrace trace, Configuration configuration)
        {
            var report = new TraceReport();
            report.TestName = configuration.TestMethodName;

            report.Settings = new TestSettings();
            report.Settings.Strategy = configuration.SchedulingStrategy;
            report.Settings.StrategyBound = configuration.StrategyBound;

            if (configuration.RandomGeneratorSeed.HasValue)
            {
                report.Settings.Seed = configuration.RandomGeneratorSeed.Value;
            }

            report.Settings.IsLivenessCheckingEnabled = configuration.IsLivenessCheckingEnabled;
            report.Settings.LivenessTemperatureThreshold = configuration.LivenessTemperatureThreshold;
            report.Settings.IsLockAccessRaceCheckingEnabled = configuration.IsLockAccessRaceCheckingEnabled;
            report.Settings.MaxFairSchedulingSteps = configuration.MaxFairSchedulingSteps;
            report.Settings.MaxUnfairSchedulingSteps = configuration.MaxUnfairSchedulingSteps;
            report.Settings.TimeoutDelay = configuration.TimeoutDelay;
            report.Settings.DeadlockTimeout = configuration.DeadlockTimeout;
            report.Settings.IsPartiallyControlledConcurrencyAllowed = configuration.IsPartiallyControlledConcurrencyAllowed;
            report.Settings.UncontrolledConcurrencyResolutionAttempts = configuration.UncontrolledConcurrencyResolutionAttempts;
            report.Settings.UncontrolledConcurrencyResolutionDelay = configuration.UncontrolledConcurrencyResolutionDelay;

            report.Decisions = new List<string>();
            for (int idx = 0; idx < trace.Length; idx++)
            {
                ExecutionTrace.Step step = trace[idx];
                if (step is null)
                {
                    continue;
                }

                if (step.Type == ExecutionTrace.DecisionType.SchedulingChoice)
                {
                    report.Decisions.Add($"op({step.ScheduledOperationId})");
                }
                else if (step.BooleanChoice != null)
                {
                    report.Decisions.Add($"bool({step.BooleanChoice.Value})");
                }
                else
                {
                    report.Decisions.Add($"int({step.IntegerChoice.Value})");
                }
            }

            return JsonSerializer.Serialize(report, new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            });
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
                configuration.SchedulingStrategy = report.Settings.Strategy;
                configuration.StrategyBound = report.Settings.StrategyBound;
                if (report.Settings.Seed.HasValue)
                {
                    configuration.RandomGeneratorSeed = report.Settings.Seed.Value;
                }

                configuration.IsLivenessCheckingEnabled = report.Settings.IsLivenessCheckingEnabled;
                configuration.LivenessTemperatureThreshold = report.Settings.LivenessTemperatureThreshold;
                configuration.IsLockAccessRaceCheckingEnabled = report.Settings.IsLockAccessRaceCheckingEnabled;
                configuration.MaxFairSchedulingSteps = report.Settings.MaxFairSchedulingSteps;
                configuration.MaxUnfairSchedulingSteps = report.Settings.MaxUnfairSchedulingSteps;
                configuration.TimeoutDelay = report.Settings.TimeoutDelay;
                configuration.DeadlockTimeout = report.Settings.DeadlockTimeout;
                configuration.IsPartiallyControlledConcurrencyAllowed = report.Settings.IsPartiallyControlledConcurrencyAllowed;
                configuration.UncontrolledConcurrencyResolutionAttempts = report.Settings.UncontrolledConcurrencyResolutionAttempts;
                configuration.UncontrolledConcurrencyResolutionDelay = report.Settings.UncontrolledConcurrencyResolutionDelay;

                foreach (var decision in report.Decisions)
                {
                    if (decision.StartsWith("op("))
                    {
                        ulong id = ulong.Parse(decision.Substring(3, decision.Length - 4));
                        trace.AddSchedulingChoice(id);
                    }
                    else if (decision.StartsWith("bool("))
                    {
                        bool value = bool.Parse(decision.Substring(5, decision.Length - 6));
                        trace.AddNondeterministicBooleanChoice(value);
                    }
                    else if (decision.StartsWith("int("))
                    {
                        int value = int.Parse(decision.Substring(4, decision.Length - 5));
                        trace.AddNondeterministicIntegerChoice(value);
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
            /// Value specifying if partially controlled concurrency is allowed or not.
            /// </summary>
            public bool IsPartiallyControlledConcurrencyAllowed { get; set; }

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
