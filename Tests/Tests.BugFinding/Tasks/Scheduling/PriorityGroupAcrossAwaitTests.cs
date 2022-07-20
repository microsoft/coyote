// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class PriorityGroupAcrossAwaitTests : BaseBugFindingTest
    {
        public PriorityGroupAcrossAwaitTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact]
        public void Test1()
        {
            this.Test(async () =>
            {
                Console.WriteLine($"Test-1 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                string opGroup1 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                await Task.Delay(10);
                string opGroup2 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                Microsoft.Coyote.Specifications.Specification.Assert(opGroup1 == opGroup2, "opGroup1 != opGroup2");

                Console.WriteLine($"Test-2 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
            },
            configuration: this.GetConfiguration().WithTestingIterations(1000).WithPrioritizationStrategy(false, 0));
        }

        // [Fact]
        // public void Test2()
        // {
        //     this.TestWithError(async () =>
        //     {
        //         Console.WriteLine($"Test-1 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

        // string opGroup1 = SchedulingPoint.GiveExecutingOperationGroup();
        //         await Task.Yield();
        //         string opGroup2 = SchedulingPoint.GiveExecutingOperationGroup();
        //         Microsoft.Coyote.Specifications.Specification.Assert(opGroup1 == opGroup2, "opGroup1 != opGroup2");

        // Console.WriteLine($"Test-2 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
        //     },
        //     configuration: this.GetConfiguration().WithTestingIterations(1000).WithRandomStrategy(),
        //     expectedError: "opGroup1 != opGroup2",
        //     replay: true);
        // }

        [Fact]
        public void Test2()
        {
            this.Test(async () =>
            {
                Console.WriteLine($"Test-1 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                string opGroup1 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                await Task.Yield();
                string opGroup2 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                Microsoft.Coyote.Specifications.Specification.Assert(opGroup1 == opGroup2, "opGroup1 != opGroup2");

                Console.WriteLine($"Test-2 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
            },
            configuration: this.GetConfiguration().WithTestingIterations(1000).WithPrioritizationStrategy(false, 0));
        }

        [Fact]
        public void Test3()
        {
            this.Test(async () =>
            {
                Console.WriteLine($"Test-1 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                string opGroup1 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                await Task.Run(() =>
                {
                    Console.WriteLine($"Test-2 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
                });
                string opGroup2 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                Microsoft.Coyote.Specifications.Specification.Assert(opGroup1 == opGroup2, "opGroup1 != opGroup2");

                Console.WriteLine($"Test-3 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
            },
            configuration: this.GetConfiguration().WithTestingIterations(1000).WithPrioritizationStrategy(false, 0));
        }

        [Fact]
        public void Test4()
        {
            this.Test(async () =>
            {
                Console.WriteLine($"Test-1 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                string opGroup1 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                await Task.Run(async () =>
                {
                    Console.WriteLine($"Test-2 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                    string opGroup2 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                    await Task.CompletedTask;
                    string opGroup3 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                    Microsoft.Coyote.Specifications.Specification.Assert(opGroup2 == opGroup3, "opGroup2 != opGroup3");

                    Console.WriteLine($"Test-3 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
                });
                string opGroup4 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                Microsoft.Coyote.Specifications.Specification.Assert(opGroup1 == opGroup4, "opGroup1 != opGroup4");

                Console.WriteLine($"Test-4 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
            },
            configuration: this.GetConfiguration().WithTestingIterations(1000).WithPrioritizationStrategy(false, 0));
        }

        [Fact]
        public void Test5()
        {
            this.Test(async () =>
            {
                Console.WriteLine($"Test-1 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                string opGroup1 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                await Task.Run(async () =>
                {
                    Console.WriteLine($"Test-2 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                    string opGroup2 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                    await Task.Delay(10);
                    string opGroup3 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                    Microsoft.Coyote.Specifications.Specification.Assert(opGroup2 == opGroup3, "opGroup2 != opGroup3");

                    Console.WriteLine($"Test-3 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
                });
                string opGroup4 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                Microsoft.Coyote.Specifications.Specification.Assert(opGroup1 == opGroup4, "opGroup1 != opGroup4");

                Console.WriteLine($"Test-4 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
            },
            configuration: this.GetConfiguration().WithTestingIterations(1000).WithPrioritizationStrategy(false, 0));
        }

        [Fact]
        public void Test6()
        {
            this.Test(async () =>
            {
                Console.WriteLine($"Test-1 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                string opGroup1 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                await Task.Run(async () =>
                {
                    Console.WriteLine($"Test-2 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                    string opGroup2 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                    await Task.Yield();
                    string opGroup3 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                    Microsoft.Coyote.Specifications.Specification.Assert(opGroup2 == opGroup3, "opGroup2 != opGroup3");

                    Console.WriteLine($"Test-3 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
                });
                string opGroup4 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                Microsoft.Coyote.Specifications.Specification.Assert(opGroup1 == opGroup4, "opGroup1 != opGroup4");

                Console.WriteLine($"Test-4 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
            },
            configuration: this.GetConfiguration().WithTestingIterations(1000).WithPrioritizationStrategy(false, 0));
        }

        [Fact]
        public void Test7()
        {
            this.Test(async () =>
            {
                Console.WriteLine($"Test-1 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                string opGroup1 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                await Task.Run(async () =>
                {
                    Console.WriteLine($"Test-2 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                    string opGroup2 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                    await Task.Run(() =>
                    {
                        Console.WriteLine($"Test-3 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
                    });
                    string opGroup3 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                    Microsoft.Coyote.Specifications.Specification.Assert(opGroup2 == opGroup3, "opGroup2 != opGroup3");

                    Console.WriteLine($"Test-4 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
                });
                string opGroup4 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                Microsoft.Coyote.Specifications.Specification.Assert(opGroup1 == opGroup4, "opGroup1 != opGroup4");

                Console.WriteLine($"Test-5 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
            },
            configuration: this.GetConfiguration().WithTestingIterations(1000).WithPrioritizationStrategy(false, 0));
        }

        [Fact]
        public void Test8()
        {
            this.Test(async () =>
            {
                Console.WriteLine($"Test-1 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                string opGroup1 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                await Task.Run(async () =>
                {
                    Console.WriteLine($"Test-2 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                    string opGroup2 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                    await Task.Run(async () =>
                    {
                        Console.WriteLine($"Test-3 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                        string opGroup3 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                        await Task.CompletedTask;
                        string opGroup4 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                        Microsoft.Coyote.Specifications.Specification.Assert(opGroup3 == opGroup4, "opGroup3 != opGroup4");

                        Console.WriteLine($"Test-4 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
                    });
                    string opGroup5 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                    Microsoft.Coyote.Specifications.Specification.Assert(opGroup2 == opGroup5, "opGroup2 != opGroup5");

                    Console.WriteLine($"Test-5 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
                });
                string opGroup6 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                Microsoft.Coyote.Specifications.Specification.Assert(opGroup1 == opGroup6, "opGroup1 != opGroup6");

                Console.WriteLine($"Test-6 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
            },
            configuration: this.GetConfiguration().WithTestingIterations(1000).WithPrioritizationStrategy(false, 0));
        }

        [Fact]
        public void Test9()
        {
            this.Test(async () =>
            {
                Console.WriteLine($"Test-1 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                string opGroup1 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                await Task.Run(async () =>
                {
                    Console.WriteLine($"Test-2 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                    string opGroup2 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                    await Task.Run(async () =>
                    {
                        Console.WriteLine($"Test-3 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                        string opGroup3 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                        await Task.Delay(10);
                        string opGroup4 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                        Microsoft.Coyote.Specifications.Specification.Assert(opGroup3 == opGroup4, "opGroup3 != opGroup4");

                        Console.WriteLine($"Test-4 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
                    });
                    string opGroup5 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                    Microsoft.Coyote.Specifications.Specification.Assert(opGroup2 == opGroup5, "opGroup2 != opGroup5");

                    Console.WriteLine($"Test-5 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
                });
                string opGroup6 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                Microsoft.Coyote.Specifications.Specification.Assert(opGroup1 == opGroup6, "opGroup1 != opGroup6");

                Console.WriteLine($"Test-6 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
            },
            configuration: this.GetConfiguration().WithTestingIterations(1000).WithPrioritizationStrategy(false, 0));
        }

        [Fact]
        public void Test10()
        {
            this.Test(async () =>
            {
                Console.WriteLine($"Test-1 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                string opGroup1 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                await Task.Run(async () =>
                {
                    Console.WriteLine($"Test-2 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                    string opGroup2 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                    await Task.Run(async () =>
                    {
                        Console.WriteLine($"Test-3 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                        string opGroup3 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                        await Task.Yield();
                        string opGroup4 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                        Microsoft.Coyote.Specifications.Specification.Assert(opGroup3 == opGroup4, "opGroup3 != opGroup4");

                        Console.WriteLine($"Test-4 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
                    });
                    string opGroup5 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                    Microsoft.Coyote.Specifications.Specification.Assert(opGroup2 == opGroup5, "opGroup2 != opGroup5");

                    Console.WriteLine($"Test-5 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
                });
                string opGroup6 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                Microsoft.Coyote.Specifications.Specification.Assert(opGroup1 == opGroup6, "opGroup1 != opGroup6");

                Console.WriteLine($"Test-6 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
            },
            configuration: this.GetConfiguration().WithTestingIterations(1000).WithPrioritizationStrategy(false, 0));
        }

        [Fact]
        public void Test11()
        {
            this.Test(async () =>
            {
                Console.WriteLine($"Test-1 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                string opGroup1 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                await Task.Run(async () =>
                {
                    Console.WriteLine($"Test-2 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                    string opGroup2 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                    await Task.Run(() =>
                    {
                        Console.WriteLine($"Test-3 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
                    });
                    string opGroup3 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                    Microsoft.Coyote.Specifications.Specification.Assert(opGroup2 == opGroup3, "opGroup2 != opGroup3");

                    Console.WriteLine($"Test-4 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                    string opGroup4 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                    await Task.Run(() =>
                    {
                        Console.WriteLine($"Test-5 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
                    });
                    string opGroup5 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                    Microsoft.Coyote.Specifications.Specification.Assert(opGroup3 == opGroup4, "opGroup3 != opGroup4");
                    Microsoft.Coyote.Specifications.Specification.Assert(opGroup4 == opGroup5, "opGroup4 != opGroup5");

                    Console.WriteLine($"Test-6 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
                });
                string opGroup6 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                Microsoft.Coyote.Specifications.Specification.Assert(opGroup1 == opGroup6, "opGroup1 != opGroup5");

                Console.WriteLine($"Test-7 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
            },
            configuration: this.GetConfiguration().WithTestingIterations(1000).WithPrioritizationStrategy(false, 0));
        }

        // TODO: fix the exception this test case
        // [Fact]
        // public void Test12()
        // {
        //     this.Test(async () =>
        //     {
        //         Console.WriteLine($"Test-1 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

        // Task spawn = new Task(() =>
        //         {
        //             Console.WriteLine($"Test-2 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

        // string opGroup1 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
        //             Microsoft.Coyote.Runtime.SchedulingPoint.Interleave();
        //             string opGroup2 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
        //             Microsoft.Coyote.Specifications.Specification.Assert(opGroup1 == opGroup2, "opGroup1 != opGroup2");

        // Console.WriteLine($"Test-3 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
        //         });
        //         spawn.Start();
        //         Console.WriteLine($"Test-4 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

        // string opGroup3 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
        //         await Task.WhenAll(spawn);
        //         string opGroup4 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
        //         Microsoft.Coyote.Specifications.Specification.Assert(opGroup3 == opGroup4, "opGroup3 != opGroup4");

        // Console.WriteLine($"Test-5 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
        //     },
        //     configuration: this.GetConfiguration().WithTestingIterations(1000).WithPrioritizationStrategy(false, 0));
        // }

        [Fact]
        public void Test13()
        {
            this.Test(async () =>
            {
                Console.WriteLine($"Test-1 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                string opGroup1 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                await Task.Run(async () =>
                {
                    string opGroup2 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();

                    Console.WriteLine($"Test-2 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
                    await Task.Run(() =>
                    {
                        Console.WriteLine($"Test-3 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                        string opGroup3 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                        Microsoft.Coyote.Runtime.SchedulingPoint.Interleave();
                        string opGroup4 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                        Microsoft.Coyote.Specifications.Specification.Assert(opGroup3 == opGroup4, "opGroup3 != opGroup4");

                        Console.WriteLine($"Test-4 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
                    });
                    string opGroup5 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                    Microsoft.Coyote.Specifications.Specification.Assert(opGroup2 == opGroup5, "opGroup2 != opGroup5");

                    Console.WriteLine($"Test-5 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                    string opGroup6 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                    await Task.Run(() =>
                    {
                        Console.WriteLine($"Test-6 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");

                        string opGroup7 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                        Microsoft.Coyote.Runtime.SchedulingPoint.Interleave();
                        string opGroup8 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                        Microsoft.Coyote.Specifications.Specification.Assert(opGroup7 == opGroup8, "opGroup7 != opGroup8");

                        Console.WriteLine($"Test-7 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
                    });
                    string opGroup9 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                    Microsoft.Coyote.Specifications.Specification.Assert(opGroup5 == opGroup6, "opGroup5 != opGroup6");
                    Microsoft.Coyote.Specifications.Specification.Assert(opGroup6 == opGroup9, "opGroup6 != opGroup9");

                    Console.WriteLine($"Test-8 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
                });
                string opGroup10 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
                Microsoft.Coyote.Specifications.Specification.Assert(opGroup1 == opGroup10, "opGroup1 != opGroup10");

                Console.WriteLine($"Test-9 (thread: {Thread.CurrentThread.ManagedThreadId}, task: {Task.CurrentId})");
            },
            configuration: this.GetConfiguration().WithTestingIterations(1000).WithPrioritizationStrategy(false, 0));
        }

        // TODO: think how to add such test cases which call lambda's of other tests
        // [Fact]
        // public void Test14()
        // {
        //     this.Test(async () =>
        //     {
        //         string opGroup1 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();

        // List<Task> list_of_tasks = new List<Task>();
        //         Task task_13 = Test13();
        //         list_of_tasks.Add(task_13);

        // for (int i=0; i<10; i++)
        //         {
        //             Task task1 = Test1();
        //             list_of_tasks.Add(task1);
        //         }

        // string opGroup2 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
        //         string opGroup3 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
        //         for(int i=0; i<list_of_tasks.Count; i++)
        //         {
        //             opGroup2 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
        //             await list_of_tasks[i];
        //             opGroup3 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();
        //             Microsoft.Coyote.Specifications.Specification.Assert(opGroup2 == opGroup3, "opGroup2 != opGroup3");
        //         }

        // string opGroup4 = Microsoft.Coyote.Runtime.SchedulingPoint.GiveExecutingOperationGroup();

        // Microsoft.Coyote.Specifications.Specification.Assert(opGroup1 == opGroup2, "opGroup1 != opGroup2");
        //         Microsoft.Coyote.Specifications.Specification.Assert(opGroup2 == opGroup3, "opGroup2 != opGroup3");
        //         Microsoft.Coyote.Specifications.Specification.Assert(opGroup3 == opGroup4, "opGroup3 != opGroup4");
        //     },
        //     configuration: this.GetConfiguration().WithTestingIterations(1000).WithPrioritizationStrategy(false, 0));
        // }
    }
}
