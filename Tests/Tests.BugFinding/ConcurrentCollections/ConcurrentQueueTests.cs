// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests.ConcurrentCollections
{
    public class ConcurrentQueueTests : BaseBugFindingTest
    {
        public ConcurrentQueueTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestConcurrentQueueProperties()
        {
            this.Test(() =>
            {
                var concurrentQueue = new ConcurrentQueue<int>();
                Assert.True(concurrentQueue.IsEmpty);

                concurrentQueue.Enqueue(1);
                var count = concurrentQueue.Count;
                Assert.Equal(1, count);
                Assert.Single(concurrentQueue);

                concurrentQueue.Clear();
                Assert.Empty(concurrentQueue);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestConcurrentQueueMethods()
        {
            this.Test(() =>
            {
                var concurrentQueue = new ConcurrentQueue<int>();
                Assert.True(concurrentQueue.IsEmpty);

                concurrentQueue.Enqueue(1);
                var count = concurrentQueue.Count;
                Assert.Equal(1, count);
                Assert.Single(concurrentQueue);

                bool peekResult = concurrentQueue.TryPeek(out int peek);
                Assert.True(peekResult);
                Assert.Equal(1, peek);

                concurrentQueue.Enqueue(2);
                int[] expectedArray = { 1, 2 };
                var actualArray = concurrentQueue.ToArray();
                Assert.Equal(2, concurrentQueue.Count);
                Assert.Equal(expectedArray, actualArray);

                bool dequeueResult = concurrentQueue.TryDequeue(out int dequeue);
                Assert.True(dequeueResult);
                Assert.Equal(1, dequeue);

                concurrentQueue.Clear();
                Assert.Empty(concurrentQueue);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestConcurrentQueueMethodsWithRaceCondition()
        {
            this.TestWithError(() =>
            {
                var concurrentQueue = new ConcurrentQueue<int>();

                var t1 = Task.Run(() =>
                {
                    concurrentQueue.Enqueue(1);
                    concurrentQueue.Enqueue(2);

                    concurrentQueue.TryDequeue(out int value);
                    Specification.Assert(value == 1, "Value is {0} instead of 1.", value);
                });

                var t2 = Task.Run(() =>
                {
                    concurrentQueue.TryDequeue(out int value);
                });

                Task.WaitAll(t1, t2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: "Value is 2 instead of 1.",
            replay: true);
        }
    }
}
