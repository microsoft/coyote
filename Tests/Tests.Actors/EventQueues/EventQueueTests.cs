// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.Tests
{
    /// <summary>
    /// Tests internal low level EventQueue implementation.
    /// This is a Production-only test.
    /// </summary>
    public class EventQueueTests : BaseActorTest
    {
        public EventQueueTests(ITestOutputHelper output)
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
            var logger = new TestOutputLogger(this.TestOutput);
            using var queue = new TestEventQueue(logger, (notification, evt, _) => { });
            Assert.Equal(0, queue.Size);

            var enqueueStatus = queue.Enqueue(new E1(), null, null);
            Assert.Equal(1, queue.Size);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);

            enqueueStatus = queue.Enqueue(new E2(), null, null);
            Assert.Equal(2, queue.Size);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);

            enqueueStatus = queue.Enqueue(new E3(), null, null);
            Assert.Equal(3, queue.Size);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
        }

        [Fact(Timeout = 5000)]
        public void TestDequeueEvent()
        {
            var logger = new TestOutputLogger(this.TestOutput);
            using var queue = new TestEventQueue(logger, (notification, evt, _) => { });
            var (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.Equal(DequeueStatus.Unavailable, deqeueStatus);
            Assert.Equal(0, queue.Size);

            queue.Enqueue(new E1(), null, null);
            (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.IsType<E1>(e);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(0, queue.Size);

            queue.Enqueue(new E3(), null, null);
            queue.Enqueue(new E2(), null, null);
            queue.Enqueue(new E1(), null, null);

            (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.IsType<E3>(e);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(2, queue.Size);

            (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.IsType<E2>(e);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(1, queue.Size);

            (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.IsType<E1>(e);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(0, queue.Size);

            (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.Equal(DequeueStatus.Unavailable, deqeueStatus);
            Assert.Equal(0, queue.Size);
        }

        [Fact(Timeout = 5000)]
        public void TestEnqueueEventWithHandlerNotRunning()
        {
            var logger = new TestOutputLogger(this.TestOutput);
            using var queue = new TestEventQueue(logger, (notification, evt, _) => { });
            var (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.Equal(DequeueStatus.Unavailable, deqeueStatus);
            Assert.Equal(0, queue.Size);

            var enqueueStatus = queue.Enqueue(new E1(), null, null);
            Assert.Equal(EnqueueStatus.EventHandlerNotRunning, enqueueStatus);
            Assert.Equal(1, queue.Size);
        }

        [Fact(Timeout = 5000)]
        public void TestRaiseEvent()
        {
            var logger = new TestOutputLogger(this.TestOutput);
            using var queue = new TestEventQueue(logger, (notification, evt, _) => { });
            queue.RaiseEvent(new E1(), null);
            Assert.True(queue.IsEventRaised);
            Assert.Equal(0, queue.Size);

            var (deqeueStatus, e, group, info) = queue.Dequeue();
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
            var logger = new TestOutputLogger(this.TestOutput);

            using var queue = new TestEventQueue(logger, (notification, evt, _) =>
            {
                notificationCount++;
                if (notificationCount is 2)
                {
                    Assert.Equal(TestEventQueue.Notification.ReceiveEvent, notification);
                    tcs.SetResult(true);
                }
            });

            var receivedEventTask = queue.ReceiveEventAsync(typeof(E1));

            var enqueueStatus = queue.Enqueue(new E1(), null, null);
            Assert.Equal(EnqueueStatus.Received, enqueueStatus);
            Assert.Equal(0, queue.Size);

            var receivedEvent = await receivedEventTask;
            Assert.IsType<E1>(receivedEvent);
            Assert.Equal(0, queue.Size);

            var (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.Equal(DequeueStatus.Unavailable, deqeueStatus);
            Assert.Equal(0, queue.Size);

            await await Task.WhenAny(tcs.Task, Task.Delay(500));
            Assert.True(tcs.Task.IsCompleted);
        }

        [Fact(Timeout = 5000)]
        public async Task TestReceiveEventWithPredicate()
        {
            int notificationCount = 0;
            var tcs = new TaskCompletionSource<bool>();
            var logger = new TestOutputLogger(this.TestOutput);

            using var queue = new TestEventQueue(logger, (notification, evt, _) =>
            {
                notificationCount++;
                if (notificationCount is 3)
                {
                    Assert.Equal(TestEventQueue.Notification.ReceiveEvent, notification);
                    tcs.SetResult(true);
                }
            });

            var receivedEventTask = queue.ReceiveEventAsync(typeof(E4), evt => (evt as E4).Value);

            var enqueueStatus = queue.Enqueue(new E4(false), null, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(1, queue.Size);

            enqueueStatus = queue.Enqueue(new E4(true), null, null);
            Assert.Equal(EnqueueStatus.Received, enqueueStatus);
            Assert.Equal(1, queue.Size);

            var receivedEvent = await receivedEventTask;
            Assert.IsType<E4>(receivedEvent);
            Assert.True((receivedEvent as E4).Value);
            Assert.Equal(1, queue.Size);

            var (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.IsType<E4>(e);
            Assert.False((e as E4).Value);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(0, queue.Size);

            (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.Equal(DequeueStatus.Unavailable, deqeueStatus);
            Assert.Equal(0, queue.Size);

            await await Task.WhenAny(tcs.Task, Task.Delay(500));
            Assert.True(tcs.Task.IsCompleted);
        }

        [Fact(Timeout = 5000)]
        public async Task TestReceiveEventWithoutWaiting()
        {
            int notificationCount = 0;
            var tcs = new TaskCompletionSource<bool>();
            var logger = new TestOutputLogger(this.TestOutput);

            using var queue = new TestEventQueue(logger, (notification, evt, _) =>
            {
                notificationCount++;
                if (notificationCount is 3)
                {
                    Assert.Equal(TestEventQueue.Notification.ReceiveEventWithoutWaiting, notification);
                    tcs.SetResult(true);
                }
            });

            var enqueueStatus = queue.Enqueue(new E4(false), null, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(1, queue.Size);

            enqueueStatus = queue.Enqueue(new E4(true), null, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(2, queue.Size);

            var receivedEvent = await queue.ReceiveEventAsync(typeof(E4), evt => (evt as E4).Value);
            Assert.IsType<E4>(receivedEvent);
            Assert.True((receivedEvent as E4).Value);
            Assert.Equal(1, queue.Size);

            var (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.IsType<E4>(e);
            Assert.False((e as E4).Value);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(0, queue.Size);

            (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.Equal(DequeueStatus.Unavailable, deqeueStatus);
            Assert.Equal(0, queue.Size);

            await await Task.WhenAny(tcs.Task, Task.Delay(500));
            Assert.True(tcs.Task.IsCompleted);
        }

        [Fact(Timeout = 5000)]
        public async Task TestReceiveEventWithPredicateWithoutWaiting()
        {
            int notificationCount = 0;
            var tcs = new TaskCompletionSource<bool>();
            var logger = new TestOutputLogger(this.TestOutput);

            using var queue = new TestEventQueue(logger, (notification, evt, _) =>
            {
                notificationCount++;
                if (notificationCount is 2)
                {
                    Assert.Equal(TestEventQueue.Notification.ReceiveEventWithoutWaiting, notification);
                    tcs.SetResult(true);
                }
            });

            var enqueueStatus = queue.Enqueue(new E1(), null, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(1, queue.Size);

            var receivedEvent = await queue.ReceiveEventAsync(typeof(E1));
            Assert.IsType<E1>(receivedEvent);
            Assert.Equal(0, queue.Size);

            var (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.Equal(DequeueStatus.Unavailable, deqeueStatus);
            Assert.Equal(0, queue.Size);

            await await Task.WhenAny(tcs.Task, Task.Delay(500));
            Assert.True(tcs.Task.IsCompleted);
        }

        [Fact(Timeout = 5000)]
        public async Task TestReceiveEventMultipleTypes()
        {
            int notificationCount = 0;
            var tcs = new TaskCompletionSource<bool>();
            var logger = new TestOutputLogger(this.TestOutput);

            using var queue = new TestEventQueue(logger, (notification, evt, _) =>
            {
                notificationCount++;
                if (notificationCount is 2)
                {
                    Assert.Equal(TestEventQueue.Notification.ReceiveEvent, notification);
                    tcs.SetResult(true);
                }
            });

            var receivedEventTask = queue.ReceiveEventAsync(typeof(E1), typeof(E2));

            var enqueueStatus = queue.Enqueue(new E2(), null, null);
            Assert.Equal(EnqueueStatus.Received, enqueueStatus);
            Assert.Equal(0, queue.Size);

            var receivedEvent = await receivedEventTask;
            Assert.IsType<E2>(receivedEvent);
            Assert.Equal(0, queue.Size);

            var (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.Equal(DequeueStatus.Unavailable, deqeueStatus);
            Assert.Equal(0, queue.Size);

            await await Task.WhenAny(tcs.Task, Task.Delay(500));
            Assert.True(tcs.Task.IsCompleted);
        }

        [Fact(Timeout = 5000)]
        public async Task TestReceiveEventAfterMultipleEnqueues()
        {
            int notificationCount = 0;
            var tcs = new TaskCompletionSource<bool>();
            var logger = new TestOutputLogger(this.TestOutput);

            using var queue = new TestEventQueue(logger, (notification, evt, _) =>
            {
                notificationCount++;
                if (notificationCount is 4)
                {
                    Assert.Equal(TestEventQueue.Notification.ReceiveEvent, notification);
                    tcs.SetResult(true);
                }
            });

            var receivedEventTask = queue.ReceiveEventAsync(typeof(E1));

            var enqueueStatus = queue.Enqueue(new E2(), null, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(1, queue.Size);

            enqueueStatus = queue.Enqueue(new E3(), null, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(2, queue.Size);

            enqueueStatus = queue.Enqueue(new E1(), null, null);
            Assert.Equal(EnqueueStatus.Received, enqueueStatus);
            Assert.Equal(2, queue.Size);

            var receivedEvent = await receivedEventTask;
            Assert.IsType<E1>(receivedEvent);
            Assert.Equal(2, queue.Size);

            var (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.IsType<E2>(e);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(1, queue.Size);

            (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.IsType<E3>(e);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(0, queue.Size);

            await await Task.WhenAny(tcs.Task, Task.Delay(500));
            Assert.True(tcs.Task.IsCompleted);
        }

        [Fact(Timeout = 5000)]
        public async Task TestReceiveEventWithoutWaitingAndWithMultipleEventsInQueue()
        {
            int notificationCount = 0;
            var tcs = new TaskCompletionSource<bool>();
            var logger = new TestOutputLogger(this.TestOutput);

            using var queue = new TestEventQueue(logger, (notification, evt, _) =>
            {
                notificationCount++;
                if (notificationCount is 4)
                {
                    Assert.Equal(TestEventQueue.Notification.ReceiveEventWithoutWaiting, notification);
                    tcs.SetResult(true);
                }
            });

            var enqueueStatus = queue.Enqueue(new E2(), null, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(1, queue.Size);

            enqueueStatus = queue.Enqueue(new E1(), null, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(2, queue.Size);

            enqueueStatus = queue.Enqueue(new E3(), null, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(3, queue.Size);

            var receivedEvent = await queue.ReceiveEventAsync(typeof(E1));
            Assert.IsType<E1>(receivedEvent);
            Assert.Equal(2, queue.Size);

            var (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.IsType<E2>(e);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(1, queue.Size);

            (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.IsType<E3>(e);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(0, queue.Size);

            (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.Equal(DequeueStatus.Unavailable, deqeueStatus);
            Assert.Equal(0, queue.Size);

            await await Task.WhenAny(tcs.Task, Task.Delay(500));
            Assert.True(tcs.Task.IsCompleted);
        }

        [Fact(Timeout = 5000)]
        public void TestIgnoreEvent()
        {
            int notificationCount = 0;
            var logger = new TestOutputLogger(this.TestOutput);

            using var queue = new TestEventQueue(logger, (notification, evt, _) =>
            {
                notificationCount++;
                if (notificationCount is 3)
                {
                    Assert.Equal(TestEventQueue.Notification.IgnoreEvent, notification);
                }
            }, ignoredEvents: new Type[1] { typeof(E1) });

            var enqueueStatus = queue.Enqueue(new E2(), null, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(1, queue.Size);

            enqueueStatus = queue.Enqueue(new E1(), null, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(2, queue.Size);

            var (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.IsType<E2>(e);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(1, queue.Size);

            (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.Equal(DequeueStatus.Unavailable, deqeueStatus);
            Assert.Equal(0, queue.Size);
        }

        [Fact(Timeout = 5000)]
        public void TestDeferEvent()
        {
            int notificationCount = 0;
            var logger = new TestOutputLogger(this.TestOutput);

            using var queue = new TestEventQueue(logger, (notification, evt, _) =>
            {
                notificationCount++;
                if (notificationCount is 4)
                {
                    Assert.Equal(TestEventQueue.Notification.DeferEvent, notification);
                }
            }, deferredEvents: new Type[1] { typeof(E1) });

            var enqueueStatus = queue.Enqueue(new E2(), null, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(1, queue.Size);

            enqueueStatus = queue.Enqueue(new E1(), null, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(2, queue.Size);

            enqueueStatus = queue.Enqueue(new E3(), null, null);
            Assert.Equal(EnqueueStatus.EventHandlerRunning, enqueueStatus);
            Assert.Equal(3, queue.Size);

            var (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.IsType<E2>(e);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(2, queue.Size);

            (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.IsType<E3>(e);
            Assert.Equal(DequeueStatus.Success, deqeueStatus);
            Assert.Equal(1, queue.Size);

            (deqeueStatus, e, group, info) = queue.Dequeue();
            Assert.Equal(DequeueStatus.Unavailable, deqeueStatus);
            Assert.Equal(1, queue.Size);
        }
    }
}
