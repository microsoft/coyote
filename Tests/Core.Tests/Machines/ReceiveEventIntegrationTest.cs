// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Machines;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests
{
    public class ReceiveEventIntegrationTest : BaseTest
    {
        public ReceiveEventIntegrationTest(ITestOutputHelper output)
            : base(output)
        {
        }

        internal class SetupEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public SetupEvent(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
            public ActorId Id;

            public E2(ActorId id)
            {
                this.Id = id;
            }
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Send(this.Id, new E1());
                await this.Receive(typeof(E1));
                tcs.SetResult(true);
            }
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Send(this.Id, new E1());
                await this.Receive(typeof(E1), e => e is E1);
                tcs.SetResult(true);
            }
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Send(this.Id, new E1());
                await this.Receive(typeof(E1), typeof(E2));
                tcs.SetResult(true);
            }
        }

        private class M4 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                var id = this.CreateMachine(typeof(M5), new E2(this.Id));
                this.Send(id, new E2(this.Id));
                await this.Receive(typeof(E2));
                this.Send(id, new E2(this.Id));
                this.Send(id, new E2(this.Id));
                await this.Receive(typeof(E2));
                tcs.SetResult(true);
            }
        }

        private class M5 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E2), nameof(Handle))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                var id = (this.ReceivedEvent as E2).Id;
                var e = (E2)await this.Receive(typeof(E2));
                this.Send(e.Id, new E2(this.Id));
            }

            private async Task Handle()
            {
                var id = (this.ReceivedEvent as E2).Id;
                var e = (E2)await this.Receive(typeof(E2));
                this.Send(e.Id, new E2(this.Id));
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestReceiveEventOneMachine()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M1), new SetupEvent(tcs));

                var result = await GetResultAsync(tcs.Task);
                Assert.True(result);
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestReceiveEventWithPredicateOneMachine()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M2), new SetupEvent(tcs));

                var result = await GetResultAsync(tcs.Task);
                Assert.True(result);
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestReceiveEventMultipleTypesOneMachine()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M3), new SetupEvent(tcs));

                var result = await GetResultAsync(tcs.Task);
                Assert.True(result);
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestReceiveEventTwoMachines()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(M4), new SetupEvent(tcs));

                var result = await GetResultAsync(tcs.Task);
                Assert.True(result);
            });
        }
    }
}
