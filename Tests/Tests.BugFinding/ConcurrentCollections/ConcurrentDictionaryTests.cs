// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
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
                Assert.True(concurrentDictionary.Count == 1);

                concurrentDictionary.Clear();
                Assert.Empty(concurrentDictionary);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestConcurrentDictionaryWithRaceCondition()
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
