// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
#if BINARY_REWRITE
using System.Threading.Tasks;
#else
using Microsoft.Coyote.Tasks;
#endif
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

#if BINARY_REWRITE
namespace Microsoft.Coyote.BinaryRewriting.Tests.Tasks
#else
namespace Microsoft.Coyote.Production.Tests.Tasks
#endif
{
    public class TaskInterleavingsTests : BaseProductionTest
    {
        public TaskInterleavingsTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private static async Task WriteAsync(SharedEntry entry, int value)
        {
            await Task.CompletedTask;
            entry.Value = value;
        }

        private static async Task WriteWithDelayAsync(SharedEntry entry, int value)
        {
            await Task.Delay(1);
            entry.Value = value;
        }

        private void AssertSharedEntryValue(SharedEntry entry, int expected, int other)
        {
            if (this.IsSystematicTest)
            {
                Specification.Assert(entry.Value == expected, "Value is {0} instead of {1}.", entry.Value, expected);
            }
            else
            {
                Specification.Assert(entry.Value == expected || entry.Value == other, "Unexpected value {0} in SharedEntry", entry.Value);
                Specification.Assert(false, "Value is {0} instead of {1}.", other, expected);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithOneSynchronousTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task task = WriteAsync(entry, 3);
                entry.Value = 5;
                await task;
                Specification.Assert(entry.Value == 5, "Value is {0} instead of {1}.", entry.Value, 5);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithOneAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                Task task = WriteWithDelayAsync(entry, 3);
                entry.Value = 5;
                await task;

                Specification.Assert(entry.Value == 5, "Value is {0} instead of {1}.", entry.Value, 5);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithOneParallelTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                Task task = Task.Run(async () =>
                {
                    await WriteAsync(entry, 3);
                });

                await WriteAsync(entry, 5);
                await task;
                this.AssertSharedEntryValue(entry, 5, 3);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithTwoSynchronousTasks()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();

                Task task1 = WriteAsync(entry, 3);
                Task task2 = WriteAsync(entry, 5);

                await task1;
                await task2;
                this.AssertSharedEntryValue(entry, 5, 3);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithTwoAsynchronousTasks()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                Task task1 = WriteWithDelayAsync(entry, 3);
                Task task2 = WriteWithDelayAsync(entry, 5);

                await task1;
                await task2;
                this.AssertSharedEntryValue(entry, 5, 3);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithTwoParallelTasks()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                Task task1 = Task.Run(async () =>
                {
                    await WriteAsync(entry, 3);
                });

                Task task2 = Task.Run(async () =>
                {
                    await WriteAsync(entry, 5);
                });

                await task1;
                await task2;
                this.AssertSharedEntryValue(entry, 5, 3);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsWithNestedParallelTasks()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                Task task1 = Task.Run(async () =>
                {
                    Task task2 = Task.Run(async () =>
                    {
                        await WriteAsync(entry, 5);
                    });

                    await WriteAsync(entry, 3);
                    await task2;
                });

                await task1;
                this.AssertSharedEntryValue(entry, 5, 3);
            },
            configuration: GetConfiguration().WithTestingIterations(500),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestExploreAllInterleavings()
        {
            if (!this.IsSystematicTest)
            {
                // production version cannot always find all combinations.
                return;
            }

            SortedSet<string> results = new SortedSet<string>();

            string success = "Explored interleavings.";
            this.TestWithError(async (runtime) =>
            {
                InMemoryLogger log = new InMemoryLogger();

                Task task1 = Task.Run(async () =>
                {
                    log.WriteLine(">foo");
                    await Task.Delay(runtime.RandomInteger(10));
                    log.WriteLine("<foo");
                });

                Task task2 = Task.Run(async () =>
                {
                    log.WriteLine(">bar");
                    await Task.Delay(runtime.RandomInteger(10));
                    log.WriteLine("<bar");
                });

                await Task.WhenAll(task1, task2);

                results.Add(log.ToString());
                Specification.Assert(results.Count < 6, success);
            },
            configuration: GetConfiguration().WithTestingIterations(1000),
            expectedError: success);

            string expected = @">bar
<bar
>foo
<foo

>bar
>foo
<bar
<foo

>bar
>foo
<foo
<bar

>foo
<foo
>bar
<bar

>foo
>bar
<bar
<foo

>foo
>bar
<foo
<bar
";
            expected = expected.NormalizeNewLines();

            string actual = string.Join("\n", results).NormalizeNewLines();
            Assert.Equal(expected, actual);
        }
    }
}
