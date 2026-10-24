// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.Coyote.Testing;
using Xunit;

namespace Microsoft.Coyote.BugFinding.Tests.Reports
{
    public class VerdictConcurrencyTests
    {
        [Fact(Timeout = 10000)]
        public void TestGetVerdictWaitsForConcurrentMerge()
        {
            var configuration = Configuration.Create().WithTestingIterations(1);
            var report = new TestReport(configuration);
            ((ITestReport)report).SetSchedulingStatistics(false, null, 1, 1, 1, 0, false, true);
            Assert.Contains(IncompleteExplorationReason.NoSchedulingDecisions, report.GetVerdict().Reasons);
            var iteration = new TestReport(configuration);
            ((ITestReport)iteration).SetSchedulingStatistics(false, null, 1, 1, 1, 1, true, true);

            var mergeLock = typeof(TestReport).GetField("Lock", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(report);
            using var started = new ManualResetEventSlim();
            using var completed = new ManualResetEventSlim();
            ExplorationVerdict verdict = null;
            Exception readerError = null;
            var reader = new Thread(() =>
            {
                try
                {
                    started.Set();
                    verdict = report.GetVerdict();
                }
                catch (Exception ex)
                {
                    readerError = ex;
                }
                finally
                {
                    completed.Set();
                }
            });
            reader.IsBackground = true;

            try
            {
                lock (mergeLock)
                {
                    reader.Start();
                    Assert.True(started.Wait(5000));
                    Assert.True(SpinWait.SpinUntil(() => completed.IsSet ||
                        (reader.ThreadState & ThreadState.WaitSleepJoin) != 0, 5000));
                    Assert.False(completed.IsSet, "GetVerdict read the report while its merge lock was held.");
                    Assert.True(report.Merge(iteration));
                }

                Assert.True(completed.Wait(5000));
                Assert.Null(readerError);
                Assert.Equal(ExplorationStatus.Incomplete, verdict.Status);
                Assert.Contains(IncompleteExplorationReason.TruncatedExecutionPaths, verdict.Reasons);
                Assert.DoesNotContain(IncompleteExplorationReason.NoSchedulingDecisions, verdict.Reasons);
            }
            finally
            {
                Assert.True(reader.Join(5000));
            }
        }

        [Theory(Timeout = 10000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestRestoredReportPreservesVerdictAndTermination(bool deserializeDirectly)
        {
            var configuration = Configuration.Create().WithTestingIterations(1).WithStrictBoundCheckingEnabled();
            var original = new TestReport(configuration);
            ((ITestReport)original).SetSchedulingStatistics(false, null, 1, 1, 1, 0, false, true);
            TestReport restored;
            if (deserializeDirectly)
            {
                var serializer = new DataContractSerializer(typeof(TestReport));
                using var stream = new MemoryStream();
                serializer.WriteObject(stream, original);
                stream.Position = 0;
                restored = (TestReport)serializer.ReadObject(stream);
            }
            else
            {
                restored = original.Clone();
            }

            Assert.Equal(original.GetVerdict().Status, restored.GetVerdict().Status);
            Assert.Equal(original.GetVerdict().Reasons, restored.GetVerdict().Reasons);
            Assert.True(restored.Configuration.IsStrictBoundCheckingEnabled);
            restored.RecordExplorationTermination(isCanceled: true);
            Assert.Equal(ExplorationStatus.Incomplete, restored.GetVerdict().Status);
            Assert.Contains(IncompleteExplorationReason.NoSchedulingDecisions, restored.GetVerdict().Reasons);
            Assert.Contains(IncompleteExplorationReason.TestingCanceled, restored.GetVerdict().Reasons);
            Assert.DoesNotContain(IncompleteExplorationReason.TestingCanceled, original.GetVerdict().Reasons);
            Assert.Equal(restored.GetVerdict().Reasons, restored.Clone().GetVerdict().Reasons);
        }
    }
}
