// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class TaskResultTests : BaseBugFindingTest
    {
        public TaskResultTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method", Justification = "This test covers the rewriting of Task-using code. The test does not actually execute the tasks as a normal xUnit test would, so the flagged operation is intentional.")]
        [Fact(Timeout = 5000)]
        public void TestParallelTaskResultBeforeWrite()
        {
            this.Test(() =>
            {
                SharedEntry entry = new SharedEntry();

                Task<int> task = Task.Run(() =>
                {
                    entry.Value = 3;
                    return 7;
                });

                int result = task.Result;
                entry.Value = 5;

                Specification.Assert(result == 7, "Result is {0} instead of 7.", result);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method", Justification = "This test covers the rewriting of Task-using code. The test does not actually execute the tasks as a normal xUnit test would, so the flagged operation is intentional.")]
        [Fact(Timeout = 5000)]
        public void TestParallelTaskResultAfterWrite()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();

                Task<int> task = Task.Run(() =>
                {
                    entry.Value = 3;
                    return 7;
                });

                entry.Value = 5;
                int result = task.Result;

                Specification.Assert(result == 7, "Result is {0} instead of 7.", result);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private static async Task WriteAsync(SharedEntry entry, int value)
        {
            await Task.CompletedTask;
            entry.Value = value;
        }

        [SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method", Justification = "This test covers the rewriting of Task-using code. The test does not actually execute the tasks as a normal xUnit test would, so the flagged operation is intentional.")]
        [Fact(Timeout = 5000)]
        public void TestParallelTaskResultWithSynchronousInvocationAfterWrite()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();

                Task<int> task = Task.Run(async () =>
                {
                    await WriteAsync(entry, 3);
                    return 7;
                });

                entry.Value = 5;
                int result = task.Result;

                Specification.Assert(result == 7, "Result is {0} instead of 7.", result);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private static async Task WriteWithDelayAsync(SharedEntry entry, int value)
        {
            await Task.Delay(1);
            entry.Value = value;
        }

        [SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method", Justification = "This test covers the rewriting of Task-using code. The test does not actually execute the tasks as a normal xUnit test would, so the flagged operation is intentional.")]
        [Fact(Timeout = 5000)]
        public void TestParallelTaskResultWithAsynchronousInvocationAfterWrite()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();

                Task<int> task = Task.Run(async () =>
                {
                    await WriteWithDelayAsync(entry, 3);
                    return 7;
                });

                entry.Value = 5;
                int result = task.Result;

                Specification.Assert(result == 7, "Result is {0} instead of 7.", result);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method", Justification = "This test covers the rewriting of Task-using code. The test does not actually execute the tasks as a normal xUnit test would, so the flagged operation is intentional.")]
        [Fact(Timeout = 5000)]
        public void TestTaskResultWithException()
        {
            this.TestWithError(() =>
            {
                var task = Task.Run(() =>
                {
                    ThrowException<InvalidOperationException>();
                    return 3;
                });

                AggregateException exception = null;

                try
                {
                    _ = task.Result;
                }
                catch (Exception ex)
                {
                    exception = ex as AggregateException;
                }

                Specification.Assert(exception != null, "Expected an `AggregateException`.");

                Exception innerException = exception.InnerExceptions.FirstOrDefault();
                Specification.Assert(innerException is InvalidOperationException,
                    $"The inner exception is `{innerException.GetType()}`.");

                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(1),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method", Justification = "This test covers the rewriting of Task-using code. The test does not actually execute the tasks as a normal xUnit test would, so the flagged operation is intentional.")]
        [Fact(Timeout = 5000)]
        public void TestAsyncTaskResultWithException()
        {
            this.TestWithError(() =>
            {
                var task = Task.Run(async () =>
                {
                    await Task.Delay(1);
                    ThrowException<InvalidOperationException>();
                    return 3;
                });

                AggregateException exception = null;

                try
                {
                    _ = task.Result;
                }
                catch (Exception ex)
                {
                    exception = ex as AggregateException;
                }

                Specification.Assert(exception != null, "Expected an `AggregateException`.");

                Exception innerException = exception.InnerExceptions.FirstOrDefault();
                Specification.Assert(innerException is InvalidOperationException,
                    $"The inner exception is `{innerException.GetType()}`.");

                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(1),
            expectedError: "Reached test assertion.",
            replay: true);
        }
    }
}
