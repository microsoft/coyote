// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Coverage;
using Microsoft.Coyote.IO;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Production.Tests.Actors
{
    public class QuiescentOperationTests : BaseProductionTest
    {
        public QuiescentOperationTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class ActorHaltedOperation : AwaitableOperation<bool>
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
                    if (Interlocked.Decrement(ref this.RunningCount) == 0)
                    {
                        // all known actors have halted so we are done!
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
            protected ActorHaltedOperation Halted;

            internal override SystemTasks.Task InitializeAsync(Event initialEvent)
            {
                this.Halted = this.CurrentOperation as ActorHaltedOperation;
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
                    System.Diagnostics.Debug.WriteLine("Actor {0} creating {1} child actors", this.Id.Name, count);
                    for (int i = 0; i < count; i++)
                    {
                        var a = this.CreateActor(typeof(NetworkActor));
                        this.Halted.AddActor(a);
                        this.SendEvent(a, new SpawnEvent() { Count = count - 1 });
                    }
                }

                // we're done!
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

                var op = new ActorHaltedOperation();
                var id = r.CreateActor(typeof(NetworkActor), null, op);
                op.AddActor(id);

                // spawn 5 children, each child spawns 4 grand children and those spawn 3, etc.
                // so we should get 1 + 5 + (5*4) + (5*4*3*2) + (5!) + (5!) actors in this network = 326
                // actors before they are all halted.
                r.SendEvent(id, new SpawnEvent() { Count = 5 });
                var result = await this.GetResultAsync(op.Task);

                string dgml = graphBuilder.Graph.ToString();

                // add one for the initial actor created here.
                Assert.Equal(326, op.Count);
            });
        }
    }
}
