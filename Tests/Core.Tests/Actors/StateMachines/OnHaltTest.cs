// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests.Actors.StateMachines
{
    public class OnHaltTest : BaseTest
    {
        public OnHaltTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
            public ActorId Id;
            public TaskCompletionSource<bool> Tcs;

            public E()
            {
            }

            public E(ActorId id)
            {
                this.Id = id;
            }

            public E(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(new HaltEvent());
            }

            protected override Task OnHaltAsync()
            {
                this.Assert(false);
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestHaltCalled()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(true);
                };

                r.CreateActor(typeof(M1));

                await WaitAsync(tcs.Task);
                Assert.True(failed);
            });
        }

        private class M2a : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(new HaltEvent());
            }

            protected override async Task OnHaltAsync()
            {
                await this.ReceiveEventAsync(typeof(Event));
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestReceiveOnHalt()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(true);
                };

                r.CreateActor(typeof(M2a));

                await WaitAsync(tcs.Task);
                Assert.True(failed);
            });
        }

        private class M2b : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(new HaltEvent());
            }

            protected override Task OnHaltAsync()
            {
                this.RaiseEvent(new E());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestRaiseOnHalt()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(true);
                };

                r.CreateActor(typeof(M2b));

                await WaitAsync(tcs.Task);
                Assert.True(failed);
            });
        }

        private class M2c : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(new HaltEvent());
            }

            protected override Task OnHaltAsync()
            {
                this.Goto<Init>();
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestGotoOnHalt()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(true);
                };

                r.CreateActor(typeof(M2c));

                await WaitAsync(tcs.Task);
                Assert.True(failed);
            });
        }

        private class Dummy : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(new HaltEvent());
            }
        }

        private class M3 : StateMachine
        {
            private TaskCompletionSource<bool> tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.tcs = (this.ReceivedEvent as E).Tcs;
                this.RaiseEvent(new HaltEvent());
            }

            protected override Task OnHaltAsync()
            {
                // No-ops, but no failure.
                this.SendEvent(this.Id, new E());
                this.Random();
                this.Assert(true);
                this.CreateActor(typeof(Dummy));
                this.tcs.TrySetResult(true);
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout=5000)]
        public async Task TestAPIsOnHalt()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.TrySetResult(true);
                };

                r.CreateActor(typeof(M3), new E(tcs));

                await WaitAsync(tcs.Task);
                Assert.False(failed);
            });
        }
    }
}
