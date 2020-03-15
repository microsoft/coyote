// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Production.Tests.Tasks
{
    public class SemaphoreTests : BaseProductionTest
    {
        public SemaphoreTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public async Task TestWaitReleaseAsyncWithOneMaxRequest()
        {
            Semaphore semaphore = Semaphore.Create(1, 1);
            Assert.Equal(1, semaphore.CurrentCount);
            await semaphore.WaitAsync();
            Assert.Equal(0, semaphore.CurrentCount);
            semaphore.Release();
            Assert.Equal(1, semaphore.CurrentCount);
            semaphore.Dispose();
        }

        [Fact(Timeout = 5000)]
        public async Task TestWaitReleaseAsyncWithTwoMaxRequests()
        {
            Semaphore semaphore = Semaphore.Create(2, 2);
            Assert.Equal(2, semaphore.CurrentCount);
            await semaphore.WaitAsync();
            Assert.Equal(1, semaphore.CurrentCount);
            await semaphore.WaitAsync();
            Assert.Equal(0, semaphore.CurrentCount);
            semaphore.Release();
            Assert.Equal(1, semaphore.CurrentCount);
            semaphore.Release();
            Assert.Equal(2, semaphore.CurrentCount);
            semaphore.Dispose();
        }

        [Fact(Timeout = 5000)]
        public async Task TestWaitReleaseAsyncWithOneInitialAndTwoMaxRequests()
        {
            Semaphore semaphore = Semaphore.Create(1, 2);
            Assert.Equal(1, semaphore.CurrentCount);
            await semaphore.WaitAsync();
            Assert.Equal(0, semaphore.CurrentCount);
            semaphore.Release();
            Assert.Equal(1, semaphore.CurrentCount);
            semaphore.Release();
            Assert.Equal(2, semaphore.CurrentCount);
            semaphore.Dispose();
        }

        [Fact(Timeout = 5000)]
        public async Task TestSynchronizeTwoAsynchronousTasksWithOneMaxRequest()
        {
            SharedEntry entry = new SharedEntry();
            Semaphore semaphore = Semaphore.Create(1, 1);

            async ControlledTask WriteAsync(int value)
            {
                await semaphore.WaitAsync();
                entry.Value = value;
                semaphore.Release();
            }

            ControlledTask task1 = WriteAsync(3);
            ControlledTask task2 = WriteAsync(5);
            await ControlledTask.WhenAll(task1, task2);

            Assert.True(task1.IsCompleted && task2.IsCompleted && semaphore.CurrentCount == 1);
            Assert.True(entry.Value == 5, $"Value is '{entry.Value}' instead of 5.");
        }

        [Fact(Timeout = 5000)]
        public async Task TestSynchronizeTwoAsynchronousTasksWithYieldAndOneMaxRequest()
        {
            SharedEntry entry = new SharedEntry();
            Semaphore semaphore = Semaphore.Create(1, 1);

            async ControlledTask WriteAsync(int value)
            {
                await semaphore.WaitAsync();
                entry.Value = value;
                await ControlledTask.Yield();
                Assert.True(entry.Value == value, $"Value is '{entry.Value}' instead of '{value}'.");
                semaphore.Release();
            }

            ControlledTask task1 = WriteAsync(3);
            ControlledTask task2 = WriteAsync(5);
            await ControlledTask.WhenAll(task1, task2);

            Assert.True(task1.IsCompleted && task2.IsCompleted && semaphore.CurrentCount == 1);
        }
    }
}
