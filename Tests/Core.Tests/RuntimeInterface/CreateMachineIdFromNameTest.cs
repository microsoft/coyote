// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests
{
    public class CreateActorIdFromNameTest : BaseTest
    {
        public CreateActorIdFromNameTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
        }

        private class Conf : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public Conf(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        private class M : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                if (this.ReceivedEvent is Conf)
                {
                    (this.ReceivedEvent as Conf).Tcs.SetResult(true);
                }
            }
        }

        [Fact(Timeout=5000)]
        public async Task TestCreateWithId1()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(false);
                };

                var m1 = r.CreateMachine(typeof(M));
                var m2 = r.CreateActorIdFromName(typeof(M), "M");
                r.Assert(!m1.Equals(m2));
                r.CreateMachine(m2, typeof(M), new Conf(tcs));

                await WaitAsync(tcs.Task);
                Assert.False(failed);
            });
        }

        [Fact(Timeout=5000)]
        public async Task TestCreateWithId2()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(false);
                };

                var m1 = r.CreateActorIdFromName(typeof(M), "M1");
                var m2 = r.CreateActorIdFromName(typeof(M), "M2");
                r.Assert(!m1.Equals(m2));
                r.CreateMachine(m1, typeof(M));
                r.CreateMachine(m2, typeof(M), new Conf(tcs));

                await WaitAsync(tcs.Task);
                Assert.False(failed);
            });
        }

        private class M2 : StateMachine
        {
            [Start]
            private class S : MachineState
            {
            }
        }

        private class M3 : StateMachine
        {
            [Start]
            private class S : MachineState
            {
            }
        }

        [Fact(Timeout=5000)]
        public async Task TestCreateWithId4()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(false);
                };

                try
                {
                    var m3 = r.CreateActorIdFromName(typeof(M3), "M3");
                    r.CreateMachine(m3, typeof(M2));
                }
                catch (Exception)
                {
                    failed = true;
                    tcs.SetResult(false);
                }

                await WaitAsync(tcs.Task);
                Assert.True(failed);
            });
        }

        [Fact(Timeout=5000)]
        public async Task TestCreateWithId5()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(false);
                };

                try
                {
                    var m1 = r.CreateActorIdFromName(typeof(M2), "M2");
                    r.CreateMachine(m1, typeof(M2));
                    r.CreateMachine(m1, typeof(M2));
                }
                catch (Exception)
                {
                    failed = true;
                    tcs.SetResult(false);
                }

                await WaitAsync(tcs.Task);
                Assert.True(failed);
            });
        }

        private class E2 : Event
        {
            public ActorId Mid;

            public E2(ActorId id)
            {
                this.Mid = id;
            }
        }

        private class M4 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(Conf), nameof(Process))]
            private class S : MachineState
            {
            }

            private void Process()
            {
                (this.ReceivedEvent as Conf).Tcs.SetResult(true);
            }
        }

        [Fact(Timeout=5000)]
        public void TestCreateWithId9()
        {
            this.Run(r =>
            {
                var m1 = r.CreateActorIdFromName(typeof(M4), "M4");
                var m2 = r.CreateActorIdFromName(typeof(M4), "M4");
                Assert.True(m1.Equals(m2));
            });
        }

        private class M6 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var m = this.Runtime.CreateActorIdFromName(typeof(M4), "M4");
                this.CreateMachine(m, typeof(M4), "friendly");
            }
        }

        [Fact(Timeout=5000)]
        public async Task TestCreateWithId10()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(false);
                };

                r.CreateMachine(typeof(M6));
                r.CreateMachine(typeof(M6));

                await WaitAsync(tcs.Task);
                Assert.True(failed);
            });
        }

        private class M7 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                await this.Runtime.CreateMachineAndExecuteAsync(typeof(M6));
                var m = this.Runtime.CreateActorIdFromName(typeof(M4), "M4");
                this.Runtime.SendEvent(m, this.ReceivedEvent);
            }
        }

        [Fact(Timeout=5000)]
        public async Task TestCreateWithId11()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(false);
                };

                r.CreateMachine(typeof(M7), new Conf(tcs));

                await WaitAsync(tcs.Task);
                Assert.False(failed);
            });
        }
    }
}
