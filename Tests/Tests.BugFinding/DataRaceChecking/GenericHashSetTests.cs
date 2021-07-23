// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests.DataRaceChecking
{
    public class GenericHashSetTests : BaseBugFindingTest
    {
        public GenericHashSetTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestGenericHashSetProperties()
        {
            this.Test(() =>
            {
                var hashSet = new HashSet<int>();
                Assert.Empty(hashSet);

                hashSet.Add(1);
                var count = hashSet.Count;
                Assert.Equal(1, count);
                Assert.Single(hashSet);

                hashSet.Clear();
                Assert.Empty(hashSet);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestGenericHashSetWriteWriteDataRace()
        {
            this.TestWithError(async () =>
            {
                var hashSet = new HashSet<int>();

                Task t1 = Task.Run(() =>
                {
                    hashSet.Add(1);
                });

                Task t2 = Task.Run(() =>
                {
                    hashSet.Add(2);
                });

                await Task.WhenAll(t1, t2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: $"Found write/write data race on '{typeof(HashSet<int>)}'.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestGenericHashSetReadWriteDataRace()
        {
            this.TestWithError(async () =>
            {
                var hashSet = new HashSet<int>();

                Task t1 = Task.Run(() =>
                {
                    hashSet.Add(1);
                });

                Task t2 = Task.Run(() =>
                {
                    hashSet.Contains(2);
                });

                await Task.WhenAll(t1, t2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: $"Found read/write data race on '{typeof(HashSet<int>)}'.",
            replay: true);
        }
    }
}
