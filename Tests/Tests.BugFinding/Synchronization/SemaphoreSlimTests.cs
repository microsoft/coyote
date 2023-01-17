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
        public void TestSemaphoreSlimWithNoInitialAccess()
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
        public void TestParallelSemaphoreSlim()
        {
            this.Test(() =>
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

                Task.WaitAll(t1, t2, t3);

                int expected = 0;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestParallelSemaphoreSlimWithDoubleAccess()
        {
            this.TestWithError(() =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(2, 2);

                bool isOrderHit = false;
                var t1 = Task.Run(() =>
                {
                    semaphore.Wait();
                    value++;
                    SchedulingPoint.Interleave();
                    isOrderHit |= value is 2;
                    value--;
                    semaphore.Release();
                });

                var t2 = Task.Run(() =>
                {
                    semaphore.Wait();
                    value++;
                    SchedulingPoint.Interleave();
                    isOrderHit |= value is 2;
                    value--;
                    semaphore.Release();
                });

                Task.WaitAll(t1, t2);

                int expected = 0;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
                Specification.Assert(!isOrderHit, "Expected assertion failed!");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: "Expected assertion failed!",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAsyncSemaphoreSlim()
        {
            this.Test(() =>
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

                Task.WaitAll(t1, t2, t3);

                int expected = 0;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestAsyncSemaphoreSlimWithDoubleAccess()
        {
            this.TestWithError(() =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(2, 2);

                bool isOrderHit = false;
                var t1 = Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    value++;
                    SchedulingPoint.Interleave();
                    isOrderHit |= value is 2;
                    value--;
                    semaphore.Release();
                });

                var t2 = Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    value++;
                    SchedulingPoint.Interleave();
                    isOrderHit |= value is 2;
                    value--;
                    semaphore.Release();
                });

                Task.WaitAll(t1, t2);

                int expected = 0;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
                Specification.Assert(!isOrderHit, "Expected assertion failed!");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: "Expected assertion failed!",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAsyncSemaphoreSlimWithBlockingWait()
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
        public void TestMixedSemaphoreSlim()
        {
            this.Test(() =>
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

                var t2 = Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
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
        public void TestMixedSemaphoreSlimWithDoubleAccess()
        {
            this.TestWithError(() =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(2, 2);

                bool isOrderHit = false;
                var t1 = Task.Run(() =>
                {
                    semaphore.Wait();
                    value++;
                    SchedulingPoint.Interleave();
                    isOrderHit |= value is 2;
                    value--;
                    semaphore.Release();
                });

                var t2 = Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    value++;
                    SchedulingPoint.Interleave();
                    isOrderHit |= value is 2;
                    value--;
                    semaphore.Release();
                });

                Task.WaitAll(t1, t2);

                int expected = 0;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
                Specification.Assert(!isOrderHit, "Expected assertion failed!");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: "Expected assertion failed!",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestParallelSemaphoreSlimWithNoInitialAccess()
        {
            this.Test(() =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(0, 1);

                int firstHolder = 0;
                var t1 = Task.Run(() =>
                {
                    semaphore.Wait();
                    firstHolder = firstHolder is 0 ? 1 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 1;
                    semaphore.Release();
                });

                var t2 = Task.Run(() =>
                {
                    semaphore.Release();
                    semaphore.Wait();
                    firstHolder = firstHolder is 0 ? 2 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 2;
                    semaphore.Release();
                });

                Task.WaitAll(t1, t2);

                int expected = firstHolder is 1 ? 2 : 1;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestParallelSemaphoreSlimWithNoInitialAccessAndExpectedOrder()
        {
            this.TestWithError(() =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(0, 1);

                int order = 0;
                int firstHolder = 0;
                var t1 = Task.Run(() =>
                {
                    order = order is 0 ? 1 : 0;
                    semaphore.Wait();
                    order = order is 3 ? 4 : 0;
                    firstHolder = firstHolder is 0 ? 1 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 1;
                    semaphore.Release();
                });

                var t2 = Task.Run(() =>
                {
                    semaphore.Release();
                    order = order is 1 ? 2 : 0;
                    semaphore.Wait();
                    order = order is 2 ? 3 : 0;
                    firstHolder = firstHolder is 0 ? 2 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 2;
                    semaphore.Release();
                });

                Task.WaitAll(t1, t2);

                int expected = firstHolder is 1 ? 2 : 1;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
                Specification.Assert(order < 4, "Expected assertion failed!");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: "Expected assertion failed!",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestParallelSemaphoreSlimWithNoInitialAccessAndExpectedAltOrder()
        {
            this.TestWithError(() =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(0, 1);

                int order = 0;
                int firstHolder = 0;
                var t1 = Task.Run(() =>
                {
                    order = order is 0 ? 1 : 0;
                    semaphore.Wait();
                    order = order is 2 ? 3 : 0;
                    firstHolder = firstHolder is 0 ? 1 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 1;
                    semaphore.Release();
                });

                var t2 = Task.Run(() =>
                {
                    semaphore.Release();
                    order = order is 1 ? 2 : 0;
                    semaphore.Wait();
                    order = order is 3 ? 4 : 0;
                    firstHolder = firstHolder is 0 ? 2 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 2;
                    semaphore.Release();
                });

                Task.WaitAll(t1, t2);

                int expected = firstHolder is 1 ? 2 : 1;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
                Specification.Assert(order < 4, "Expected assertion failed!");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100)
                .WithLockAccessRaceCheckingEnabled(),
            expectedError: "Expected assertion failed!",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAsyncSemaphoreSlimWithNoInitialAccess()
        {
            this.Test(() =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(0, 1);

                int firstHolder = 0;
                var t1 = Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    firstHolder = firstHolder is 0 ? 1 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 1;
                    semaphore.Release();
                });

                var t2 = Task.Run(async () =>
                {
                    semaphore.Release();
                    await semaphore.WaitAsync();
                    firstHolder = firstHolder is 0 ? 2 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 2;
                    semaphore.Release();
                });

                Task.WaitAll(t1, t2);

                int expected = firstHolder is 1 ? 2 : 1;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestAsyncSemaphoreSlimWithNoInitialAccessAndExpectedOrder()
        {
            this.TestWithError(() =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(0, 1);

                int order = 0;
                int firstHolder = 0;
                var t1 = Task.Run(async () =>
                {
                    order = order is 0 ? 1 : 0;
                    await semaphore.WaitAsync();
                    order = order is 3 ? 4 : 0;
                    firstHolder = firstHolder is 0 ? 1 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 1;
                    semaphore.Release();
                });

                var t2 = Task.Run(async () =>
                {
                    semaphore.Release();
                    order = order is 1 ? 2 : 0;
                    await semaphore.WaitAsync();
                    order = order is 2 ? 3 : 0;
                    firstHolder = firstHolder is 0 ? 2 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 2;
                    semaphore.Release();
                });

                Task.WaitAll(t1, t2);

                int expected = firstHolder is 1 ? 2 : 1;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
                Specification.Assert(order < 4, "Expected assertion failed!");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100)
                .WithLockAccessRaceCheckingEnabled(),
            expectedError: "Expected assertion failed!",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAsyncSemaphoreSlimWithNoInitialAccessAndExpectedAltOrder()
        {
            this.TestWithError(() =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(0, 1);

                int order = 0;
                int firstHolder = 0;
                var t1 = Task.Run(async () =>
                {
                    order = order is 0 ? 1 : 0;
                    await semaphore.WaitAsync();
                    order = order is 2 ? 3 : 0;
                    firstHolder = firstHolder is 0 ? 1 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 1;
                    semaphore.Release();
                });

                var t2 = Task.Run(async () =>
                {
                    semaphore.Release();
                    order = order is 1 ? 2 : 0;
                    await semaphore.WaitAsync();
                    order = order is 3 ? 4 : 0;
                    firstHolder = firstHolder is 0 ? 2 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 2;
                    semaphore.Release();
                });

                Task.WaitAll(t1, t2);

                int expected = firstHolder is 1 ? 2 : 1;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
                Specification.Assert(order < 4, "Expected assertion failed!");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: "Expected assertion failed!",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestMixedSemaphoreSlimWithNoInitialAccess()
        {
            this.Test(() =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(0, 1);

                int firstHolder = 0;
                var t1 = Task.Run(() =>
                {
                    semaphore.Wait();
                    firstHolder = firstHolder is 0 ? 1 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 1;
                    semaphore.Release();
                });

                var t2 = Task.Run(async () =>
                {
                    semaphore.Release();
                    await semaphore.WaitAsync();
                    firstHolder = firstHolder is 0 ? 2 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 2;
                    semaphore.Release();
                });

                Task.WaitAll(t1, t2);

                int expected = firstHolder is 1 ? 2 : 1;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestMixedSemaphoreSlimWithNoInitialAccessAndExpectedOrder()
        {
            this.TestWithError(() =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(0, 1);

                int order = 0;
                int firstHolder = 0;
                var t1 = Task.Run(() =>
                {
                    order = order is 0 ? 1 : 0;
                    semaphore.Wait();
                    order = order is 3 ? 4 : 0;
                    firstHolder = firstHolder is 0 ? 1 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 1;
                    semaphore.Release();
                });

                var t2 = Task.Run(async () =>
                {
                    semaphore.Release();
                    order = order is 1 ? 2 : 0;
                    await semaphore.WaitAsync();
                    order = order is 2 ? 3 : 0;
                    firstHolder = firstHolder is 0 ? 2 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 2;
                    semaphore.Release();
                });

                Task.WaitAll(t1, t2);

                int expected = firstHolder is 1 ? 2 : 1;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
                Specification.Assert(order < 4, "Expected assertion failed!");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: "Expected assertion failed!",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestMixedSemaphoreSlimWithNoInitialAccessAndExpectedAltOrder()
        {
            this.TestWithError(() =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(0, 1);

                int order = 0;
                int firstHolder = 0;
                var t1 = Task.Run(() =>
                {
                    order = order is 0 ? 1 : 0;
                    semaphore.Wait();
                    order = order is 2 ? 3 : 0;
                    firstHolder = firstHolder is 0 ? 1 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 1;
                    semaphore.Release();
                });

                var t2 = Task.Run(async () =>
                {
                    semaphore.Release();
                    order = order is 1 ? 2 : 0;
                    await semaphore.WaitAsync();
                    order = order is 3 ? 4 : 0;
                    firstHolder = firstHolder is 0 ? 2 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 2;
                    semaphore.Release();
                });

                Task.WaitAll(t1, t2);

                int expected = firstHolder is 1 ? 2 : 1;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
                Specification.Assert(order < 4, "Expected assertion failed!");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100)
                .WithLockAccessRaceCheckingEnabled(),
            expectedError: "Expected assertion failed!",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestMixedAltSemaphoreSlimWithNoInitialAccess()
        {
            this.Test(() =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(0, 1);

                int firstHolder = 0;
                var t1 = Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    firstHolder = firstHolder is 0 ? 1 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 1;
                    semaphore.Release();
                });

                var t2 = Task.Run(() =>
                {
                    semaphore.Release();
                    semaphore.Wait();
                    firstHolder = firstHolder is 0 ? 2 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 2;
                    semaphore.Release();
                });

                Task.WaitAll(t1, t2);

                int expected = firstHolder is 1 ? 2 : 1;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestMixedAltSemaphoreSlimWithNoInitialAccessAndExpectedOrder()
        {
            this.TestWithError(() =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(0, 1);

                int order = 0;
                int firstHolder = 0;
                var t1 = Task.Run(async () =>
                {
                    order = order is 0 ? 1 : 0;
                    await semaphore.WaitAsync();
                    order = order is 3 ? 4 : 0;
                    firstHolder = firstHolder is 0 ? 1 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 1;
                    semaphore.Release();
                });

                var t2 = Task.Run(() =>
                {
                    semaphore.Release();
                    order = order is 1 ? 2 : 0;
                    semaphore.Wait();
                    order = order is 2 ? 3 : 0;
                    firstHolder = firstHolder is 0 ? 2 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 2;
                    semaphore.Release();
                });

                Task.WaitAll(t1, t2);

                int expected = firstHolder is 1 ? 2 : 1;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
                Specification.Assert(order < 4, "Expected assertion failed!");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100)
                .WithLockAccessRaceCheckingEnabled(),
            expectedError: "Expected assertion failed!",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestMixedAltSemaphoreSlimWithNoInitialAccessAndExpectedAltOrder()
        {
            this.TestWithError(() =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(0, 1);

                int order = 0;
                int firstHolder = 0;
                var t1 = Task.Run(async () =>
                {
                    order = order is 0 ? 1 : 0;
                    await semaphore.WaitAsync();
                    order = order is 2 ? 3 : 0;
                    firstHolder = firstHolder is 0 ? 1 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 1;
                    semaphore.Release();
                });

                var t2 = Task.Run(() =>
                {
                    semaphore.Release();
                    order = order is 1 ? 2 : 0;
                    semaphore.Wait();
                    order = order is 3 ? 4 : 0;
                    firstHolder = firstHolder is 0 ? 2 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 2;
                    semaphore.Release();
                });

                Task.WaitAll(t1, t2);

                int expected = firstHolder is 1 ? 2 : 1;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
                Specification.Assert(order < 4, "Expected assertion failed!");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: "Expected assertion failed!",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestMixedSemaphoreSlimForUnexpectedOrder()
        {
            this.Test(() =>
            {
                int value = 0;
                var semaphore = new SemaphoreSlim(0, 1);

                int order = 0;
                int firstHolder = 0;
                var t1 = Task.Run(async () =>
                {
                    Task awaiter = semaphore.WaitAsync();
                    order = order is 0 ? 1 : order;
                    await awaiter;
                    order = order is 1 ? 0 : order;
                    firstHolder = firstHolder is 0 ? 1 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 1;
                    semaphore.Release();
                });

                var t2 = Task.Run(() =>
                {
                    semaphore.Release();
                    semaphore.Wait();
                    order = order is 1 ? 2 : order;
                    firstHolder = firstHolder is 0 ? 2 : firstHolder;
                    SchedulingPoint.Interleave();
                    value = 2;
                    semaphore.Release();
                });

                Task.WaitAll(t1, t2);

                int expectedOrder = 2;
                int expectedValue = 2;
                Specification.Assert(order < expectedOrder, "Expected order must be less than {0}.", expectedOrder);
                Specification.Assert(order < 2 || value == expectedValue, "Value is {0} instead of {1}.", value, expectedValue);
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
            configuration: this.GetConfiguration().WithDeadlockTimeout(10),
            errorChecker: (e) =>
            {
                Assert.StartsWith("Deadlock detected.", e);
            },
            replay: true);
        }
    }
}
