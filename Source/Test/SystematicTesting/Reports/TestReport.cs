// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.Coyote.Actors.Coverage;
using Microsoft.Coyote.Testing;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Report containing information from a test run.
    /// </summary>
    [DataContract]
    public class TestReport : ITestReport
    {
        /// <summary>
        /// Configuration of the program-under-test.
        /// </summary>
        [DataMember]
        public Configuration Configuration { get; private set; }

        /// <summary>
        /// Information regarding code coverage.
        /// </summary>
        [DataMember]
        public ActorCoverageInfo CoverageInfo { get; private set; }

        /// <summary>
        /// Number of explored fair schedules.
        /// </summary>
        [DataMember]
        public int NumOfExploredFairSchedules { get; internal set; }

        /// <summary>
        /// Number of explored unfair schedules.
        /// </summary>
        [DataMember]
        public int NumOfExploredUnfairSchedules { get; internal set; }

        /// <summary>
        /// Number of found bugs.
        /// </summary>
        [DataMember]
        public int NumOfFoundBugs { get; internal set; }

        /// <summary>
        /// Set of bug reports.
        /// </summary>
        [DataMember]
        public HashSet<string> BugReports { get; internal set; }

        /// <summary>
        /// Set of uncontrolled invocations.
        /// </summary>
        [DataMember]
        public HashSet<string> UncontrolledInvocations { get; internal set; }

        /// <summary>
        /// The minimum number of controlled operations.
        /// </summary>
        [DataMember]
        public int MinControlledOperations { get; internal set; }

        /// <summary>
        /// The maximum number of controlled operations.
        /// </summary>
        [DataMember]
        public int MaxControlledOperations { get; internal set; }

        /// <summary>
        /// The total number of controlled operations.
        /// </summary>
        [DataMember]
        public int TotalControlledOperations { get; internal set; }

        /// <summary>
        /// The min degree of concurrency.
        /// </summary>
        [DataMember]
        public int MinConcurrencyDegree { get; internal set; }

        /// <summary>
        /// The max degree of concurrency.
        /// </summary>
        [DataMember]
        public int MaxConcurrencyDegree { get; internal set; }

        /// <summary>
        /// The total degree of concurrency (across all testing iterations).
        /// </summary>
        [DataMember]
        public int TotalConcurrencyDegree { get; internal set; }

        /// <summary>
        /// The minimum degree of operation grouping.
        /// </summary>
        [DataMember]
        public int MinOperationGroupingDegree { get; internal set; }

        /// <summary>
        /// The maximum degree of operation grouping.
        /// </summary>
        [DataMember]
        public int MaxOperationGroupingDegree { get; internal set; }

        /// <summary>
        /// The total degree of operation grouping (across all testing iterations).
        /// </summary>
        [DataMember]
        public int TotalOperationGroupingDegree { get; internal set; }

        /// <summary>
        /// The min explored scheduling steps in fair tests.
        /// </summary>
        [DataMember]
        public int MinExploredFairSteps { get; internal set; }

        /// <summary>
        /// The max explored scheduling steps in fair tests.
        /// </summary>
        [DataMember]
        public int MaxExploredFairSteps { get; internal set; }

        /// <summary>
        /// The total explored scheduling steps (across all testing iterations) in fair tests.
        /// </summary>
        [DataMember]
        public int TotalExploredFairSteps { get; internal set; }

        /// <summary>
        /// The min explored scheduling steps in unfair tests.
        /// </summary>
        [DataMember]
        public int MinExploredUnfairSteps { get; internal set; }

        /// <summary>
        /// The max explored scheduling steps in unfair tests.
        /// </summary>
        [DataMember]
        public int MaxExploredUnfairSteps { get; internal set; }

        /// <summary>
        /// The total explored scheduling steps (across all testing iterations) in unfair tests.
        /// </summary>
        [DataMember]
        public int TotalExploredUnfairSteps { get; internal set; }

        /// <summary>
        /// Number of times the fair max steps bound was hit in fair tests.
        /// </summary>
        [DataMember]
        public int MaxFairStepsHitInFairTests { get; internal set; }

        /// <summary>
        /// Number of times the unfair max steps bound was hit in fair tests.
        /// </summary>
        [DataMember]
        public int MaxUnfairStepsHitInFairTests { get; internal set; }

        /// <summary>
        /// Number of times the unfair max steps bound was hit in unfair tests.
        /// </summary>
        [DataMember]
        public int MaxUnfairStepsHitInUnfairTests { get; internal set; }

        /// <summary>
        /// Set of internal errors. If no internal errors occurred, then this set is empty.
        /// </summary>
        [DataMember]
        public HashSet<string> InternalErrors { get; internal set; }

        /// <summary>
        /// Lock for the test report.
        /// </summary>
        private readonly object Lock;

        /// <summary>
        /// Unhandled exception caught by RunNextIteration.
        /// </summary>
        internal Exception ThrownException { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestReport"/> class.
        /// </summary>
        public TestReport(Configuration configuration)
        {
            this.Configuration = configuration;

            this.CoverageInfo = new ActorCoverageInfo();

            this.NumOfExploredFairSchedules = 0;
            this.NumOfExploredUnfairSchedules = 0;
            this.NumOfFoundBugs = 0;
            this.BugReports = new HashSet<string>();
            this.UncontrolledInvocations = new HashSet<string>();

            this.MinControlledOperations = -1;
            this.MaxControlledOperations = -1;
            this.TotalControlledOperations = 0;
            this.MinConcurrencyDegree = -1;
            this.MaxConcurrencyDegree = -1;
            this.TotalConcurrencyDegree = 0;
            this.MinOperationGroupingDegree = -1;
            this.MaxOperationGroupingDegree = -1;
            this.TotalOperationGroupingDegree = 0;
            this.MinExploredFairSteps = -1;
            this.MaxExploredFairSteps = -1;
            this.TotalExploredFairSteps = 0;
            this.MinExploredUnfairSteps = -1;
            this.MaxExploredUnfairSteps = -1;
            this.TotalExploredUnfairSteps = 0;
            this.MaxFairStepsHitInFairTests = 0;
            this.MaxUnfairStepsHitInFairTests = 0;
            this.MaxUnfairStepsHitInUnfairTests = 0;

            this.InternalErrors = new HashSet<string>();

            this.Lock = new object();
        }

        /// <inheritdoc/>
        void ITestReport.SetSchedulingStatistics(bool isBugFound, string bugReport, int numOperations, int concurrencyDegree,
            int groupingDegree, int scheduledSteps, bool isMaxScheduledStepsBoundReached, bool isScheduleFair)
        {
            if (isBugFound)
            {
                this.NumOfFoundBugs++;
                this.BugReports.Add(bugReport);
            }

            this.TotalControlledOperations += numOperations;
            this.MaxControlledOperations = Math.Max(this.MaxControlledOperations, numOperations);
            if (this.MinControlledOperations < 0 ||
                this.MinControlledOperations > numOperations)
            {
                this.MinControlledOperations = numOperations;
            }

            this.TotalConcurrencyDegree += concurrencyDegree;
            this.MaxConcurrencyDegree = Math.Max(this.MaxConcurrencyDegree, concurrencyDegree);
            if (this.MinConcurrencyDegree < 0 ||
                this.MinConcurrencyDegree > concurrencyDegree)
            {
                this.MinConcurrencyDegree = concurrencyDegree;
            }

            this.TotalOperationGroupingDegree += groupingDegree;
            this.MaxOperationGroupingDegree = Math.Max(this.MaxOperationGroupingDegree, groupingDegree);
            if (this.MinOperationGroupingDegree < 0 ||
                this.MinOperationGroupingDegree > groupingDegree)
            {
                this.MinOperationGroupingDegree = groupingDegree;
            }

            if (isScheduleFair)
            {
                this.NumOfExploredFairSchedules++;
                this.TotalExploredFairSteps += scheduledSteps;
                this.MaxExploredFairSteps = Math.Max(this.MaxExploredFairSteps, scheduledSteps);
                if (this.MinExploredFairSteps < 0 ||
                    this.MinExploredFairSteps > scheduledSteps)
                {
                    this.MinExploredFairSteps = scheduledSteps;
                }

                if (isMaxScheduledStepsBoundReached)
                {
                    this.MaxFairStepsHitInFairTests++;
                }

                if (scheduledSteps >= this.Configuration.MaxUnfairSchedulingSteps)
                {
                    this.MaxUnfairStepsHitInFairTests++;
                }
            }
            else
            {
                this.NumOfExploredUnfairSchedules++;
                this.TotalExploredUnfairSteps += scheduledSteps;
                this.MaxExploredUnfairSteps = Math.Max(this.MaxExploredUnfairSteps, scheduledSteps);
                if (this.MinExploredUnfairSteps < 0 ||
                    this.MinExploredUnfairSteps > scheduledSteps)
                {
                    this.MinExploredUnfairSteps = scheduledSteps;
                }

                if (isMaxScheduledStepsBoundReached)
                {
                    this.MaxUnfairStepsHitInUnfairTests++;
                }
            }
        }

        /// <inheritdoc/>
        void ITestReport.SetUnhandledException(Exception exception)
        {
            this.ThrownException = exception;
        }

        /// <inheritdoc/>
        void ITestReport.SetUncontrolledInvocations(HashSet<string> invocations)
        {
            this.UncontrolledInvocations.UnionWith(invocations);
        }

        /// <summary>
        /// Merges the information from the specified test report.
        /// </summary>
        /// <returns>True if merged successfully.</returns>
        public bool Merge(TestReport testReport)
        {
            if (!this.Configuration.AssemblyToBeAnalyzed.Equals(testReport.Configuration.AssemblyToBeAnalyzed))
            {
                // Only merge test reports that have the same program name.
                return false;
            }

            lock (this.Lock)
            {
                this.CoverageInfo.Merge(testReport.CoverageInfo);

                this.NumOfFoundBugs += testReport.NumOfFoundBugs;

                this.BugReports.UnionWith(testReport.BugReports);
                this.UncontrolledInvocations.UnionWith(testReport.UncontrolledInvocations);

                this.TotalControlledOperations += testReport.TotalControlledOperations;
                this.MaxControlledOperations = Math.Max(this.MaxControlledOperations, testReport.MaxControlledOperations);
                if (testReport.MinControlledOperations >= 0 &&
                    (this.MinControlledOperations < 0 ||
                    this.MinControlledOperations > testReport.MinControlledOperations))
                {
                    this.MinControlledOperations = testReport.MinControlledOperations;
                }

                this.TotalConcurrencyDegree += testReport.TotalConcurrencyDegree;
                this.MaxConcurrencyDegree = Math.Max(this.MaxConcurrencyDegree, testReport.MaxConcurrencyDegree);
                if (testReport.MinConcurrencyDegree >= 0 &&
                    (this.MinConcurrencyDegree < 0 ||
                    this.MinConcurrencyDegree > testReport.MinConcurrencyDegree))
                {
                    this.MinConcurrencyDegree = testReport.MinConcurrencyDegree;
                }

                this.TotalOperationGroupingDegree += testReport.TotalOperationGroupingDegree;
                this.MaxOperationGroupingDegree = Math.Max(this.MaxOperationGroupingDegree, testReport.MaxOperationGroupingDegree);
                if (testReport.MinOperationGroupingDegree >= 0 &&
                    (this.MinOperationGroupingDegree < 0 ||
                    this.MinOperationGroupingDegree > testReport.MinOperationGroupingDegree))
                {
                    this.MinOperationGroupingDegree = testReport.MinOperationGroupingDegree;
                }

                this.NumOfExploredFairSchedules += testReport.NumOfExploredFairSchedules;
                this.NumOfExploredUnfairSchedules += testReport.NumOfExploredUnfairSchedules;

                this.TotalExploredFairSteps += testReport.TotalExploredFairSteps;
                this.MaxExploredFairSteps = Math.Max(this.MaxExploredFairSteps, testReport.MaxExploredFairSteps);
                if (testReport.MinExploredFairSteps >= 0 &&
                    (this.MinExploredFairSteps < 0 ||
                    this.MinExploredFairSteps > testReport.MinExploredFairSteps))
                {
                    this.MinExploredFairSteps = testReport.MinExploredFairSteps;
                }

                this.TotalExploredUnfairSteps += testReport.TotalExploredUnfairSteps;
                this.MaxExploredUnfairSteps = Math.Max(this.MaxExploredUnfairSteps, testReport.MaxExploredUnfairSteps);
                if (testReport.MinExploredUnfairSteps >= 0 &&
                    (this.MinExploredUnfairSteps < 0 ||
                    this.MinExploredUnfairSteps > testReport.MinExploredUnfairSteps))
                {
                    this.MinExploredUnfairSteps = testReport.MinExploredUnfairSteps;
                }

                this.MaxFairStepsHitInFairTests += testReport.MaxFairStepsHitInFairTests;
                this.MaxUnfairStepsHitInFairTests += testReport.MaxUnfairStepsHitInFairTests;
                this.MaxUnfairStepsHitInUnfairTests += testReport.MaxUnfairStepsHitInUnfairTests;

                if (this.ThrownException is null)
                {
                    this.ThrownException = testReport.ThrownException;
                }

                this.InternalErrors.UnionWith(testReport.InternalErrors);
            }

            return true;
        }

        /// <summary>
        /// Returns the testing report as a string, given a configuration and an optional prefix.
        /// </summary>
        public string GetText(Configuration configuration, string prefix = "")
        {
            StringBuilder report = new StringBuilder();

            report.AppendFormat("{0} Testing statistics:", prefix);

            report.AppendLine();
            report.AppendFormat(
                "{0} Found {1} bug{2}.",
                prefix.Equals("...") ? "....." : prefix,
                this.NumOfFoundBugs,
                this.NumOfFoundBugs is 1 ? string.Empty : "s");

            int numUncontrolledInvocations = this.UncontrolledInvocations.Count;
            if (numUncontrolledInvocations > 0)
            {
                report.AppendLine();
                report.AppendFormat(
                    "{0} Found {1} uncontrolled invocation{2}.",
                    prefix.Equals("...") ? "....." : prefix,
                    numUncontrolledInvocations,
                    numUncontrolledInvocations is 1 ? string.Empty : "s");
            }

            report.AppendLine();
            report.AppendFormat("{0} Scheduling statistics:", prefix);

            int totalExploredSchedules = this.NumOfExploredFairSchedules +
                this.NumOfExploredUnfairSchedules;

            report.AppendLine();
            report.AppendFormat(
                "{0} Explored {1} schedule{2}: {3} fair and {4} unfair.",
                prefix.Equals("...") ? "....." : prefix,
                totalExploredSchedules,
                totalExploredSchedules is 1 ? string.Empty : "s",
                this.NumOfExploredFairSchedules,
                this.NumOfExploredUnfairSchedules);

            if (totalExploredSchedules > 0 &&
                this.NumOfFoundBugs > 0)
            {
                report.AppendLine();
                report.AppendFormat(
                    "{0} Found {1:F2}% buggy schedules.",
                    prefix.Equals("...") ? "....." : prefix,
                    this.NumOfFoundBugs * 100.0 / totalExploredSchedules);
            }

            int visitedStatesCount = this.CoverageInfo.VisitedStates.Count;
            if (visitedStatesCount > 0)
            {
                report.AppendLine();
                report.AppendFormat(
                    "{0} Visited {1} state{2}.",
                    prefix.Equals("...") ? "....." : prefix,
                    visitedStatesCount,
                    visitedStatesCount is 1 ? string.Empty : "s");
            }

            if (this.TotalControlledOperations > 0)
            {
                report.AppendLine();
                report.AppendFormat(
                    "{0} Controlled {1} operation{2}: {3} (min), {4} (avg), {5} (max).",
                    prefix.Equals("...") ? "....." : prefix,
                    this.TotalControlledOperations,
                    this.TotalControlledOperations is 1 ? string.Empty : "s",
                    this.MinControlledOperations,
                    this.TotalControlledOperations / totalExploredSchedules,
                    this.MaxControlledOperations);
            }

            if (this.TotalConcurrencyDegree > 0)
            {
                report.AppendLine();
                report.AppendFormat(
                    "{0} Degree of concurrency: {1} (min), {2} (avg), {3} (max).",
                    prefix.Equals("...") ? "....." : prefix,
                    this.MinConcurrencyDegree,
                    this.TotalConcurrencyDegree / totalExploredSchedules,
                    this.MaxConcurrencyDegree);
            }

            if (this.TotalOperationGroupingDegree > 0)
            {
                report.AppendLine();
                report.AppendFormat(
                    "{0} Degree of operation grouping: {1} (min), {2} (avg), {3} (max).",
                    prefix.Equals("...") ? "....." : prefix,
                    this.MinOperationGroupingDegree,
                    this.TotalOperationGroupingDegree / totalExploredSchedules,
                    this.MaxOperationGroupingDegree);
            }

            if (this.NumOfExploredFairSchedules > 0)
            {
                report.AppendLine();
                report.AppendFormat(
                    "{0} Number of scheduling decisions in fair terminating schedules: {1} (min), {2} (avg), {3} (max).",
                    prefix.Equals("...") ? "....." : prefix,
                    this.MinExploredFairSteps < 0 ? 0 : this.MinExploredFairSteps,
                    this.TotalExploredFairSteps / this.NumOfExploredFairSchedules,
                    this.MaxExploredFairSteps < 0 ? 0 : this.MaxExploredFairSteps);

                if (configuration.MaxUnfairSchedulingSteps > 0 &&
                    this.MaxUnfairStepsHitInFairTests > 0)
                {
                    report.AppendLine();
                    report.AppendFormat(
                        "{0} Exceeded the max-steps bound of '{1}' in {2:F2}% of the fair schedules.",
                        prefix.Equals("...") ? "....." : prefix,
                        configuration.MaxUnfairSchedulingSteps,
                        (double)this.MaxUnfairStepsHitInFairTests / this.NumOfExploredFairSchedules * 100);
                }

                if (configuration.UserExplicitlySetMaxFairSchedulingSteps &&
                    configuration.MaxFairSchedulingSteps > 0 &&
                    this.MaxFairStepsHitInFairTests > 0)
                {
                    report.AppendLine();
                    report.AppendFormat(
                        "{0} Hit the max-steps bound of '{1}' in {2:F2}% of the fair schedules.",
                        prefix.Equals("...") ? "....." : prefix,
                        configuration.MaxFairSchedulingSteps,
                        (double)this.MaxFairStepsHitInFairTests / this.NumOfExploredFairSchedules * 100);
                }
            }

            if (this.NumOfExploredUnfairSchedules > 0)
            {
                report.AppendLine();
                report.AppendFormat(
                    "{0} Number of scheduling decisions in unfair terminating schedules: {1} (min), {2} (avg), {3} (max).",
                    prefix.Equals("...") ? "....." : prefix,
                    this.MinExploredUnfairSteps < 0 ? 0 : this.MinExploredUnfairSteps,
                    this.TotalExploredUnfairSteps / this.NumOfExploredUnfairSchedules,
                    this.MaxExploredUnfairSteps < 0 ? 0 : this.MaxExploredUnfairSteps);

                if (configuration.MaxUnfairSchedulingSteps > 0 &&
                    this.MaxUnfairStepsHitInUnfairTests > 0)
                {
                    report.AppendLine();
                    report.AppendFormat(
                        "{0} Hit the max-steps bound of '{1}' in {2:F2}% of the unfair schedules.",
                        prefix.Equals("...") ? "....." : prefix,
                        configuration.MaxUnfairSchedulingSteps,
                        (double)this.MaxUnfairStepsHitInUnfairTests / this.NumOfExploredUnfairSchedules * 100);
                }
            }

            report.AppendLine();
            report.AppendLine("===READSET===");
            foreach (var read in Runtime.SchedulingPoint.ReadSet.ToList())
            {
                report.AppendLine(read);
            }

            report.AppendLine("===WRITESET===");
            foreach (var write in Runtime.SchedulingPoint.WriteSet.ToList())
            {
                report.AppendLine(write);
            }

            return report.ToString();
        }

        /// <summary>
        /// Clones the test report.
        /// </summary>
        public TestReport Clone()
        {
            var serializerSettings = new DataContractSerializerSettings();
            serializerSettings.PreserveObjectReferences = true;
            var serializer = new DataContractSerializer(typeof(TestReport), serializerSettings);
            using (var ms = new System.IO.MemoryStream())
            {
                lock (this.Lock)
                {
                    serializer.WriteObject(ms, this);
                    ms.Position = 0;
                    return (TestReport)serializer.ReadObject(ms);
                }
            }
        }
    }
}
