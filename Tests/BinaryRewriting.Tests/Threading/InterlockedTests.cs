// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BinaryRewriting.Tests.Threading.Tasks
{
    public class InterlockedTests : BaseProductionTest
    {
        public InterlockedTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedIncrement()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                entry.Value = 0;

                Task task1 = Task.Run(() =>
               {
                   Interlocked.Exchange(ref entry.Value, 0);
                   Interlocked.Increment(ref entry.Value);
               });

                Task task2 = Task.Run(() =>
               {
                   Interlocked.Exchange(ref entry.Value, 0);
                   Interlocked.Increment(ref entry.Value);
               });

                await task1;
                await task2;
                Specification.Assert(entry.Value == 1, $"Value is {entry.Value} instead of 1.");
            },
            configuration: GetConfiguration().WithTestingIterations(500),
            replay: true,
            expectedError: "Value is 2 instead of 1.");
        }

        [Fact(Timeout = 5000)]
        public void TestInterlockedDecrement()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                entry.Value = 1;

                Task task1 = Task.Run(() =>
                {
                    Interlocked.Exchange(ref entry.Value, 1);
                    Interlocked.Decrement(ref entry.Value);
                });

                Task task2 = Task.Run(() =>
                {
                    Interlocked.Exchange(ref entry.Value, 1);
                    Interlocked.Decrement(ref entry.Value);
                });

                await task1;
                await task2;
                Specification.Assert(entry.Value == 0, $"Value is {entry.Value} instead of 0.");
            },
            configuration: GetConfiguration().WithTestingIterations(500),
            replay: true,
            expectedError: "Value is -1 instead of 0.");
        }
    }
}
