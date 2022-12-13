// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.SystematicTesting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Runtime.Tests
{
    public class ExecutionTraceTests : BaseRuntimeTest
    {
        public ExecutionTraceTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionTraceAddSchedulingChoices()
        {
            ExecutionTrace trace = ExecutionTrace.Create();
            Assert.True(trace.Length is 0);

            trace.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 0);
            trace.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 2);
            trace.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 3);
            trace.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 1);
            trace.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 2);
            this.LogTrace(trace);
            Assert.True(trace.Length is 5);

            Assert.True(trace[0].Index is 0);
            Assert.True(trace[0] is ExecutionTrace.SchedulingStep);
            Assert.True((trace[0] as ExecutionTrace.SchedulingStep).Value is 0);

            Assert.True(trace[1].Index is 1);
            Assert.True(trace[1] is ExecutionTrace.SchedulingStep);
            Assert.True((trace[1] as ExecutionTrace.SchedulingStep).Value is 2);

            Assert.True(trace[2].Index is 2);
            Assert.True(trace[2] is ExecutionTrace.SchedulingStep);
            Assert.True((trace[2] as ExecutionTrace.SchedulingStep).Value is 3);

            Assert.True(trace[3].Index is 3);
            Assert.True(trace[3] is ExecutionTrace.SchedulingStep);
            Assert.True((trace[3] as ExecutionTrace.SchedulingStep).Value is 1);

            Assert.True(trace[4].Index is 4);
            Assert.True(trace[4] is ExecutionTrace.SchedulingStep);
            Assert.True((trace[4] as ExecutionTrace.SchedulingStep).Value is 2);
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionTraceAddNondeterministicBooleanChoices()
        {
            ExecutionTrace trace = ExecutionTrace.Create();
            Assert.True(trace.Length is 0);

            trace.AddNondeterministicBooleanDecision(0, true);
            trace.AddNondeterministicBooleanDecision(0, false);
            trace.AddNondeterministicBooleanDecision(0, true);
            this.LogTrace(trace);
            Assert.True(trace.Length is 3);

            Assert.True(trace[0].Index is 0);
            Assert.True(trace[0] is ExecutionTrace.BooleanChoiceStep);
            Assert.True((trace[0] as ExecutionTrace.BooleanChoiceStep).Value is true);

            Assert.True(trace[1].Index is 1);
            Assert.True(trace[1] is ExecutionTrace.BooleanChoiceStep);
            Assert.True((trace[1] as ExecutionTrace.BooleanChoiceStep).Value is false);

            Assert.True(trace[2].Index is 2);
            Assert.True(trace[2] is ExecutionTrace.BooleanChoiceStep);
            Assert.True((trace[2] as ExecutionTrace.BooleanChoiceStep).Value is true);
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionTraceAddNondeterministicIntegerChoices()
        {
            ExecutionTrace trace = ExecutionTrace.Create();
            Assert.True(trace.Length is 0);

            trace.AddNondeterministicIntegerDecision(0, 3);
            trace.AddNondeterministicIntegerDecision(0, 7);
            trace.AddNondeterministicIntegerDecision(0, 4);
            this.LogTrace(trace);
            Assert.True(trace.Length is 3);

            Assert.True(trace[0].Index is 0);
            Assert.True(trace[0] is ExecutionTrace.IntegerChoiceStep);
            Assert.True((trace[0] as ExecutionTrace.IntegerChoiceStep).Value is 3);

            Assert.True(trace[1].Index is 1);
            Assert.True(trace[1] is ExecutionTrace.IntegerChoiceStep);
            Assert.True((trace[1] as ExecutionTrace.IntegerChoiceStep).Value is 7);

            Assert.True(trace[2].Index is 2);
            Assert.True(trace[2] is ExecutionTrace.IntegerChoiceStep);
            Assert.True((trace[2] as ExecutionTrace.IntegerChoiceStep).Value is 4);
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionTraceAddMixedChoices()
        {
            ExecutionTrace trace = ExecutionTrace.Create();
            Assert.True(trace.Length is 0);

            trace.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 0);
            trace.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 2);
            trace.AddNondeterministicBooleanDecision(0, true);
            trace.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 1);
            trace.AddNondeterministicIntegerDecision(0, 5);
            this.LogTrace(trace);
            Assert.True(trace.Length is 5);

            Assert.True(trace[0].Index is 0);
            Assert.True(trace[0] is ExecutionTrace.SchedulingStep);
            Assert.True((trace[0] as ExecutionTrace.SchedulingStep).Value is 0);

            Assert.True(trace[1].Index is 1);
            Assert.True(trace[1] is ExecutionTrace.SchedulingStep);
            Assert.True((trace[1] as ExecutionTrace.SchedulingStep).Value is 2);

            Assert.True(trace[2].Index is 2);
            Assert.True(trace[2] is ExecutionTrace.BooleanChoiceStep);
            Assert.True((trace[2] as ExecutionTrace.BooleanChoiceStep).Value is true);

            Assert.True(trace[3].Index is 3);
            Assert.True(trace[3] is ExecutionTrace.SchedulingStep);
            Assert.True((trace[3] as ExecutionTrace.SchedulingStep).Value is 1);

            Assert.True(trace[4].Index is 4);
            Assert.True(trace[4] is ExecutionTrace.IntegerChoiceStep);
            Assert.True((trace[4] as ExecutionTrace.IntegerChoiceStep).Value is 5);
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionTraceExtendWithShorter()
        {
            ExecutionTrace trace = ExecutionTrace.Create();
            Assert.True(trace.Length is 0);

            trace.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 0);
            trace.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 2);
            trace.AddNondeterministicBooleanDecision(0, true);
            this.LogTrace(trace);
            Assert.True(trace.Length is 3);

            ExecutionTrace other = ExecutionTrace.Create();
            Assert.True(other.Length is 0);

            other.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 0);
            other.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 2);
            this.LogTrace(other);
            Assert.True(other.Length is 2);

            trace.ExtendOrReplace(other);
            this.LogTrace(trace);
            Assert.True(trace.Length is 3);

            Assert.True(trace[0].Index is 0);
            Assert.True(trace[0] is ExecutionTrace.SchedulingStep);
            Assert.True((trace[0] as ExecutionTrace.SchedulingStep).Value is 0);

            Assert.True(trace[1].Index is 1);
            Assert.True(trace[1] is ExecutionTrace.SchedulingStep);
            Assert.True((trace[1] as ExecutionTrace.SchedulingStep).Value is 2);

            Assert.True(trace[2].Index is 2);
            Assert.True(trace[2] is ExecutionTrace.BooleanChoiceStep);
            Assert.True((trace[2] as ExecutionTrace.BooleanChoiceStep).Value is true);
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionTraceExtendWithLonger()
        {
            ExecutionTrace trace = ExecutionTrace.Create();
            Assert.True(trace.Length is 0);

            trace.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 0);
            trace.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 2);
            trace.AddNondeterministicBooleanDecision(0, true);
            this.LogTrace(trace);
            Assert.True(trace.Length is 3);

            ExecutionTrace other = ExecutionTrace.Create();
            Assert.True(other.Length is 0);

            other.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 0);
            other.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 2);
            other.AddNondeterministicBooleanDecision(0, true);
            other.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 1);
            other.AddNondeterministicIntegerDecision(0, 5);
            this.LogTrace(other);
            Assert.True(other.Length is 5);

            trace.ExtendOrReplace(other);
            this.LogTrace(trace);
            Assert.True(trace.Length is 5);

            Assert.True(trace[0].Index is 0);
            Assert.True(trace[0] is ExecutionTrace.SchedulingStep);
            Assert.True((trace[0] as ExecutionTrace.SchedulingStep).Value is 0);

            Assert.True(trace[1].Index is 1);
            Assert.True(trace[1] is ExecutionTrace.SchedulingStep);
            Assert.True((trace[1] as ExecutionTrace.SchedulingStep).Value is 2);

            Assert.True(trace[2].Index is 2);
            Assert.True(trace[2] is ExecutionTrace.BooleanChoiceStep);
            Assert.True((trace[2] as ExecutionTrace.BooleanChoiceStep).Value is true);

            Assert.True(trace[3].Index is 3);
            Assert.True(trace[3] is ExecutionTrace.SchedulingStep);
            Assert.True((trace[3] as ExecutionTrace.SchedulingStep).Value is 1);

            Assert.True(trace[4].Index is 4);
            Assert.True(trace[4] is ExecutionTrace.IntegerChoiceStep);
            Assert.True((trace[4] as ExecutionTrace.IntegerChoiceStep).Value is 5);
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionTraceReplaceWithEmpty()
        {
            ExecutionTrace trace = ExecutionTrace.Create();
            Assert.True(trace.Length is 0);

            trace.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 0);
            trace.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 3);
            trace.AddNondeterministicBooleanDecision(0, false);
            this.LogTrace(trace);
            Assert.True(trace.Length is 3);

            ExecutionTrace other = ExecutionTrace.Create();
            this.LogTrace(other);
            Assert.True(other.Length is 0);

            trace.ExtendOrReplace(other);
            this.LogTrace(trace);
            Assert.True(trace.Length is 0);
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionTraceReplaceWithShorter()
        {
            ExecutionTrace trace = ExecutionTrace.Create();
            Assert.True(trace.Length is 0);

            trace.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 0);
            trace.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 3);
            trace.AddNondeterministicBooleanDecision(0, true);
            this.LogTrace(trace);
            Assert.True(trace.Length is 3);

            ExecutionTrace other = ExecutionTrace.Create();
            Assert.True(other.Length is 0);

            other.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 0);
            other.AddNondeterministicIntegerDecision(0, 5);
            this.LogTrace(other);
            Assert.True(other.Length is 2);

            trace.ExtendOrReplace(other);
            this.LogTrace(trace);
            Assert.True(trace.Length is 2);

            Assert.True(trace[0].Index is 0);
            Assert.True(trace[0] is ExecutionTrace.SchedulingStep);
            Assert.True((trace[0] as ExecutionTrace.SchedulingStep).Value is 0);

            Assert.True(trace[1].Index is 1);
            Assert.True(trace[1] is ExecutionTrace.IntegerChoiceStep);
            Assert.True((trace[1] as ExecutionTrace.IntegerChoiceStep).Value is 5);
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionTraceReplaceWithLonger()
        {
            ExecutionTrace trace = ExecutionTrace.Create();
            Assert.True(trace.Length is 0);

            trace.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 0);
            trace.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 2);
            trace.AddNondeterministicBooleanDecision(0, true);
            this.LogTrace(trace);
            Assert.True(trace.Length is 3);

            ExecutionTrace other = ExecutionTrace.Create();
            Assert.True(other.Length is 0);

            other.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 0);
            other.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 3);
            other.AddNondeterministicBooleanDecision(0, false);
            other.AddSchedulingDecision(0, SchedulingPointType.Default, 0, 1);
            other.AddNondeterministicIntegerDecision(0, 5);
            this.LogTrace(other);
            Assert.True(other.Length is 5);

            trace.ExtendOrReplace(other);
            this.LogTrace(trace);
            Assert.True(trace.Length is 5);

            Assert.True(trace[0].Index is 0);
            Assert.True(trace[0] is ExecutionTrace.SchedulingStep);
            Assert.True((trace[0] as ExecutionTrace.SchedulingStep).Value is 0);

            Assert.True(trace[1].Index is 1);
            Assert.True(trace[1] is ExecutionTrace.SchedulingStep);
            Assert.True((trace[1] as ExecutionTrace.SchedulingStep).Value is 3);

            Assert.True(trace[2].Index is 2);
            Assert.True(trace[2] is ExecutionTrace.BooleanChoiceStep);
            Assert.True((trace[2] as ExecutionTrace.BooleanChoiceStep).Value is false);

            Assert.True(trace[3].Index is 3);
            Assert.True(trace[3] is ExecutionTrace.SchedulingStep);
            Assert.True((trace[3] as ExecutionTrace.SchedulingStep).Value is 1);

            Assert.True(trace[4].Index is 4);
            Assert.True(trace[4] is ExecutionTrace.IntegerChoiceStep);
            Assert.True((trace[4] as ExecutionTrace.IntegerChoiceStep).Value is 5);
        }

        private void LogTrace(ExecutionTrace trace)
        {
            var report = new TraceReport();
            report.ReportTrace(trace);
            this.TestOutput.WriteLine(report.ToJson());
        }
    }
}
