// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class SemaphoreTests : BaseSystematicTest
    {
        public SemaphoreTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestCreateSemaphoreWithUnexpectedInitialCount()
        {
            this.TestWithError(() =>
            {
                Semaphore semaphore = Semaphore.Create(-1, 1);
            },
            expectedError: "Cannot create semaphore with initial count of -1. " +
                "The count must be equal or greater than 0.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestCreateSemaphoreWithUnexpectedMaxCount()
        {
            this.TestWithError(() =>
            {
                Semaphore semaphore = Semaphore.Create(0, 0);
            },
            expectedError: "Cannot create semaphore with max count of 0. " +
                "The count must be greater than 0.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestCreateSemaphoreWithUnexpectedInitialAndMaxCount()
        {
            this.TestWithError(() =>
            {
                Semaphore semaphore = Semaphore.Create(2, 1);
            },
            expectedError: "Cannot create semaphore with initial count of 2. " +
                "The count be equal or less than max count of 1.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitReleaseAsyncWithOneMaxRequest()
        {
            this.Test(async () =>
            {
                Semaphore semaphore = Semaphore.Create(1, 1);
                Specification.Assert(semaphore.CurrentCount == 1, "Semaphore count is {0} instead of 1.",
                    semaphore.CurrentCount);
                await semaphore.WaitAsync();
                Specification.Assert(semaphore.CurrentCount == 0, "Semaphore count is {0} instead of 0.",
                    semaphore.CurrentCount);
                semaphore.Release();
                Specification.Assert(semaphore.CurrentCount == 1, "Semaphore count is {0} instead of 1.",
                    semaphore.CurrentCount);
                semaphore.Dispose();
            });
        }

        [Fact(Timeout = 5000)]
        public void TestWaitReleaseAsyncWithTwoMaxRequests()
        {
            this.Test(async () =>
            {
                Semaphore semaphore = Semaphore.Create(2, 2);
                Specification.Assert(semaphore.CurrentCount == 2, "Semaphore count is {0} instead of 2.",
                    semaphore.CurrentCount);
                await semaphore.WaitAsync();
                Specification.Assert(semaphore.CurrentCount == 1, "Semaphore count is {0} instead of 1.",
                    semaphore.CurrentCount);
                await semaphore.WaitAsync();
                Specification.Assert(semaphore.CurrentCount == 0, "Semaphore count is {0} instead of 0.",
                    semaphore.CurrentCount);
                semaphore.Release();
                Specification.Assert(semaphore.CurrentCount == 1, "Semaphore count is {0} instead of 1.",
                    semaphore.CurrentCount);
                semaphore.Release();
                Specification.Assert(semaphore.CurrentCount == 2, "Semaphore count is {0} instead of 2.",
                    semaphore.CurrentCount);
                semaphore.Dispose();
            });
        }

        [Fact(Timeout = 5000)]
        public void TestWaitReleaseAsyncWithOneInitialAndTwoMaxRequests()
        {
            this.Test(async () =>
            {
                Semaphore semaphore = Semaphore.Create(1, 2);
                Specification.Assert(semaphore.CurrentCount == 1, "Semaphore count is {0} instead of 1.",
                    semaphore.CurrentCount);
                await semaphore.WaitAsync();
                Specification.Assert(semaphore.CurrentCount == 0, "Semaphore count is {0} instead of 0.",
                    semaphore.CurrentCount);
                semaphore.Release();
                Specification.Assert(semaphore.CurrentCount == 1, "Semaphore count is {0} instead of 1.",
                    semaphore.CurrentCount);
                semaphore.Release();
                Specification.Assert(semaphore.CurrentCount == 2, "Semaphore count is {0} instead of 2.",
                    semaphore.CurrentCount);
                semaphore.Dispose();
            });
        }

        [Fact(Timeout = 5000)]
        public void TestWaitTwiceWithOneMaxRequest()
        {
            this.TestWithError(async () =>
            {
                Semaphore semaphore = Semaphore.Create(1, 1);
                await semaphore.WaitAsync();
                await semaphore.WaitAsync();
            },
            expectedError: "Deadlock detected. Task() is waiting to acquire a resource " +
                "that is already acquired, but no other controlled tasks are enabled.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestReleaseTwiceWithOneMaxRequest()
        {
            this.TestWithError(async () =>
            {
                Semaphore semaphore = Semaphore.Create(1, 1);
                await semaphore.WaitAsync();
                semaphore.Release();
                semaphore.Release();
            },
            expectedError: "Cannot release semaphore as it has reached max count of 1.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronizeTwoAsynchronousTasksAndOneMaxRequest()
        {
            this.Test(async () =>
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
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronizeTwoParallelTasksAndOneMaxRequest()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Semaphore semaphore = Semaphore.Create(1, 1);

                async ControlledTask WriteAsync(int value)
                {
                    await semaphore.WaitAsync();
                    entry.Value = value;
                    semaphore.Release();
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
                Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is '' instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronizeTwoAsynchronousTasksWithYieldAndOneMaxRequest()
        {
            this.Test(async () =>
            {
                Semaphore semaphore = Semaphore.Create(1, 1);

                SharedEntry entry = new SharedEntry();
                async ControlledTask WriteAsync(int value)
                {
                    await semaphore.WaitAsync();
                    entry.Value = value;
                    await ControlledTask.Yield();
                    Specification.Assert(entry.Value == value, "Value is '{0}' instead of '{1}'.", entry.Value, value);
                    semaphore.Release();
                }

                ControlledTask task1 = WriteAsync(3);
                ControlledTask task2 = WriteAsync(5);
                await ControlledTask.WhenAll(task1, task2);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }
    }
}
