// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class MixedControlledTaskAwaitTests : BaseSystematicTest
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
                async SystemTasks.Task CallAsync()
                {
                    await Task.CompletedTask;
                }

                await CallAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestMixedControlledAwaitAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                async SystemTasks.Task CallAsync()
                {
                    await Task.Delay(100);
                }

                await CallAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedErrors: GetUncontrolledTaskErrorMessages());
        }

        [Fact(Timeout = 5000)]
        public void TestMixedControlledAwaitNestedSynchronousTask()
        {
            this.Test(async () =>
            {
                async SystemTasks.Task NestedCallAsync()
                {
                    async SystemTasks.Task CallAsync()
                    {
                        await Task.CompletedTask;
                    }

                    await Task.CompletedTask;
                    await CallAsync();
                }

                await NestedCallAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestMixedControlledAwaitNestedAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                async SystemTasks.Task NestedCallAsync()
                {
                    async SystemTasks.Task CallAsync()
                    {
                        await Task.Delay(100);
                    }

                    await Task.Delay(100);
                    await CallAsync();
                }

                await NestedCallAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedErrors: GetUncontrolledTaskErrorMessages());
        }

        [Fact(Timeout = 5000)]
        public void TestMixedControlledAwaitSynchronousTaskWithResult()
        {
            this.Test(async () =>
            {
                async SystemTasks.Task<int> GetWriteResultAsync()
                {
                    await Task.CompletedTask;
                    return 5;
                }

                await GetWriteResultAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestMixedControlledAwaitAsynchronousTaskWithResult()
        {
            this.TestWithError(async () =>
            {
                async SystemTasks.Task<int> GetWriteResultWithDelayAsync()
                {
                    await Task.Delay(100);
                    return 5;
                }

                await GetWriteResultWithDelayAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedErrors: GetUncontrolledTaskErrorMessages());
        }

        [Fact(Timeout = 5000)]
        public void TestMixedControlledAwaitNestedSynchronousTaskWithResult()
        {
            this.Test(async () =>
            {
                async SystemTasks.Task<int> NestedGetWriteResultAsync()
                {
                    async SystemTasks.Task<int> GetWriteResultAsync()
                    {
                        await Task.CompletedTask;
                        return 5;
                    }

                    await Task.CompletedTask;
                    return await GetWriteResultAsync();
                }

                await NestedGetWriteResultAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestMixedControlledAwaitNestedAsynchronousTaskWithResult()
        {
            this.TestWithError(async () =>
            {
                async SystemTasks.Task<int> NestedGetWriteResultWithDelayAsync()
                {
                    async SystemTasks.Task<int> GetWriteResultWithDelayAsync()
                    {
                        await Task.Delay(100);
                        return 5;
                    }

                    await Task.Delay(100);
                    return await GetWriteResultWithDelayAsync();
                }

                await NestedGetWriteResultWithDelayAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedErrors: GetUncontrolledTaskErrorMessages());
        }
    }
}
