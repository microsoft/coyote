// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tests.Common.Events;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.SystematicTesting.Tests
{
    public class CurrentStateTests : BaseSystematicActorTest
    {
        public CurrentStateTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Server : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(UnitEvent), typeof(Active))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Assert(this.CurrentState == typeof(Init));
                this.RaiseEvent(UnitEvent.Instance);
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Assert(this.CurrentState == typeof(Active));
            }
        }

        /// <summary>
        /// Coyote semantics test: current state must be of the expected type.
        /// </summary>
        [Fact(Timeout = 5000)]
        public void TestCurrentState()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(Server));
            },
            configuration: GetConfiguration().WithDFSStrategy());
        }
    }
}
