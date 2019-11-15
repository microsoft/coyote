// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class OverloadedEventHandlerTest : BaseTest
    {
        public OverloadedEventHandlerTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(HandleUnitEvent))]
        private class A : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.SendEvent(this.Id, new UnitEvent());
                return Task.CompletedTask;
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

        [Fact(Timeout = 5000)]
        public void TestOverloadedEventHandlerInActor()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(A));
            });
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

        [Fact(Timeout = 5000)]
        public void TestOverloadedEventHandlerInStateMachine()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M));
            });
        }
    }
}
