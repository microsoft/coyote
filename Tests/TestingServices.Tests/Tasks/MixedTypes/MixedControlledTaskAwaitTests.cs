// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Tasks
{
    public class MixedControlledTaskAwaitTests : BaseTest
    {
        public MixedControlledTaskAwaitTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestMixedControlledAwaitSynchronousTask()
        {
            this.Test(async () =>
            {
                async Task CallAsync()
                {
                    await ControlledTask.CompletedTask;
                }

                await CallAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestMixedControlledAwaitAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                async Task CallAsync()
                {
                    await ControlledTask.Delay(100);
                }

                await CallAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Controlled task '' is trying to wait for an uncontrolled task or awaiter to complete. " +
                "Please make sure to use Coyote APIs to express concurrency ().",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestMixedControlledAwaitNestedSynchronousTask()
        {
            this.Test(async () =>
            {
                async Task NestedCallAsync()
                {
                    async Task CallAsync()
                    {
                        await ControlledTask.CompletedTask;
                    }

                    await ControlledTask.CompletedTask;
                    await CallAsync();
                }

                await NestedCallAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestMixedControlledAwaitNestedAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                async Task NestedCallAsync()
                {
                    async Task CallAsync()
                    {
                        await ControlledTask.Delay(100);
                    }

                    await ControlledTask.Delay(100);
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
        public void TestMixedControlledAwaitSynchronousTaskWithResult()
        {
            this.Test(async () =>
            {
                async Task<int> GetWriteResultAsync()
                {
                    await ControlledTask.CompletedTask;
                    return 5;
                }

                await GetWriteResultAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestMixedControlledAwaitAsynchronousTaskWithResult()
        {
            this.TestWithError(async () =>
            {
                async Task<int> GetWriteResultWithDelayAsync()
                {
                    await ControlledTask.Delay(100);
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
        public void TestMixedControlledAwaitNestedSynchronousTaskWithResult()
        {
            this.Test(async () =>
            {
                async Task<int> NestedGetWriteResultAsync()
                {
                    async Task<int> GetWriteResultAsync()
                    {
                        await ControlledTask.CompletedTask;
                        return 5;
                    }

                    await ControlledTask.CompletedTask;
                    return await GetWriteResultAsync();
                }

                await NestedGetWriteResultAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestMixedControlledAwaitNestedAsynchronousTaskWithResult()
        {
            this.TestWithError(async () =>
            {
                async Task<int> NestedGetWriteResultWithDelayAsync()
                {
                    async Task<int> GetWriteResultWithDelayAsync()
                    {
                        await ControlledTask.Delay(100);
                        return 5;
                    }

                    await ControlledTask.Delay(100);
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
