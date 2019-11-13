// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors.StateMachines
{
    public class AmbiguousEventHandlerTest : BaseTest
    {
        public AmbiguousEventHandlerTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(UnitEvent), nameof(HandleUnitEvent))]
            public class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, new UnitEvent());
            }

            private void HandleUnitEvent()
            {
            }

#pragma warning disable CA1801 // Parameter not used
#pragma warning disable IDE0060 // Parameter not used
            private void HandleUnitEvent(int k)
            {
            }
#pragma warning restore IDE0060 // Parameter not used
#pragma warning restore CA1801 // Parameter not used
        }

        private class Safety : Monitor
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(UnitEvent), nameof(HandleUnitEvent))]
            public class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(new UnitEvent());
            }

            private void HandleUnitEvent()
            {
            }

#pragma warning disable CA1801 // Parameter not used
#pragma warning disable IDE0060 // Parameter not used
            private void HandleUnitEvent(int k)
            {
            }
#pragma warning restore IDE0060 // Parameter not used
#pragma warning restore CA1801 // Parameter not used
        }

        [Fact(Timeout=5000)]
        public void TestAmbiguousMachineEventHandler()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M));
            });
        }

        [Fact(Timeout=5000)]
        public void TestAmbiguousMonitorEventHandler()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(Safety));
            });
        }
    }
}
