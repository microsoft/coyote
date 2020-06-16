// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

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
                async SystemTasks.Task CallAsync()
                {
                    _ = Task.Delay(10);
                    await SystemTasks.Task.Delay(10);
                }

                await CallAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedErrors: GetUncontrolledTaskErrorMessages(),
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestMixedSkipAwaitAsynchronousTasksInControlledTask()
        {
            this.TestWithError(async () =>
            {
                async Task CallAsync()
                {
                    _ = Task.Delay(10);
                    await SystemTasks.Task.Delay(10);
                }

                await CallAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedErrors: GetUncontrolledTaskErrorMessages(),
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestMixedSkipAwaitNestedAsynchronousTasks()
        {
            this.TestWithError(async () =>
            {
                async SystemTasks.Task NestedCallAsync()
                {
                    async SystemTasks.Task CallAsync()
                    {
                        _ = Task.Delay(10);
                        await SystemTasks.Task.Delay(10);
                    }

                    await SystemTasks.Task.Delay(10);
                    await CallAsync();
                }

                await NestedCallAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedErrors: GetUncontrolledTaskErrorMessages());
        }

        [Fact(Timeout = 5000)]
        public void TestMixedSkipAwaitNestedAsynchronousTasksInControlledTask()
        {
            this.TestWithError(async () =>
            {
                async SystemTasks.Task NestedCallAsync()
                {
                    async Task CallAsync()
                    {
                        _ = Task.Delay(10);
                        await SystemTasks.Task.Delay(10);
                    }

                    await SystemTasks.Task.Delay(10);
                    await CallAsync();
                }

                await NestedCallAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedErrors: GetUncontrolledTaskErrorMessages());
        }

        [Fact(Timeout = 5000)]
        public void TestMixedSkipAwaitAsynchronousTasksWithResult()
        {
            this.TestWithError(async () =>
            {
                async SystemTasks.Task<int> GetWriteResultWithDelayAsync()
                {
                    _ = Task.Delay(10);
                    await SystemTasks.Task.Delay(10);
                    return 5;
                }

                await GetWriteResultWithDelayAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedErrors: GetUncontrolledTaskErrorMessages());
        }

        [Fact(Timeout = 5000)]
        public void TestMixedSkipAwaitAsynchronousTasksInControlledTaskWithResult()
        {
            this.TestWithError(async () =>
            {
                async Task<int> GetWriteResultWithDelayAsync()
                {
                    _ = Task.Delay(10);
                    await SystemTasks.Task.Delay(10);
                    return 5;
                }

                await GetWriteResultWithDelayAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedErrors: GetUncontrolledTaskErrorMessages());
        }

        [Fact(Timeout = 5000)]
        public void TestMixedSkipAwaitNestedAsynchronousTasksWithResult()
        {
            this.TestWithError(async () =>
            {
                async SystemTasks.Task<int> NestedGetWriteResultWithDelayAsync()
                {
                    async SystemTasks.Task<int> GetWriteResultWithDelayAsync()
                    {
                        _ = Task.Delay(10);
                        await SystemTasks.Task.Delay(10);
                        return 5;
                    }

                    await SystemTasks.Task.Delay(10);
                    return await GetWriteResultWithDelayAsync();
                }

                await NestedGetWriteResultWithDelayAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedErrors: GetUncontrolledTaskErrorMessages());
        }

        [Fact(Timeout = 5000)]
        public void TestMixedSkipAwaitNestedAsynchronousTasksInControlledTaskWithResult()
        {
            this.TestWithError(async () =>
            {
                async SystemTasks.Task<int> NestedGetWriteResultWithDelayAsync()
                {
                    async Task<int> GetWriteResultWithDelayAsync()
                    {
                        _ = Task.Delay(10);
                        await SystemTasks.Task.Delay(10);
                        return 5;
                    }

                    await SystemTasks.Task.Delay(10);
                    return await GetWriteResultWithDelayAsync();
                }

                await NestedGetWriteResultWithDelayAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedErrors: GetUncontrolledTaskErrorMessages());
        }
    }
}
