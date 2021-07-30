// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests
{
    public class FinalizerTests : BaseActorBugFindingTest
    {
        public FinalizerTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class GCTracker
        {
            internal bool IsFinalized;
        }

        private class SetupEvent : Event
        {
            internal readonly GCTracker Tracker;

            internal SetupEvent(GCTracker tracker)
            {
                this.Tracker = tracker;
            }
        }

        public class A : Actor
        {
            private GCTracker Tracker;

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.Tracker = (initialEvent as SetupEvent).Tracker;
                return Task.CompletedTask;
            }

            ~A()
            {
                this.Tracker.IsFinalized = true;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestActorFinalizerInvoked()
        {
            var tracker = new GCTracker();
            this.Test(r =>
            {
                var setup = new SetupEvent(tracker);
                r.CreateActor(typeof(A), setup);
            },
            configuration: this.GetConfiguration().WithTestingIterations(2));

            // Force a full GC.
            GC.Collect(2);
            GC.WaitForFullGCComplete();
            GC.WaitForPendingFinalizers();
            Assert.True(tracker.IsFinalized, "Finalizer was not called.");
        }

        public class M : StateMachine
        {
            private GCTracker Tracker;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            public class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.Tracker = (e as SetupEvent).Tracker;
            }

            ~M()
            {
                this.Tracker.IsFinalized = true;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestStateMachineFinalizerInvoked()
        {
            var tracker = new GCTracker();
            this.Test(r =>
            {
                var setup = new SetupEvent(tracker);
                r.CreateActor(typeof(M), setup);
            },
            configuration: this.GetConfiguration().WithTestingIterations(2));

            // Force a full GC.
            GC.Collect(2);
            GC.WaitForFullGCComplete();
            GC.WaitForPendingFinalizers();
            Assert.True(tracker.IsFinalized, "Finalizer was not called.");
        }
    }
}
