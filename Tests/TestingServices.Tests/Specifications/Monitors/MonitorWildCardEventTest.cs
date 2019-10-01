// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Machines;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class MonitorWildCardEventTest : BaseTest
    {
        public MonitorWildCardEventTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M1 : Monitor
        {
            [Start]
            [IgnoreEvents(typeof(WildCardEvent))]
            private class S0 : MonitorState
            {
            }
        }

        private class M2 : Monitor
        {
            [Start]
            [OnEventDoAction(typeof(WildCardEvent), nameof(Check))]
            private class S0 : MonitorState
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
            private class S0 : MonitorState
            {
            }

            [OnEntry(nameof(Check))]
            private class S1 : MonitorState
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

        [Fact(Timeout=5000)]
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
