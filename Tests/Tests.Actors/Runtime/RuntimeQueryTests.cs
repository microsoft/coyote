// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.Tests
{
    public class RuntimeQueryTests : BaseActorTest
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
                this.Assert(count is 1, "Found {0} actors instead of 1.", count);
                var status = this.Id.Runtime.GetActorExecutionStatus(this.Id);
                this.Assert(status is ActorExecutionStatus.Active, "Actor status is {0} instead of Active.", status);
                return base.OnInitializeAsync(initialEvent);
            }

            private void ProcessEvent()
            {
                this.RaiseHaltEvent();
                int count = this.Id.Runtime.GetCurrentActorCount();
                this.Assert(count is 1, "Found {0} actors instead of 1.", count);
                var status = this.Id.Runtime.GetActorExecutionStatus(this.Id);
                this.Assert(status is ActorExecutionStatus.Halting, "Actor status is {0} instead of Halting.", status);
            }

            protected override Task OnHaltAsync(Event e)
            {
                int count = this.Id.Runtime.GetCurrentActorCount();
                this.Assert(count is 1, "Found {0} actors instead of 1.", count);
                var status = this.Id.Runtime.GetActorExecutionStatus(this.Id);
                this.Assert(status is ActorExecutionStatus.Halted, "Actor status is {0} instead of Halted.", status);
                return Task.CompletedTask;
            }
        }

        private class B : Actor
        {
        }

        private class C : Actor
        {
        }

        private class D : Actor
        {
        }

        [Fact(Timeout = 5000)]
        public async Task TestRuntimeQueries()
        {
            await this.RunAsync(async r =>
            {
                var called = false;
                var tcs = new TaskCompletionSource<bool>();

                var actorId = r.CreateActor(typeof(A));
                OnActorHaltedHandler onHaltedHandler = (id) =>
                {
                    called = true;
                    int count = r.GetCurrentActorCount();
                    Assert.True(count is 0, $"Found {count} actors instead of 0.");
                    var status = r.GetActorExecutionStatus(id);
                    Assert.True(status is ActorExecutionStatus.None, $"Actor status is {status} instead of None.");
                    Assert.Equal(actorId, id);
                    tcs.SetResult(true);
                };

                r.OnActorHalted += onHaltedHandler;
                r.SendEvent(actorId, new E());

                await this.WaitAsync(tcs.Task);
                Assert.True(called);

                var actorId1 = r.CreateActor(typeof(C));
                var actorId2 = r.CreateActor(typeof(B));
                var actorId3 = r.CreateActor(typeof(D));

                int count = r.GetCurrentActorCount();
                Assert.True(count is 3, $"Found {count} actors instead of 3.");
                var types = r.GetCurrentActorTypes().ToList();
                Assert.True(types.Count is 3, $"Found {types.Count} actor types instead of 3.");
                Assert.False(types.Contains(typeof(A)), $"Actor types contain {typeof(A)}.");
                Assert.True(types.Contains(typeof(B)), $"Actor types does not contain {typeof(B)}.");
                Assert.True(types.Contains(typeof(C)), $"Actor types does not contain {typeof(C)}.");
                Assert.True(types.Contains(typeof(D)), $"Actor types does not contain {typeof(D)}.");
                var ids = r.GetCurrentActorIds().ToList();
                Assert.True(ids.Count is 3, $"Found {ids.Count} actor ids instead of 3.");
                Assert.False(ids.Contains(actorId), $"Actor ids contain {actorId}.");
                Assert.True(ids.Contains(actorId1), $"Actor ids does not contain {actorId1}.");
                Assert.True(ids.Contains(actorId2), $"Actor ids does not contain {actorId2}.");
                Assert.True(ids.Contains(actorId3), $"Actor ids does not contain {actorId3}.");

                count = 0;
                called = false;
                tcs = new TaskCompletionSource<bool>();
                r.OnActorHalted -= onHaltedHandler;
                r.OnActorHalted += (id) =>
                {
                    lock (tcs)
                    {
                        count++;
                        if (count is 3)
                        {
                            called = true;
                            tcs.SetResult(true);
                        }
                    }
                };

                r.SendEvent(actorId1, HaltEvent.Instance);
                r.SendEvent(actorId2, HaltEvent.Instance);
                r.SendEvent(actorId3, HaltEvent.Instance);

                await this.WaitAsync(tcs.Task);
                Assert.True(called);

                count = r.GetCurrentActorCount();
                Assert.True(count is 0, $"Found {count} actors instead of 0.");
                types = r.GetCurrentActorTypes().ToList();
                Assert.True(types.Count is 0, $"Found {types.Count} actor types instead of 0.");
                ids = r.GetCurrentActorIds().ToList();
                Assert.True(ids.Count is 0, $"Found {ids.Count} actor ids instead of 0.");
            });
        }
    }
}
