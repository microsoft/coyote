// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Production.Tests.Tasks
{
    public class TaskExceptionTests : BaseProductionTest
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
        public async SystemTasks.Task TestNoSynchronousTaskExceptionStatus()
        {
            SharedEntry entry = new SharedEntry();
            var task = WriteAsync(entry, 5);
            await task;

            Assert.Equal(SystemTasks.TaskStatus.RanToCompletion, task.Status);
            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestNoAsynchronousTaskExceptionStatus()
        {
            SharedEntry entry = new SharedEntry();
            var task = WriteWithDelayAsync(entry, 5);
            await task;

            Assert.Equal(SystemTasks.TaskStatus.RanToCompletion, task.Status);
            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestNoParallelSynchronousTaskExceptionStatus()
        {
            SharedEntry entry = new SharedEntry();
            var task = Task.Run(() =>
            {
                entry.Value = 5;
            });

            await task;

            Assert.Equal(SystemTasks.TaskStatus.RanToCompletion, task.Status);
            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestNoParallelAsynchronousTaskExceptionStatus()
        {
            SharedEntry entry = new SharedEntry();
            var task = Task.Run(async () =>
            {
                entry.Value = 5;
                await Task.Delay(1);
            });
            await task;

            Assert.Equal(SystemTasks.TaskStatus.RanToCompletion, task.Status);
            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestNoParallelFuncTaskExceptionStatus()
        {
            SharedEntry entry = new SharedEntry();
            async Task func()
            {
                entry.Value = 5;
                await Task.Delay(1);
            }

            var task = Task.Run(func);
            await task;

            Assert.Equal(SystemTasks.TaskStatus.RanToCompletion, task.Status);
            Assert.Equal(5, entry.Value);
        }

        private static async Task WriteWithExceptionAsync(SharedEntry entry, int value)
        {
            await Task.CompletedTask;
            entry.Value = value;
            throw new InvalidOperationException();
        }

        private static async Task WriteWithDelayedExceptionAsync(SharedEntry entry, int value)
        {
            await Task.Delay(1);
            entry.Value = value;
            throw new InvalidOperationException();
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestSynchronousTaskExceptionStatus()
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

            Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal(SystemTasks.TaskStatus.Faulted, task.Status);
            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestAsynchronousTaskExceptionStatus()
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

            Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal(SystemTasks.TaskStatus.Faulted, task.Status);
            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestParallelSynchronousTaskExceptionStatus()
        {
            SharedEntry entry = new SharedEntry();
            var task = Task.Run(() =>
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

            Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal(SystemTasks.TaskStatus.Faulted, task.Status);
            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestParallelAsynchronousTaskExceptionStatus()
        {
            SharedEntry entry = new SharedEntry();
            var task = Task.Run(async () =>
            {
                entry.Value = 5;
                await Task.Delay(1);
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

            Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal(SystemTasks.TaskStatus.Faulted, task.Status);
            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestParallelFuncTaskExceptionStatus()
        {
            SharedEntry entry = new SharedEntry();
            async Task func()
            {
                entry.Value = 5;
                await Task.Delay(1);
                throw new InvalidOperationException();
            }

            var task = Task.Run(func);

            Exception exception = null;
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal(SystemTasks.TaskStatus.Faulted, task.Status);
            Assert.Equal(5, entry.Value);
        }
    }
}
