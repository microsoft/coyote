// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Production.Tests.Tasks
{
    public class AsyncLockTests : BaseProductionTest
    {
        public AsyncLockTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestAcquireRelease()
        {
            AsyncLock mutex = AsyncLock.Create();
            Assert.True(!mutex.IsAcquired);
            var releaser = await mutex.AcquireAsync();
            Assert.True(mutex.IsAcquired);
            releaser.Dispose();
            Assert.True(!mutex.IsAcquired);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestSynchronizeTwoAsynchronousTasks()
        {
            SharedEntry entry = new SharedEntry();
            AsyncLock mutex = AsyncLock.Create();

            async Task WriteAsync(int value)
            {
                using (await mutex.AcquireAsync())
                {
                    entry.Value = value;
                }
            }

            Task task1 = WriteAsync(3);
            Task task2 = WriteAsync(5);
            await Task.WhenAll(task1, task2);

            Assert.True(task1.IsCompleted && task2.IsCompleted && !mutex.IsAcquired);
            Assert.True(entry.Value == 5, $"Value is '{entry.Value}' instead of 5.");
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestSynchronizeTwoAsynchronousTasksWithYield()
        {
            SharedEntry entry = new SharedEntry();
            AsyncLock mutex = AsyncLock.Create();

            async Task WriteAsync(int value)
            {
                using (await mutex.AcquireAsync())
                {
                    entry.Value = value;
                    await Task.Yield();
                    Assert.True(entry.Value == value, $"Value is '{entry.Value}' instead of '{value}'.");
                }
            }

            Task task1 = WriteAsync(3);
            Task task2 = WriteAsync(5);
            await Task.WhenAll(task1, task2);

            Assert.True(task1.IsCompleted && task2.IsCompleted && !mutex.IsAcquired);
        }
    }
}
