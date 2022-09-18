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

            trace.AddSchedulingChoice(0);
            trace.AddSchedulingChoice(2);
            trace.AddSchedulingChoice(3);
            trace.AddSchedulingChoice(1);
            trace.AddSchedulingChoice(2);
            this.LogTrace(trace);
            Assert.True(trace.Length is 5);

            Assert.True(trace[0].Index is 0);
            Assert.True(trace[0].Type is ExecutionTrace.DecisionType.SchedulingChoice);
            Assert.True(trace[0].ScheduledOperationId is 0);
            Assert.True(!trace[0].BooleanChoice.HasValue);
            Assert.True(!trace[0].IntegerChoice.HasValue);

            Assert.True(trace[1].Index is 1);
            Assert.True(trace[1].Type is ExecutionTrace.DecisionType.SchedulingChoice);
            Assert.True(trace[1].ScheduledOperationId is 2);
            Assert.True(!trace[1].BooleanChoice.HasValue);
            Assert.True(!trace[1].IntegerChoice.HasValue);

            Assert.True(trace[2].Index is 2);
            Assert.True(trace[2].Type is ExecutionTrace.DecisionType.SchedulingChoice);
            Assert.True(trace[2].ScheduledOperationId is 3);
            Assert.True(!trace[2].BooleanChoice.HasValue);
            Assert.True(!trace[2].IntegerChoice.HasValue);

            Assert.True(trace[3].Index is 3);
            Assert.True(trace[3].Type is ExecutionTrace.DecisionType.SchedulingChoice);
            Assert.True(trace[3].ScheduledOperationId is 1);
            Assert.True(!trace[3].BooleanChoice.HasValue);
            Assert.True(!trace[3].IntegerChoice.HasValue);

            Assert.True(trace[4].Index is 4);
            Assert.True(trace[4].Type is ExecutionTrace.DecisionType.SchedulingChoice);
            Assert.True(trace[4].ScheduledOperationId is 2);
            Assert.True(!trace[4].BooleanChoice.HasValue);
            Assert.True(!trace[4].IntegerChoice.HasValue);
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionTraceAddNondeterministicBooleanChoices()
        {
            ExecutionTrace trace = ExecutionTrace.Create();
            Assert.True(trace.Length is 0);

            trace.AddNondeterministicBooleanChoice(true);
            trace.AddNondeterministicBooleanChoice(false);
            trace.AddNondeterministicBooleanChoice(true);
            this.LogTrace(trace);
            Assert.True(trace.Length is 3);

            Assert.True(trace[0].Index is 0);
            Assert.True(trace[0].Type is ExecutionTrace.DecisionType.NondeterministicChoice);
            Assert.True(trace[0].ScheduledOperationId is 0);
            Assert.True(trace[0].BooleanChoice.HasValue);
            Assert.True(trace[0].BooleanChoice.Value is true);
            Assert.True(!trace[0].IntegerChoice.HasValue);

            Assert.True(trace[1].Index is 1);
            Assert.True(trace[1].Type is ExecutionTrace.DecisionType.NondeterministicChoice);
            Assert.True(trace[1].ScheduledOperationId is 0);
            Assert.True(trace[1].BooleanChoice.HasValue);
            Assert.True(trace[1].BooleanChoice.Value is false);
            Assert.True(!trace[1].IntegerChoice.HasValue);

            Assert.True(trace[2].Index is 2);
            Assert.True(trace[2].Type is ExecutionTrace.DecisionType.NondeterministicChoice);
            Assert.True(trace[2].ScheduledOperationId is 0);
            Assert.True(trace[2].BooleanChoice.HasValue);
            Assert.True(trace[2].BooleanChoice.Value is true);
            Assert.True(!trace[2].IntegerChoice.HasValue);
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionTraceAddNondeterministicIntegerChoices()
        {
            ExecutionTrace trace = ExecutionTrace.Create();
            Assert.True(trace.Length is 0);

            trace.AddNondeterministicIntegerChoice(3);
            trace.AddNondeterministicIntegerChoice(7);
            trace.AddNondeterministicIntegerChoice(4);
            this.LogTrace(trace);
            Assert.True(trace.Length is 3);

            Assert.True(trace[0].Index is 0);
            Assert.True(trace[0].Type is ExecutionTrace.DecisionType.NondeterministicChoice);
            Assert.True(trace[0].ScheduledOperationId is 0);
            Assert.True(!trace[0].BooleanChoice.HasValue);
            Assert.True(trace[0].IntegerChoice.HasValue);
            Assert.True(trace[0].IntegerChoice.Value is 3);

            Assert.True(trace[1].Index is 1);
            Assert.True(trace[1].Type is ExecutionTrace.DecisionType.NondeterministicChoice);
            Assert.True(trace[1].ScheduledOperationId is 0);
            Assert.True(!trace[1].BooleanChoice.HasValue);
            Assert.True(trace[1].IntegerChoice.HasValue);
            Assert.True(trace[1].IntegerChoice.Value is 7);

            Assert.True(trace[2].Index is 2);
            Assert.True(trace[2].Type is ExecutionTrace.DecisionType.NondeterministicChoice);
            Assert.True(trace[2].ScheduledOperationId is 0);
            Assert.True(!trace[2].BooleanChoice.HasValue);
            Assert.True(trace[2].IntegerChoice.HasValue);
            Assert.True(trace[2].IntegerChoice.Value is 4);
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionTraceAddMixedChoices()
        {
            ExecutionTrace trace = ExecutionTrace.Create();
            Assert.True(trace.Length is 0);

            trace.AddSchedulingChoice(0);
            trace.AddSchedulingChoice(2);
            trace.AddNondeterministicBooleanChoice(true);
            trace.AddSchedulingChoice(1);
            trace.AddNondeterministicIntegerChoice(5);
            this.LogTrace(trace);
            Assert.True(trace.Length is 5);

            Assert.True(trace[0].Index is 0);
            Assert.True(trace[0].Type is ExecutionTrace.DecisionType.SchedulingChoice);
            Assert.True(trace[0].ScheduledOperationId is 0);
            Assert.True(!trace[0].BooleanChoice.HasValue);
            Assert.True(!trace[0].IntegerChoice.HasValue);

            Assert.True(trace[1].Index is 1);
            Assert.True(trace[1].Type is ExecutionTrace.DecisionType.SchedulingChoice);
            Assert.True(trace[1].ScheduledOperationId is 2);
            Assert.True(!trace[1].BooleanChoice.HasValue);
            Assert.True(!trace[1].IntegerChoice.HasValue);

            Assert.True(trace[2].Index is 2);
            Assert.True(trace[2].Type is ExecutionTrace.DecisionType.NondeterministicChoice);
            Assert.True(trace[2].ScheduledOperationId is 0);
            Assert.True(trace[2].BooleanChoice.HasValue);
            Assert.True(trace[2].BooleanChoice.Value is true);
            Assert.True(!trace[2].IntegerChoice.HasValue);

            Assert.True(trace[3].Index is 3);
            Assert.True(trace[3].Type is ExecutionTrace.DecisionType.SchedulingChoice);
            Assert.True(trace[3].ScheduledOperationId is 1);
            Assert.True(!trace[3].BooleanChoice.HasValue);
            Assert.True(!trace[3].IntegerChoice.HasValue);

            Assert.True(trace[4].Index is 4);
            Assert.True(trace[4].Type is ExecutionTrace.DecisionType.NondeterministicChoice);
            Assert.True(trace[4].ScheduledOperationId is 0);
            Assert.True(!trace[4].BooleanChoice.HasValue);
            Assert.True(trace[4].IntegerChoice.HasValue);
            Assert.True(trace[4].IntegerChoice.Value is 5);
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionTraceExtendWithShorter()
        {
            ExecutionTrace trace = ExecutionTrace.Create();
            Assert.True(trace.Length is 0);

            trace.AddSchedulingChoice(0);
            trace.AddSchedulingChoice(2);
            trace.AddNondeterministicBooleanChoice(true);
            this.LogTrace(trace);
            Assert.True(trace.Length is 3);

            ExecutionTrace other = ExecutionTrace.Create();
            Assert.True(other.Length is 0);

            other.AddSchedulingChoice(0);
            other.AddSchedulingChoice(2);
            this.LogTrace(other);
            Assert.True(other.Length is 2);

            trace.ExtendOrReplace(other);
            this.LogTrace(trace);
            Assert.True(trace.Length is 3);

            Assert.True(trace[0].Index is 0);
            Assert.True(trace[0].Type is ExecutionTrace.DecisionType.SchedulingChoice);
            Assert.True(trace[0].ScheduledOperationId is 0);
            Assert.True(!trace[0].BooleanChoice.HasValue);
            Assert.True(!trace[0].IntegerChoice.HasValue);

            Assert.True(trace[1].Index is 1);
            Assert.True(trace[1].Type is ExecutionTrace.DecisionType.SchedulingChoice);
            Assert.True(trace[1].ScheduledOperationId is 2);
            Assert.True(!trace[1].BooleanChoice.HasValue);
            Assert.True(!trace[1].IntegerChoice.HasValue);

            Assert.True(trace[2].Index is 2);
            Assert.True(trace[2].Type is ExecutionTrace.DecisionType.NondeterministicChoice);
            Assert.True(trace[2].ScheduledOperationId is 0);
            Assert.True(trace[2].BooleanChoice.HasValue);
            Assert.True(trace[2].BooleanChoice.Value is true);
            Assert.True(!trace[2].IntegerChoice.HasValue);
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionTraceExtendWithLonger()
        {
            ExecutionTrace trace = ExecutionTrace.Create();
            Assert.True(trace.Length is 0);

            trace.AddSchedulingChoice(0);
            trace.AddSchedulingChoice(2);
            trace.AddNondeterministicBooleanChoice(true);
            this.LogTrace(trace);
            Assert.True(trace.Length is 3);

            ExecutionTrace other = ExecutionTrace.Create();
            Assert.True(other.Length is 0);

            other.AddSchedulingChoice(0);
            other.AddSchedulingChoice(2);
            other.AddNondeterministicBooleanChoice(true);
            other.AddSchedulingChoice(1);
            other.AddNondeterministicIntegerChoice(5);
            this.LogTrace(other);
            Assert.True(other.Length is 5);

            trace.ExtendOrReplace(other);
            this.LogTrace(trace);
            Assert.True(trace.Length is 5);

            Assert.True(trace[0].Index is 0);
            Assert.True(trace[0].Type is ExecutionTrace.DecisionType.SchedulingChoice);
            Assert.True(trace[0].ScheduledOperationId is 0);
            Assert.True(!trace[0].BooleanChoice.HasValue);
            Assert.True(!trace[0].IntegerChoice.HasValue);

            Assert.True(trace[1].Index is 1);
            Assert.True(trace[1].Type is ExecutionTrace.DecisionType.SchedulingChoice);
            Assert.True(trace[1].ScheduledOperationId is 2);
            Assert.True(!trace[1].BooleanChoice.HasValue);
            Assert.True(!trace[1].IntegerChoice.HasValue);

            Assert.True(trace[2].Index is 2);
            Assert.True(trace[2].Type is ExecutionTrace.DecisionType.NondeterministicChoice);
            Assert.True(trace[2].ScheduledOperationId is 0);
            Assert.True(trace[2].BooleanChoice.HasValue);
            Assert.True(trace[2].BooleanChoice.Value is true);
            Assert.True(!trace[2].IntegerChoice.HasValue);

            Assert.True(trace[3].Index is 3);
            Assert.True(trace[3].Type is ExecutionTrace.DecisionType.SchedulingChoice);
            Assert.True(trace[3].ScheduledOperationId is 1);
            Assert.True(!trace[3].BooleanChoice.HasValue);
            Assert.True(!trace[3].IntegerChoice.HasValue);

            Assert.True(trace[4].Index is 4);
            Assert.True(trace[4].Type is ExecutionTrace.DecisionType.NondeterministicChoice);
            Assert.True(trace[4].ScheduledOperationId is 0);
            Assert.True(!trace[4].BooleanChoice.HasValue);
            Assert.True(trace[4].IntegerChoice.HasValue);
            Assert.True(trace[4].IntegerChoice.Value is 5);
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionTraceReplaceWithEmpty()
        {
            ExecutionTrace trace = ExecutionTrace.Create();
            Assert.True(trace.Length is 0);

            trace.AddSchedulingChoice(0);
            trace.AddSchedulingChoice(3);
            trace.AddNondeterministicBooleanChoice(false);
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

            trace.AddSchedulingChoice(0);
            trace.AddSchedulingChoice(3);
            trace.AddNondeterministicBooleanChoice(true);
            this.LogTrace(trace);
            Assert.True(trace.Length is 3);

            ExecutionTrace other = ExecutionTrace.Create();
            Assert.True(other.Length is 0);

            other.AddSchedulingChoice(0);
            other.AddNondeterministicIntegerChoice(5);
            this.LogTrace(other);
            Assert.True(other.Length is 2);

            trace.ExtendOrReplace(other);
            this.LogTrace(trace);
            Assert.True(trace.Length is 2);

            Assert.True(trace[0].Index is 0);
            Assert.True(trace[0].Type is ExecutionTrace.DecisionType.SchedulingChoice);
            Assert.True(trace[0].ScheduledOperationId is 0);
            Assert.True(!trace[0].BooleanChoice.HasValue);
            Assert.True(!trace[0].IntegerChoice.HasValue);

            Assert.True(trace[1].Index is 1);
            Assert.True(trace[1].Type is ExecutionTrace.DecisionType.NondeterministicChoice);
            Assert.True(trace[1].ScheduledOperationId is 0);
            Assert.True(!trace[1].BooleanChoice.HasValue);
            Assert.True(trace[1].IntegerChoice.HasValue);
            Assert.True(trace[1].IntegerChoice.Value is 5);
        }

        [Fact(Timeout = 5000)]
        public void TestExecutionTraceReplaceWithLonger()
        {
            ExecutionTrace trace = ExecutionTrace.Create();
            Assert.True(trace.Length is 0);

            trace.AddSchedulingChoice(0);
            trace.AddSchedulingChoice(2);
            trace.AddNondeterministicBooleanChoice(true);
            this.LogTrace(trace);
            Assert.True(trace.Length is 3);

            ExecutionTrace other = ExecutionTrace.Create();
            Assert.True(other.Length is 0);

            other.AddSchedulingChoice(0);
            other.AddSchedulingChoice(3);
            other.AddNondeterministicBooleanChoice(false);
            other.AddSchedulingChoice(1);
            other.AddNondeterministicIntegerChoice(5);
            this.LogTrace(other);
            Assert.True(other.Length is 5);

            trace.ExtendOrReplace(other);
            this.LogTrace(trace);
            Assert.True(trace.Length is 5);

            Assert.True(trace[0].Index is 0);
            Assert.True(trace[0].Type is ExecutionTrace.DecisionType.SchedulingChoice);
            Assert.True(trace[0].ScheduledOperationId is 0);
            Assert.True(!trace[0].BooleanChoice.HasValue);
            Assert.True(!trace[0].IntegerChoice.HasValue);

            Assert.True(trace[1].Index is 1);
            Assert.True(trace[1].Type is ExecutionTrace.DecisionType.SchedulingChoice);
            Assert.True(trace[1].ScheduledOperationId is 3);
            Assert.True(!trace[1].BooleanChoice.HasValue);
            Assert.True(!trace[1].IntegerChoice.HasValue);

            Assert.True(trace[2].Index is 2);
            Assert.True(trace[2].Type is ExecutionTrace.DecisionType.NondeterministicChoice);
            Assert.True(trace[2].ScheduledOperationId is 0);
            Assert.True(trace[2].BooleanChoice.HasValue);
            Assert.True(trace[2].BooleanChoice.Value is false);
            Assert.True(!trace[2].IntegerChoice.HasValue);

            Assert.True(trace[3].Index is 3);
            Assert.True(trace[3].Type is ExecutionTrace.DecisionType.SchedulingChoice);
            Assert.True(trace[3].ScheduledOperationId is 1);
            Assert.True(!trace[3].BooleanChoice.HasValue);
            Assert.True(!trace[3].IntegerChoice.HasValue);

            Assert.True(trace[4].Index is 4);
            Assert.True(trace[4].Type is ExecutionTrace.DecisionType.NondeterministicChoice);
            Assert.True(trace[4].ScheduledOperationId is 0);
            Assert.True(!trace[4].BooleanChoice.HasValue);
            Assert.True(trace[4].IntegerChoice.HasValue);
            Assert.True(trace[4].IntegerChoice.Value is 5);
        }

        private void LogTrace(ExecutionTrace trace)
        {
            string json = TraceReport.GetJson(trace, Configuration.Create());
            this.TestOutput.WriteLine(json);
        }
    }
}
