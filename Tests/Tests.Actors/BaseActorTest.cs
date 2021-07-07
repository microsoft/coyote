// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.Tests
{
    public abstract class BaseActorTest : BaseTest
    {
        public BaseActorTest(ITestOutputHelper output)
            : base(output)
        {
        }

        protected class SharedEntry
        {
            public volatile int Value = 0;

            public async Task<int> GetWriteResultAsync(int value)
            {
                this.Value = value;
                await Task.CompletedTask;
                return this.Value;
            }

            public async Task<int> GetWriteResultWithDelayAsync(int value)
            {
                this.Value = value;
                await Task.Delay(5);
                return this.Value;
            }
        }

        protected async Task WaitAsync(Task task, int millisecondsDelay = 5000)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                await Task.WhenAny(task, Task.Delay(GetErrorWaitingTimeout(millisecondsDelay)));
            }
            else
            {
                // The TestEngine will throw a Deadlock exception if this task can't possibly complete.
                await task;
            }

            if (task.IsFaulted)
            {
                // unwrap the AggregateException so unit tests can more easily
                // Assert.Throws to match a more specific inner exception.
                throw task.Exception.InnerException;
            }

            Assert.True(task.IsCompleted);
        }

        protected async Task<TResult> GetResultAsync<TResult>(TaskCompletionSource<TResult> tcs, int millisecondsDelay = 5000)
        {
            return await this.GetResultAsync(tcs.Task, millisecondsDelay);
        }

        protected async Task<TResult> GetResultAsync<TResult>(Task<TResult> task, int millisecondsDelay = 5000)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                await Task.WhenAny(task, Task.Delay(GetErrorWaitingTimeout(millisecondsDelay)));
            }
            else
            {
                // The TestEngine will throw a Deadlock exception if this task can't possibly complete.
                await task;
            }

            if (task.IsFaulted)
            {
                // unwrap the AggregateException so unit tests can more easily
                // Assert.Throws to match a more specific inner exception.
                throw task.Exception.InnerException;
            }

            Assert.True(task.IsCompleted, string.Format("Task timed out after '{0}' milliseconds", millisecondsDelay));
            return await task;
        }
    }
}
