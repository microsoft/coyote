// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Production.Tests.Actors.StateMachines
{
    public class OnEventDroppedTests : BaseProductionTest
    {
        public OnEventDroppedTests(ITestOutputHelper output)
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
            private class Init : State
            {
            }

            protected override Task OnHaltAsync(Event e)
            {
                this.SendEvent(this.Id, new E());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestOnDroppedCalled1()
        {
            await this.RunAsync(async r =>
            {
                var called = false;
                var tcs = new TaskCompletionSource<bool>();

                r.OnEventDropped += (e, target) =>
                {
                    called = true;
                    tcs.SetResult(true);
                };

                var m = r.CreateActor(typeof(M1));
                r.SendEvent(m, HaltEvent.Instance);

                await WaitAsync(tcs.Task);
                Assert.True(called);
            });
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, HaltEvent.Instance);
                this.SendEvent(this.Id, new E());
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestOnDroppedCalled2()
        {
            await this.RunAsync(async r =>
            {
                var called = false;
                var tcs = new TaskCompletionSource<bool>();

                r.OnEventDropped += (e, target) =>
                {
                    called = true;
                    tcs.SetResult(true);
                };

                var m = r.CreateActor(typeof(M2));

                await WaitAsync(tcs.Task);
                Assert.True(called);
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestOnDroppedParams()
        {
            await this.RunAsync(async r =>
            {
                var called = false;
                var tcs = new TaskCompletionSource<bool>();

                var m = r.CreateActor(typeof(M1));

                r.OnEventDropped += (e, target) =>
                {
                    Assert.True(e is E);
                    Assert.True(target == m);
                    called = true;
                    tcs.SetResult(true);
                };

                r.SendEvent(m, HaltEvent.Instance);

                await WaitAsync(tcs.Task);
                Assert.True(called);
            });
        }

        private class EventProcessed : Event
        {
        }

        private class EventDropped : Event
        {
        }

        private class Monitor3 : Monitor
        {
            private TaskCompletionSource<bool> Tcs;

            [Start]
            [OnEventDoAction(typeof(E), nameof(InitOnEntry))]
            private class S0 : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.Tcs = (e as E).Tcs;
                this.RaiseGotoStateEvent<S1>();
            }

            [OnEventGotoState(typeof(EventProcessed), typeof(S2))]
            [OnEventGotoState(typeof(EventDropped), typeof(S2))]
            private class S1 : State
            {
            }

            [OnEntry(nameof(Done))]
            private class S2 : State
            {
            }

            private void Done()
            {
                this.Tcs.SetResult(true);
            }
        }

        private class M3a : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.SendEvent((e as E).Id, HaltEvent.Instance);
            }
        }

        private class M3b : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.SendEvent((e as E).Id, new E());
            }
        }

        private class M3c : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Processed))]
            private class Init : State
            {
            }

            private void Processed()
            {
                this.Monitor<Monitor3>(new EventProcessed());
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestProcessedOrDropped()
        {
            var config = GetConfiguration();
            config.IsMonitoringEnabledInInProduction = true;
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();

                r.RegisterMonitor(typeof(Monitor3));
                r.InvokeMonitor(typeof(Monitor3), new E(tcs));

                r.OnFailure += (ex) =>
                {
                    Assert.True(false);
                    tcs.SetResult(false);
                };

                r.OnEventDropped += (e, target) =>
                {
                    r.InvokeMonitor(typeof(Monitor3), new EventDropped());
                };

                var m = r.CreateActor(typeof(M3c));
                r.CreateActor(typeof(M3a), new E(m));
                r.CreateActor(typeof(M3b), new E(m));

                await WaitAsync(tcs.Task);
            }, config);
        }
    }
}
