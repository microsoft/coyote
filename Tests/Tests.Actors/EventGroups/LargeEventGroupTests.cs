// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors.Coverage;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.Tests
{
    public class LargeEventGroupTest : BaseActorTest
    {
        public LargeEventGroupTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class ActorHaltedEventGroup : AwaitableEventGroup<bool>
        {
            public ConcurrentDictionary<ActorId, bool> Running = new ConcurrentDictionary<ActorId, bool>();
            public int RunningCount;

            public int Count => this.Running.Count;

            public void AddActor(ActorId owner)
            {
                if (this.Running.TryAdd(owner, true))
                {
                    Interlocked.Increment(ref this.RunningCount);
                }
            }

            public void NotifyHalted(ActorId owner)
            {
                if (this.Running.TryUpdate(owner, false, true))
                {
                    if (Interlocked.Decrement(ref this.RunningCount) is 0)
                    {
                        this.SetResult(true);
                    }
                }
            }
        }

        private class SpawnEvent : Event
        {
            public int Count;
        }

        private class HaltTrackingActor : Actor
        {
            protected ActorHaltedEventGroup Halted;

            internal override Task InitializeAsync(Event initialEvent)
            {
                this.Halted = this.CurrentEventGroup as ActorHaltedEventGroup;
                return base.InitializeAsync(initialEvent);
            }

            protected override Task OnHaltAsync(Event e)
            {
                this.Halted.NotifyHalted(this.Id);
                return base.OnHaltAsync(e);
            }
        }

        [OnEventDoAction(typeof(SpawnEvent), nameof(HandleSpawn))]
        private class NetworkActor : HaltTrackingActor
        {
            private void HandleSpawn(Event e)
            {
                if (e is SpawnEvent s)
                {
                    int count = s.Count;
                    if (count - 1 > 0)
                    {
                        this.Logger.WriteLine("Actor {0} creating {1} child actors", this.Id.Name, count);
                        for (int i = 0; i < count; i++)
                        {
                            var a = this.CreateActor(typeof(NetworkActor));
                            this.Halted.AddActor(a);
                            this.SendEvent(a, new SpawnEvent() { Count = count - 1 });
                        }
                    }
                }

                this.SendEvent(this.Id, HaltEvent.Instance);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestQuiescentNetwork()
        {
            this.Test(async r =>
            {
                var graphBuilder = new ActorRuntimeLogGraphBuilder(false);
                r.RegisterLog(graphBuilder);

                var op = new ActorHaltedEventGroup();
                var id = r.CreateActor(typeof(NetworkActor), null, op);
                op.AddActor(id);

                // Spawn 4 children, each child spawns 3 children and those spawn 2, etc.
                // So we should get 41 (1 + 4 + (4*3) + (4*3*2)) actors in the network
                // before they are all halted.
                r.SendEvent(id, new SpawnEvent() { Count = 4 }, op);
                var result = await this.GetResultAsync(op.Task);

                string dgml = graphBuilder.Graph.ToString();
                Assert.Equal(41, op.Count);
            },
            configuration: Configuration.Create().WithPCTStrategy(true));
        }
    }
}
