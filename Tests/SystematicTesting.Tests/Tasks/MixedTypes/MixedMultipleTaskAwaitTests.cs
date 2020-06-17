// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class MixedMultipleTaskAwaitTests : BaseSystematicTest
    {
        public MixedMultipleTaskAwaitTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestMixedMultipleAwaitAsynchronousTasks()
        {
            this.TestWithError(async () =>
            {
                async SystemTasks.Task CallAsync()
                {
                    await Task.Delay(10);
                    await SystemTasks.Task.Delay(10);
                }

                await CallAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedErrors: GetUncontrolledTaskErrorMessages());
        }

        [Fact(Timeout = 5000)]
        public void TestMixedMultipleAwaitAsynchronousTasksInControlledTask()
        {
            this.TestWithError(async () =>
            {
                async Task CallAsync()
                {
                    await Task.Delay(10);
                    await SystemTasks.Task.Delay(10);
                }

                await CallAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedErrors: GetUncontrolledTaskErrorMessages());
        }

        [Fact(Timeout = 5000)]
        public void TestMixedMultipleAwaitNestedAsynchronousTasks()
        {
            this.TestWithError(async () =>
            {
                async SystemTasks.Task NestedCallAsync()
                {
                    async SystemTasks.Task CallAsync()
                    {
                        await Task.Delay(10);
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
        public void TestMixedMultipleAwaitNestedAsynchronousTasksInControlledTask()
        {
            this.TestWithError(async () =>
            {
                async SystemTasks.Task NestedCallAsync()
                {
                    async Task CallAsync()
                    {
                        await Task.Delay(10);
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
        public void TestMixedMultipleAwaitAsynchronousTasksWithResult()
        {
            this.TestWithError(async () =>
            {
                async SystemTasks.Task<int> GetWriteResultWithDelayAsync()
                {
                    await Task.Delay(10);
                    await SystemTasks.Task.Delay(10);
                    return 5;
                }

                await GetWriteResultWithDelayAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedErrors: GetUncontrolledTaskErrorMessages());
        }

        [Fact(Timeout = 5000)]
        public void TestMixedMultipleAwaitAsynchronousTasksInControlledTaskWithResult()
        {
            this.TestWithError(async () =>
            {
                async Task<int> GetWriteResultWithDelayAsync()
                {
                    await Task.Delay(10);
                    await SystemTasks.Task.Delay(10);
                    return 5;
                }

                await GetWriteResultWithDelayAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedErrors: GetUncontrolledTaskErrorMessages());
        }

        [Fact(Timeout = 5000)]
        public void TestMixedMultipleAwaitNestedAsynchronousTasksWithResult()
        {
            this.TestWithError(async () =>
            {
                async SystemTasks.Task<int> NestedGetWriteResultWithDelayAsync()
                {
                    async SystemTasks.Task<int> GetWriteResultWithDelayAsync()
                    {
                        await Task.Delay(10);
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
        public void TestMixedMultipleAwaitNestedAsynchronousTasksInControlledTaskWithResult()
        {
            this.TestWithError(async () =>
            {
                async SystemTasks.Task<int> NestedGetWriteResultWithDelayAsync()
                {
                    async Task<int> GetWriteResultWithDelayAsync()
                    {
                        await Task.Delay(10);
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
