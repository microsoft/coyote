// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Specifications
{
    public class IdempotentRegisterMonitorTests : BaseSystematicTest
    {
        public IdempotentRegisterMonitorTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Counter
        {
            public int Value;

            public Counter()
            {
                this.Value = 0;
            }
        }

        private class E : Event
        {
            public Counter Counter;

            public E(Counter counter)
            {
                this.Counter = counter;
            }
        }

        private class M : Monitor
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Check))]
            private class Init : State
            {
            }

            private void Check(Event e)
            {
                (e as E).Counter.Value++;
            }
        }

        private class N : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                Counter counter = new Counter();
                this.Monitor(typeof(M), new E(counter));
                this.Assert(counter.Value == 1, "Monitor created more than once.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestIdempotentRegisterMonitorInvocation()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<M>();
                r.RegisterMonitor<M>();
                ActorId n = r.CreateActor(typeof(N));
            });
        }
    }
}
