// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Production.Tests.Actors.Operations
{
    public class OperationGroupingTests : BaseProductionTest
    {
        public OperationGroupingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private static Guid OperationGroup1 = Guid.NewGuid();
        private static Guid OperationGroup2 = Guid.NewGuid();

        private class SetupEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public SetupEvent(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        private class SetupMultipleEvent : Event
        {
            public TaskCompletionSource<bool>[] Tcss;

            public SetupMultipleEvent(params TaskCompletionSource<bool>[] tcss)
            {
                this.Tcss = tcss;
            }
        }

        private class E : Event
        {
            public ActorId Id;

            public E()
            {
            }

            public E(ActorId id)
            {
                this.Id = id;
            }
        }

        private class M1 : Actor
        {
            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                var tcs = (e as SetupEvent).Tcs;
                tcs.SetResult(this.OperationGroupId == Guid.Empty);
                return base.OnInitializeAsync(e);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationGroupingSingleActorNoSend()
        {
            this.Test(async r =>
            {
                var tcs = TaskCompletionSource.Create<bool>();
                r.CreateActor(typeof(M1), new SetupEvent(tcs));

                var result = await this.GetResultAsync(tcs);
                Assert.True(result);
            });
        }

        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M2 : Actor
        {
            private TaskCompletionSource<bool> Tcs;

            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                this.Tcs = (e as SetupEvent).Tcs;
                this.SendEvent(this.Id, new E());
                return base.OnInitializeAsync(e);
            }

            private void CheckEvent()
            {
                this.Tcs.SetResult(this.OperationGroupId == Guid.Empty);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationGroupingSingleActorSend()
        {
            this.Test(async r =>
            {
                var tcs = TaskCompletionSource.Create<bool>();
                r.CreateActor(typeof(M2), new SetupEvent(tcs));

                var result = await this.GetResultAsync(tcs);
                Assert.True(result);
            });
        }

        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M3 : Actor
        {
            private TaskCompletionSource<bool> Tcs;

            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                this.Tcs = (e as SetupEvent).Tcs;
                this.Runtime.SendEvent(this.Id, new E(), OperationGroup1);
                return base.OnInitializeAsync(e);
            }

            private void CheckEvent()
            {
                this.Tcs.SetResult(this.OperationGroupId == OperationGroup1);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationGroupingSingleActorSendStarter()
        {
            this.Test(async r =>
            {
                var tcs = TaskCompletionSource.Create<bool>();
                r.CreateActor(typeof(M3), new SetupEvent(tcs));

                var result = await this.GetResultAsync(tcs);
                Assert.True(result);
            });
        }

        private class M4A : Actor
        {
            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                var tcs = (e as SetupEvent).Tcs;
                this.CreateActor(typeof(M4B), new SetupEvent(tcs));
                return base.OnInitializeAsync(e);
            }
        }

        private class M4B : Actor
        {
            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                var tcs = (e as SetupEvent).Tcs;
                tcs.SetResult(this.OperationGroupId == Guid.Empty);
                return base.OnInitializeAsync(e);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationGroupingTwoActorsCreate()
        {
            this.Test(async r =>
            {
                var tcs = TaskCompletionSource.Create<bool>();
                r.CreateActor(typeof(M4A), new SetupEvent(tcs));

                var result = await this.GetResultAsync(tcs);
                Assert.True(result);
            });
        }

        private class M5A : Actor
        {
            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                var tcs = (e as SetupEvent).Tcs;
                var target = this.CreateActor(typeof(M5B), new SetupEvent(tcs));
                this.SendEvent(target, new E());
                return base.OnInitializeAsync(e);
            }
        }

        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M5B : Actor
        {
            private TaskCompletionSource<bool> Tcs;

            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                this.Tcs = (e as SetupEvent).Tcs;
                return base.OnInitializeAsync(e);
            }

            private void CheckEvent()
            {
                this.Tcs.SetResult(this.OperationGroupId == Guid.Empty);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationGroupingTwoActorsSend()
        {
            this.Test(async r =>
            {
                var tcs = TaskCompletionSource.Create<bool>();
                r.CreateActor(typeof(M5A), new SetupEvent(tcs));

                var result = await this.GetResultAsync(tcs);
                Assert.True(result);
            });
        }

        private class M6A : Actor
        {
            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                var tcs = (e as SetupEvent).Tcs;
                var target = this.CreateActor(typeof(M6B), new SetupEvent(tcs));
                this.Runtime.SendEvent(target, new E(), OperationGroup1);
                return base.OnInitializeAsync(e);
            }
        }

        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M6B : Actor
        {
            private TaskCompletionSource<bool> Tcs;

            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                this.Tcs = (e as SetupEvent).Tcs;
                return base.OnInitializeAsync(e);
            }

            private void CheckEvent()
            {
                this.Tcs.SetResult(this.OperationGroupId == OperationGroup1);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationGroupingTwoActorsSendStarter()
        {
            this.Test(async r =>
            {
                var tcs = TaskCompletionSource.Create<bool>();
                r.CreateActor(typeof(M6A), new SetupEvent(tcs));

                var result = await this.GetResultAsync(tcs);
                Assert.True(result);
            });
        }

        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M7A : Actor
        {
            private TaskCompletionSource<bool> Tcs;

            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                var tcss = (e as SetupMultipleEvent).Tcss;
                this.Tcs = tcss[0];
                var target = this.CreateActor(typeof(M7B), new SetupEvent(tcss[1]));
                // bugbug: why isn't this "this.SendEvent" ?
                this.Runtime.SendEvent(target, new E(this.Id), OperationGroup1);
                return base.OnInitializeAsync(e);
            }

            private void CheckEvent()
            {
                this.Tcs.SetResult(this.OperationGroupId == OperationGroup1);
            }
        }

        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M7B : Actor
        {
            private TaskCompletionSource<bool> Tcs;

            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                this.Tcs = (e as SetupEvent).Tcs;
                return base.OnInitializeAsync(e);
            }

            private void CheckEvent(Event e)
            {
                this.Tcs.SetResult(this.OperationGroupId == OperationGroup1);
                this.SendEvent((e as E).Id, new E());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationGroupingTwoActorsSendBack()
        {
            this.Test(async r =>
            {
                var tcs1 = TaskCompletionSource.Create<bool>();
                var tcs2 = TaskCompletionSource.Create<bool>();
                r.CreateActor(typeof(M7A), new SetupMultipleEvent(tcs1, tcs2));

                var result = await this.GetResultAsync(tcs1, 500000);
                Assert.True(result);

                result = await this.GetResultAsync(tcs2, 500000);
                Assert.True(result);
            });
        }

        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M8A : Actor
        {
            private TaskCompletionSource<bool> Tcs;

            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                var tcss = (e as SetupMultipleEvent).Tcss;
                this.Tcs = tcss[0];
                var target = this.CreateActor(typeof(M8B), new SetupEvent(tcss[1]));
                this.Runtime.SendEvent(target, new E(this.Id), OperationGroup1);
                return base.OnInitializeAsync(e);
            }

            private void CheckEvent()
            {
                this.Tcs.SetResult(this.OperationGroupId == OperationGroup2);
            }
        }

        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M8B : Actor
        {
            private TaskCompletionSource<bool> Tcs;

            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                this.Tcs = (e as SetupEvent).Tcs;
                return base.OnInitializeAsync(e);
            }

            private void CheckEvent(Event e)
            {
                this.Tcs.SetResult(this.OperationGroupId == OperationGroup1);
                this.Runtime.SendEvent((e as E).Id, new E(), OperationGroup2);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationGroupingTwoActorsSendBackStarter()
        {
            this.Test(async r =>
            {
                var tcs1 = TaskCompletionSource.Create<bool>();
                var tcs2 = TaskCompletionSource.Create<bool>();
                r.CreateActor(typeof(M8A), new SetupMultipleEvent(tcs1, tcs2));

                var result = await this.GetResultAsync(tcs1);
                Assert.True(result);

                result = await this.GetResultAsync(tcs2);
                Assert.True(result);
            });
        }

        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M9A : Actor
        {
            private TaskCompletionSource<bool> Tcs;

            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                var tcss = (e as SetupMultipleEvent).Tcss;
                this.Tcs = tcss[0];
                var target = this.CreateActor(typeof(M9B), new SetupMultipleEvent(tcss[1], tcss[2]));
                this.Runtime.SendEvent(target, new E(this.Id), OperationGroup1);
                return base.OnInitializeAsync(e);
            }

            private void CheckEvent()
            {
                this.Tcs.SetResult(this.OperationGroupId == OperationGroup2);
            }
        }

        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M9B : Actor
        {
            private TaskCompletionSource<bool> Tcs;
            private TaskCompletionSource<bool> TargetTcs;

            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                var tcss = (e as SetupMultipleEvent).Tcss;
                this.Tcs = tcss[0];
                this.TargetTcs = tcss[1];
                return base.OnInitializeAsync(e);
            }

            private void CheckEvent(Event e)
            {
                this.CreateActor(typeof(M9C), new SetupEvent(this.TargetTcs));
                this.Tcs.SetResult(this.OperationGroupId == OperationGroup1);
                this.Runtime.SendEvent((e as E).Id, new E(), OperationGroup2);
            }
        }

        private class M9C : Actor
        {
            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                var tcs = (e as SetupEvent).Tcs;
                tcs.SetResult(this.OperationGroupId == OperationGroup1);
                return base.OnInitializeAsync(e);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationGroupingThreeActorsSendStarter()
        {
            this.Test(async r =>
            {
                var tcs1 = TaskCompletionSource.Create<bool>();
                var tcs2 = TaskCompletionSource.Create<bool>();
                var tcs3 = TaskCompletionSource.Create<bool>();
                r.CreateActor(typeof(M9A), new SetupMultipleEvent(tcs1, tcs2, tcs3));

                var result = await this.GetResultAsync(tcs1);
                Assert.True(result);

                result = await this.GetResultAsync(tcs2);
                Assert.True(result);

                result = await this.GetResultAsync(tcs3);
                Assert.True(result);
            });
        }
    }
}
