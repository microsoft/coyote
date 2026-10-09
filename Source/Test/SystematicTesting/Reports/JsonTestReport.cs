// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Machine-readable report of a test run that can be serialized in JSON format.
    /// </summary>
    internal sealed class JsonTestReport
    {
        /// <summary>
        /// The version of Coyote used during testing.
        /// </summary>
        public string CoyoteVersion { get; set; }

        /// <summary>
        /// The name of the test method.
        /// </summary>
        public string TestName { get; set; }

        /// <summary>
        /// Information about the assembly that contains the test.
        /// </summary>
        public JsonAssemblyInfo Assembly { get; set; }

        /// <summary>
        /// The settings that were used during testing.
        /// </summary>
        public JsonTestSettings Settings { get; set; }

        /// <summary>
        /// The verdict of the exploration.
        /// </summary>
        public JsonVerdict Verdict { get; set; }

        /// <summary>
        /// The bugs found during testing.
        /// </summary>
        public JsonBugs Bugs { get; set; }

        /// <summary>
        /// The internal errors that occurred during testing.
        /// </summary>
        public List<string> InternalErrors { get; set; }

        /// <summary>
        /// The uncontrolled invocations detected during testing.
        /// </summary>
        public List<string> UncontrolledInvocations { get; set; }

        /// <summary>
        /// Statistics about the exploration.
        /// </summary>
        public JsonExploration Exploration { get; set; }

        /// <summary>
        /// The report files emitted alongside this report, if any.
        /// </summary>
        public JsonArtifacts Artifacts { get; set; }

        /// <summary>
        /// The elapsed testing time in seconds.
        /// </summary>
        public double ElapsedSeconds { get; set; }

        /// <summary>
        /// Creates a <see cref="JsonTestReport"/> from the specified test report and context.
        /// </summary>
        internal static JsonTestReport Create(TestReport report, Configuration configuration, string testName,
            Assembly testAssembly, bool isTestRewritten, string strategy, double elapsedSeconds,
            IEnumerable<string> artifactPaths)
        {
            var verdict = report.GetVerdict();
            int totalExploredPaths = report.NumOfExploredFairPaths + report.NumOfExploredUnfairPaths;

            var result = new JsonTestReport();
            result.CoyoteVersion = typeof(JsonTestReport).Assembly.GetName().Version.ToString();
            result.TestName = testName;
            result.Assembly = JsonAssemblyInfo.Create(testAssembly, isTestRewritten);
            result.Settings = new JsonTestSettings
            {
                Strategy = strategy,
                StrategyBound = configuration.StrategyBound,
                Seed = configuration.RandomGeneratorSeed,
                Iterations = configuration.TestingIterations,
                TimeoutSeconds = configuration.TestingTimeout,
                MaxFairSchedulingSteps = configuration.MaxFairSchedulingSteps,
                MaxUnfairSchedulingSteps = configuration.MaxUnfairSchedulingSteps,
                PortfolioMode = configuration.PortfolioMode.ToString().ToLower(),
                IsLivenessCheckingEnabled = configuration.IsLivenessCheckingEnabled,
                IsPartiallyControlledConcurrencyAllowed = configuration.IsPartiallyControlledConcurrencyAllowed,
                IsPartiallyControlledDataNondeterminismAllowed =
                    configuration.IsPartiallyControlledDataNondeterminismAllowed,
                IsSystematicFuzzingEnabled = configuration.IsSystematicFuzzingEnabled,
                IsSystematicFuzzingFallbackEnabled = configuration.IsSystematicFuzzingFallbackEnabled,
                IsStrictExplorationEnabled = configuration.IsStrictExplorationEnabled,
                IsStrictBoundCheckingEnabled = configuration.IsStrictBoundCheckingEnabled,
                RunTestIterationsToCompletion = configuration.RunTestIterationsToCompletion
            };

            result.Verdict = new JsonVerdict
            {
                Status = verdict.Status,
                Reasons = verdict.Reasons.ToList(),
                Warnings = verdict.Warnings.ToList()
            };

            result.Bugs = new JsonBugs
            {
                Count = report.NumOfFoundBugs,
                Reports = report.BugReports.ToList()
            };

            result.InternalErrors = report.InternalErrors.ToList();
            result.UncontrolledInvocations = report.UncontrolledInvocations.ToList();

            result.Exploration = new JsonExploration
            {
                FairPaths = report.NumOfExploredFairPaths,
                UnfairPaths = report.NumOfExploredUnfairPaths,
                UniquePaths = report.CoverageInfo.ExploredPaths.Count,
                TruncatedFairPaths = report.MaxFairStepsHitInFairTests,
                TruncatedUnfairPaths = report.MaxUnfairStepsHitInUnfairTests,
                FairPathsExceedingUnfairBound = report.MaxUnfairStepsHitInFairTests,
                VisitedStates = report.CoverageInfo.VisitedStates.Count,
                FairSteps = JsonRange.Create(report.MinExploredFairSteps, report.TotalExploredFairSteps,
                    report.MaxExploredFairSteps, report.NumOfExploredFairPaths),
                UnfairSteps = JsonRange.Create(report.MinExploredUnfairSteps, report.TotalExploredUnfairSteps,
                    report.MaxExploredUnfairSteps, report.NumOfExploredUnfairPaths),
                ControlledOperations = JsonRange.Create(report.MinControlledOperations,
                    report.TotalControlledOperations, report.MaxControlledOperations, totalExploredPaths),
                ConcurrencyDegree = JsonRange.Create(report.MinConcurrencyDegree, report.TotalConcurrencyDegree,
                    report.MaxConcurrencyDegree, totalExploredPaths),
                OperationGroupingDegree = JsonRange.Create(report.MinOperationGroupingDegree,
                    report.TotalOperationGroupingDegree, report.MaxOperationGroupingDegree, totalExploredPaths)
            };

            result.Artifacts = JsonArtifacts.Create(artifactPaths);
            result.ElapsedSeconds = elapsedSeconds;
            return result;
        }

        /// <summary>
        /// Returns the report in JSON format.
        /// </summary>
        internal string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() },
            WriteIndented = true
        });

        /// <summary>
        /// Information about the assembly that contains the test.
        /// </summary>
        public sealed class JsonAssemblyInfo
        {
            /// <summary>
            /// The name of the assembly.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The file path of the assembly, if available.
            /// </summary>
            public string Path { get; set; }

            /// <summary>
            /// The SHA-256 hash of the assembly file in hexadecimal format, if available.
            /// </summary>
            public string Sha256 { get; set; }

            /// <summary>
            /// True if the assembly was rewritten for systematic testing, else false.
            /// </summary>
            public bool IsRewritten { get; set; }

            /// <summary>
            /// Creates a <see cref="JsonAssemblyInfo"/> for the specified assembly.
            /// </summary>
            internal static JsonAssemblyInfo Create(Assembly assembly, bool isRewritten)
            {
                var info = new JsonAssemblyInfo();
                info.Name = assembly?.GetName().Name;
                info.IsRewritten = isRewritten;

                string location = assembly?.Location;
                if (!string.IsNullOrEmpty(location))
                {
                    info.Path = location;
                    info.Sha256 = TryComputeSha256(location);
                }

                return info;
            }

            /// <summary>
            /// Computes the SHA-256 hash of the specified file, or returns null if the file cannot be read.
            /// </summary>
            private static string TryComputeSha256(string path)
            {
                try
                {
                    using (var sha256 = SHA256.Create())
                    {
                        using (var stream = File.OpenRead(path))
                        {
                            byte[] hash = sha256.ComputeHash(stream);
                            return BitConverter.ToString(hash).Replace("-", string.Empty);
                        }
                    }
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException ||
                    ex is SecurityException)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// The settings that were used during testing.
        /// </summary>
        public sealed class JsonTestSettings
        {
            /// <summary>
            /// The name of the exploration strategy.
            /// </summary>
            public string Strategy { get; set; }

            /// <summary>
            /// The bound used by the exploration strategy, if any.
            /// </summary>
            public int StrategyBound { get; set; }

            /// <summary>
            /// The random value generator seed, if it was explicitly set.
            /// </summary>
            public uint? Seed { get; set; }

            /// <summary>
            /// The number of requested testing iterations.
            /// </summary>
            public uint Iterations { get; set; }

            /// <summary>
            /// The testing timeout in seconds, or 0 if disabled.
            /// </summary>
            public int TimeoutSeconds { get; set; }

            /// <summary>
            /// The max fair scheduling steps bound.
            /// </summary>
            public int MaxFairSchedulingSteps { get; set; }

            /// <summary>
            /// The max unfair scheduling steps bound.
            /// </summary>
            public int MaxUnfairSchedulingSteps { get; set; }

            /// <summary>
            /// The portfolio mode.
            /// </summary>
            public string PortfolioMode { get; set; }

            /// <summary>
            /// True if liveness checking was enabled, else false.
            /// </summary>
            public bool IsLivenessCheckingEnabled { get; set; }

            /// <summary>
            /// True if partially controlled concurrency was allowed, else false.
            /// </summary>
            public bool IsPartiallyControlledConcurrencyAllowed { get; set; }

            /// <summary>
            /// True if partially controlled data non-determinism was allowed, else false.
            /// </summary>
            public bool IsPartiallyControlledDataNondeterminismAllowed { get; set; }

            /// <summary>
            /// True if systematic fuzzing was enabled, else false.
            /// </summary>
            public bool IsSystematicFuzzingEnabled { get; set; }

            /// <summary>
            /// True if the fallback to systematic fuzzing was enabled, else false.
            /// </summary>
            public bool IsSystematicFuzzingFallbackEnabled { get; set; }

            /// <summary>
            /// True if strict exploration was enabled, else false.
            /// </summary>
            public bool IsStrictExplorationEnabled { get; set; }

            /// <summary>
            /// True if strict bound checking was enabled, else false.
            /// </summary>
            public bool IsStrictBoundCheckingEnabled { get; set; }

            /// <summary>
            /// True if all testing iterations were run to completion, even after finding a bug.
            /// </summary>
            public bool RunTestIterationsToCompletion { get; set; }
        }

        /// <summary>
        /// The verdict of the exploration.
        /// </summary>
        public sealed class JsonVerdict
        {
            /// <summary>
            /// The status of the exploration.
            /// </summary>
            public ExplorationStatus Status { get; set; }

            /// <summary>
            /// The reasons why the exploration is considered incomplete, if any.
            /// </summary>
            public List<IncompleteExplorationReason> Reasons { get; set; }

            /// <summary>
            /// Warnings about the exploration, if any.
            /// </summary>
            public List<ExplorationWarning> Warnings { get; set; }
        }

        /// <summary>
        /// The bugs found during testing.
        /// </summary>
        public sealed class JsonBugs
        {
            /// <summary>
            /// The number of bugs found during testing.
            /// </summary>
            public int Count { get; set; }

            /// <summary>
            /// The unique bug reports.
            /// </summary>
            public List<string> Reports { get; set; }
        }

        /// <summary>
        /// Statistics about the exploration.
        /// </summary>
        public sealed class JsonExploration
        {
            /// <summary>
            /// Number of explored fair execution paths.
            /// </summary>
            public int FairPaths { get; set; }

            /// <summary>
            /// Number of explored unfair execution paths.
            /// </summary>
            public int UnfairPaths { get; set; }

            /// <summary>
            /// Number of unique explored execution paths.
            /// </summary>
            public int UniquePaths { get; set; }

            /// <summary>
            /// Number of fair execution paths truncated because they reached the fair max-steps bound.
            /// </summary>
            public int TruncatedFairPaths { get; set; }

            /// <summary>
            /// Number of unfair execution paths truncated because they reached the unfair max-steps bound.
            /// </summary>
            public int TruncatedUnfairPaths { get; set; }

            /// <summary>
            /// Number of fair execution paths that exceeded the unfair max-steps bound before terminating.
            /// </summary>
            public int FairPathsExceedingUnfairBound { get; set; }

            /// <summary>
            /// Number of unique program states visited during testing.
            /// </summary>
            public int VisitedStates { get; set; }

            /// <summary>
            /// Scheduling steps in fair execution paths.
            /// </summary>
            public JsonRange FairSteps { get; set; }

            /// <summary>
            /// Scheduling steps in unfair execution paths.
            /// </summary>
            public JsonRange UnfairSteps { get; set; }

            /// <summary>
            /// Controlled operations per execution path.
            /// </summary>
            public JsonRange ControlledOperations { get; set; }

            /// <summary>
            /// Degree of concurrency per execution path.
            /// </summary>
            public JsonRange ConcurrencyDegree { get; set; }

            /// <summary>
            /// Degree of operation grouping per execution path.
            /// </summary>
            public JsonRange OperationGroupingDegree { get; set; }
        }

        /// <summary>
        /// Minimum, average and maximum of a statistic across execution paths.
        /// </summary>
        public sealed class JsonRange
        {
            /// <summary>
            /// The minimum value.
            /// </summary>
            public int Min { get; set; }

            /// <summary>
            /// The average value.
            /// </summary>
            public int Avg { get; set; }

            /// <summary>
            /// The maximum value.
            /// </summary>
            public int Max { get; set; }

            /// <summary>
            /// Creates a <see cref="JsonRange"/>, or returns null if no execution paths were explored.
            /// </summary>
            internal static JsonRange Create(int min, int total, int max, int count)
            {
                if (count <= 0)
                {
                    return null;
                }

                return new JsonRange
                {
                    Min = min < 0 ? 0 : min,
                    Avg = total / count,
                    Max = max < 0 ? 0 : max
                };
            }
        }

        /// <summary>
        /// The report files emitted alongside the JSON report.
        /// </summary>
        public sealed class JsonArtifacts
        {
            /// <summary>
            /// The path of the human-readable trace file, if any.
            /// </summary>
            public string ReadableTrace { get; set; }

            /// <summary>
            /// The path of the reproducible trace file, if any.
            /// </summary>
            public string ReproducibleTrace { get; set; }

            /// <summary>
            /// The path of the XML formatted runtime log file, if any.
            /// </summary>
            public string XmlTrace { get; set; }

            /// <summary>
            /// The path of the DGML trace visualization file, if any.
            /// </summary>
            public string TraceGraph { get; set; }

            /// <summary>
            /// The path of the DGML actor trace visualization file, if any.
            /// </summary>
            public string ActorTraceGraph { get; set; }

            /// <summary>
            /// The path of the uncontrolled invocations report file, if any.
            /// </summary>
            public string UncontrolledInvocations { get; set; }

            /// <summary>
            /// Creates a <see cref="JsonArtifacts"/> from the specified report paths, or returns null if there are none.
            /// </summary>
            internal static JsonArtifacts Create(IEnumerable<string> paths)
            {
                var artifacts = new JsonArtifacts();
                bool hasAny = false;
                foreach (string path in paths ?? Enumerable.Empty<string>())
                {
                    hasAny = true;
                    if (path.EndsWith(".actors.trace.dgml", StringComparison.OrdinalIgnoreCase))
                    {
                        artifacts.ActorTraceGraph = path;
                    }
                    else if (path.EndsWith(".trace.dgml", StringComparison.OrdinalIgnoreCase))
                    {
                        artifacts.TraceGraph = path;
                    }
                    else if (path.EndsWith(".trace.xml", StringComparison.OrdinalIgnoreCase))
                    {
                        artifacts.XmlTrace = path;
                    }
                    else if (path.EndsWith(".uncontrolled.json", StringComparison.OrdinalIgnoreCase))
                    {
                        artifacts.UncontrolledInvocations = path;
                    }
                    else if (path.EndsWith(".trace", StringComparison.OrdinalIgnoreCase))
                    {
                        artifacts.ReproducibleTrace = path;
                    }
                    else if (path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        artifacts.ReadableTrace = path;
                    }
                }

                return hasAny ? artifacts : null;
            }
        }
    }
}
