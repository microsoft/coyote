// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;
using TCS = Microsoft.Coyote.Tasks.TaskCompletionSource<int>;

namespace Microsoft.Coyote.Production.Tests.Tasks
{
    public class TaskCompletionSourceTests : BaseProductionTest
    {
        public TaskCompletionSourceTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public async Task TestSetResult()
        {
            var tcs = TCS.Create();
            tcs.SetResult(3);
            int result = await tcs.Task;
            Assert.Equal(TaskStatus.RanToCompletion, tcs.Task.Status);
            Assert.Equal(3, result);
        }

        [Fact(Timeout = 5000)]
        public async Task TestTrySetResult()
        {
            var tcs = TCS.Create();
            tcs.SetResult(3);
            bool check = tcs.TrySetResult(5);
            int result = await tcs.Task;
            Assert.False(check);
            Assert.Equal(TaskStatus.RanToCompletion, tcs.Task.Status);
            Assert.Equal(3, result);
        }

        [Fact(Timeout = 5000)]
        public async Task TestAsynchronousSetResult()
        {
            var tcs = TCS.Create();
            var task = ControlledTask.Run(async () =>
            {
                return await tcs.Task;
            });

            tcs.SetResult(3);
            int result = await task;
            Assert.Equal(TaskStatus.RanToCompletion, tcs.Task.Status);
            Assert.Equal(3, result);
        }

        [Fact(Timeout = 5000)]
        public async Task TestAsynchronousSetResultTask()
        {
            var tcs = TCS.Create();
            var task1 = ControlledTask.Run(async () =>
            {
                return await tcs.Task;
            });

            var task2 = ControlledTask.Run(() =>
            {
                tcs.SetResult(3);
            });

            int result = await task1;
            Assert.Equal(TaskStatus.RanToCompletion, tcs.Task.Status);
            Assert.Equal(3, result);
        }

        [Fact(Timeout = 5000)]
        public async Task TestAsynchronousSetResultWithTwoAwaiters()
        {
            var tcs = TCS.Create();
            var task1 = ControlledTask.Run(async () =>
            {
                return await tcs.Task;
            });

            var task2 = ControlledTask.Run(async () =>
            {
                return await tcs.Task;
            });

            tcs.SetResult(3);
            await ControlledTask.WhenAll(task1, task2);
            int result1 = task1.Result;
            int result2 = task2.Result;
            Assert.Equal(TaskStatus.RanToCompletion, tcs.Task.Status);
            Assert.Equal(3, result1);
            Assert.Equal(3, result2);
        }

        [Fact(Timeout = 5000)]
        public async Task TestSetCanceled()
        {
            var tcs = TCS.Create();
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

            Assert.IsType<TaskCanceledException>(exception);
            Assert.Equal(TaskStatus.Canceled, tcs.Task.Status);
            Assert.Equal(default, result);
        }

        [Fact(Timeout = 5000)]
        public async Task TestTrySetCanceled()
        {
            var tcs = TCS.Create();
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
            Assert.IsType<TaskCanceledException>(exception);
            Assert.Equal(TaskStatus.Canceled, tcs.Task.Status);
            Assert.Equal(default, result);
        }

        [Fact(Timeout = 5000)]
        public async Task TestAsynchronousSetCanceled()
        {
            var tcs = TCS.Create();
            var task = ControlledTask.Run(() =>
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

            Assert.IsType<TaskCanceledException>(exception);
            Assert.Equal(TaskStatus.Canceled, tcs.Task.Status);
            Assert.Equal(default, result);
        }

        [Fact(Timeout = 5000)]
        public async Task TestSetException()
        {
            var tcs = TCS.Create();
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
            Assert.Equal(TaskStatus.Faulted, tcs.Task.Status);
            Assert.Equal(default, result);
        }

        [Fact(Timeout = 5000)]
        public async Task TestTrySetException()
        {
            var tcs = TCS.Create();
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
            Assert.Equal(TaskStatus.Faulted, tcs.Task.Status);
            Assert.Equal(default, result);
        }

        [Fact(Timeout = 5000)]
        public async Task TestAsynchronousSetException()
        {
            var tcs = TCS.Create();
            var task = ControlledTask.Run(() =>
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
            Assert.Equal(TaskStatus.Faulted, tcs.Task.Status);
            Assert.Equal(default, result);
        }
    }
}
