// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class AsyncLockTests : BaseSystematicTest
    {
        public AsyncLockTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestAcquireRelease()
        {
            this.Test(async () =>
            {
                AsyncLock mutex = AsyncLock.Create();
                Specification.Assert(!mutex.IsAcquired, "Mutex is acquired.");
                var releaser = await mutex.AcquireAsync();
                Specification.Assert(mutex.IsAcquired, "Mutex is not acquired.");
                releaser.Dispose();
                Specification.Assert(!mutex.IsAcquired, "Mutex is acquired.");
            });
        }

        [Fact(Timeout = 5000)]
        public void TestAcquireTwice()
        {
            this.TestWithError(async () =>
            {
                AsyncLock mutex = AsyncLock.Create();
                await mutex.AcquireAsync();
                await Task.Run(async () =>
                {
                    await mutex.AcquireAsync();
                });
            },
            expectedError: "Deadlock detected. Task() is waiting for a task to complete, but no other " +
                "controlled tasks are enabled. Task() is waiting to acquire a resource that is already " +
                "acquired, but no other controlled tasks are enabled.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronizeTwoAsynchronousTasks()
        {
            this.Test(async () =>
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
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronizeTwoParallelTasks()
        {
            this.TestWithError(async () =>
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

                Task task1 = Task.Run(async () =>
                {
                    await WriteAsync(3);
                });

                Task task2 = Task.Run(async () =>
                {
                    await WriteAsync(5);
                });

                await Task.WhenAll(task1, task2);
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronizeTwoAsynchronousTasksWithYield()
        {
            this.Test(async () =>
            {
                AsyncLock mutex = AsyncLock.Create();

                SharedEntry entry = new SharedEntry();
                async Task WriteAsync(int value)
                {
                    using (await mutex.AcquireAsync())
                    {
                        entry.Value = value;
                        await Task.Yield();
                        Specification.Assert(entry.Value == value, "Value is '{0}' instead of '{1}'.", entry.Value, value);
                    }
                }

                Task task1 = WriteAsync(3);
                Task task2 = WriteAsync(5);
                await Task.WhenAll(task1, task2);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }
    }
}
