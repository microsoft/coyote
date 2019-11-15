// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime.Exploration;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class CurrentStateTest : BaseTest
    {
        public CurrentStateTest(ITestOutputHelper output)
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
                this.RaiseEvent(new UnitEvent());
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
        [Fact(Timeout=5000)]
        public void TestCurrentState()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(Server));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS));
        }
    }
}
