// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;
using TaskStatus = System.Threading.Tasks.TaskStatus;

namespace Microsoft.Coyote.BinaryRewriting.Tests.Tasks
{
    public class TaskExceptionTests : BaseRewritingTest
    {
        public TaskExceptionTests(ITestOutputHelper output)
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
        public void TestNoSynchronousTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = WriteAsync(entry, 5);
                await task;

                Specification.Assert(task.Status == TaskStatus.RanToCompletion,
                    $"Status is '{task.Status}' instead of 'RanToCompletion'.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestNoAsynchronousTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = WriteWithDelayAsync(entry, 5);
                await task;

                Specification.Assert(task.Status == TaskStatus.RanToCompletion,
                    $"Status is '{task.Status}' instead of 'RanToCompletion'.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestNoParallelSynchronousTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = Task.Run(() =>
                {
                    entry.Value = 5;
                });

                await task;

                Specification.Assert(task.Status == TaskStatus.RanToCompletion,
                    $"Status is '{task.Status}' instead of 'RanToCompletion'.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestNoParallelAsynchronousTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = Task.Run(async () =>
                {
                    entry.Value = 5;
                    await Task.Delay(1);
                });
                await task;

                Specification.Assert(task.Status == TaskStatus.RanToCompletion,
                    $"Status is '{task.Status}' instead of 'RanToCompletion'.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestNoParallelFuncTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                async Task Func()
                {
                    entry.Value = 5;
                    await Task.Delay(1);
                }

                var task = Task.Run(Func);
                await task;

                Specification.Assert(task.Status == TaskStatus.RanToCompletion,
                    $"Status is '{task.Status}' instead of 'RanToCompletion'.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        private static async Task WriteWithExceptionAsync(SharedEntry entry, int value)
        {
            await Task.CompletedTask;
            entry.Value = value;
            ThrowException<InvalidOperationException>();
        }

        private static async Task WriteWithDelayedExceptionAsync(SharedEntry entry, int value)
        {
            await Task.Delay(1);
            entry.Value = value;
            ThrowException<InvalidOperationException>();
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronousTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = WriteWithExceptionAsync(entry, 5);

                Exception exception = null;
                try
                {
                    await task;
                }
                catch (Exception ex) when (!(ex is ExecutionCanceledException))
                {
                    exception = ex;
                }

                Specification.Assert(exception is InvalidOperationException,
                    $"Exception is not '{typeof(InvalidOperationException)}'.");
                Specification.Assert(task.Status == TaskStatus.Faulted,
                    $"Status is '{task.Status}' instead of 'Faulted'.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAsynchronousTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = WriteWithDelayedExceptionAsync(entry, 5);

                Exception exception = null;
                try
                {
                    await task;
                }
                catch (Exception ex) when (!(ex is ExecutionCanceledException))
                {
                    exception = ex;
                }

                Specification.Assert(exception is InvalidOperationException,
                    $"Exception is not '{typeof(InvalidOperationException)}'.");
                Specification.Assert(task.Status == TaskStatus.Faulted,
                    $"Status is '{task.Status}' instead of 'Faulted'.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestParallelSynchronousTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = Task.Run(() =>
                {
                    entry.Value = 5;
                    ThrowException<InvalidOperationException>();
                });

                Exception exception = null;
                try
                {
                    await task;
                }
                catch (Exception ex) when (!(ex is ExecutionCanceledException))
                {
                    exception = ex;
                }

                Specification.Assert(exception is InvalidOperationException,
                    $"Exception is not '{typeof(InvalidOperationException)}'.");
                Specification.Assert(task.Status == TaskStatus.Faulted,
                    $"Status is '{task.Status}' instead of 'Faulted'.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestParallelAsynchronousTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = Task.Run(async () =>
                {
                    entry.Value = 5;
                    await Task.Delay(1);
                    ThrowException<InvalidOperationException>();
                });

                Exception exception = null;
                try
                {
                    await task;
                }
                catch (Exception ex) when (!(ex is ExecutionCanceledException))
                {
                    exception = ex;
                }

                Specification.Assert(exception is InvalidOperationException,
                    $"Exception is not '{typeof(InvalidOperationException)}'.");
                Specification.Assert(task.Status == TaskStatus.Faulted,
                    $"Status is '{task.Status}' instead of 'Faulted'.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestParallelFuncTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                async Task Func()
                {
                    entry.Value = 5;
                    await Task.Delay(1);
                    ThrowException<InvalidOperationException>();
                }

                var task = Task.Run(Func);

                Exception exception = null;
                try
                {
                    await task;
                }
                catch (Exception ex) when (!(ex is ExecutionCanceledException))
                {
                    exception = ex;
                }

                Specification.Assert(exception is InvalidOperationException,
                    $"Exception is not '{typeof(InvalidOperationException)}'.");
                Specification.Assert(task.Status == TaskStatus.Faulted,
                    $"Status is '{task.Status}' instead of 'Faulted'.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }
    }
}
