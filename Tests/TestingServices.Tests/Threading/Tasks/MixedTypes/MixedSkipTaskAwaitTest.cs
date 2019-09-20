// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Coyote.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class MixedSkipTaskAwaitTest : BaseTest
    {
        public MixedSkipTaskAwaitTest(ITestOutputHelper output)
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
            expectedError: "Controlled task '' is trying to wait for an uncontrolled task or awaiter to complete. " +
                "Please make sure to use Coyote APIs to express concurrency ().",
            replay: true);
        }
    }
}
