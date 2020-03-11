// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests
{
    public class ActorAssertionFailureTests : BaseTest
    {
        private class E : Event
        {
        }

        private class F : Event
        {
        }

        [OnEventDoAction(typeof(E), nameof(HandleE))]
        private class A : Actor
        {
            internal void HandleE()
            {
            }
        }

        private class B : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.SendEvent(this.Id, null);
                return base.OnInitializeAsync(initialEvent);
            }
        }

        [OnEventDoAction(typeof(E), nameof(HandleE))]
        private class C : Actor
        {
            private void HandleE()
            {
                // test null sender.
                this.SendEvent(null, new F());
            }
        }

        public ActorAssertionFailureTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestSendNullEvent()
        {
            var passed = false;
            var runtime = ActorRuntimeFactory.Create();
            var id = runtime.CreateActor(typeof(A));
            try
            {
                runtime.SendEvent(id, null);
            }
            catch (AssertionFailureException)
            {
                passed = true;
            }

            Assert.True(passed, "Sending null event didn't raise an Assert");

            passed = false;

            try
            {
                runtime.SendEvent(null, new E());
            }
            catch (AssertionFailureException)
            {
                passed = true;
            }

            Assert.True(passed, "Sending event to null actor didn't raise an Assert");
        }

        [Fact(Timeout = 5000)]
        public async Task TestSendNullEventInActor()
        {
            TaskCompletionSource<bool> completed = new TaskCompletionSource<bool>();
            var runtime = ActorRuntimeFactory.Create();
            runtime.OnFailure += (e) =>
            {
                completed.SetResult(true);
            };
            var id = runtime.CreateActor(typeof(B));
            var result = await completed.Task;
            Assert.True(result, "Sending null event didn't raise an Assert on null exception");
        }

        [Fact(Timeout = 5000)]
        public async Task TestSendNullSendorInActor()
        {
            TaskCompletionSource<bool> completed = new TaskCompletionSource<bool>();
            var runtime = ActorRuntimeFactory.Create();
            runtime.OnFailure += (e) =>
            {
                completed.SetResult(true);
            };
            var id = runtime.CreateActor(typeof(C));
            runtime.SendEvent(id, new E());
            var result = await completed.Task;
            Assert.True(result, "Sending null event didn't raise an Assert on null sender");
        }
    }
}
