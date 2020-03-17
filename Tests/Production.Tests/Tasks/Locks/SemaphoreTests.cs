// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Production.Tests.Tasks
{
    public class SemaphoreTests : BaseProductionTest
    {
        public SemaphoreTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestWaitReleaseAsyncWithOneMaxRequest()
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
        public async SystemTasks.Task TestWaitReleaseAsyncWithTwoMaxRequests()
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
        public async SystemTasks.Task TestWaitReleaseAsyncWithOneInitialAndTwoMaxRequests()
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
        public async SystemTasks.Task TestSynchronizeTwoAsynchronousTasksWithOneMaxRequest()
        {
            SharedEntry entry = new SharedEntry();
            Semaphore semaphore = Semaphore.Create(1, 1);

            async Task WriteAsync(int value)
            {
                await semaphore.WaitAsync();
                entry.Value = value;
                semaphore.Release();
            }

            Task task1 = WriteAsync(3);
            Task task2 = WriteAsync(5);
            await Task.WhenAll(task1, task2);

            Assert.True(task1.IsCompleted && task2.IsCompleted && semaphore.CurrentCount == 1);
            Assert.True(entry.Value == 5, $"Value is '{entry.Value}' instead of 5.");
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestSynchronizeTwoAsynchronousTasksWithYieldAndOneMaxRequest()
        {
            SharedEntry entry = new SharedEntry();
            Semaphore semaphore = Semaphore.Create(1, 1);

            async Task WriteAsync(int value)
            {
                await semaphore.WaitAsync();
                entry.Value = value;
                await Task.Yield();
                Assert.True(entry.Value == value, $"Value is '{entry.Value}' instead of '{value}'.");
                semaphore.Release();
            }

            Task task1 = WriteAsync(3);
            Task task2 = WriteAsync(5);
            await Task.WhenAll(task1, task2);

            Assert.True(task1.IsCompleted && task2.IsCompleted && semaphore.CurrentCount == 1);
        }
    }
}
