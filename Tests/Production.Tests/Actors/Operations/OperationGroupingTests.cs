// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Production.Tests.Actors
{
    public class OperationGroupingTests : BaseProductionTest
    {
        public OperationGroupingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private const string OperationGroup1 = "OperationGroup1";
        private const string OperationGroup2 = "OperationGroup2";

        private class SetupEvent : Event
        {
            public TaskCompletionSource<string> Tcs = TaskCompletionSource.Create<string>();
            public string Name;

            public SetupEvent()
            {
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
                tcs.SetResult(this.CurrentOperation?.Name);
                return base.OnInitializeAsync(e);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestNullOperation()
        {
            this.Test(async r =>
            {
                var e = new SetupEvent();
                r.CreateActor(typeof(M1), e);
                var result = await this.GetResultAsync(e.Tcs);
                Assert.True(result == null);
            });
        }

        //----------------------------------------------------------------------------------------------------
        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M3 : Actor
        {
            private SetupEvent Setup;

            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                this.Setup = e as SetupEvent;
                this.SendEvent(this.Id, new E(), new Operation() { Name = this.Setup.Name });
                return base.OnInitializeAsync(e);
            }

            private void CheckEvent()
            {
                this.Setup.Tcs.SetResult(this.CurrentOperation?.Name);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationSetBySend()
        {
            this.Test(async r =>
            {
                var e = new SetupEvent() { Name = OperationGroup1 };
                r.CreateActor(typeof(M3), e);
                var result = await this.GetResultAsync(e.Tcs);
                Assert.Equal(OperationGroup1, result);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestOperationChangedBySend()
        {
            this.Test(async r =>
            {
                var e = new SetupEvent() { Name = OperationGroup1 };
                r.CreateActor(typeof(M3), e, new Operation { Name = OperationGroup2 });
                var result = await this.GetResultAsync(e.Tcs);
                Assert.Equal(OperationGroup1, result);
            });
        }

        //----------------------------------------------------------------------------------------------------
        private class M4A : Actor
        {
            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                this.CurrentOperation = null; // clear the operation
                this.CreateActor(typeof(M4B), e);
                return base.OnInitializeAsync(e);
            }
        }

        private class M4B : Actor
        {
            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                var tcs = (e as SetupEvent).Tcs;
                tcs.SetResult(this.CurrentOperation?.Name);
                return base.OnInitializeAsync(e);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationClearedByCreate()
        {
            this.Test(async r =>
            {
                var e = new SetupEvent();
                r.CreateActor(typeof(M4A), e, new Operation() { Name = OperationGroup1 });
                var result = await this.GetResultAsync(e.Tcs);
                Assert.True(result == null);
            });
        }

        //----------------------------------------------------------------------------------------------------
        private class M5A : Actor
        {
            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                var target = this.CreateActor(typeof(M5B), e);
                this.SendEvent(target, new E(), Operation.NullOperation);
                return base.OnInitializeAsync(e);
            }
        }

        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M5B : Actor
        {
            private SetupEvent Setup;

            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                this.Setup = e as SetupEvent;
                return base.OnInitializeAsync(e);
            }

            private void CheckEvent()
            {
                this.Setup.Tcs.SetResult(this.CurrentOperation?.Name);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationClearedBySend()
        {
            this.Test(async r =>
            {
                var e = new SetupEvent();
                r.CreateActor(typeof(M5A), e, new Operation() { Name = OperationGroup1 });
                var result = await this.GetResultAsync(e.Tcs);
                Assert.True(result == null);
            });
        }

        //----------------------------------------------------------------------------------------------------
        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M7A : Actor
        {
            private SetupEvent Setup;

            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                this.Setup = e as SetupEvent;
                var target = this.CreateActor(typeof(M7B), e);
                this.SendEvent(target, new E(this.Id));
                return base.OnInitializeAsync(e);
            }

            private void CheckEvent()
            {
                this.Setup.Tcs.SetResult(this.CurrentOperation?.Name);
            }
        }

        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M7B : Actor
        {
            private void CheckEvent(Event e)
            {
                // change the operation on the send back to the caller.
                this.SendEvent((e as E).Id, new E(), new Operation() { Name = OperationGroup2 });
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationTwoActorsSendBack()
        {
            this.Test(async r =>
            {
                var e = new SetupEvent();
                r.CreateActor(typeof(M7A), e, new Operation() { Name = OperationGroup1 });
                var result = await this.GetResultAsync(e.Tcs);
                Assert.Equal(OperationGroup2, result);
            });
        }

        //----------------------------------------------------------------------------------------------------
        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M8A : Actor
        {
            private SetupEvent Setup;

            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                this.Setup = e as SetupEvent;
                var target = this.CreateActor(typeof(M8B));
                this.SendEvent(target, new E(this.Id));
                return base.OnInitializeAsync(e);
            }

            private void CheckEvent()
            {
                this.Setup.Tcs.SetResult(this.CurrentOperation?.Name);
            }
        }

        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M8B : Actor
        {
            private void CheckEvent(Event e)
            {
                this.SendEvent((e as E).Id, new E(), Operation.NullOperation);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationTwoActorsSendBackCleared()
        {
            this.Test(async r =>
            {
                var e = new SetupEvent();
                r.CreateActor(typeof(M8A), e, new Operation() { Name = OperationGroup1 });

                var result = await this.GetResultAsync(e.Tcs);
                Assert.True(result == null);
            });
        }

        //----------------------------------------------------------------------------------------------------
        private class M9A : Actor
        {
            protected override SystemTasks.Task OnInitializeAsync(Event e)
            {
                var op = this.CurrentOperation as OperationCounter;
                this.Assert(op != null, "M9A has unexpected null CurrentOperation");
                op.SetResult(true);
                var target = this.CreateActor(typeof(M9B));
                this.SendEvent(target, new E());
                return base.OnInitializeAsync(e);
            }
        }

        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M9B : Actor
        {
            private void CheckEvent()
            {
                var op = this.CurrentOperation as OperationCounter;
                this.Assert(op != null, "M9B has unexpected null CurrentOperation");
                op.SetResult(true);
                var c = this.CreateActor(typeof(M9C));
                this.SendEvent(c, new E());
            }
        }

        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M9C : Actor
        {
            private void CheckEvent()
            {
                // now we can complete the outer operation
                var op = this.CurrentOperation as OperationCounter;
                this.Assert(op != null, "M9C has unexpected null CurrentOperation");
                op.SetResult(true);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOperationThreeActorGroup()
        {
            this.Test(async r =>
            {
                // setup an operation that will be completed 3 times by 3 different actors
                var op = new OperationCounter(3);
                r.CreateActor(typeof(M9A), null, op);
                var result = await op;
                Assert.True(result);
            });
        }
    }
}
