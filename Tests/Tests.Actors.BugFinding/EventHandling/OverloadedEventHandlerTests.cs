// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Tests.Common.Events;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests
{
    public class OverloadedEventHandlerTests : BaseActorBugFindingTest
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

#pragma warning disable CA1822 // Mark members as static
            private void HandleUnitEvent()
#pragma warning restore CA1822 // Mark members as static
            {
            }

#pragma warning disable CA1801 // Parameter not used
#pragma warning disable IDE0060 // Parameter not used
#pragma warning disable CA1822 // Mark members as static
            private void HandleUnitEvent(int k)
#pragma warning restore CA1822 // Mark members as static
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

#pragma warning disable CA1822 // Mark members as static
            private void HandleUnitEvent()
#pragma warning restore CA1822 // Mark members as static
            {
            }

#pragma warning disable CA1801 // Parameter not used
#pragma warning disable IDE0060 // Parameter not used
#pragma warning disable CA1822 // Mark members as static
            private void HandleUnitEvent(int k)
#pragma warning restore CA1822 // Mark members as static
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
