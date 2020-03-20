// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common.Events;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Actors
{
    public class OverloadedEventHandlerTests : BaseSystematicTest
    {
        public OverloadedEventHandlerTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(HandleUnitEvent))]
        private class A : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
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
                this.SendEvent(this.Id, UnitEvent.Instance);
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
