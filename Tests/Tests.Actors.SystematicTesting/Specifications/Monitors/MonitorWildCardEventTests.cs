// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.SystematicTesting.Tests.Specifications
{
    public class MonitorWildCardEventTests : BaseSystematicActorTest
    {
        public MonitorWildCardEventTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M1 : Monitor
        {
            [Start]
            [IgnoreEvents(typeof(WildCardEvent))]
            private class S0 : State
            {
            }
        }

        private class M2 : Monitor
        {
            [Start]
            [OnEventDoAction(typeof(WildCardEvent), nameof(Check))]
            private class S0 : State
            {
            }

            private void Check()
            {
                this.Assert(false, "Check reached.");
            }
        }

        private class M3 : Monitor
        {
            [Start]
            [OnEventGotoState(typeof(WildCardEvent), typeof(S1))]
            private class S0 : State
            {
            }

            [OnEntry(nameof(Check))]
            private class S1 : State
            {
            }

            private void Check()
            {
                this.Assert(false, "Check reached.");
            }
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        private class E3 : Event
        {
        }

        [Fact(Timeout = 5000)]
        public void TestIgnoreWildCardEvent()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<M1>();
                r.Monitor<M1>(new E1());
                r.Monitor<M1>(new E2());
                r.Monitor<M1>(new E3());
            });
        }

        [Fact(Timeout = 5000)]
        public void TestDoActionOnWildCardEvent()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor<M2>();
                r.Monitor<M2>(new E1());
            },
            expectedError: "Check reached.");
        }

        [Fact(Timeout = 5000)]
        public void TestGotoStateOnWildCardEvent()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor<M3>();
                r.Monitor<M3>(new E1());
            },
            expectedError: "Check reached.");
        }
    }
}
