// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class SchedulingPointTests : BaseBugFindingTest
    {
        public SchedulingPointTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestInterleave()
        {
            this.TestWithError(async () =>
            {
                int x = 0;
                int a = 0;
                int b = 0;

                var t1 = Task.Run(async () =>
                {
                    a = x + 1;
                    SchedulingPoint.Interleave();
                    x = a;
                    await Task.CompletedTask;
                });

                var t2 = Task.Run(async () =>
                {
                    b = x + 1;
                    SchedulingPoint.Interleave();
                    x = b;
                    await Task.CompletedTask;
                });

                await Task.WhenAll(t1, t2);
                Specification.Assert(a > 1 || b > 1, string.Format("A: {0}, B: {1}", a, b));
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "A: 1, B: 1",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestYield()
        {
            this.TestWithError(async () =>
            {
                int x = 0;
                int a = 0;
                int b = 0;

                var t1 = Task.Run(async () =>
                {
                    a = x + 1;
                    SchedulingPoint.Yield();
                    x = a;
                    await Task.CompletedTask;
                });

                var t2 = Task.Run(async () =>
                {
                    b = x + 1;
                    SchedulingPoint.Yield();
                    x = b;
                    await Task.CompletedTask;
                });

                await Task.WhenAll(t1, t2);
                Specification.Assert(a > 1 || b > 1, string.Format("A: {0}, B: {1}", a, b));
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "A: 1, B: 1",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSuppressTaskInterleaving()
        {
            this.Test(async () =>
            {
                int value = 0;

                SchedulingPoint.Suppress();
                var t = Task.Run(() =>
                {
                    value = 2;
                });

                SchedulingPoint.Resume();

                value = 1;
                await t;

                Specification.Assert(value is 2, $"Value is {value}.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestAvoidSuppressTaskInterleaving()
        {
            this.TestWithError(async () =>
            {
                int value = 0;

                SchedulingPoint.Suppress();
                var t = Task.Run(() =>
                {
                    value = 2;
                });

                SchedulingPoint.Interleave();
                SchedulingPoint.Resume();

                value = 1;
                await t;

                Specification.Assert(value is 2, $"Value is {value}.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: "Value is 1.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSuppressAndResumeTaskInterleaving()
        {
            this.TestWithError(async () =>
            {
                int value = 0;

                SchedulingPoint.Suppress();
                SchedulingPoint.Resume();
                var t = Task.Run(() =>
                {
                    value = 2;
                });

                value = 1;
                await t;

                Specification.Assert(value is 2, $"Value is {value}.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            expectedError: "Value is 1.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestSuppressLockInterleaving()
        {
            this.Test(async () =>
            {
                var set = new HashSet<int>();

                var t1 = Task.Run(() =>
                {
                    SchedulingPoint.Suppress();
                    lock (set)
                    {
                        set.Remove(1);
                    }

                    lock (set)
                    {
                        set.Add(2);
                    }

                    SchedulingPoint.Resume();
                });

                var t2 = Task.Run(() =>
                {
                    SchedulingPoint.Suppress();
                    lock (set)
                    {
                        set.Remove(2);
                    }

                    lock (set)
                    {
                        set.Add(1);
                    }

                    SchedulingPoint.Resume();
                });

                await Task.WhenAll(t1, t2);

                Specification.Assert(set.Count is 1, $"Count is {set.Count}.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestSuppressNoResumeTaskInterleaving()
        {
            this.Test(async () =>
            {
                // Make sure the scheduler does not deadlock.
                SchedulingPoint.Suppress();
                // Only interleavings of enabled operations should be suppressed.
                await Task.Run(() => { });
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestNestedSuppressInterleaving()
        {
            this.Test(() =>
            {
                int counter = 0;

                Task t1 = Task.Run(() =>
                {
                    SchedulingPoint.Suppress();
                    SchedulingPoint.Suppress();
                    SchedulingPoint.Resume();
                    counter++;
                    Operation.ScheduleNext();
                    counter--;
                    SchedulingPoint.Resume();
                });

                Task t2 = Task.Run(() =>
                {
                    Specification.Assert(counter == 0, "Counter is not zero.");
                });

                Task.WaitAll(t1, t2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }
    }
}
