// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class MixedUncontrolledTaskAwaitTest : BaseTest
    {
        public MixedUncontrolledTaskAwaitTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestMixedUncontrolledAwaitSynchronousTask()
        {
            this.Test(async () =>
            {
                async Task CallAsync()
                {
                    await Task.CompletedTask;
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
                async ControlledTask CallAsync()
                {
                    await Task.CompletedTask;
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
                async Task CallAsync()
                {
                    await Task.Delay(10);
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
                async ControlledTask CallAsync()
                {
                    await Task.Delay(10);
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
                async Task NestedCallAsync()
                {
                    async Task CallAsync()
                    {
                        await Task.CompletedTask;
                    }

                    await Task.CompletedTask;
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
                async Task NestedCallAsync()
                {
                    async ControlledTask CallAsync()
                    {
                        await Task.CompletedTask;
                    }

                    await Task.CompletedTask;
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
                async Task NestedCallAsync()
                {
                    async Task CallAsync()
                    {
                        await Task.Delay(10);
                    }

                    await Task.Delay(10);
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
                async Task NestedCallAsync()
                {
                    async ControlledTask CallAsync()
                    {
                        await Task.Delay(10);
                    }

                    await Task.Delay(10);
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
                async Task<int> GetWriteResultAsync()
                {
                    await Task.CompletedTask;
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
                async ControlledTask<int> GetWriteResultAsync()
                {
                    await Task.CompletedTask;
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
                async Task<int> GetWriteResultWithDelayAsync()
                {
                    await Task.Delay(10);
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
                async ControlledTask<int> GetWriteResultWithDelayAsync()
                {
                    await Task.Delay(10);
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
                async Task<int> NestedGetWriteResultAsync()
                {
                    async Task<int> GetWriteResultAsync()
                    {
                        await Task.CompletedTask;
                        return 5;
                    }

                    await Task.CompletedTask;
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
                async Task<int> NestedGetWriteResultAsync()
                {
                    async ControlledTask<int> GetWriteResultAsync()
                    {
                        await Task.CompletedTask;
                        return 5;
                    }

                    await Task.CompletedTask;
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
                async Task<int> NestedGetWriteResultWithDelayAsync()
                {
                    async Task<int> GetWriteResultWithDelayAsync()
                    {
                        await Task.Delay(10);
                        return 5;
                    }

                    await Task.Delay(10);
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
                async Task<int> NestedGetWriteResultWithDelayAsync()
                {
                    async ControlledTask<int> GetWriteResultWithDelayAsync()
                    {
                        await Task.Delay(10);
                        return 5;
                    }

                    await Task.Delay(10);
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
