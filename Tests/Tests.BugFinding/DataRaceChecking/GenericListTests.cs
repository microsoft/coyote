// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests.DataRaceChecking
{
    public class GenericListTests : BaseBugFindingTest
    {
        public GenericListTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestGenericListAddDataRace()
        {
            this.TestWithError(async () =>
            {
                var list = new List<int>();

                Task t1 = Task.Run(() =>
                {
                    list.Add(1);
                });

                Task t2 = Task.Run(() =>
                {
                    list.Add(2);
                });

                await Task.WhenAll(t1, t2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: $"Found write/write data race on '{typeof(List<int>)}'.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestGenericListIndex()
        {
            this.Test(async () =>
            {
                var list = new List<int>
                {
                    1
                };

                Task t1 = Task.Run(() =>
                {
                    _ = list[0];
                });

                Task t2 = Task.Run(() =>
                {
                    _ = list[0];
                });

                await Task.WhenAll(t1, t2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestGenericListIndexDataRace()
        {
            this.TestWithError(async () =>
            {
                var list = new List<int>
                {
                    1
                };

                Task t1 = Task.Run(() =>
                {
                    _ = list[0];
                });

                Task t2 = Task.Run(() =>
                {
                    list[0] = 2;
                });

                await Task.WhenAll(t1, t2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: $"Found read/write data race on '{typeof(List<int>)}'.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestGenericListCapacityDataRace()
        {
            this.TestWithError(async () =>
            {
                var list = new List<int>();

                Task t1 = Task.Run(() =>
                {
                    list.Capacity = 2;
                });

                Task t2 = Task.Run(() =>
                {
                    list.Capacity = 5;
                });

                await Task.WhenAll(t1, t2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: $"Found write/write data race on '{typeof(List<int>)}'.",
            replay: true);
        }
    }
}
