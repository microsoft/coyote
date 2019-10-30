// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests
{
    public class OnEventDroppedTest : BaseTest
    {
        public OnEventDroppedTest(ITestOutputHelper output)
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
            private class Init : MachineState
            {
            }

            protected override void OnHalt()
            {
                this.Send(this.Id, new E());
            }
        }

        [Fact(Timeout=5000)]
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

                var m = r.CreateMachine(typeof(M1));
                r.SendEvent(m, new Halt());

                await WaitAsync(tcs.Task);
                Assert.True(called);
            });
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new Halt());
                this.Send(this.Id, new E());
            }
        }

        [Fact(Timeout=5000)]
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

                var m = r.CreateMachine(typeof(M2));

                await WaitAsync(tcs.Task);
                Assert.True(called);
            });
        }

        [Fact(Timeout=5000)]
        public async Task TestOnDroppedParams()
        {
            await this.RunAsync(async r =>
            {
                var called = false;
                var tcs = new TaskCompletionSource<bool>();

                var m = r.CreateMachine(typeof(M1));

                r.OnEventDropped += (e, target) =>
                {
                    Assert.True(e is E);
                    Assert.True(target == m);
                    called = true;
                    tcs.SetResult(true);
                };

                r.SendEvent(m, new Halt());

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
            private class S0 : MonitorState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as E).Tcs;
                this.Goto<S1>();
            }

            [OnEventGotoState(typeof(EventProcessed), typeof(S2))]
            [OnEventGotoState(typeof(EventDropped), typeof(S2))]
            private class S1 : MonitorState
            {
            }

            [OnEntry(nameof(Done))]
            private class S2 : MonitorState
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
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send((this.ReceivedEvent as E).Id, new Halt());
            }
        }

        private class M3b : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send((this.ReceivedEvent as E).Id, new E());
            }
        }

        private class M3c : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Processed))]
            private class Init : MachineState
            {
            }

            private void Processed()
            {
                this.Monitor<Monitor3>(new EventProcessed());
            }
        }

        [Fact(Timeout=5000)]
        public async Task TestProcessedOrDropped()
        {
            var config = GetConfiguration();
            config.EnableMonitorsInProduction = true;
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

                var m = r.CreateMachine(typeof(M3c));
                r.CreateMachine(typeof(M3a), new E(m));
                r.CreateMachine(typeof(M3b), new E(m));

                await WaitAsync(tcs.Task);
            }, config);
        }
    }
}
