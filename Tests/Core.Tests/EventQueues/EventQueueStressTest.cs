// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests
{
    public class EventQueueStressTest : BaseTest
    {
        public EventQueueStressTest(ITestOutputHelper output)
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
            var logger = new TestOutputLogger(this.TestOutput, false);
            var machineStateManager = new MockMachineStateManager(logger,
                (notification, evt, _) => { });

            var queue = new EventQueue(machineStateManager);
            int numMessages = 10000;

            var enqueueTask = Task.Run(() =>
            {
                for (int i = 0; i < numMessages; i++)
                {
                    queue.Enqueue(new E1(), Guid.Empty, null);
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
        public async Task TestEnqueueReceiveEvents()
        {
            var logger = new TestOutputLogger(this.TestOutput, false);
            var machineStateManager = new MockMachineStateManager(logger,
                (notification, evt, _) => { });

            var queue = new EventQueue(machineStateManager);
            int numMessages = 10000;

            var enqueueTask = Task.Run(() =>
            {
                for (int i = 0; i < numMessages; i++)
                {
                    queue.Enqueue(new E1(), Guid.Empty, null);
                }
            });

            var receiveTask = Task.Run(async () =>
            {
                for (int i = 0; i < numMessages; i++)
                {
                    await queue.ReceiveAsync(typeof(E1));
                }
            });

            await Task.WhenAny(Task.WhenAll(enqueueTask, receiveTask), Task.Delay(3000));
            Assert.True(enqueueTask.IsCompleted);
            Assert.True(receiveTask.IsCompleted);
        }

        [Fact(Timeout = 5000)]
        public async Task TestEnqueueReceiveEventsAlternateType()
        {
            var logger = new TestOutputLogger(this.TestOutput, false);
            var machineStateManager = new MockMachineStateManager(logger,
                (notification, evt, _) => { });

            var queue = new EventQueue(machineStateManager);
            int numMessages = 10000;

            var enqueueTask = Task.Run(() =>
            {
                for (int i = 0; i < numMessages; i++)
                {
                    if (i % 2 == 0)
                    {
                        queue.Enqueue(new E1(), Guid.Empty, null);
                    }
                    else
                    {
                        queue.Enqueue(new E2(), Guid.Empty, null);
                    }
                }
            });

            var receiveTask = Task.Run(async () =>
            {
                for (int i = 0; i < numMessages; i++)
                {
                    if (i % 2 == 0)
                    {
                        var e = await queue.ReceiveAsync(typeof(E1));
                        Assert.IsType<E1>(e);
                    }
                    else
                    {
                        var e = await queue.ReceiveAsync(typeof(E2));
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
