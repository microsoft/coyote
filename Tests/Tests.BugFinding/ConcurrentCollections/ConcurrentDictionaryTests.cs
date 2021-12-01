// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests.ConcurrentCollections
{
    public class ConcurrentDictionaryTests : BaseBugFindingTest
    {
        public ConcurrentDictionaryTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestConcurrentDictionaryProperties()
        {
            this.Test(() =>
            {
                var concurrentDictionary = new ConcurrentDictionary<int, bool>();
                Assert.True(concurrentDictionary.IsEmpty);

                concurrentDictionary[0] = false;
                Assert.True(concurrentDictionary[0] == false);
                Assert.Single(concurrentDictionary);

                concurrentDictionary.Clear();
                Assert.Empty(concurrentDictionary);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestConcurrentDictionaryMethodsSubsetOne()
        {
            this.Test(() =>
            {
                var concurrentDictionary = new ConcurrentDictionary<int, int>();
                Assert.True(concurrentDictionary.IsEmpty);

                int value = concurrentDictionary.GetOrAdd(1, (key) => 1);
                Assert.Equal(1, value);
                Assert.True(concurrentDictionary.ContainsKey(1));
                Assert.Single(concurrentDictionary);

                concurrentDictionary.AddOrUpdate(1, 1, (key, oldValue) => oldValue * 10);
                Assert.True(concurrentDictionary[1] == 10);
                Assert.True(concurrentDictionary.ContainsKey(1));
                Assert.Single(concurrentDictionary);

                value = concurrentDictionary.GetOrAdd(1, 100);
                Assert.Equal(10, value);
                Assert.True(concurrentDictionary.ContainsKey(1));
                Assert.Single(concurrentDictionary);

                concurrentDictionary.AddOrUpdate(2, (key) => 2, (key, oldValue) => oldValue * 10);
                Assert.True(concurrentDictionary[2] == 2);
                Assert.True(concurrentDictionary.Count == 2);
                Assert.True(concurrentDictionary.ContainsKey(1));
                Assert.True(concurrentDictionary.ContainsKey(2));

                concurrentDictionary.TryUpdate(2, 20, 2);
                Assert.True(concurrentDictionary[2] == 20);
                Assert.True(concurrentDictionary.Count == 2);

                concurrentDictionary.Clear();
                Assert.Empty(concurrentDictionary);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestConcurrentDictionaryMethodsSubsetOneWithRaceCondition()
        {
            this.TestWithError(() =>
            {
                var concurrentDictionary = new ConcurrentDictionary<int, int>();

                var t1 = Task.Run(() =>
                {
                    var value1 = concurrentDictionary.AddOrUpdate(1, 1, (key, oldValue) => oldValue * 10);
                    var value2 = concurrentDictionary.GetOrAdd(1, 2);

                    Specification.Assert(value1 == value2, "Value is {0} instead of {1}.", value2, value1);
                });

                var t2 = Task.Run(() =>
                {
                    concurrentDictionary.AddOrUpdate(1, 2, (key, oldValue) => 2);
                });

                Task.WaitAll(t1, t2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: "Value is 2 instead of 1.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestConcurrentDictionaryMethodsSubsetTwo()
        {
            this.Test(() =>
            {
                var concurrentDictionary = new ConcurrentDictionary<int, int>();
                Assert.True(concurrentDictionary.IsEmpty);

                bool result = concurrentDictionary.TryAdd(1, 1);
                Assert.True(result);
                Assert.True(concurrentDictionary.ContainsKey(1));
                Assert.Single(concurrentDictionary);

                result = concurrentDictionary.TryGetValue(1, out int value);
                Assert.True(result);
                Assert.Equal(1, value);

                result = concurrentDictionary.TryUpdate(1, 10, 1);
                Assert.True(result);
                Assert.True(concurrentDictionary[1] == 10);

                concurrentDictionary.TryRemove(1, out value);
                Assert.Empty(concurrentDictionary);
                Assert.Equal(10, value);

                result = concurrentDictionary.TryGetValue(1, out value);
                Assert.False(result);

                concurrentDictionary.Clear();
                Assert.Empty(concurrentDictionary);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestConcurrentDictionaryMethodsSubsetTwoWithRaceCondition()
        {
            this.TestWithError(() =>
            {
                var concurrentDictionary = new ConcurrentDictionary<int, bool>();

                var t1 = Task.Run(() =>
                {
                    concurrentDictionary.TryAdd(1, true);
                    var count = concurrentDictionary.Count;
                    Specification.Assert(count is 1, "Count is {0} instead of 1.", count);
                });

                var t2 = Task.Run(() =>
                {
                    concurrentDictionary.Clear();
                });

                Task.WaitAll(t1, t2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: "Count is 0 instead of 1.",
            replay: true);
        }
    }
}
