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
    public class EventQueueTest : BaseTest
    {
        public EventQueueTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        private class E3 : Event
        {
        }

        private class E4 : Event
        {
            public bool Value;

            public E4(bool value)
            {
                this.Value = value;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEnqueueEvent()
        {
            var logger = new TestOutputLogger(this.TestOutput, false);
            var machineStateManager = new MockMachineStateManager(logger,
                (notification, evt, _) => { });

            var queue = new EventQueue(machineStateManager);
            Assert.Equal(0, queue.Size);

            var enqueueStatus = queue.Enqueue(new E1(), Guid.Empty, null);
            Assert.Equal(1, queue.Size);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);

            enqueueStatus = queue.Enqueue(new E2(), Guid.Empty, null);
            Assert.Equal(2, queue.Size);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);

            enqueueStatus = queue.Enqueue(new E3(), Guid.Empty, null);
            Assert.Equal(3, queue.Size);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
        }

        [Fact(Timeout = 5000)]
        public void TestDequeueEvent()
        {
            var logger = new TestOutputLogger(this.TestOutput, false);
            var machineStateManager = new MockMachineStateManager(logger,
                (notification, evt, _) => { });

            var queue = new EventQueue(machineStateManager);
            var (deqeueStatus, e, opGroupId, info) = queue.Dequeue();
            Assert.Equal(DequeueStatus.NotAvailable, deqeueStatus);
            Assert.Equal(0, queue.Size);

            queue.Enqueue(new E1(), Guid.Empty, null);
            (deqeueStatus, e, opGroupId, info) = queue.Dequeue();
            Assert.IsType<E1>(e);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(0, queue.Size);

            queue.Enqueue(new E3(), Guid.Empty, null);
            queue.Enqueue(new E2(), Guid.Empty, null);
            queue.Enqueue(new E1(), Guid.Empty, null);

            (deqeueStatus, e, opGroupId, info) = queue.Dequeue();
            Assert.IsType<E3>(e);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(2, queue.Size);

            (deqeueStatus, e, opGroupId, info) = queue.Dequeue();
            Assert.IsType<E2>(e);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(1, queue.Size);

            (deqeueStatus, e, opGroupId, info) = queue.Dequeue();
            Assert.IsType<E1>(e);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(0, queue.Size);

            (deqeueStatus, e, opGroupId, info) = queue.Dequeue();
            Assert.Equal(DequeueStatus.NotAvailable, deqeueStatus);
            Assert.Equal(0, queue.Size);
        }

        [Fact(Timeout = 5000)]
        public void TestEnqueueEventWithHandlerNotRunning()
        {
            var logger = new TestOutputLogger(this.TestOutput, false);
            var machineStateManager = new MockMachineStateManager(logger,
                (notification, evt, _) => { });

            var queue = new EventQueue(machineStateManager);
            var (deqeueStatus, e, opGroupId, info) = queue.Dequeue();
            Assert.Equal(DequeueStatus.NotAvailable, deqeueStatus);
            Assert.Equal(0, queue.Size);

            var enqueueStatus = queue.Enqueue(new E1(), Guid.Empty, null);
            Assert.Equal(EnqueueStatus.EventHandlerNotRunning, enqueueStatus);
            Assert.Equal(1, queue.Size);
        }

        [Fact(Timeout = 5000)]
        public void TestRaiseEvent()
        {
            var logger = new TestOutputLogger(this.TestOutput, false);
            var machineStateManager = new MockMachineStateManager(logger,
                (notification, evt, _) => { });

            var queue = new EventQueue(machineStateManager);
            queue.Raise(new E1(), Guid.Empty);
            Assert.True(queue.IsEventRaised);
            Assert.Equal(0, queue.Size);

            var (deqeueStatus, e, opGroupId, info) = queue.Dequeue();
            Assert.IsType<E1>(e);
            Assert.Equal(DequeueStatus.Raised, deqeueStatus);
            Assert.False(queue.IsEventRaised);
            Assert.Equal(0, queue.Size);
        }

        [Fact(Timeout = 5000)]
        public async Task TestReceiveEvent()
        {
            int notificationCount = 0;
            var tcs = new TaskCompletionSource<bool>();
            var logger = new TestOutputLogger(this.TestOutput, false);
            var machineStateManager = new MockMachineStateManager(logger,
                (notification, evt, _) =>
                {
                    notificationCount++;
                    if (notificationCount == 2)
                    {
                        Assert.Equal(MockMachineStateManager.Notification.ReceiveEvent, notification);
                        tcs.SetResult(true);
                    }
                });

            var queue = new EventQueue(machineStateManager);

            var task = Task.Run(async () =>
            {
                var receivedEvent = await queue.ReceiveAsync(typeof(E1));
                Assert.IsType<E1>(receivedEvent);
                Assert.Equal(0, queue.Size);
            });

            // Small delay to force ordering.
            await Task.Delay(300);

            var enqueueStatus = queue.Enqueue(new E1(), Guid.Empty, null);
            Assert.Equal(EnqueueStatus.Received, enqueueStatus);
            Assert.Equal(0, queue.Size);

            await task;

            var (deqeueStatus, e, opGroupId, info) = queue.Dequeue();
            Assert.Equal(DequeueStatus.NotAvailable, deqeueStatus);
            Assert.Equal(0, queue.Size);

            await Task.WhenAny(tcs.Task, Task.Delay(500));
            Assert.True(tcs.Task.IsCompleted);
        }

        [Fact(Timeout = 5000)]
        public async Task TestReceiveEventWithPredicate()
        {
            int notificationCount = 0;
            var tcs = new TaskCompletionSource<bool>();
            var logger = new TestOutputLogger(this.TestOutput, false);
            var machineStateManager = new MockMachineStateManager(logger,
                (notification, evt, _) =>
                {
                    notificationCount++;
                    if (notificationCount == 3)
                    {
                        Assert.Equal(MockMachineStateManager.Notification.ReceiveEvent, notification);
                        tcs.SetResult(true);
                    }
                });

            var queue = new EventQueue(machineStateManager);

            var task = Task.Run(async () =>
            {
                var receivedEvent = await queue.ReceiveAsync(typeof(E4), evt => (evt as E4).Value);
                Assert.IsType<E4>(receivedEvent);
                Assert.True((receivedEvent as E4).Value);
                Assert.Equal(1, queue.Size);
            });

            // Small delay to force ordering.
            await Task.Delay(300);

            var enqueueStatus = queue.Enqueue(new E4(false), Guid.Empty, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(1, queue.Size);

            enqueueStatus = queue.Enqueue(new E4(true), Guid.Empty, null);
            Assert.Equal(EnqueueStatus.Received, enqueueStatus);
            Assert.Equal(1, queue.Size);

            await task;

            var (deqeueStatus, e, opGroupId, info) = queue.Dequeue();
            Assert.IsType<E4>(e);
            Assert.False((e as E4).Value);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(0, queue.Size);

            (deqeueStatus, e, opGroupId, info) = queue.Dequeue();
            Assert.Equal(DequeueStatus.NotAvailable, deqeueStatus);
            Assert.Equal(0, queue.Size);

            await Task.WhenAny(tcs.Task, Task.Delay(500));
            Assert.True(tcs.Task.IsCompleted);
        }

        [Fact(Timeout = 5000)]
        public async Task TestReceiveEventWithoutWaiting()
        {
            int notificationCount = 0;
            var tcs = new TaskCompletionSource<bool>();
            var logger = new TestOutputLogger(this.TestOutput, false);
            var machineStateManager = new MockMachineStateManager(logger,
                (notification, evt, _) =>
                {
                    notificationCount++;
                    if (notificationCount == 3)
                    {
                        Assert.Equal(MockMachineStateManager.Notification.ReceiveEventWithoutWaiting, notification);
                        tcs.SetResult(true);
                    }
                });

            var queue = new EventQueue(machineStateManager);
            var enqueueStatus = queue.Enqueue(new E4(false), Guid.Empty, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(1, queue.Size);

            enqueueStatus = queue.Enqueue(new E4(true), Guid.Empty, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(2, queue.Size);

            var receivedEvent = await queue.ReceiveAsync(typeof(E4), evt => (evt as E4).Value);
            Assert.IsType<E4>(receivedEvent);
            Assert.True((receivedEvent as E4).Value);
            Assert.Equal(1, queue.Size);

            var (deqeueStatus, e, opGroupId, info) = queue.Dequeue();
            Assert.IsType<E4>(e);
            Assert.False((e as E4).Value);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(0, queue.Size);

            (deqeueStatus, e, opGroupId, info) = queue.Dequeue();
            Assert.Equal(DequeueStatus.NotAvailable, deqeueStatus);
            Assert.Equal(0, queue.Size);

            await Task.WhenAny(tcs.Task, Task.Delay(500));
            Assert.True(tcs.Task.IsCompleted);
        }

        [Fact(Timeout = 5000)]
        public async Task TestReceiveEventWithPredicateWithoutWaiting()
        {
            int notificationCount = 0;
            var tcs = new TaskCompletionSource<bool>();
            var logger = new TestOutputLogger(this.TestOutput, false);
            var machineStateManager = new MockMachineStateManager(logger,
                (notification, evt, _) =>
                {
                    notificationCount++;
                    if (notificationCount == 2)
                    {
                        Assert.Equal(MockMachineStateManager.Notification.ReceiveEventWithoutWaiting, notification);
                        tcs.SetResult(true);
                    }
                });

            var queue = new EventQueue(machineStateManager);
            var enqueueStatus = queue.Enqueue(new E1(), Guid.Empty, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(1, queue.Size);

            var receivedEvent = await queue.ReceiveAsync(typeof(E1));
            Assert.IsType<E1>(receivedEvent);
            Assert.Equal(0, queue.Size);

            var (deqeueStatus, e, opGroupId, info) = queue.Dequeue();
            Assert.Equal(DequeueStatus.NotAvailable, deqeueStatus);
            Assert.Equal(0, queue.Size);

            await Task.WhenAny(tcs.Task, Task.Delay(500));
            Assert.True(tcs.Task.IsCompleted);
        }

        [Fact(Timeout = 5000)]
        public async Task TestReceiveEventMultipleTypes()
        {
            int notificationCount = 0;
            var tcs = new TaskCompletionSource<bool>();
            var logger = new TestOutputLogger(this.TestOutput, false);
            var machineStateManager = new MockMachineStateManager(logger,
                (notification, evt, _) =>
                {
                    notificationCount++;
                    if (notificationCount == 2)
                    {
                        Assert.Equal(MockMachineStateManager.Notification.ReceiveEvent, notification);
                        tcs.SetResult(true);
                    }
                });

            var queue = new EventQueue(machineStateManager);

            var task = Task.Run(async () =>
            {
                var receivedEvent = await queue.ReceiveAsync(typeof(E1), typeof(E2));
                Assert.IsType<E2>(receivedEvent);
                Assert.Equal(0, queue.Size);
            });

            // Small delay to force ordering.
            await Task.Delay(300);

            var enqueueStatus = queue.Enqueue(new E2(), Guid.Empty, null);
            Assert.Equal(EnqueueStatus.Received, enqueueStatus);
            Assert.Equal(0, queue.Size);

            await task;

            var (deqeueStatus, e, opGroupId, info) = queue.Dequeue();
            Assert.Equal(DequeueStatus.NotAvailable, deqeueStatus);
            Assert.Equal(0, queue.Size);

            await Task.WhenAny(tcs.Task, Task.Delay(500));
            Assert.True(tcs.Task.IsCompleted);
        }

        [Fact(Timeout = 5000)]
        public async Task TestReceiveEventAfterMultipleEnqueues()
        {
            int notificationCount = 0;
            var tcs = new TaskCompletionSource<bool>();
            var logger = new TestOutputLogger(this.TestOutput, false);
            var machineStateManager = new MockMachineStateManager(logger,
                (notification, evt, _) =>
                {
                    notificationCount++;
                    if (notificationCount == 4)
                    {
                        Assert.Equal(MockMachineStateManager.Notification.ReceiveEvent, notification);
                        tcs.SetResult(true);
                    }
                });

            var queue = new EventQueue(machineStateManager);

            var task = Task.Run(async () =>
            {
                var receivedEvent = await queue.ReceiveAsync(typeof(E1));
                Assert.IsType<E1>(receivedEvent);
                Assert.Equal(2, queue.Size);
            });

            // Small delay to force ordering.
            await Task.Delay(300);

            var enqueueStatus = queue.Enqueue(new E2(), Guid.Empty, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(1, queue.Size);

            enqueueStatus = queue.Enqueue(new E3(), Guid.Empty, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(2, queue.Size);

            enqueueStatus = queue.Enqueue(new E1(), Guid.Empty, null);
            Assert.Equal(EnqueueStatus.Received, enqueueStatus);
            Assert.Equal(2, queue.Size);

            await task;

            var (deqeueStatus, e, opGroupId, info) = queue.Dequeue();
            Assert.IsType<E2>(e);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(1, queue.Size);

            (deqeueStatus, e, opGroupId, info) = queue.Dequeue();
            Assert.IsType<E3>(e);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(0, queue.Size);

            await Task.WhenAny(tcs.Task, Task.Delay(500));
            Assert.True(tcs.Task.IsCompleted);
        }

        [Fact(Timeout = 5000)]
        public async Task TestReceiveEventWithoutWaitingAndWithMultipleEventsInQueue()
        {
            int notificationCount = 0;
            var tcs = new TaskCompletionSource<bool>();
            var logger = new TestOutputLogger(this.TestOutput, false);
            var machineStateManager = new MockMachineStateManager(logger,
                (notification, evt, _) =>
                {
                    notificationCount++;
                    if (notificationCount == 4)
                    {
                        Assert.Equal(MockMachineStateManager.Notification.ReceiveEventWithoutWaiting, notification);
                        tcs.SetResult(true);
                    }
                });

            var queue = new EventQueue(machineStateManager);
            var enqueueStatus = queue.Enqueue(new E2(), Guid.Empty, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(1, queue.Size);

            enqueueStatus = queue.Enqueue(new E1(), Guid.Empty, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(2, queue.Size);

            enqueueStatus = queue.Enqueue(new E3(), Guid.Empty, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(3, queue.Size);

            var receivedEvent = await queue.ReceiveAsync(typeof(E1));
            Assert.IsType<E1>(receivedEvent);
            Assert.Equal(2, queue.Size);

            var (deqeueStatus, e, opGroupId, info) = queue.Dequeue();
            Assert.IsType<E2>(e);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(1, queue.Size);

            (deqeueStatus, e, opGroupId, info) = queue.Dequeue();
            Assert.IsType<E3>(e);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(0, queue.Size);

            (deqeueStatus, e, opGroupId, info) = queue.Dequeue();
            Assert.Equal(DequeueStatus.NotAvailable, deqeueStatus);
            Assert.Equal(0, queue.Size);

            await Task.WhenAny(tcs.Task, Task.Delay(500));
            Assert.True(tcs.Task.IsCompleted);
        }
    }
}
