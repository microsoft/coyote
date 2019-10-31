﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime.Exploration;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class CurrentStateTest : BaseTest
    {
        public CurrentStateTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Unit : Event
        {
        }

        private class Server : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(Active))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Assert(this.CurrentState == typeof(Init));
                this.Raise(new Unit());
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
                r.CreateMachine(typeof(Server));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS));
        }
    }
}
