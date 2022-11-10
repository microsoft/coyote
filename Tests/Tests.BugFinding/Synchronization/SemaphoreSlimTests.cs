// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class SemaphoreSlimTests : BaseBugFindingTest
    {
        public SemaphoreSlimTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestSemaphoreSlimWithSingleAccess()
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
        public void TestSemaphoreSlimWithDoubleAccess()
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
        public void TestSemaphoreSlimWithInitialAccess()
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
        public void TestSemaphoreSlimWithParallelAccess()
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
        public void TestSemaphoreSlimWithMultiParallelAccess()
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

                var t3 = Task.Run(() =>
                {
                    semaphore.Wait();
                    value++;
                    SchedulingPoint.Interleave();
                    value--;
                    semaphore.Release();
                });

                await Task.WhenAll(t1, t2, t3);

                int expected = 0;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestSemaphoreSlimWithParallelAccessAndForcedOrder()
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
        public void TestSemaphoreSlimWithAsyncAccess()
        {
            this.Test(async () =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(1, 1);

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
        public void TestSemaphoreSlimWithMultiAsyncAccess()
        {
            this.Test(async () =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(1, 1);

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
                    await semaphore.WaitAsync();
                    value++;
                    SchedulingPoint.Interleave();
                    value--;
                    semaphore.Release();
                });

                var t3 = Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    value++;
                    SchedulingPoint.Interleave();
                    value--;
                    semaphore.Release();
                });

                await Task.WhenAll(t1, t2, t3);

                int expected = 0;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestSemaphoreSlimWithAsyncAccessAndForcedOrder()
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
        public void TestSemaphoreSlimWithAsyncAccessAndBlockingWait()
        {
            this.Test(() =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(1, 1);

                var t1 = Task.Run(() =>
                {
                    semaphore.WaitAsync().Wait();
                    value++;
                    SchedulingPoint.Interleave();
                    value--;
                    semaphore.Release();
                });

                var t2 = Task.Run(() =>
                {
                    semaphore.WaitAsync().Wait();
                    value++;
                    SchedulingPoint.Interleave();
                    value--;
                    semaphore.Release();
                });

                Task.WaitAll(t1, t2);

                int expected = 0;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestSemaphoreSlimWithAsyncContinuationAfterAwait()
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
        public void TestSemaphoreSlimWithDeadlock()
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
        public void TestSemaphoreSlimWithAsyncDeadlock()
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
