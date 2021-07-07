// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.Tests
{
    public class EventGroupingTests : BaseActorTest
    {
        public EventGroupingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private const string EventGroup1 = "EventGroup1";
        private const string EventGroup2 = "EventGroup2";

        private class SetupEvent : Event
        {
            public TaskCompletionSource<string> Tcs = new TaskCompletionSource<string>();
            public string Name;
        }

        private class E : Event
        {
            public ActorId Id;

            public E(ActorId id = null)
            {
                this.Id = id;
            }
        }

        private class M1 : Actor
        {
            protected override Task OnInitializeAsync(Event e)
            {
                var tcs = (e as SetupEvent).Tcs;
                tcs.SetResult(this.CurrentEventGroup?.Name);
                return base.OnInitializeAsync(e);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestNullEventGroup()
        {
            this.Test(async r =>
            {
                var e = new SetupEvent();
                r.CreateActor(typeof(M1), e);
                var result = await this.GetResultAsync(e.Tcs);
                Assert.True(result is null);
            });
        }

        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M3 : Actor
        {
            private SetupEvent Setup;

            protected override Task OnInitializeAsync(Event e)
            {
                this.Setup = e as SetupEvent;
                this.SendEvent(this.Id, new E(), new EventGroup(name: this.Setup.Name));
                return base.OnInitializeAsync(e);
            }

            private void CheckEvent()
            {
                this.Setup.Tcs.SetResult(this.CurrentEventGroup?.Name);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventGroupSetByHand()
        {
            this.Test(async r =>
            {
                var e = new SetupEvent() { Name = EventGroup1 };
                r.CreateActor(typeof(M3), e);
                var result = await this.GetResultAsync(e.Tcs);
                Assert.Equal(EventGroup1, result);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestEventGroupChangedBySend()
        {
            this.Test(async r =>
            {
                var e = new SetupEvent() { Name = EventGroup1 };
                r.CreateActor(typeof(M3), e, new EventGroup(name: EventGroup2));
                var result = await this.GetResultAsync(e.Tcs);
                Assert.Equal(EventGroup1, result);
            });
        }

        private class M4A : Actor
        {
            protected override Task OnInitializeAsync(Event e)
            {
                this.CurrentEventGroup = null; // clear the EventGroup
                this.CreateActor(typeof(M4B), e);
                return base.OnInitializeAsync(e);
            }
        }

        private class M4B : Actor
        {
            protected override Task OnInitializeAsync(Event e)
            {
                var tcs = (e as SetupEvent).Tcs;
                tcs.SetResult(this.CurrentEventGroup?.Name);
                return base.OnInitializeAsync(e);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventGroupClearedByCreate()
        {
            this.Test(async r =>
            {
                var e = new SetupEvent();
                r.CreateActor(typeof(M4A), e, new EventGroup(name: EventGroup1));
                var result = await this.GetResultAsync(e.Tcs);
                Assert.True(result is null);
            });
        }

        private class M5A : Actor
        {
            protected override Task OnInitializeAsync(Event e)
            {
                var target = this.CreateActor(typeof(M5B), e);
                this.SendEvent(target, new E(), EventGroup.Null);
                return base.OnInitializeAsync(e);
            }
        }

        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M5B : Actor
        {
            private SetupEvent Setup;

            protected override Task OnInitializeAsync(Event e)
            {
                this.Setup = e as SetupEvent;
                return base.OnInitializeAsync(e);
            }

            private void CheckEvent()
            {
                this.Setup.Tcs.SetResult(this.CurrentEventGroup?.Name);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventGroupClearedBySend()
        {
            this.Test(async r =>
            {
                var e = new SetupEvent();
                r.CreateActor(typeof(M5A), e, new EventGroup(name: EventGroup1));
                var result = await this.GetResultAsync(e.Tcs);
                Assert.True(result is null);
            });
        }

        [OnEventDoAction(typeof(E), nameof(HandleEvent))]
        private class M6A : Actor
        {
            private SetupEvent Setup;
            private ActorId Child;

            protected override Task OnInitializeAsync(Event e)
            {
                this.Setup = e as SetupEvent;
                this.Assert(this.CurrentEventGroup?.Name == EventGroup1);
                this.Child = this.CreateActor(typeof(M6B), e);
                return base.OnInitializeAsync(e);
            }

            private void HandleEvent()
            {
                this.Assert(this.CurrentEventGroup is null, "M6A event group is not null");
                // propagate the null event group.
                this.SendEvent(this.Child, new E(this.Id));
            }
        }

        [OnEventDoAction(typeof(E), nameof(HandleEvent))]
        private class M6B : Actor
        {
            private SetupEvent Setup;

            protected override Task OnInitializeAsync(Event e)
            {
                this.Setup = e as SetupEvent;
                this.Assert(this.CurrentEventGroup?.Name == EventGroup1);
                return base.OnInitializeAsync(e);
            }

            private void HandleEvent()
            {
                this.Assert(this.CurrentEventGroup is null, "M6B event group is not null");
                this.Setup.Tcs.SetResult("ok");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestNullEventGroupPropagation()
        {
            this.Test(async r =>
            {
                var e = new SetupEvent();
                var a = r.CreateActor(typeof(M6A), e, new EventGroup(name: EventGroup1));
                r.SendEvent(a, new E(), EventGroup.Null); // clear the event group!
                var result = await this.GetResultAsync(e.Tcs);
                Assert.True(result is "ok", string.Format("result is {0}", result));
            });
        }

        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M7A : Actor
        {
            private SetupEvent Setup;

            protected override Task OnInitializeAsync(Event e)
            {
                this.Setup = e as SetupEvent;
                var target = this.CreateActor(typeof(M7B), e);
                this.SendEvent(target, new E(this.Id));
                return base.OnInitializeAsync(e);
            }

            private void CheckEvent()
            {
                this.Setup.Tcs.SetResult(this.CurrentEventGroup?.Name);
            }
        }

        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M7B : Actor
        {
            private void CheckEvent(Event e)
            {
                // change the EventGroup on the send back to the caller.
                this.SendEvent((e as E).Id, new E(), new EventGroup(name: EventGroup2));
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventGroupTwoActorsSendBack()
        {
            this.Test(async r =>
            {
                var e = new SetupEvent();
                r.CreateActor(typeof(M7A), e, new EventGroup(name: EventGroup1));
                var result = await this.GetResultAsync(e.Tcs);
                Assert.Equal(EventGroup2, result);
            });
        }

        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M8A : Actor
        {
            private SetupEvent Setup;

            protected override Task OnInitializeAsync(Event e)
            {
                this.Setup = e as SetupEvent;
                var target = this.CreateActor(typeof(M8B));
                this.SendEvent(target, new E(this.Id));
                return base.OnInitializeAsync(e);
            }

            private void CheckEvent()
            {
                this.Setup.Tcs.SetResult(this.CurrentEventGroup?.Name);
            }
        }

        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M8B : Actor
        {
            private void CheckEvent(Event e)
            {
                this.SendEvent((e as E).Id, new E(), EventGroup.Null);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventGroupTwoActorsSendBackCleared()
        {
            this.Test(async r =>
            {
                var e = new SetupEvent();
                r.CreateActor(typeof(M8A), e, new EventGroup(name: EventGroup1));

                var result = await this.GetResultAsync(e.Tcs);
                Assert.True(result is null);
            });
        }

        private class M9A : Actor
        {
            protected override Task OnInitializeAsync(Event e)
            {
                var op = this.CurrentEventGroup as EventGroupCounter;
                this.Assert(op != null, "M9A has unexpected null CurrentEventGroup");
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
                var op = this.CurrentEventGroup as EventGroupCounter;
                this.Assert(op != null, "M9B has unexpected null CurrentEventGroup");
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
                // now we can complete the outer EventGroup
                var op = this.CurrentEventGroup as EventGroupCounter;
                this.Assert(op != null, "M9C has unexpected null CurrentEventGroup");
                op.SetResult(true);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventGroupThreeActorGroup()
        {
            this.Test(async r =>
            {
                // setup an EventGroup that will be completed 3 times by 3 different actors
                var op = new EventGroupCounter(3);
                r.CreateActor(typeof(M9A), null, op);
                var result = await op;
                Assert.True(result);
            });
        }

        private class F : Event
        {
        }

        private class M10 : StateMachine
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.Assert(this.CurrentEventGroup is null, "CurrentEventGroup should be null");
                this.RaiseEvent(new E());
                return base.OnInitializeAsync(initialEvent);
            }

            [Start]
            [OnEventDoAction(typeof(E), nameof(HandleE))]
            [OnEventDoAction(typeof(F), nameof(HandleF))]
            public class Init : State
            {
            }

            private async Task HandleE()
            {
                this.Assert(this.CurrentEventGroup is null, "CurrentEventGroup should be null");
                await this.ReceiveEventAsync(typeof(F));
                var op = this.CurrentEventGroup as AwaitableEventGroup<bool>;
                this.Assert(op != null, "CurrentEventGroup should now be set!");
                op.SetResult(true);
            }

            private void HandleF()
            {
                this.Assert(false, "Receive didn't work?");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventGroupSetOnReceive()
        {
            this.Test(async r =>
            {
                var g = new AwaitableEventGroup<bool>();
                var a = r.CreateActor(typeof(M10));
                r.SendEvent(a, new F(), g);
                var result = await g;
            });
        }
    }
}
