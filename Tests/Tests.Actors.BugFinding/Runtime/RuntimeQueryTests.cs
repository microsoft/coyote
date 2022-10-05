// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests.Runtime
{
    public class RuntimeQueryTests : BaseActorBugFindingTest
    {
        public RuntimeQueryTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
        }

        [OnEventDoAction(typeof(E), nameof(ProcessEvent))]
        private class A : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                int count = this.Id.Runtime.GetCurrentActorCount();
                this.Assert(count is 1, "Actor count is {0} instead of 1.", count);
                var status = this.Id.Runtime.GetActorExecutionStatus(this.Id);
                this.Assert(status is ActorExecutionStatus.Active, "Actor status is {0} instead of Active.", status);
                return base.OnInitializeAsync(initialEvent);
            }

            private void ProcessEvent()
            {
                this.RaiseHaltEvent();
                int count = this.Id.Runtime.GetCurrentActorCount();
                this.Assert(count is 1, "Actor count is {0} instead of 1.", count);
                var status = this.Id.Runtime.GetActorExecutionStatus(this.Id);
                this.Assert(status is ActorExecutionStatus.Halting, "Actor status is {0} instead of Halting.", status);
            }

            protected override Task OnHaltAsync(Event e)
            {
                int count = this.Id.Runtime.GetCurrentActorCount();
                this.Assert(count is 1, "Actor count is {0} instead of 1.", count);
                var status = this.Id.Runtime.GetActorExecutionStatus(this.Id);
                this.Assert(status is ActorExecutionStatus.Halted, "Actor status is {0} instead of Halted.", status);
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestRuntimeQueries()
        {
            var called = false;
            this.Test(r =>
            {
                var actorId = r.CreateActor(typeof(A));
                r.OnActorHalted += (id) =>
                {
                    called = true;
                    int count = r.GetCurrentActorCount();
                    Assert.True(count is 0, $"Actor count is {count} instead of 0.");
                    var status = r.GetActorExecutionStatus(id);
                    Assert.True(status is ActorExecutionStatus.None, $"Actor status is {status} instead of None.");
                    Assert.Equal(actorId, id);
                };

                r.SendEvent(actorId, new E());
            });

            Assert.True(called);
        }
    }
}
