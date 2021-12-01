// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests.ConcurrentCollections
{
    public class ConcurrentBagTests : BaseBugFindingTest
    {
        public ConcurrentBagTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestConcurrentBagProperties()
        {
            this.Test(() =>
            {
                var concurrentBag = new ConcurrentBag<int>();
                Assert.True(concurrentBag.IsEmpty);

                concurrentBag.Add(1);
                var count = concurrentBag.Count;
                Assert.Equal(1, count);
                Assert.Single(concurrentBag);

#if NET || NETCOREAPP3_1
                concurrentBag.Clear();
                Assert.Empty(concurrentBag);
#endif
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestConcurrentBagMethods()
        {
            this.Test(() =>
            {
                var concurrentBag = new ConcurrentBag<int>();
                Assert.True(concurrentBag.IsEmpty);

                concurrentBag.Add(1);
                Assert.Single(concurrentBag);

                bool peekResult = concurrentBag.TryPeek(out int peek);
                Assert.True(peekResult);
                Assert.Equal(1, peek);

                int[] expectedArray = { 1 };
                var actualArray = concurrentBag.ToArray();
                Assert.Equal(expectedArray, actualArray);

                bool takeResult = concurrentBag.TryTake(out int _);
                Assert.True(takeResult);
                Assert.Empty(concurrentBag);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestConcurrentBagMethodsWithRaceCondition()
        {
            this.TestWithError(() =>
            {
                var concurrentBag = new ConcurrentBag<int>();

                var t1 = Task.Run(() =>
                {
                    concurrentBag.Add(1);
                    concurrentBag.Add(2);

                    Specification.Assert(concurrentBag.Count == 2, "Value is {0} instead of 2.", concurrentBag.Count);
                });

                var t2 = Task.Run(() =>
                {
                    concurrentBag.TryTake(out int _);
                });

                Task.WaitAll(t1, t2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: "Value is 1 instead of 2.",
            replay: true);
        }
    }
}
