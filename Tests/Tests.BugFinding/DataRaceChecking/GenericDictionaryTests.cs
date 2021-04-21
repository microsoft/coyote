// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests.DataRaceChecking
{
    public class GenericDictionaryTests : BaseBugFindingTest
    {
        public GenericDictionaryTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestGenericDictionaryAddDataRace()
        {
            this.TestWithError(async () =>
            {
                var dictionary = new Dictionary<int, bool>();

                Task t1 = Task.Run(() =>
                {
                    dictionary.Add(1, true);
                });

                Task t2 = Task.Run(() =>
                {
                    dictionary.Add(2, false);
                });

                await Task.WhenAll(t1, t2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: $"Found write/write data race on '{typeof(Dictionary<int, bool>)}'.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestGenericDictionaryIndex()
        {
            this.Test(async () =>
            {
                var dictionary = new Dictionary<int, bool>
                {
                    { 1, true }
                };

                Task t1 = Task.Run(() =>
                {
                    _ = dictionary[1];
                });

                Task t2 = Task.Run(() =>
                {
                    _ = dictionary[1];
                });

                await Task.WhenAll(t1, t2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestGenericDictionaryIndexDataRace()
        {
            this.TestWithError(async () =>
            {
                var dictionary = new Dictionary<int, bool>
                {
                    { 1, true }
                };

                Task t1 = Task.Run(() =>
                {
                    _ = dictionary[1];
                });

                Task t2 = Task.Run(() =>
                {
                    dictionary[1] = false;
                });

                await Task.WhenAll(t1, t2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: $"Found read/write data race on '{typeof(Dictionary<int, bool>)}'.",
            replay: true);
        }
    }
}
