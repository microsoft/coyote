// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class GotoStateTest : BaseTest
    {
        public GotoStateTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.GotoState<Done>();
            }

            private class Done : State
            {
            }
        }

        internal static int MonitorValue;

        private class Safety : Monitor
        {
            [Start]
            [OnEntry(nameof(Init))]
            private class S1 : State
            {
            }

            [OnEntry(nameof(IncrementValue))]
            private class S2 : State
            {
            }

            private void Init()
            {
                this.Goto<S2>();
            }

            private void IncrementValue()
            {
                MonitorValue = 101;
            }
        }

        [Fact(Timeout=5000)]
        public void TestGotoMachineState()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M));
            });
        }

        [Fact(Timeout=5000)]
        public void TestGotoMonitorState()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(Safety));
            });

            Assert.Equal(101, MonitorValue);
        }
    }
}
