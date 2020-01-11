// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Specifications
{
    public class MonitorWildCardEventTests : BaseTest
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
                r.RegisterMonitor(typeof(M1));
                r.InvokeMonitor<M1>(new E1());
                r.InvokeMonitor<M1>(new E2());
                r.InvokeMonitor<M1>(new E3());
            });
        }

        [Fact(Timeout = 5000)]
        public void TestDoActionOnWildCardEvent()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(M2));
                r.InvokeMonitor<M2>(new E1());
            },
            expectedError: "Check reached.");
        }

        [Fact(Timeout = 5000)]
        public void TestGotoStateOnWildCardEvent()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(M3));
                r.InvokeMonitor<M3>(new E1());
            },
            expectedError: "Check reached.");
        }
    }
}
