// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading;

namespace Microsoft.Coyote.Production.Tests.Tasks
{
    public class SemaphoreTests : BaseTest
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
            expectedErrors: new string[]
            {
                "Cannot create semaphore with initial count of -1. The count must be equal or greater than 0.",
                @"The initialCount argument must be non-negative and less than or equal to the maximumCount.
Parameter name: initialCount
Actual value was -1.",
                @"The initialCount argument must be non-negative and less than or equal to the maximumCount. (Parameter 'initialCount')
Actual value was -1."
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestCreateSemaphoreWithUnexpectedMaxCount()
        {
            this.TestWithError(() =>
            {
                Semaphore semaphore = Semaphore.Create(0, 0);
            },
            expectedErrors: new string[]
            {
                "Cannot create semaphore with max count of 0. The count must be greater than 0.",
                @"The maximumCount argument must be a positive number. If a maximum is not required, use the constructor without a maxCount parameter. (Parameter 'maxCount')
Actual value was 0.",
                @"The maximumCount argument must be a positive number. If a maximum is not required, use the constructor without a maxCount parameter.
Parameter name: maxCount
Actual value was 0."
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestCreateSemaphoreWithUnexpectedInitialAndMaxCount()
        {
            this.TestWithError(() =>
            {
                Semaphore semaphore = Semaphore.Create(2, 1);
            },
            expectedErrors: new string[]
            {
                "Cannot create semaphore with initial count of 2. The count be equal or less than max count of 1.",
                @"The initialCount argument must be non-negative and less than or equal to the maximumCount. (Parameter 'initialCount')
Actual value was 2.",
                @"The initialCount argument must be non-negative and less than or equal to the maximumCount.
Parameter name: initialCount
Actual value was 2."
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitReleaseAsyncWithOneMaxRequest()
        {
            this.Test(async () =>
            {
                Semaphore semaphore = Semaphore.Create(1, 1);
                Specification.Assert(semaphore.CurrentCount is 1, "Semaphore count is {0} instead of 1.",
                    semaphore.CurrentCount);
                await semaphore.WaitAsync();
                Specification.Assert(semaphore.CurrentCount is 0, "Semaphore count is {0} instead of 0.",
                    semaphore.CurrentCount);
                semaphore.Release();
                Specification.Assert(semaphore.CurrentCount is 1, "Semaphore count is {0} instead of 1.",
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
                Specification.Assert(semaphore.CurrentCount is 2, "Semaphore count is {0} instead of 2.",
                    semaphore.CurrentCount);
                await semaphore.WaitAsync();
                Specification.Assert(semaphore.CurrentCount is 1, "Semaphore count is {0} instead of 1.",
                    semaphore.CurrentCount);
                await semaphore.WaitAsync();
                Specification.Assert(semaphore.CurrentCount is 0, "Semaphore count is {0} instead of 0.",
                    semaphore.CurrentCount);
                semaphore.Release();
                Specification.Assert(semaphore.CurrentCount is 1, "Semaphore count is {0} instead of 1.",
                    semaphore.CurrentCount);
                semaphore.Release();
                Specification.Assert(semaphore.CurrentCount is 2, "Semaphore count is {0} instead of 2.",
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
                Specification.Assert(semaphore.CurrentCount is 1, "Semaphore count is {0} instead of 1.",
                    semaphore.CurrentCount);
                await semaphore.WaitAsync();
                Specification.Assert(semaphore.CurrentCount is 0, "Semaphore count is {0} instead of 0.",
                    semaphore.CurrentCount);
                semaphore.Release();
                Specification.Assert(semaphore.CurrentCount is 1, "Semaphore count is {0} instead of 1.",
                    semaphore.CurrentCount);
                semaphore.Release();
                Specification.Assert(semaphore.CurrentCount is 2, "Semaphore count is {0} instead of 2.",
                    semaphore.CurrentCount);
                semaphore.Dispose();
            });
        }

        [Fact(Timeout = 5000)]
        public void TestWaitTwiceWithOneMaxRequest()
        {
            if (!this.SystematicTest)
            {
                // .NET semaphores cannot detect deadlocks, that's why you need Coyote test :-)
                return;
            }

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
            expectedErrors: new string[]
            {
                "Cannot release semaphore as it has reached max count of 1.",
                "Adding the specified count to the semaphore would cause it to exceed its maximum count."
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronizeTwoAsynchronousTasksAndOneMaxRequest()
        {
            this.Test(async () =>
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
                Specification.Assert(entry.Value is 5, "Value is '{0}' instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronizeTwoParallelTasksAndOneMaxRequest()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Semaphore semaphore = Semaphore.Create(1, 1);

                async Task WriteAsync(int value)
                {
                    await semaphore.WaitAsync();
                    entry.Value = value;
                    semaphore.Release();
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
                Specification.Assert(entry.Value is 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSynchronizeTwoAsynchronousTasksWithYieldAndOneMaxRequest()
        {
            this.Test(async () =>
            {
                Semaphore semaphore = Semaphore.Create(1, 1);

                SharedEntry entry = new SharedEntry();
                async Task WriteAsync(int value)
                {
                    await semaphore.WaitAsync();
                    entry.Value = value;
                    await Task.Yield();
                    Specification.Assert(entry.Value == value, "Value is '{0}' instead of '{1}'.", entry.Value, value);
                    semaphore.Release();
                }

                Task task1 = WriteAsync(3);
                Task task2 = WriteAsync(5);
                await Task.WhenAll(task1, task2);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestWaitReleaseAsyncWithIntTimeout()
        {
            // TODO: rewrite test once timeouts are properly supported during systematic testing.
            this.Test(async () =>
            {
                Semaphore semaphore = Semaphore.Create(1, 1);
                Specification.Assert(semaphore.CurrentCount is 1, "Semaphore count is {0} instead of 1.",
                    semaphore.CurrentCount);
                await semaphore.WaitAsync(1);
                Specification.Assert(semaphore.CurrentCount is 0 || semaphore.CurrentCount is 1,
                    "Semaphore count is {0} instead of 0 or 1.", semaphore.CurrentCount);
                semaphore.Release();
                Specification.Assert(semaphore.CurrentCount is 1, "Semaphore count is {0} instead of 1.",
                    semaphore.CurrentCount);
                semaphore.Dispose();
            });
        }

        [Fact(Timeout = 5000)]
        public void TestWaitReleaseAsyncWithTimeSpanTimeout()
        {
            // TODO: rewrite test once timeouts are properly supported during systematic testing.
            this.Test(async () =>
            {
                Semaphore semaphore = Semaphore.Create(1, 1);
                Specification.Assert(semaphore.CurrentCount is 1, "Semaphore count is {0} instead of 1.",
                    semaphore.CurrentCount);
                await semaphore.WaitAsync(TimeSpan.FromMilliseconds(1));
                Specification.Assert(semaphore.CurrentCount is 0 || semaphore.CurrentCount is 1,
                    "Semaphore count is {0} instead of 0 or 1.", semaphore.CurrentCount);
                semaphore.Release();
                Specification.Assert(semaphore.CurrentCount is 1, "Semaphore count is {0} instead of 1.",
                    semaphore.CurrentCount);
                semaphore.Dispose();
            });
        }

        [Fact(Timeout = 5000)]
        public void TestWaitReleaseAsyncWithCancellation()
        {
            // TODO: rewrite test once cancellation is properly supported during systematic testing.
            this.Test(async () =>
            {
                Semaphore semaphore = Semaphore.Create(1, 1);
                Specification.Assert(semaphore.CurrentCount is 1, "Semaphore count is {0} instead of 1.",
                    semaphore.CurrentCount);

                SystemTasks.CancellationTokenSource tokenSource = new SystemTasks.CancellationTokenSource();
                Task task = Task.Run(async () =>
                {
                    await Task.Delay(1);
                    tokenSource.Cancel();
                });

                await semaphore.WaitAsync(tokenSource.Token);
                Specification.Assert(semaphore.CurrentCount is 0 || semaphore.CurrentCount is 1,
                    "Semaphore count is {0} instead of 0 or 1.", semaphore.CurrentCount);
                semaphore.Release();
                Specification.Assert(semaphore.CurrentCount is 1, "Semaphore count is {0} instead of 1.",
                    semaphore.CurrentCount);

                await task;
                semaphore.Dispose();
            });
        }
    }
}
