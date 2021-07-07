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
                ConcurrentDictionary<int, bool> concurrentDictionary = new ConcurrentDictionary<int, bool>();
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
                ConcurrentDictionary<int, int> concurrentDictionary = new ConcurrentDictionary<int, int>();
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
                ConcurrentDictionary<int, int> concurrentDictionary = new ConcurrentDictionary<int, int>();

                var t1 = Task.Run(() =>
                {
                    concurrentDictionary.GetOrAdd(1, 1);
                });

                var t2 = Task.Run(() =>
                {
                    concurrentDictionary.AddOrUpdate(1, 1, (key, oldValue) => oldValue * 10);
                });

                var t3 = Task.Run(() =>
                {
                    concurrentDictionary.TryUpdate(1, 1, 10);
                });

                Task.WaitAll(t1, t2, t3);

                Specification.Assert(concurrentDictionary[1] is 1, "Value is {0} instead of 1.", concurrentDictionary[1]);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: "Value is 10 instead of 1.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestConcurrentDictionaryMethodsSubsetTwo()
        {
            this.Test(() =>
            {
                ConcurrentDictionary<int, int> concurrentDictionary = new ConcurrentDictionary<int, int>();
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
                ConcurrentDictionary<int, bool> concurrentDictionary = new ConcurrentDictionary<int, bool>();

                var t1 = Task.Run(() =>
                {
                    concurrentDictionary.TryAdd(0, true);
                    concurrentDictionary.TryRemove(0, out bool _);
                });

                var t2 = Task.Run(() =>
                {
                    concurrentDictionary.TryAdd(0, false);
                });

                var t3 = Task.Run(() =>
                {
                    concurrentDictionary.Clear();
                });

                Task.WaitAll(t1, t2, t3);

                Specification.Assert(concurrentDictionary.Count is 0, "Value is {0} instead of 0.", concurrentDictionary.Count);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: "Value is 1 instead of 0.",
            replay: true);
        }
    }
}
