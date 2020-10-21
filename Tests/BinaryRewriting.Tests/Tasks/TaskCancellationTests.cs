// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using TaskCanceledException = System.Threading.Tasks.TaskCanceledException;

namespace Microsoft.Coyote.BinaryRewriting.Tests.Tasks
{
    public class TaskCancellationTests : BaseRewritingTest
    {
        public TaskCancellationTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestAlreadyCanceledParallelTask()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationToken ct = new CancellationToken(true);
                await Task.Run(() => { }, ct);
            },
            configuration: GetConfiguration().WithTestingIterations(1),
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAlreadyCanceledAsynchronousTask()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationToken ct = new CancellationToken(true);
                await Task.Run(async () =>
                {
                    await Task.Delay(1);
                }, ct);
            },
            configuration: GetConfiguration().WithTestingIterations(1),
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAlreadyCanceledParallelTaskWithResult()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationToken ct = new CancellationToken(true);
                await Task.Run(() => 3, ct);
            },
            configuration: GetConfiguration().WithTestingIterations(1),
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAlreadyCanceledAsynchronousTaskWithResult()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationToken ct = new CancellationToken(true);
                await Task.Run(async () =>
                {
                    await Task.Delay(1);
                    return 3;
                }, ct);
            },
            configuration: GetConfiguration().WithTestingIterations(1),
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestCancelParallelTask()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                var task = Task.Run(() => { }, cts.Token);
                cts.Cancel();
                await task;
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestCancelAsynchronousTask()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                var task = Task.Run(async () =>
                {
                    await Task.Delay(1);
                }, cts.Token);

                cts.Cancel();
                await task;
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestCancelParallelTaskWithResult()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                var task = Task.Run(() => 3, cts.Token);
                cts.Cancel();
                await task;
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestCancelAsynchronousTaskWithResult()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                var task = Task.Run(async () =>
                {
                    await Task.Delay(1);
                    return 3;
                }, cts.Token);

                cts.Cancel();
                await task;
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedAsynchronousTask()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                var task = Task.Run(async () =>
                {
                    await Task.Run(async () =>
                    {
                        await Task.Delay(1);
                    }, cts.Token);
                });

                cts.Cancel();
                await task;
            },
            configuration: GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestCancelNestedAsynchronousTaskWithResult()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                var task = Task.Run(async () =>
                {
                    return await Task.Run(async () =>
                    {
                        await Task.Delay(1);
                        return 3;
                    }, cts.Token);
                });

                cts.Cancel();
                await task;
            },
            configuration: GetConfiguration().WithTestingIterations(100));
        }
    }
}
