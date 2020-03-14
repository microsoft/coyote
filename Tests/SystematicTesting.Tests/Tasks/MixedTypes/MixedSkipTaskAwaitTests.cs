// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class MixedSkipTaskAwaitTests : BaseSystematicTest
    {
        public MixedSkipTaskAwaitTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestMixedSkipAwaitAsynchronousTasks()
        {
            this.TestWithError(async () =>
            {
                async Task CallAsync()
                {
                    _ = ControlledTask.Delay(10);
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
        public void TestMixedSkipAwaitAsynchronousTasksInControlledTask()
        {
            this.TestWithError(async () =>
            {
                async ControlledTask CallAsync()
                {
                    _ = ControlledTask.Delay(10);
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
        public void TestMixedSkipAwaitNestedAsynchronousTasks()
        {
            this.TestWithError(async () =>
            {
                async Task NestedCallAsync()
                {
                    async Task CallAsync()
                    {
                        _ = ControlledTask.Delay(10);
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
        public void TestMixedSkipAwaitNestedAsynchronousTasksInControlledTask()
        {
            this.TestWithError(async () =>
            {
                async Task NestedCallAsync()
                {
                    async ControlledTask CallAsync()
                    {
                        _ = ControlledTask.Delay(10);
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
        public void TestMixedSkipAwaitAsynchronousTasksWithResult()
        {
            this.TestWithError(async () =>
            {
                async Task<int> GetWriteResultWithDelayAsync()
                {
                    _ = ControlledTask.Delay(10);
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
        public void TestMixedSkipAwaitAsynchronousTasksInControlledTaskWithResult()
        {
            this.TestWithError(async () =>
            {
                async ControlledTask<int> GetWriteResultWithDelayAsync()
                {
                    _ = ControlledTask.Delay(10);
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
        public void TestMixedSkipAwaitNestedAsynchronousTasksWithResult()
        {
            this.TestWithError(async () =>
            {
                async Task<int> NestedGetWriteResultWithDelayAsync()
                {
                    async Task<int> GetWriteResultWithDelayAsync()
                    {
                        _ = ControlledTask.Delay(10);
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
        public void TestMixedSkipAwaitNestedAsynchronousTasksInControlledTaskWithResult()
        {
            this.TestWithError(async () =>
            {
                async Task<int> NestedGetWriteResultWithDelayAsync()
                {
                    async ControlledTask<int> GetWriteResultWithDelayAsync()
                    {
                        _ = ControlledTask.Delay(10);
                        await Task.Delay(10);
                        return 5;
                    }

                    await Task.Delay(10);
                    return await GetWriteResultWithDelayAsync();
                }

                await NestedGetWriteResultWithDelayAsync();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedErrors: new string[]
            {
                "Controlled task '' is trying to wait for an uncontrolled task or awaiter to complete. " +
                "Please make sure to use Coyote APIs to express concurrency ().",
                "Uncontrolled task '' invoked a runtime method. Please make sure to avoid using concurrency APIs " +
                "such as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside actor handlers or controlled tasks. If you are " +
                "using external libraries that are executing concurrently, you will need to mock them during testing.",
            },
            replay: true);
        }
    }
}
