// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class FairRandomTest : BaseTest
    {
        public FairRandomTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        private class Engine
        {
            public static bool FairRandom(ICoyoteRuntime runtime)
            {
                return runtime.FairRandom();
            }
        }

        private class UntilDone : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(E2), typeof(End))]
            private class Waiting : MonitorState
            {
            }

            [Cold]
            private class End : MonitorState
            {
            }
        }

        private class M : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(HandleEvent1))]
            [OnEventDoAction(typeof(E2), nameof(HandleEvent2))]
            private class Init : MachineState
            {
            }

            private void HandleEvent1()
            {
                if (Engine.FairRandom(this.Id.Runtime))
                {
                    this.Send(this.Id, new E2());
                }
                else
                {
                    this.Send(this.Id, new E1());
                }
            }

            private void HandleEvent2()
            {
                this.Monitor<UntilDone>(new E2());
                this.Raise(new Halt());
            }
        }

        [Fact(Timeout=5000)]
        public void TestFairRandom()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(UntilDone));
                var m = r.CreateMachine(typeof(M));
                r.SendEvent(m, new E1());
            });
        }
    }
}
