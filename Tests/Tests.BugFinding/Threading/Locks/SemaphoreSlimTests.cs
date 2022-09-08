﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;
using Monitor = System.Threading.Monitor;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class SemaphoreSlimTests : BaseBugFindingTest
    {
        public SemaphoreSlimTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestSemaphoreWithSingleAccess()
        {
            this.Test(() =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(1, 1);
                semaphore.Wait();
                value++;
                semaphore.Release();

                semaphore.Wait();
                value++;
                semaphore.Release();

                int expected = 2;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestSemaphoreWithDoubleAccess()
        {
            this.Test(() =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(2, 2);
                semaphore.Wait();
                semaphore.Wait();
                value++;
                semaphore.Release(2);

                semaphore.Wait();
                semaphore.Wait();
                value++;
                semaphore.Release(2);

                int expected = 2;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestSemaphoreWithInitialAccess()
        {
            this.Test(() =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(0, 2);
                semaphore.Release(2);
                semaphore.Wait();
                semaphore.Wait();
                value++;
                semaphore.Release(2);

                int expected = 1;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestSemaphoreWithParallelAccess()
        {
            this.Test(async () =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(1, 1);

                var t1 = Task.Run(() =>
                {
                    semaphore.Wait();
                    value++;
                    SchedulingPoint.Interleave();
                    value--;
                    semaphore.Release();
                });

                var t2 = Task.Run(() =>
                {
                    semaphore.Wait();
                    value++;
                    SchedulingPoint.Interleave();
                    value--;
                    semaphore.Release();
                });

                await Task.WhenAll(t1, t2);

                int expected = 0;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestSemaphoreWithParallelAccessAndForcedOrder()
        {
            this.Test(async () =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(0, 1);

                var t1 = Task.Run(() =>
                {
                    semaphore.Wait();
                    SchedulingPoint.Interleave();
                    value = 2;
                    semaphore.Release();
                });

                var t2 = Task.Run(() =>
                {
                    semaphore.Release();
                    semaphore.Wait();
                    SchedulingPoint.Interleave();
                    value = 1;
                    semaphore.Release();
                });

                await Task.WhenAll(t1, t2);

                int expected = 2;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestSemaphoreWithAsyncAccess()
        {
            this.Test(async () =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(1, 1);

                IO.Debug.WriteLine($">>> [0] PRE-TASK-RUN-1 {Thread.CurrentThread.ManagedThreadId}");
                var t1 = Task.Run(async () =>
                {
                    IO.Debug.WriteLine($">>> [1] PRE-WAIT {Thread.CurrentThread.ManagedThreadId}");
                    await semaphore.WaitAsync();
                    IO.Debug.WriteLine($">>> [1] AFTER-WAIT {Thread.CurrentThread.ManagedThreadId}");
                    value++;
                    SchedulingPoint.Interleave();
                    IO.Debug.WriteLine($">>> [1] PRE-RELEASE {Thread.CurrentThread.ManagedThreadId}");
                    value--;
                    semaphore.Release();
                    IO.Debug.WriteLine($">>> [1] AFTER-RELEASE {Thread.CurrentThread.ManagedThreadId}");
                });

                IO.Debug.WriteLine($">>> [0] PRE-TASK-RUN-2 {Thread.CurrentThread.ManagedThreadId}");
                var t2 = Task.Run(async () =>
                {
                    IO.Debug.WriteLine($">>> [2] PRE-WAIT {Thread.CurrentThread.ManagedThreadId}");
                    await semaphore.WaitAsync();
                    IO.Debug.WriteLine($">>> [2] AFTER-WAIT {Thread.CurrentThread.ManagedThreadId}");
                    value++;
                    SchedulingPoint.Interleave();
                    IO.Debug.WriteLine($">>> [2] PRE-RELEASE {Thread.CurrentThread.ManagedThreadId}");
                    value--;
                    semaphore.Release();
                    IO.Debug.WriteLine($">>> [2] AFTER-RELEASE {Thread.CurrentThread.ManagedThreadId}");
                });

                IO.Debug.WriteLine($">>> [0] PRE-WAIT {Thread.CurrentThread.ManagedThreadId}");
                await Task.WhenAll(t1, t2);
                IO.Debug.WriteLine($">>> [0] END {Thread.CurrentThread.ManagedThreadId}");

                int expected = 0;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
            },
            configuration: this.GetConfiguration().WithDebugLoggingEnabled().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestSemaphoreWithAsyncAccessAndForcedOrder()
        {
            this.Test(async () =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(0, 1);

                var t1 = Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    value++;
                    SchedulingPoint.Interleave();
                    value--;
                    semaphore.Release();
                });

                var t2 = Task.Run(async () =>
                {
                    semaphore.Release();
                    await semaphore.WaitAsync();
                    value++;
                    SchedulingPoint.Interleave();
                    value--;
                    semaphore.Release();
                });

                await Task.WhenAll(t1, t2);

                int expected = 0;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestSemaphoreWithAsyncContinuationAfterAwait()
        {
            this.Test(async () =>
            {
                var semaphore = new SemaphoreSlim(1, 1);
                Task task = Task.Run(() =>
                {
                    semaphore.Wait();
                    SchedulingPoint.Interleave();
                    semaphore.Release();
                });

                await semaphore.WaitAsync();
                semaphore.Release();
                await task;
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestSemaphoreWithDeadlock()
        {
            this.TestWithError(() =>
            {
                var semaphore = new SemaphoreSlim(1, 1);
                semaphore.Wait();
                semaphore.Wait();
            },
            errorChecker: (e) =>
            {
                Assert.StartsWith("Deadlock detected.", e);
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSemaphoreWithAsyncDeadlock()
        {
            this.TestWithError(async () =>
            {
                var semaphore = new SemaphoreSlim(1, 1);
                await semaphore.WaitAsync();
                await semaphore.WaitAsync();
            },
            errorChecker: (e) =>
            {
                Assert.StartsWith("Deadlock detected.", e);
            },
            replay: true);
        }
    }
}
