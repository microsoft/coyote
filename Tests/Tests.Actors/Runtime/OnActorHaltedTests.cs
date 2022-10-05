// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.Tests
{
    public class OnActorHaltedTests : BaseActorTest
    {
        public OnActorHaltedTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class A : Actor
        {
        }

        [Fact(Timeout = 5000)]
        public async Task TestOnActorHalted()
        {
            await this.RunAsync(async r =>
            {
                var called = false;
                var tcs = new TaskCompletionSource<bool>();

                var actorId = r.CreateActor(typeof(A));
                r.OnActorHalted += (id) =>
                {
                    called = true;
                    Assert.Equal(actorId, id);
                    tcs.SetResult(true);
                };

                r.SendEvent(actorId, HaltEvent.Instance);

                await this.WaitAsync(tcs.Task);
                Assert.True(called);
            });
        }
    }
}
