// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Threading;
using Microsoft.Coyote.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class ControlledLockTest : BaseTest
    {
        public ControlledLockTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestLockUnlock()
        {
            this.Test(async () =>
            {
                ControlledLock mutex = ControlledLock.Create();
                var releaser = await mutex.AcquireAsync();
                releaser.Dispose();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestLockTwice()
        {
            this.TestWithError(async () =>
            {
                ControlledLock mutex = ControlledLock.Create();
                await mutex.AcquireAsync();
                await mutex.AcquireAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Deadlock detected. 'ControlledTask()' is waiting to access a concurrent resource " +
                "that is acquired by another task, but no other controlled tasks are enabled.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronizeTwoAsynchronousTasks()
        {
            this.Test(async () =>
            {
                int entry = 0;
                ControlledLock mutex = ControlledLock.Create();

                async ControlledTask WriteAsync(int value)
                {
                    using (await mutex.AcquireAsync())
                    {
                        entry = value;
                    }
                }

                ControlledTask task1 = WriteAsync(3);
                ControlledTask task2 = WriteAsync(5);
                await ControlledTask.WhenAll(task1, task2);
                Specification.Assert(entry == 5, "Value is '{0}' instead of 5.", entry);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronizeTwoParallelTasks()
        {
            this.TestWithError(async () =>
            {
                int entry = 0;
                ControlledLock mutex = ControlledLock.Create();

                async ControlledTask WriteAsync(int value)
                {
                    using (await mutex.AcquireAsync())
                    {
                        entry = value;
                    }
                }

                ControlledTask task1 = ControlledTask.Run(async () =>
                {
                    await WriteAsync(3);
                });

                ControlledTask task2 = ControlledTask.Run(async () =>
                {
                    await WriteAsync(5);
                });

                await ControlledTask.WhenAll(task1, task2);
                Specification.Assert(entry == 5, "Value is '{0}' instead of 5.", entry);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronizeTwoParallelTasksWithYield()
        {
            this.Test(async () =>
            {
                int entry = 0;
                ControlledLock mutex = ControlledLock.Create();

                async ControlledTask WriteAsync(int value)
                {
                    using (await mutex.AcquireAsync())
                    {
                        entry = value;
                        await ControlledTask.Yield();
                        Specification.Assert(entry == value, "Value is '{0}' instead of '{1}'.", entry, value);
                    }
                }

                ControlledTask task1 = WriteAsync(3);
                ControlledTask task2 = WriteAsync(5);
                await ControlledTask.WhenAll(task1, task2);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }
    }
}
