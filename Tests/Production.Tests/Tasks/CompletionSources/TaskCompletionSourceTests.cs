// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Production.Tests.Tasks
{
    public class TaskCompletionSourceTests : BaseProductionTest
    {
        public TaskCompletionSourceTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestSetResult()
        {
            var tcs = TaskCompletionSource.Create<int>();
            tcs.SetResult(3);
            int result = await tcs.Task;
            Assert.Equal(SystemTasks.TaskStatus.RanToCompletion, tcs.Task.Status);
            Assert.Equal(3, result);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestTrySetResult()
        {
            var tcs = TaskCompletionSource.Create<int>();
            tcs.SetResult(3);
            bool check = tcs.TrySetResult(5);
            int result = await tcs.Task;
            Assert.False(check);
            Assert.Equal(SystemTasks.TaskStatus.RanToCompletion, tcs.Task.Status);
            Assert.Equal(3, result);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestAsynchronousSetResult()
        {
            var tcs = TaskCompletionSource.Create<int>();
            var task = Task.Run(async () =>
            {
                return await tcs.Task;
            });

            tcs.SetResult(3);
            int result = await task;
            Assert.Equal(SystemTasks.TaskStatus.RanToCompletion, tcs.Task.Status);
            Assert.Equal(3, result);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestAsynchronousSetResultTask()
        {
            var tcs = TaskCompletionSource.Create<int>();
            var task1 = Task.Run(async () =>
            {
                return await tcs.Task;
            });

            var task2 = Task.Run(() =>
            {
                tcs.SetResult(3);
            });

            int result = await task1;
            Assert.Equal(SystemTasks.TaskStatus.RanToCompletion, tcs.Task.Status);
            Assert.Equal(3, result);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestAsynchronousSetResultWithTwoAwaiters()
        {
            var tcs = TaskCompletionSource.Create<int>();
            var task1 = Task.Run(async () =>
            {
                return await tcs.Task;
            });

            var task2 = Task.Run(async () =>
            {
                return await tcs.Task;
            });

            tcs.SetResult(3);
            await Task.WhenAll(task1, task2);
            int result1 = task1.Result;
            int result2 = task2.Result;
            Assert.Equal(SystemTasks.TaskStatus.RanToCompletion, tcs.Task.Status);
            Assert.Equal(3, result1);
            Assert.Equal(3, result2);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestSetCanceled()
        {
            var tcs = TaskCompletionSource.Create<int>();
            tcs.SetCanceled();

            int result = default;
            Exception exception = null;
            try
            {
                result = await tcs.Task;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.IsType<SystemTasks.TaskCanceledException>(exception);
            Assert.Equal(SystemTasks.TaskStatus.Canceled, tcs.Task.Status);
            Assert.Equal(default, result);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestTrySetCanceled()
        {
            var tcs = TaskCompletionSource.Create<int>();
            tcs.SetCanceled();
            bool check = tcs.TrySetCanceled();

            int result = default;
            Exception exception = null;
            try
            {
                result = await tcs.Task;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.False(check);
            Assert.IsType<SystemTasks.TaskCanceledException>(exception);
            Assert.Equal(SystemTasks.TaskStatus.Canceled, tcs.Task.Status);
            Assert.Equal(default, result);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestAsynchronousSetCanceled()
        {
            var tcs = TaskCompletionSource.Create<int>();
            var task = Task.Run(() =>
            {
                tcs.SetCanceled();
            });

            int result = default;
            Exception exception = null;
            try
            {
                result = await tcs.Task;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.IsType<SystemTasks.TaskCanceledException>(exception);
            Assert.Equal(SystemTasks.TaskStatus.Canceled, tcs.Task.Status);
            Assert.Equal(default, result);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestSetException()
        {
            var tcs = TaskCompletionSource.Create<int>();
            tcs.SetException(new InvalidOperationException());

            int result = default;
            Exception exception = null;
            try
            {
                result = await tcs.Task;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal(SystemTasks.TaskStatus.Faulted, tcs.Task.Status);
            Assert.Equal(default, result);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestTrySetException()
        {
            var tcs = TaskCompletionSource.Create<int>();
            tcs.SetException(new InvalidOperationException());
            bool check = tcs.TrySetException(new NotImplementedException());

            int result = default;
            Exception exception = null;
            try
            {
                result = await tcs.Task;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.False(check);
            Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal(SystemTasks.TaskStatus.Faulted, tcs.Task.Status);
            Assert.Equal(default, result);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestAsynchronousSetException()
        {
            var tcs = TaskCompletionSource.Create<int>();
            var task = Task.Run(() =>
            {
                tcs.SetException(new InvalidOperationException());
            });

            int result = default;
            Exception exception = null;
            try
            {
                result = await tcs.Task;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal(SystemTasks.TaskStatus.Faulted, tcs.Task.Status);
            Assert.Equal(default, result);
        }
    }
}
