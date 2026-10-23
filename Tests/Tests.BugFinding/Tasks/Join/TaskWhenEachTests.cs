// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET10_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class TaskWhenEachTests : BaseBugFindingTest
    {
        public TaskWhenEachTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestWhenEachWithTasksCompletedByAnotherOperation()
        {
            this.Test(async () =>
            {
                var first = new TaskCompletionSource<bool>();
                var second = new TaskCompletionSource<bool>();

                // Complete the tasks from another operation, which pauses the enumeration.
                Task producer = Task.Run(async () =>
                {
                    await Task.Yield();
                    first.SetResult(true);
                    await Task.Yield();
                    second.SetResult(true);
                });

                var yielded = new List<Task>();
                await foreach (Task task in Task.WhenEach(first.Task, second.Task))
                {
                    Specification.Assert(task.IsCompleted, "Yielded a task that has not completed.");
                    yielded.Add(task);
                }

                await producer;
                Specification.Assert(yielded.Count is 2, "Yielded {0} tasks instead of 2.", yielded.Count);
                Specification.Assert(ReferenceEquals(yielded[0], first.Task), "Yielded an unexpected first task.");
                Specification.Assert(ReferenceEquals(yielded[1], second.Task), "Yielded an unexpected second task.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestWhenEachYieldsTasksInCompletionOrder()
        {
            this.Test(async () =>
            {
                var first = new TaskCompletionSource<bool>();
                var second = new TaskCompletionSource<bool>();
                await using IAsyncEnumerator<Task> enumerator =
                    Task.WhenEach(first.Task, second.Task).GetAsyncEnumerator();

                // Complete the tasks in the reverse order from other operations, which pauses
                // the enumeration until each of the tasks completes.
                Task completeSecond = Task.Run(() => second.SetResult(true));
                Specification.Assert(await enumerator.MoveNextAsync(), "The enumeration completed early.");
                Specification.Assert(ReferenceEquals(enumerator.Current, second.Task),
                    "Yielded a task that is not the first task to complete.");

                Task completeFirst = Task.Run(() => first.SetResult(true));
                Specification.Assert(await enumerator.MoveNextAsync(), "The enumeration completed early.");
                Specification.Assert(ReferenceEquals(enumerator.Current, first.Task),
                    "Yielded a task that is not the second task to complete.");
                Specification.Assert(!await enumerator.MoveNextAsync(), "The enumeration did not complete.");

                await Task.WhenAll(completeSecond, completeFirst);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Theory(Timeout = 5000)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void TestWhenEachYieldsBatchedCompletionsInOrder(bool isGeneric, bool runContinuationsAsynchronously)
        {
            this.Test(async () =>
            {
                TaskCreationOptions options = runContinuationsAsynchronously ?
                    TaskCreationOptions.RunContinuationsAsynchronously : TaskCreationOptions.None;
                var first = new TaskCompletionSource<int>(options);
                var second = new TaskCompletionSource<int>(options);
                IAsyncEnumerable<Task> tasks = isGeneric ?
                    Task.WhenEach(first.Task, second.Task) :
                    Task.WhenEach((Task)first.Task, (Task)second.Task);

                second.SetResult(2);
                first.SetResult(1);

                var yielded = new List<Task>();
                await foreach (Task task in tasks)
                {
                    yielded.Add(task);
                }

                Specification.Assert(yielded.Count is 2, "Yielded {0} tasks instead of 2.", yielded.Count);
                Specification.Assert(ReferenceEquals(yielded[0], second.Task),
                    "Yielded a task that is not the first task to complete.");
                Specification.Assert(ReferenceEquals(yielded[1], first.Task),
                    "Yielded a task that is not the second task to complete.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestWhenEachCapturesBatchedCompletionsBetweenMoves(bool runContinuationsAsynchronously)
        {
            this.Test(async () =>
            {
                TaskCreationOptions options = runContinuationsAsynchronously ?
                    TaskCreationOptions.RunContinuationsAsynchronously : TaskCreationOptions.None;
                var first = new TaskCompletionSource<bool>(options);
                var second = new TaskCompletionSource<bool>(options);
                var third = new TaskCompletionSource<bool>(options);
                IEnumerable<Task> tasks = new Task[] { first.Task, second.Task, third.Task };
                await using IAsyncEnumerator<Task> enumerator = Task.WhenEach(tasks).GetAsyncEnumerator();

                first.SetResult(true);
                Specification.Assert(await enumerator.MoveNextAsync(), "The enumeration completed early.");
                Specification.Assert(ReferenceEquals(enumerator.Current, first.Task), "Yielded an unexpected task.");

                third.SetResult(true);
                second.SetResult(true);
                Specification.Assert(await enumerator.MoveNextAsync(), "The enumeration completed early.");
                Specification.Assert(ReferenceEquals(enumerator.Current, third.Task),
                    "Lost the completion order while enumeration was inactive.");
                Specification.Assert(await enumerator.MoveNextAsync(), "The enumeration completed early.");
                Specification.Assert(ReferenceEquals(enumerator.Current, second.Task), "Yielded an unexpected task.");
                Specification.Assert(!await enumerator.MoveNextAsync(), "The enumeration did not complete.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestWhenEachWithGenericTasksCompletedByOtherOperations()
        {
            this.Test(async () =>
            {
                var first = new TaskCompletionSource<int>();
                var second = new TaskCompletionSource<int>();

                // Complete each task from a different operation, which pauses the enumeration.
                Task completeFirst = Task.Run(() => first.SetResult(3));
                Task completeSecond = Task.Run(() => second.SetResult(5));

                int count = 0;
                int sum = 0;
                await foreach (Task<int> task in Task.WhenEach(first.Task, second.Task))
                {
                    Specification.Assert(task.IsCompleted, "Yielded a task that has not completed.");
                    count++;
                    sum += task.Result;
                }

                await Task.WhenAll(completeFirst, completeSecond);
                Specification.Assert(count is 2, "Yielded {0} tasks instead of 2.", count);
                Specification.Assert(sum is 8, "Yielded results that sum to {0} instead of 8.", sum);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestWhenEachWithEnumerableCompletedByOtherOperations()
        {
            this.Test(async () =>
            {
                var first = new TaskCompletionSource<bool>();
                var second = new TaskCompletionSource<bool>();
                IEnumerable<Task> tasks = new List<Task> { first.Task, second.Task };

                // Complete each task from a different operation, which pauses the enumeration.
                Task completeFirst = Task.Run(() => first.SetResult(true));
                Task completeSecond = Task.Run(() => second.SetResult(true));

                var yielded = new List<Task>();
                await foreach (Task task in Task.WhenEach(tasks))
                {
                    Specification.Assert(task.IsCompleted, "Yielded a task that has not completed.");
                    yielded.Add(task);
                }

                await Task.WhenAll(completeFirst, completeSecond);
                Specification.Assert(yielded.Count is 2, "Yielded {0} tasks instead of 2.", yielded.Count);
                Specification.Assert(yielded.Contains(first.Task), "The first task was not yielded.");
                Specification.Assert(yielded.Contains(second.Task), "The second task was not yielded.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestWhenEachStopsEnumerationEarlyWithPendingTask()
        {
            this.Test(async () =>
            {
                var first = new TaskCompletionSource<bool>();
                var second = new TaskCompletionSource<bool>();
                Task completeFirst = Task.Run(() => first.SetResult(true));

                int count = 0;
                await foreach (Task task in Task.WhenEach(first.Task, second.Task))
                {
                    Specification.Assert(ReferenceEquals(task, first.Task), "Yielded an unexpected task.");
                    count++;
                    break;
                }

                await completeFirst;
                second.SetResult(true);
                await second.Task;
                Specification.Assert(count is 1, "Yielded {0} tasks instead of 1.", count);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestWhenEachCancelledByAnotherOperation()
        {
            this.Test(async () =>
            {
                var pending = new TaskCompletionSource<bool>();
                using var source = new CancellationTokenSource();
                await using IAsyncEnumerator<Task> enumerator =
                    Task.WhenEach(pending.Task).GetAsyncEnumerator(source.Token);

                // Cancel from another operation, which resumes the paused enumeration.
                Task canceller = Task.Run(() => source.Cancel());

                bool isCancelled = false;
                try
                {
                    await enumerator.MoveNextAsync();
                }
                catch (OperationCanceledException)
                {
                    isCancelled = true;
                }

                Specification.Assert(isCancelled, "The enumeration was not cancelled.");
                await canceller;
                pending.SetResult(true);
                await pending.Task;
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestWhenEachDeadlock()
        {
            this.TestWithError(async () =>
            {
                // Test that the enumeration deadlocks because one of the tasks cannot complete until later.
                var tcs = new TaskCompletionSource<bool>();
                await foreach (Task task in Task.WhenEach(tcs.Task, Task.Delay(1)))
                {
                }

                tcs.SetResult(true);
                await tcs.Task;
            },
            errorChecker: (e) =>
            {
                Assert.StartsWith("Deadlock detected.", e);
            },
            replay: true);
        }
    }
}
#endif
