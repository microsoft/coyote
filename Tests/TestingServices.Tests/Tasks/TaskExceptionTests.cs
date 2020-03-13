// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Tasks
{
    public class TaskExceptionTests : BaseTest
    {
        public TaskExceptionTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private static async ControlledTask WriteAsync(SharedEntry entry, int value)
        {
            await ControlledTask.CompletedTask;
            entry.Value = value;
        }

        private static async ControlledTask WriteWithDelayAsync(SharedEntry entry, int value)
        {
            await ControlledTask.Delay(1);
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
            configuration: GetConfiguration().WithNumberOfIterations(200));
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
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestNoParallelSynchronousTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = ControlledTask.Run(() =>
                {
                    entry.Value = 5;
                });

                await task;

                Specification.Assert(task.Status == TaskStatus.RanToCompletion,
                    $"Status is '{task.Status}' instead of 'RanToCompletion'.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestNoParallelAsynchronousTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = ControlledTask.Run(async () =>
                {
                    entry.Value = 5;
                    await ControlledTask.Delay(1);
                });
                await task;

                Specification.Assert(task.Status == TaskStatus.RanToCompletion,
                    $"Status is '{task.Status}' instead of 'RanToCompletion'.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestNoParallelFuncTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                async ControlledTask func()
                {
                    entry.Value = 5;
                    await ControlledTask.Delay(1);
                }

                var task = ControlledTask.Run(func);
                await task;

                Specification.Assert(task.Status == TaskStatus.RanToCompletion,
                    $"Status is '{task.Status}' instead of 'RanToCompletion'.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        private static async ControlledTask WriteWithExceptionAsync(SharedEntry entry, int value)
        {
            await ControlledTask.CompletedTask;
            entry.Value = value;
            throw new InvalidOperationException();
        }

        private static async ControlledTask WriteWithDelayedExceptionAsync(SharedEntry entry, int value)
        {
            await ControlledTask.Delay(1);
            entry.Value = value;
            throw new InvalidOperationException();
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
                catch (Exception ex)
                {
                    exception = ex;
                }

                Specification.Assert(exception is InvalidOperationException,
                    $"Exception is not '{typeof(InvalidOperationException)}'.");
                Specification.Assert(task.Status == TaskStatus.Faulted,
                    $"Status is '{task.Status}' instead of 'Faulted'.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
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
                catch (Exception ex)
                {
                    exception = ex;
                }

                Specification.Assert(exception is InvalidOperationException,
                    $"Exception is not '{typeof(InvalidOperationException)}'.");
                Specification.Assert(task.Status == TaskStatus.Faulted,
                    $"Status is '{task.Status}' instead of 'Faulted'.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestParallelSynchronousTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = ControlledTask.Run(() =>
                {
                    entry.Value = 5;
                    throw new InvalidOperationException();
                });

                Exception exception = null;
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                Specification.Assert(exception is InvalidOperationException,
                    $"Exception is not '{typeof(InvalidOperationException)}'.");
                Specification.Assert(task.Status == TaskStatus.Faulted,
                    $"Status is '{task.Status}' instead of 'Faulted'.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestParallelAsynchronousTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = ControlledTask.Run(async () =>
                {
                    entry.Value = 5;
                    await ControlledTask.Delay(1);
                    throw new InvalidOperationException();
                });

                Exception exception = null;
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                Specification.Assert(exception is InvalidOperationException,
                    $"Exception is not '{typeof(InvalidOperationException)}'.");
                Specification.Assert(task.Status == TaskStatus.Faulted,
                    $"Status is '{task.Status}' instead of 'Faulted'.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestParallelFuncTaskExceptionStatus()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                async ControlledTask func()
                {
                    entry.Value = 5;
                    await ControlledTask.Delay(1);
                    throw new InvalidOperationException();
                }

                var task = ControlledTask.Run(func);

                Exception exception = null;
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                Specification.Assert(exception is InvalidOperationException,
                    $"Exception is not '{typeof(InvalidOperationException)}'.");
                Specification.Assert(task.Status == TaskStatus.Faulted,
                    $"Status is '{task.Status}' instead of 'Faulted'.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }
    }
}
