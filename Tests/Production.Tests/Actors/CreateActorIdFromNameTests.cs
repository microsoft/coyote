// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Production.Tests.Actors
{
    public class CreateActorIdFromNameTests : BaseProductionTest
    {
        public CreateActorIdFromNameTests(ITestOutputHelper output)
            : base(output)
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
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                if (e is Conf)
                {
                    (e as Conf).Tcs.SetResult(true);
                }
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestCreateWithId1()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = TaskCompletionSource.Create<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(false);
                };

                var m1 = r.CreateActor(typeof(M));
                var m2 = r.CreateActorIdFromName(typeof(M), "M");
                r.Assert(!m1.Equals(m2));
                r.CreateActor(m2, typeof(M), new Conf(tcs));

                await this.WaitAsync(tcs.Task);
                Assert.False(failed);
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestCreateWithId2()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = TaskCompletionSource.Create<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(false);
                };

                var m1 = r.CreateActorIdFromName(typeof(M), "M1");
                var m2 = r.CreateActorIdFromName(typeof(M), "M2");
                r.Assert(!m1.Equals(m2));
                r.CreateActor(m1, typeof(M));
                r.CreateActor(m2, typeof(M), new Conf(tcs));

                await this.WaitAsync(tcs.Task);
                Assert.False(failed);
            });
        }

        private class M2 : StateMachine
        {
            [Start]
            private class S : State
            {
            }
        }

        private class M3 : StateMachine
        {
            [Start]
            private class S : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestCreateWithId4()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = TaskCompletionSource.Create<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(false);
                };

                try
                {
                    var m3 = r.CreateActorIdFromName(typeof(M3), "M3");
                    r.CreateActor(m3, typeof(M2));
                }
                catch (Exception)
                {
                    failed = true;
                    tcs.SetResult(false);
                }

                await this.WaitAsync(tcs.Task);
                Assert.True(failed);
            });
        }

        [Fact(Timeout = 5000)]
        public async Task TestCreateWithId5()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = TaskCompletionSource.Create<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(false);
                };

                try
                {
                    var m1 = r.CreateActorIdFromName(typeof(M2), "M2");
                    r.CreateActor(m1, typeof(M2));
                    r.CreateActor(m1, typeof(M2));
                }
                catch (Exception)
                {
                    failed = true;
                    tcs.SetResult(false);
                }

                await this.WaitAsync(tcs.Task);
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
            private class S : State
            {
            }

            private void Process(Event e)
            {
                (e as Conf).Tcs.SetResult(true);
            }
        }

        [Fact(Timeout = 5000)]
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
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                var m = this.Runtime.CreateActorIdFromName(typeof(M4), "M4");
                this.CreateActor(m, typeof(M4), "friendly");
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestCreateWithId10()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = TaskCompletionSource.Create<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(false);
                };

                r.CreateActor(typeof(M6));
                r.CreateActor(typeof(M6));

                await this.WaitAsync(tcs.Task);
                Assert.True(failed);
            });
        }

        private class M7 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry(Event e)
            {
                await this.Runtime.CreateActorAndExecuteAsync(typeof(M6));
                var m = this.Runtime.CreateActorIdFromName(typeof(M4), "M4");
                this.Runtime.SendEvent(m, e);
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestCreateWithId11()
        {
            await this.RunAsync(async r =>
            {
                var failed = false;
                var tcs = TaskCompletionSource.Create<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(false);
                };

                r.CreateActor(typeof(M7), new Conf(tcs));

                await this.WaitAsync(tcs.Task);
                Assert.False(failed);
            });
        }
    }
}
