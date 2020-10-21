// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Actors.Tests
{
    public class GetEventGroupIdTests : BaseActorTest
    {
        public GetEventGroupIdTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private const string EventGroupId = "EventGroupId";

        private class SetupEvent : Event
        {
            public TaskCompletionSource<string> Tcs = TaskCompletionSource.Create<string>();

            public SetupEvent()
            {
            }
        }

        private class E : Event
        {
            public ActorId Id;

            public E(ActorId id)
            {
                this.Id = id;
            }
        }

        private class M1 : Actor
        {
            protected override SystemTasks.Task OnInitializeAsync(Event initialEvent)
            {
                var tcs = (initialEvent as SetupEvent).Tcs;
                tcs.SetResult(this.CurrentEventGroup?.Name);
                return base.OnInitializeAsync(initialEvent);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestGetEventGroupIdNotSet()
        {
            this.Test(async r =>
            {
                var e = new SetupEvent();
                r.CreateActor(typeof(M1), e);
                var result = await this.GetResultAsync(e.Tcs);
                Assert.True(result == null);
            });
        }

        [OnEventDoAction(typeof(E), nameof(CheckEvent))]
        private class M2 : Actor
        {
            private TaskCompletionSource<string> Tcs;

            protected override SystemTasks.Task OnInitializeAsync(Event initialEvent)
            {
                this.Tcs = (initialEvent as SetupEvent).Tcs;
                this.Context.SendEvent(this.Id, new E(this.Id), new EventGroup(name: EventGroupId));
                return base.OnInitializeAsync(initialEvent);
            }

            private void CheckEvent()
            {
                this.Tcs.SetResult(this.CurrentEventGroup == null ? null : this.CurrentEventGroup.Name);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestGetEventGroupIdSet()
        {
            this.Test(async r =>
            {
                var e = new SetupEvent();
                r.CreateActor(typeof(M2), e);
                var result = await this.GetResultAsync(e.Tcs);
                Assert.Equal(EventGroupId, result);
            });
        }
    }
}
