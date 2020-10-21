// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;
using TaskCanceledException = System.Threading.Tasks.TaskCanceledException;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler
    public class TaskFactoryTests : BaseSystematicTest
    {
        public TaskFactoryTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Factory.StartNew(() =>
                {
                    entry.Value = 5;
                });

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewTaskWithSynchronousAwait()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Factory.StartNew(async () =>
                {
                    await Task.CompletedTask;
                    entry.Value = 5;
                }).Unwrap();

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewTaskWithAsynchronousAwait()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(1);
                    entry.Value = 5;
                }).Unwrap();

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewNestedTaskWithSynchronousAwait()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Factory.StartNew(async () =>
                {
                    await Task.Factory.StartNew(async () =>
                    {
                        await Task.CompletedTask;
                        entry.Value = 3;
                    }).Unwrap();

                    entry.Value = 5;
                }).Unwrap();

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewNestedTaskWithAsynchronousAwait()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Factory.StartNew(async () =>
                {
                    await Task.Factory.StartNew(async () =>
                    {
                        await Task.Delay(1);
                        entry.Value = 3;
                    }).Unwrap();

                    entry.Value = 5;
                }).Unwrap();

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewTaskWithResult()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Factory.StartNew(() =>
                {
                    entry.Value = 5;
                    return entry.Value;
                });

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewTaskWithSynchronousResult()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Factory.StartNew(async () =>
                {
                    await Task.CompletedTask;
                    entry.Value = 5;
                    return entry.Value;
                }).Unwrap();

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewTaskWithAsynchronousResult()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(1);
                    entry.Value = 5;
                    return entry.Value;
                }).Unwrap();

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewNestedTaskWithSynchronousResult()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Factory.StartNew(async () =>
                {
                    return await Task.Factory.StartNew(async () =>
                    {
                        await Task.CompletedTask;
                        entry.Value = 5;
                        return entry.Value;
                    }).Unwrap();
                }).Unwrap();

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewNestedTaskWithAsynchronousResult()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Factory.StartNew(async () =>
                {
                    return await Task.Factory.StartNew(async () =>
                    {
                        await Task.Delay(1);
                        entry.Value = 5;
                        return entry.Value;
                    }).Unwrap();
                }).Unwrap();

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestGenericStartNewTaskWithResult()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task<int>.Factory.StartNew(() =>
                {
                    entry.Value = 5;
                    return entry.Value;
                });

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestGenericStartNewTaskWithSynchronousResult()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task<Task<int>>.Factory.StartNew(async () =>
                {
                    await Task.CompletedTask;
                    entry.Value = 5;
                    return entry.Value;
                }).Unwrap();

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestGenericStartNewTaskWithAsynchronousResult()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task<Task<int>>.Factory.StartNew(async () =>
                {
                    await Task.Delay(1);
                    entry.Value = 5;
                    return entry.Value;
                }).Unwrap();

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestGenericStartNewNestedTaskWithSynchronousResult()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task<Task<int>>.Factory.StartNew(async () =>
                {
                    return await Task<Task<int>>.Factory.StartNew(async () =>
                    {
                        await Task.CompletedTask;
                        entry.Value = 5;
                        return entry.Value;
                    }).Unwrap();
                }).Unwrap();

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestGenericStartNewNestedTaskWithAsynchronousResult()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task<Task<int>>.Factory.StartNew(async () =>
                {
                    return await Task<Task<int>>.Factory.StartNew(async () =>
                    {
                        await Task.Delay(1);
                        entry.Value = 5;
                        return entry.Value;
                    }).Unwrap();
                }).Unwrap();

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewCanceledTask()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationToken ct = new CancellationToken(true);
                await Task.Factory.StartNew(() => { }, ct);
            },
            configuration: GetConfiguration().WithTestingIterations(1),
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewTaskCancelation()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                var task = Task.Factory.StartNew(() => { }, cts.Token);
                cts.Cancel();
                await task;
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewCanceledTaskWithAsynchronousAwait()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationToken ct = new CancellationToken(true);
                await Task.Factory.StartNew(async () => await Task.Delay(1), ct).Unwrap();
            },
            configuration: GetConfiguration().WithTestingIterations(1),
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewTaskCancelationWithAsynchronousAwait()
        {
            this.TestWithException<TaskCanceledException>(async () =>
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                var task = Task.Factory.StartNew(async () => await Task.Delay(1), cts.Token).Unwrap();
                cts.Cancel();
                await task;
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            replay: true);
        }
    }
#pragma warning restore CA2008 // Do not create tasks without passing a TaskScheduler
}
