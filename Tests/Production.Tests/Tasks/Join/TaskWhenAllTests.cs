// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
#if BINARY_REWRITE
using System.Threading.Tasks;
#else
using Microsoft.Coyote.Tasks;
#endif
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

#if BINARY_REWRITE
namespace Microsoft.Coyote.BinaryRewriting.Tests.Tasks
#else
namespace Microsoft.Coyote.Production.Tests.Tasks
#endif
{
    public class TaskWhenAllTests : BaseProductionTest
    {
        public TaskWhenAllTests(ITestOutputHelper output)
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

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithTwoSynchronousTasks()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task task1 = WriteAsync(entry, 5);
                Task task2 = WriteAsync(entry, 3);
                await Task.WhenAll(task1, task2);
                if (this.IsSystematicTest)
                {
                    Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
                }
                else
                {
                    // production version of this test we don't know which order the tasks execute.
                    Specification.Assert(entry.Value == 5 || entry.Value == 3, "Value is {0} instead of 5.", entry.Value);
                }
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithTwoAsynchronousTasks()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task task1 = WriteWithDelayAsync(entry, 3);
                Task task2 = WriteWithDelayAsync(entry, 5);
                await Task.WhenAll(task1, task2);
                if (this.IsSystematicTest)
                {
                    Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
                }
                else
                {
                    // production version of this test we don't know which order the tasks execute.
                    Specification.Assert(entry.Value == 5 || entry.Value == 3, "Value is {0} instead of 5.", entry.Value);
                }
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithTwoParallelTasks()
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

                await Task.WhenAll(task1, task2);

                if (this.IsSystematicTest)
                {
                    Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
                }
                else
                {
                    // production version of this test we don't know which order the tasks execute.
                    Specification.Assert(entry.Value == 5 || entry.Value == 3, "Value is {0} instead of 5.", entry.Value);
                }
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithTwoSynchronousTaskWithResults()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task<int> task1 = entry.GetWriteResultAsync(5);
                Task<int> task2 = entry.GetWriteResultAsync(3);
                int[] results = await Task.WhenAll(task1, task2);
                Specification.Assert(results.Length == 2, "Result count is '{0}' instead of 2.", results.Length);
                if (this.IsSystematicTest)
                {
                    Specification.Assert(results[0] == 5 && results[1] == 3, "Found unexpected value.");
                    Specification.Assert(results[0] == results[1], "Results are not equal.");
                }
                else
                {
                    // production version of this test we don't know which order the tasks execute.
                    Specification.Assert((results[0] == 5 && results[1] == 3) ||
                        (results[0] == 3 && results[1] == 5), "Found unexpected values.");
                }
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Results are not equal.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithTwoAsynchronousTaskWithResults()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task<int> task1 = entry.GetWriteResultWithDelayAsync(5);
                Task<int> task2 = entry.GetWriteResultWithDelayAsync(3);
                int[] results = await Task.WhenAll(task1, task2);
                Specification.Assert(results.Length == 2, "Result count is '{0}' instead of 2.", results.Length);
                if (this.IsSystematicTest)
                {
                    Specification.Assert(results[0] == 5 && results[1] == 3, "Found unexpected value.");
                }
                else
                {
                    Specification.Assert((results[0] == 5 && results[1] == 3) ||
                        (results[0] == 3 && results[1] == 5), "Found unexpected value.");
                }
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Found unexpected value.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithTwoParallelSynchronousTaskWithResults()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                Task<int> task1 = Task.Run(async () =>
                {
                    return await entry.GetWriteResultAsync(5);
                });

                Task<int> task2 = Task.Run(async () =>
                {
                    return await entry.GetWriteResultAsync(3);
                });

                int[] results = await Task.WhenAll(task1, task2);

                Specification.Assert(results.Length == 2, "Result count is '{0}' instead of 2.", results.Length);
                if (this.IsSystematicTest)
                {
                    Specification.Assert(results[0] == 5, $"The first task result is {results[0]} instead of 5.");
                    Specification.Assert(results[1] == 3, $"The second task result is {results[1]} instead of 3.");
                }
                else
                {
                    // production version of this test we don't know which order the tasks execute and
                    // with statement level interleaving we can even end up with duplicate values.
                }

                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithTwoParallelAsynchronousTaskWithResults()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                Task<int> task1 = Task.Run(async () =>
                {
                    return await entry.GetWriteResultWithDelayAsync(5);
                });

                Task<int> task2 = Task.Run(async () =>
                {
                    return await entry.GetWriteResultWithDelayAsync(3);
                });

                int[] results = await Task.WhenAll(task1, task2);

                Specification.Assert(results.Length == 2, "Result count is '{0}' instead of 2.", results.Length);
                if (this.IsSystematicTest)
                {
                    Specification.Assert(results[0] == 5 && results[1] == 3, "Found unexpected value.");
                }
                else
                {
                    // production version of this test we don't know which order the tasks execute.
                    Specification.Assert((results[0] == 5 && results[1] == 3) ||
                        (results[0] == 3 && results[1] == 5), "Found unexpected value.");
                }
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Found unexpected value.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithException()
        {
            string expected = "Value is 3 instead of 5.";
            if (!this.IsSystematicTest)
            {
                // bugbug: production WhenAll has different behavior, it does not aggregate the exceptions.
                expected = "Operation is not valid due to the current state of the object.";
            }

            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                Task task1 = Task.Run(async () =>
                {
                    await WriteAsync(entry, 3);
                    throw new InvalidOperationException();
                });

                Task task2 = Task.Run(async () =>
                {
                    await WriteAsync(entry, 5);
                    throw new NotSupportedException();
                });

                try
                {
                    await Task.WhenAll(task1, task2);
                }
                catch (AggregateException ex)
                {
                    Specification.Assert(ex.InnerExceptions.Count == 2, "Expected two exceptions.");
                    Specification.Assert(ex.InnerExceptions[0].InnerException.GetType() == typeof(InvalidOperationException),
                        "The first exception is not of the expected type.");
                    Specification.Assert(ex.InnerExceptions[1].InnerException.GetType() == typeof(NotSupportedException),
                        "The second exception is not of the expected type.");
                }

                Specification.Assert(task1.IsFaulted && task2.IsFaulted, "One task has not faulted.");
                Specification.Assert(task1.Exception.InnerException.GetType() == typeof(InvalidOperationException),
                    "The first task exception is not of the expected type.");
                Specification.Assert(task2.Exception.InnerException.GetType() == typeof(NotSupportedException),
                    "The second task exception is not of the expected type.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: expected,
            replay: true);
        }
    }
}
