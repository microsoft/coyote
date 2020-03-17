// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class MixedUncontrolledTaskAwaitTests : BaseSystematicTest
    {
        public MixedUncontrolledTaskAwaitTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestMixedUncontrolledAwaitSynchronousTask()
        {
            this.Test(async () =>
            {
                async SystemTasks.Task CallAsync()
                {
                    await SystemTasks.Task.CompletedTask;
                }

                await CallAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestMixedUncontrolledAwaitSynchronousTaskInControlledTask()
        {
            this.Test(async () =>
            {
                async Task CallAsync()
                {
                    await SystemTasks.Task.CompletedTask;
                }

                await CallAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestMixedUncontrolledAwaitAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                async SystemTasks.Task CallAsync()
                {
                    await SystemTasks.Task.Delay(10);
                }

                await CallAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Controlled task '' is trying to wait for an uncontrolled task or awaiter to complete. " +
                "Please make sure to use Coyote APIs to express concurrency ().",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestMixedUncontrolledAwaitAsynchronousTaskInControlledTask()
        {
            this.TestWithError(async () =>
            {
                async Task CallAsync()
                {
                    await SystemTasks.Task.Delay(10);
                }

                await CallAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Controlled task '' is trying to wait for an uncontrolled task or awaiter to complete. " +
                "Please make sure to use Coyote APIs to express concurrency ().",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestMixedUncontrolledAwaitNestedSynchronousTask()
        {
            this.Test(async () =>
            {
                async SystemTasks.Task NestedCallAsync()
                {
                    async SystemTasks.Task CallAsync()
                    {
                        await SystemTasks.Task.CompletedTask;
                    }

                    await SystemTasks.Task.CompletedTask;
                    await CallAsync();
                }

                await NestedCallAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestMixedUncontrolledAwaitNestedSynchronousTaskInControlledTask()
        {
            this.Test(async () =>
            {
                async SystemTasks.Task NestedCallAsync()
                {
                    async Task CallAsync()
                    {
                        await SystemTasks.Task.CompletedTask;
                    }

                    await SystemTasks.Task.CompletedTask;
                    await CallAsync();
                }

                await NestedCallAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestMixedUncontrolledAwaitNestedAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                async SystemTasks.Task NestedCallAsync()
                {
                    async SystemTasks.Task CallAsync()
                    {
                        await SystemTasks.Task.Delay(10);
                    }

                    await SystemTasks.Task.Delay(10);
                    await CallAsync();
                }

                await NestedCallAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Controlled task '' is trying to wait for an uncontrolled task or awaiter to complete. " +
                "Please make sure to use Coyote APIs to express concurrency ().",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestMixedUncontrolledAwaitNestedAsynchronousTaskInControlledTask()
        {
            this.TestWithError(async () =>
            {
                async SystemTasks.Task NestedCallAsync()
                {
                    async Task CallAsync()
                    {
                        await SystemTasks.Task.Delay(10);
                    }

                    await SystemTasks.Task.Delay(10);
                    await CallAsync();
                }

                await NestedCallAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Controlled task '' is trying to wait for an uncontrolled task or awaiter to complete. " +
                "Please make sure to use Coyote APIs to express concurrency ().",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestMixedUncontrolledAwaitSynchronousTaskWithResult()
        {
            this.Test(async () =>
            {
                async SystemTasks.Task<int> GetWriteResultAsync()
                {
                    await SystemTasks.Task.CompletedTask;
                    return 5;
                }

                await GetWriteResultAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestMixedUncontrolledAwaitSynchronousTaskInControlledTaskWithResult()
        {
            this.Test(async () =>
            {
                async Task<int> GetWriteResultAsync()
                {
                    await SystemTasks.Task.CompletedTask;
                    return 5;
                }

                await GetWriteResultAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestMixedUncontrolledAwaitAsynchronousTaskWithResult()
        {
            this.TestWithError(async () =>
            {
                async SystemTasks.Task<int> GetWriteResultWithDelayAsync()
                {
                    await SystemTasks.Task.Delay(10);
                    return 5;
                }

                await GetWriteResultWithDelayAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Controlled task '' is trying to wait for an uncontrolled task or awaiter to complete. " +
                "Please make sure to use Coyote APIs to express concurrency ().",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestMixedUncontrolledAwaitAsynchronousTaskInControlledTaskWithResult()
        {
            this.TestWithError(async () =>
            {
                async Task<int> GetWriteResultWithDelayAsync()
                {
                    await SystemTasks.Task.Delay(10);
                    return 5;
                }

                await GetWriteResultWithDelayAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Controlled task '' is trying to wait for an uncontrolled task or awaiter to complete. " +
                "Please make sure to use Coyote APIs to express concurrency ().",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestMixedUncontrolledAwaitNestedSynchronousTaskWithResult()
        {
            this.Test(async () =>
            {
                async SystemTasks.Task<int> NestedGetWriteResultAsync()
                {
                    async SystemTasks.Task<int> GetWriteResultAsync()
                    {
                        await SystemTasks.Task.CompletedTask;
                        return 5;
                    }

                    await SystemTasks.Task.CompletedTask;
                    return await GetWriteResultAsync();
                }

                await NestedGetWriteResultAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestMixedUncontrolledAwaitNestedSynchronousTaskInControlledTaskWithResult()
        {
            this.Test(async () =>
            {
                async SystemTasks.Task<int> NestedGetWriteResultAsync()
                {
                    async Task<int> GetWriteResultAsync()
                    {
                        await SystemTasks.Task.CompletedTask;
                        return 5;
                    }

                    await SystemTasks.Task.CompletedTask;
                    return await GetWriteResultAsync();
                }

                await NestedGetWriteResultAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestMixedUncontrolledAwaitNestedAsynchronousTaskWithResult()
        {
            this.TestWithError(async () =>
            {
                async SystemTasks.Task<int> NestedGetWriteResultWithDelayAsync()
                {
                    async SystemTasks.Task<int> GetWriteResultWithDelayAsync()
                    {
                        await SystemTasks.Task.Delay(10);
                        return 5;
                    }

                    await SystemTasks.Task.Delay(10);
                    return await GetWriteResultWithDelayAsync();
                }

                await NestedGetWriteResultWithDelayAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Controlled task '' is trying to wait for an uncontrolled task or awaiter to complete. " +
                "Please make sure to use Coyote APIs to express concurrency ().",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestMixedUncontrolledAwaitNestedAsynchronousTaskInControlledTaskWithResult()
        {
            this.TestWithError(async () =>
            {
                async SystemTasks.Task<int> NestedGetWriteResultWithDelayAsync()
                {
                    async Task<int> GetWriteResultWithDelayAsync()
                    {
                        await SystemTasks.Task.Delay(10);
                        return 5;
                    }

                    await SystemTasks.Task.Delay(10);
                    return await GetWriteResultWithDelayAsync();
                }

                await NestedGetWriteResultWithDelayAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Controlled task '' is trying to wait for an uncontrolled task or awaiter to complete. " +
                "Please make sure to use Coyote APIs to express concurrency ().",
            replay: true);
        }
    }
}
