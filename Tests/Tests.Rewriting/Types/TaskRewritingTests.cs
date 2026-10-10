// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class TaskRewritingTests : BaseRewritingTest
    {
        public TaskRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWhenAll()
        {
            Task.WhenAll(Task.CompletedTask);
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingGenericTaskWhenAll()
        {
            Task.WhenAll(Task.FromResult(1));
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWhenAny()
        {
            Task.WhenAny(Task.CompletedTask);
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingGenericTaskWhenAny()
        {
            Task.WhenAny(Task.FromResult(1));
        }

#if NET10_0_OR_GREATER
        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWhenAllWithCompilerSelectedSpanOverload()
        {
            Task.WhenAll(Task.CompletedTask, Task.CompletedTask);
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWhenAllWithSpan()
        {
            ReadOnlySpan<Task> tasks = new Task[] { Task.CompletedTask, Task.CompletedTask };
            Task.WhenAll(tasks);
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingGenericTaskWhenAllWithSpan()
        {
            ReadOnlySpan<Task<int>> tasks = new Task<int>[] { Task.FromResult(1), Task.FromResult(2) };
            Task.WhenAll(tasks);
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingGenericTaskWhenAllWithCompilerSelectedSpanOverload()
        {
            Task.WhenAll(Task.FromResult(1), Task.FromResult(2));
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWhenAnyWithSpan()
        {
            ReadOnlySpan<Task> tasks = new Task[] { Task.CompletedTask, Task.CompletedTask };
            Task.WhenAny(tasks);
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingGenericTaskWhenAnyWithSpan()
        {
            ReadOnlySpan<Task<int>> tasks = new Task<int>[] { Task.FromResult(1), Task.FromResult(2) };
            Task.WhenAny(tasks);
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWaitAllWithCompilerSelectedSpanOverload()
        {
            Task.WaitAll(Task.CompletedTask, Task.CompletedTask);
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWaitAllWithSpan()
        {
            ReadOnlySpan<Task> tasks = new Task[] { Task.CompletedTask, Task.CompletedTask };
            Task.WaitAll(tasks);
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWhenAllWithSpanSemantics()
        {
            this.Test(async () =>
            {
                await Task.WhenAll([]);
                await Task.WhenAll([Task.CompletedTask]);
                await Task.WhenAll(Task.Run(() => { }), Task.Run(() => { }));

                int[] results = await Task.WhenAll([Task.FromResult(2), Task.FromResult(1)]);
                Assert.Equal(new[] { 2, 1 }, results);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWhenAllWithSpanFailureSemantics()
        {
            this.Test(async () =>
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    Task.WhenAll([Task.FromException(new InvalidOperationException())]));

                using var source = new CancellationTokenSource();
                source.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    Task.WhenAll([Task.FromCanceled(source.Token)]));

                Assert.Throws<ArgumentException>(() =>
                {
                    _ = Task.WhenAll((ReadOnlySpan<Task>)new Task[] { null });
                });
            });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWhenAnyWithSpanSemantics()
        {
            this.Test(async () =>
            {
                Task first = Task.CompletedTask;
                Task<int> genericFirst = Task.FromResult(1);
                Assert.Same(first, await Task.WhenAny([first]));
                Assert.Same(first, await Task.WhenAny(first, Task.Delay(1)));
                Assert.Same(genericFirst, await Task.WhenAny<int>([genericFirst, Task.FromResult(2)]));
                Assert.Throws<ArgumentException>(() =>
                {
                    _ = Task.WhenAny((ReadOnlySpan<Task>)[]);
                });
                Assert.Throws<ArgumentException>(() =>
                {
                    _ = Task.WhenAny((ReadOnlySpan<Task>)new Task[] { null });
                });
            });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWaitAllWithSpanSemantics()
        {
            this.Test(() =>
            {
                Task.WaitAll([]);
                Task.WaitAll([Task.CompletedTask]);
                Task.WaitAll(Task.Run(() => { }), Task.Run(() => { }));
                Assert.Throws<ArgumentException>(() =>
                    Task.WaitAll((ReadOnlySpan<Task>)new Task[] { null }));
                Assert.Throws<AggregateException>(() =>
                    Task.WaitAll((ReadOnlySpan<Task>)new Task[]
                    {
                        Task.FromException(new InvalidOperationException())
                    }));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWhenEachOverloads()
        {
            Task[] tasks = new[] { Task.CompletedTask };
            ReadOnlySpan<Task> taskSpan = tasks;
            IEnumerable<Task> taskEnumerable = tasks;
            _ = Task.WhenEach(tasks);
            _ = Task.WhenEach(taskSpan);
            _ = Task.WhenEach(taskEnumerable);

            Task<int>[] genericTasks = new[] { Task.FromResult(1) };
            ReadOnlySpan<Task<int>> genericTaskSpan = genericTasks;
            IEnumerable<Task<int>> genericTaskEnumerable = genericTasks;
            _ = Task.WhenEach(genericTasks);
            _ = Task.WhenEach(genericTaskSpan);
            _ = Task.WhenEach(genericTaskEnumerable);
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWhenEachCompletionOrder()
        {
            this.Test(async () =>
            {
                var first = new TaskCompletionSource<bool>();
                var second = new TaskCompletionSource<bool>();
                await using IAsyncEnumerator<Task> enumerator =
                    Task.WhenEach(first.Task, second.Task).GetAsyncEnumerator();

                second.SetResult(true);
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Same(second.Task, enumerator.Current);

                first.SetResult(true);
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Same(first.Task, enumerator.Current);
                Assert.False(await enumerator.MoveNextAsync());
            });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingGenericTaskWhenEachCompletionStates()
        {
            this.Test(async () =>
            {
                using var source = new CancellationTokenSource();
                source.Cancel();
                Task<int> completed = Task.FromResult(1);
                Task<int> faulted = Task.FromException<int>(new InvalidOperationException());
                Task<int> canceled = Task.FromCanceled<int>(source.Token);
                var yielded = new List<Task<int>>();

                await foreach (Task<int> task in Task.WhenEach<int>([completed, faulted, canceled]))
                {
                    yielded.Add(task);
                }

                Assert.Equal(3, yielded.Count);
                Assert.Contains(completed, yielded);
                Assert.Contains(faulted, yielded);
                Assert.Contains(canceled, yielded);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWhenEachEmptyDuplicateAndEarlyStop()
        {
            this.Test(async () =>
            {
                int count = 0;
                await foreach (Task task in Task.WhenEach((Task[])[]))
                {
                    count++;
                }

                Assert.Equal(0, count);

                Task duplicate = Task.CompletedTask;
                await foreach (Task task in Task.WhenEach([duplicate, duplicate]))
                {
                    Assert.Same(duplicate, task);
                    count++;
                }

                Assert.Equal(2, count);

                await foreach (Task task in Task.WhenEach([Task.CompletedTask, Task.CompletedTask]))
                {
                    count++;
                    break;
                }

                Assert.Equal(3, count);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWhenEachArgumentValidation()
        {
            // Assert the same validation semantics with and without a controlled runtime.
            AssertWhenEachArgumentValidation();
            this.Test(() => AssertWhenEachArgumentValidation());
        }

        private static void AssertWhenEachArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                _ = Task.WhenEach((IEnumerable<Task>)null);
            });
            Assert.Throws<ArgumentNullException>(() =>
            {
                _ = Task.WhenEach((IEnumerable<Task<int>>)null);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                _ = Task.WhenEach((ReadOnlySpan<Task>)new Task[] { null });
            });
            Assert.Throws<ArgumentException>(() =>
            {
                _ = Task.WhenEach((IEnumerable<Task>)new Task[] { null });
            });
            Assert.Throws<ArgumentException>(() =>
            {
                _ = Task.WhenEach((ReadOnlySpan<Task<int>>)new Task<int>[] { null });
            });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWaitAsyncOverloads()
        {
            this.Test(async () =>
            {
                using var source = new CancellationTokenSource();
                await Task.CompletedTask.WaitAsync(source.Token);
                await Task.CompletedTask.WaitAsync(TimeSpan.Zero);
                await Task.CompletedTask.WaitAsync(TimeSpan.Zero, source.Token);
                await Task.CompletedTask.WaitAsync(TimeSpan.Zero, TimeProvider.System);
                await Task.CompletedTask.WaitAsync(TimeSpan.Zero, TimeProvider.System, source.Token);

                Assert.Equal(1, await Task.FromResult(1).WaitAsync(source.Token));
                Assert.Equal(1, await Task.FromResult(1).WaitAsync(TimeSpan.Zero));
                Assert.Equal(1, await Task.FromResult(1).WaitAsync(TimeSpan.Zero, source.Token));
                Assert.Equal(1, await Task.FromResult(1).WaitAsync(TimeSpan.Zero, TimeProvider.System));
                Assert.Equal(1, await Task.FromResult(1).WaitAsync(
                    TimeSpan.Zero, TimeProvider.System, source.Token));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWaitAsyncTimeoutAndCancellation()
        {
            this.Test(async () =>
            {
                var pending = new TaskCompletionSource<bool>();
                await Assert.ThrowsAsync<TimeoutException>(() => pending.Task.WaitAsync(TimeSpan.Zero));

                using var source = new CancellationTokenSource();
                source.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.Task.WaitAsync(source.Token));
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    Task.FromException(new InvalidOperationException()).WaitAsync(TimeSpan.FromMilliseconds(1)));
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    _ = pending.Task.WaitAsync(TimeSpan.FromMilliseconds(-2));
                });
            });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskDelayWithSystemTimeProvider()
        {
            this.Test(async () =>
            {
                await Task.Delay(TimeSpan.Zero, TimeProvider.System);
                await Task.Delay(TimeSpan.Zero, TimeProvider.System, default);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingCustomTimeProviderIsExplicitlyUnsupported()
        {
            this.TestWithError(async () =>
            {
                await Task.Delay(TimeSpan.Zero, new CustomTimeProvider());
            },
            expectedError: "Custom time providers are not supported in systematic testing.");
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingWaitAsyncCustomTimeProviderIsExplicitlyUnsupported()
        {
            this.TestWithError(async () =>
            {
                var pending = new TaskCompletionSource<bool>();
                await pending.Task.WaitAsync(TimeSpan.Zero, new CustomTimeProvider());
            },
            expectedError: "Custom time providers are not supported in systematic testing.");
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWaitAllWithEnumerable()
        {
            this.Test(() =>
            {
                IEnumerable<Task> tasks = new[] { Task.CompletedTask, Task.CompletedTask };
                Task.WaitAll(tasks);
                Task.WaitAll(tasks, default);
            });
        }

        private sealed class CustomTimeProvider : TimeProvider
        {
        }

        [Fact(Timeout = 5000)]
        public void TestNet10CompilerCallsAreRewrittenToControlledSignatures()
        {
            string assemblyPath = typeof(TaskRewritingTests).Assembly.Location;
            string diff = File.ReadAllText(Path.ChangeExtension(assemblyPath, ".diff.json"));
            Assert.Contains(
                "System.Threading.Tasks.Task System.Threading.Tasks.Task::WhenAll(" +
                "System.ReadOnlySpan`1<System.Threading.Tasks.Task>)",
                diff);
            Assert.Contains(
                "System.Threading.Tasks.Task " +
                "Microsoft.Coyote.Rewriting.Types.Threading.Tasks.Task::WhenAll(" +
                "System.ReadOnlySpan`1<System.Threading.Tasks.Task>)",
                diff);
            Assert.Contains(
                "System.Threading.Lock/Scope System.Threading.Lock::EnterScope()",
                diff);
            Assert.Contains(
                "Microsoft.Coyote.Rewriting.Types.Threading.Lock::EnterScope(System.Threading.Lock)",
                diff);
            Assert.Contains(
                "System.Threading.Tasks.Task::WhenEach(System.ReadOnlySpan`1<System.Threading.Tasks.Task>)",
                diff);
            Assert.Contains(
                "Microsoft.Coyote.Rewriting.Types.Threading.Tasks.Task::WhenEach(" +
                "System.ReadOnlySpan`1<System.Threading.Tasks.Task>)",
                diff);
        }
#endif
    }
}
