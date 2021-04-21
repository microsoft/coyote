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
        public void TestGenericListDataRace()
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
    }
}
