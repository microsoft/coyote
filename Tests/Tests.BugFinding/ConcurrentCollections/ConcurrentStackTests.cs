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
    public class ConcurrentStackTests : BaseBugFindingTest
    {
        public ConcurrentStackTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestConcurrentStackProperties()
        {
            this.Test(() =>
            {
                var concurrentStack = new ConcurrentStack<int>();
                Assert.True(concurrentStack.IsEmpty);

                concurrentStack.Push(1);
                var count = concurrentStack.Count;
                Assert.Equal(1, count);
                Assert.Single(concurrentStack);

                concurrentStack.Clear();
                Assert.Empty(concurrentStack);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestConcurrentStackMethods()
        {
            this.Test(() =>
            {
                var concurrentStack = new ConcurrentStack<int>();
                Assert.True(concurrentStack.IsEmpty);

                concurrentStack.Push(1);
                Assert.Single(concurrentStack);

                bool peekResult = concurrentStack.TryPeek(out int peekValue);
                Assert.True(peekResult);
                Assert.Equal(1, peekValue);

                int[] array = { 2, 3 };
                concurrentStack.PushRange(array);
                int[] expectedArray = { 3, 2, 1 };
                var actualArray = concurrentStack.ToArray();
                Assert.Equal(3, concurrentStack.Count);
                Assert.Equal(expectedArray, actualArray);

                bool popResult = concurrentStack.TryPop(out int popValue);
                Assert.True(popResult);
                Assert.Equal(3, popValue);
                Assert.Equal(2, concurrentStack.Count);

                concurrentStack.Clear();
                Assert.Empty(concurrentStack);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestConcurrentStackMethodsWithRaceCondition()
        {
            this.TestWithError(() =>
            {
                var concurrentStack = new ConcurrentStack<int>();

                var t1 = Task.Run(() =>
                {
                    concurrentStack.Push(1);
                    concurrentStack.Push(2);
                    concurrentStack.TryPop(out int value);

                    Specification.Assert(value == 2, "Value is {0} instead of 2.", value);
                });

                var t2 = Task.Run(() =>
                {
                    concurrentStack.TryPop(out int _);
                });

                Task.WaitAll(t1, t2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: "Value is 1 instead of 2.",
            replay: true);
        }
    }
}
