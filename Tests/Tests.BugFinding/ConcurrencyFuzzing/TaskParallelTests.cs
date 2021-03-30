// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests.ConcurrencyFuzzing
{
    public class TaskParallelTests : BaseBugFindingTest
    {
        public TaskParallelTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestParallelFor()
        {
            this.Test(() =>
            {
                int value = 0;
                Parallel.For(3, 7, i =>
                {
                    Interlocked.Add(ref value, i);
                });

                int expected = 18;
                Specification.Assert(value == expected, "Value is {0} instead of {expected}.");
            },
            configuration: GetConfiguration().WithConcurrencyFuzzingEnabled().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestParallelForEach()
        {
            this.Test(() =>
            {
                var list = new List<int> { 3, 4, 5, 6 };

                int value = 0;
                Parallel.ForEach(list, i =>
                {
                    Interlocked.Add(ref value, i);
                });

                int expected = 18;
                Specification.Assert(value == expected, "Value is {0} instead of {expected}.");
            },
            configuration: GetConfiguration().WithConcurrencyFuzzingEnabled().WithTestingIterations(10));
        }
    }
}
