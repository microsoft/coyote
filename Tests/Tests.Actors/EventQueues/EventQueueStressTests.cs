// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.Tests
{
    /// <summary>
    /// Stresses the thread safety of the event queue on an Actor.
    /// This is a Production-only test.
    /// </summary>
    public class EventQueueStressTests : BaseActorTest
    {
        public EventQueueStressTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        [Fact(Timeout = 5000)]
        public async Task TestEnqueueDequeueEvents()
        {
            int numMessages = 10000;
            var logger = new TestOutputLogger(this.TestOutput);

            using var queue = new TestEventQueue(logger, (notification, evt, _) => { });

            var enqueueTask = Task.Run(() =>
            {
                for (int i = 0; i < numMessages; i++)
                {
                    queue.Enqueue(new E1(), null, null);
                }
            });

            var dequeueTask = Task.Run(() =>
            {
                for (int i = 0; i < numMessages; i++)
                {
                    var (deqeueStatus, e, g, info) = queue.Dequeue();
                    if (deqeueStatus is DequeueStatus.Success)
                    {
                        Assert.IsType<E1>(e);
                    }
                }
            });

            await await Task.WhenAny(Task.WhenAll(enqueueTask, dequeueTask), Task.Delay(3000));
            Assert.True(enqueueTask.IsCompleted);
            Assert.True(dequeueTask.IsCompleted);
        }

        [Fact(Timeout = 5000)]
        public async Task TestEnqueueReceiveEvents()
        {
            int numMessages = 10000;
            var logger = new TestOutputLogger(this.TestOutput);

            using var queue = new TestEventQueue(logger, (notification, evt, _) => { });

            var enqueueTask = Task.Run(() =>
            {
                for (int i = 0; i < numMessages; i++)
                {
                    queue.Enqueue(new E1(), null, null);
                }
            });

            var receiveTask = Task.Run(async () =>
            {
                for (int i = 0; i < numMessages; i++)
                {
                    await queue.ReceiveEventAsync(typeof(E1));
                }
            });

            await await Task.WhenAny(Task.WhenAll(enqueueTask, receiveTask), Task.Delay(3000));
            Assert.True(enqueueTask.IsCompleted);
            Assert.True(receiveTask.IsCompleted);
        }

        [Fact(Timeout = 5000)]
        public async Task TestEnqueueReceiveEventsAlternateType()
        {
            int numMessages = 10000;
            var logger = new TestOutputLogger(this.TestOutput);

            using var queue = new TestEventQueue(logger, (notification, evt, _) => { });

            var enqueueTask = Task.Run(() =>
            {
                for (int i = 0; i < numMessages; i++)
                {
                    if (i % 2 is 0)
                    {
                        queue.Enqueue(new E1(), null, null);
                    }
                    else
                    {
                        queue.Enqueue(new E2(), null, null);
                    }
                }
            });

            var receiveTask = Task.Run(async () =>
            {
                for (int i = 0; i < numMessages; i++)
                {
                    if (i % 2 is 0)
                    {
                        var e = await queue.ReceiveEventAsync(typeof(E1));
                        Assert.IsType<E1>(e);
                    }
                    else
                    {
                        var e = await queue.ReceiveEventAsync(typeof(E2));
                        Assert.IsType<E2>(e);
                    }
                }
            });

            await await Task.WhenAny(Task.WhenAll(enqueueTask, receiveTask), Task.Delay(3000));
            Assert.True(enqueueTask.IsCompleted);
            Assert.True(receiveTask.IsCompleted);
        }
    }
}
