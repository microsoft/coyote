// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;
using TaskCanceledException = System.Threading.Tasks.TaskCanceledException;

#if BINARY_REWRITE
namespace Microsoft.Coyote.BinaryRewriting.Tests.Tasks
#else
namespace Microsoft.Coyote.Production.Tests.Tasks
#endif
{
#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler
    public class TaskFactoryTests : BaseProductionTest
    {
        public TaskFactoryTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Factory.StartNew(() =>
                {
                    entry.Value = 5;
                });

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Factory.StartNew(() =>
                {
                    entry.Value = 3;
                });

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewTaskWithSynchronousAwait()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Factory.StartNew(async () =>
                {
                    await Task.CompletedTask;
                    entry.Value = 5;
                }).Unwrap();

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewTaskWithSynchronousFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Factory.StartNew(async () =>
                {
                    await Task.CompletedTask;
                    entry.Value = 3;
                }).Unwrap();

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewTaskWithAsynchronousAwait()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(1);
                    entry.Value = 5;
                }).Unwrap();

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewTaskWithAsynchronousFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(1);
                    entry.Value = 3;
                }).Unwrap();

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewNestedTaskWithSynchronousAwait()
        {
            this.Test(async () =>
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
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedTaskWithSynchronousFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Factory.StartNew(async () =>
                {
                    await Task.Factory.StartNew(async () =>
                    {
                        await Task.CompletedTask;
                        entry.Value = 5;
                    }).Unwrap();

                    entry.Value = 3;
                }).Unwrap();

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedTaskWithAsynchronousAwait()
        {
            this.Test(async () =>
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
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedTaskWithAsynchronousFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await Task.Factory.StartNew(async () =>
                {
                    await Task.Factory.StartNew(async () =>
                    {
                        await Task.Delay(1);
                        entry.Value = 5;
                    }).Unwrap();

                    entry.Value = 3;
                }).Unwrap();

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Factory.StartNew(() =>
                {
                    entry.Value = 5;
                    return entry.Value;
                });

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Factory.StartNew(() =>
                {
                    entry.Value = 3;
                    return entry.Value;
                });

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewTaskWithSynchronousResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Factory.StartNew(async () =>
                {
                    await Task.CompletedTask;
                    entry.Value = 5;
                    return entry.Value;
                }).Unwrap();

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewTaskWithSynchronousResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Factory.StartNew(async () =>
                {
                    await Task.CompletedTask;
                    entry.Value = 3;
                    return entry.Value;
                }).Unwrap();

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewTaskWithAsynchronousResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(1);
                    entry.Value = 5;
                    return entry.Value;
                }).Unwrap();

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewTaskWithAsynchronousResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(1);
                    entry.Value = 3;
                    return entry.Value;
                }).Unwrap();

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewNestedTaskWithSynchronousResult()
        {
            this.Test(async () =>
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
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewNestedTaskWithSynchronousResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Factory.StartNew(async () =>
                {
                    return await Task.Factory.StartNew(async () =>
                    {
                        await Task.CompletedTask;
                        entry.Value = 3;
                        return entry.Value;
                    }).Unwrap();
                }).Unwrap();

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewNestedTaskWithAsynchronousResult()
        {
            this.Test(async () =>
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
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestStartNewNestedTaskWithAsynchronousResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await Task.Factory.StartNew(async () =>
                {
                    return await Task.Factory.StartNew(async () =>
                    {
                        await Task.Delay(1);
                        entry.Value = 3;
                        return entry.Value;
                    }).Unwrap();
                }).Unwrap();

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
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
