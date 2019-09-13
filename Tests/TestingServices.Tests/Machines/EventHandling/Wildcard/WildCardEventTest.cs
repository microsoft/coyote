// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote.Machines;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class WildCardEventTest : BaseTest
    {
        public WildCardEventTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class A : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Foo))]
            [OnEventGotoState(typeof(E2), typeof(S1))]
            [DeferEvents(typeof(WildCardEvent))]
            private class S0 : MachineState
            {
            }

            [OnEventDoAction(typeof(E3), nameof(Bar))]
            private class S1 : MachineState
            {
            }

            private void Foo()
            {
            }

            private void Bar()
            {
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

        private class B : Machine
        {
            [Start]
            [OnEntry(nameof(Conf))]
            private class Init : MachineState
            {
            }

            private void Conf()
            {
                var a = this.CreateMachine(typeof(A));
                this.Send(a, new E3());
                this.Send(a, new E1());
                this.Send(a, new E2());
            }
        }

        [Fact(Timeout=5000)]
        public void TestWildCardEvent()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(B));
            });
        }
    }
}
