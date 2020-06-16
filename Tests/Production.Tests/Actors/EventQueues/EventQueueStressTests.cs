// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Production.Tests.Actors
{
    /// <summary>
    /// Stresses the thread safety of the event queue on an Actor.
    /// This is a Production-only test.
    /// </summary>
    public class EventQueueStressTests : BaseProductionTest
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
        public async SystemTasks.Task TestEnqueueDequeueEvents()
        {
            var logger = new TestOutputLogger(this.TestOutput, false);
            var machineStateManager = new MockActorManager(logger,
                (notification, evt, _) => { });

            var queue = new EventQueue(machineStateManager);
            int numMessages = 10000;

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
                    var (deqeueStatus, e, opGroupId, info) = queue.Dequeue();
                    if (deqeueStatus is DequeueStatus.Success)
                    {
                        Assert.IsType<E1>(e);
                    }
                }
            });

            await Task.WhenAny(Task.WhenAll(enqueueTask, dequeueTask), Task.Delay(3000));
            Assert.True(enqueueTask.IsCompleted);
            Assert.True(dequeueTask.IsCompleted);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestEnqueueReceiveEvents()
        {
            var logger = new TestOutputLogger(this.TestOutput, false);
            var machineStateManager = new MockActorManager(logger,
                (notification, evt, _) => { });

            var queue = new EventQueue(machineStateManager);
            int numMessages = 10000;

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

            await Task.WhenAny(Task.WhenAll(enqueueTask, receiveTask), Task.Delay(3000));
            Assert.True(enqueueTask.IsCompleted);
            Assert.True(receiveTask.IsCompleted);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestEnqueueReceiveEventsAlternateType()
        {
            var logger = new TestOutputLogger(this.TestOutput, false);
            var machineStateManager = new MockActorManager(logger,
                (notification, evt, _) => { });

            var queue = new EventQueue(machineStateManager);
            int numMessages = 10000;

            var enqueueTask = Task.Run(() =>
            {
                for (int i = 0; i < numMessages; i++)
                {
                    if (i % 2 == 0)
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
                    if (i % 2 == 0)
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

            await Task.WhenAny(Task.WhenAll(enqueueTask, receiveTask), Task.Delay(3000));
            Assert.True(enqueueTask.IsCompleted);
            Assert.True(receiveTask.IsCompleted);
        }
    }
}
