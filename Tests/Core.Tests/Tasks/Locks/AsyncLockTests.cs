// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests.Tasks
{
    public class AsyncLockTests : BaseTest
    {
        public AsyncLockTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public async Task TestAcquireRelease()
        {
            AsyncLock mutex = AsyncLock.Create();
            Assert.True(!mutex.IsAcquired);
            var releaser = await mutex.AcquireAsync();
            Assert.True(mutex.IsAcquired);
            releaser.Dispose();
            Assert.True(!mutex.IsAcquired);
        }

        [Fact(Timeout = 5000)]
        public async Task TestSynchronizeTwoAsynchronousTasks()
        {
            SharedEntry entry = new SharedEntry();
            AsyncLock mutex = AsyncLock.Create();

            async ControlledTask WriteAsync(int value)
            {
                using (await mutex.AcquireAsync())
                {
                    entry.Value = value;
                }
            }

            ControlledTask task1 = WriteAsync(3);
            ControlledTask task2 = WriteAsync(5);
            await ControlledTask.WhenAll(task1, task2);

            Assert.True(task1.IsCompleted && task2.IsCompleted && !mutex.IsAcquired);
            Assert.True(entry.Value == 5, $"Value is '{entry.Value}' instead of 5.");
        }

        [Fact(Timeout = 5000)]
        public async Task TestSynchronizeTwoAsynchronousTasksWithYield()
        {
            SharedEntry entry = new SharedEntry();
            AsyncLock mutex = AsyncLock.Create();

            async ControlledTask WriteAsync(int value)
            {
                using (await mutex.AcquireAsync())
                {
                    entry.Value = value;
                    await ControlledTask.Yield();
                    Assert.True(entry.Value == value, $"Value is '{entry.Value}' instead of '{value}'.");
                }
            }

            ControlledTask task1 = WriteAsync(3);
            ControlledTask task2 = WriteAsync(5);
            await ControlledTask.WhenAll(task1, task2);

            Assert.True(task1.IsCompleted && task2.IsCompleted && !mutex.IsAcquired);
        }
    }
}
