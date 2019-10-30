// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class SingleStateMachineTest : BaseTest
    {
        public SingleStateMachineTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
            public int Counter;
            public ActorId Id;

            public E(ActorId id)
            {
                this.Counter = 0;
                this.Id = id;
            }

            public E(int c, ActorId id)
            {
                this.Counter = c;
                this.Id = id;
            }
        }

        private class M : SingleStateMachine
        {
            private int count;
            private ActorId sender;

            protected override Task InitOnEntry(Event e)
            {
                this.count = 1;
                this.sender = (e as E).Id;
                return Task.CompletedTask;
            }

            protected override Task ProcessEvent(Event e)
            {
                this.count++;
                return Task.CompletedTask;
            }

            protected override void OnHalt()
            {
                this.count++;
                this.Runtime.SendEvent(this.sender, new E(this.count, this.Id));
            }
        }

        private class Harness : SingleStateMachine
        {
            protected override async Task InitOnEntry(Event e)
            {
                var m = this.CreateMachine(typeof(M), new E(this.Id));
                this.Send(m, new E(this.Id));
                this.Send(m, new Halt());
                var r = await this.Receive(typeof(E));
                this.Assert((r as E).Counter == 3);
            }

            protected override Task ProcessEvent(Event e)
            {
                throw new NotImplementedException();
            }
        }

        [Fact(Timeout=5000)]
        public void TestSingleStateMachine()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(Harness));
            },
            configuration: Configuration.Create().WithNumberOfIterations(100));
        }
    }
}
