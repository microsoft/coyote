// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Coyote.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests
{
    public class TaskRunConfigureAwaitFalseTest : BaseTest
    {
        public TaskRunConfigureAwaitFalseTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SharedEntry
        {
            public volatile int Value = 0;
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelTask()
        {
            SharedEntry entry = new SharedEntry();
            await ControlledTask.Run(() =>
            {
                entry.Value = 5;
            }).ConfigureAwait(false);

            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelSynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await ControlledTask.Run(async () =>
            {
                await ControlledTask.CompletedTask;
                entry.Value = 5;
            }).ConfigureAwait(false);

            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelAsynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await ControlledTask.Run(async () =>
            {
                await ControlledTask.Delay(1).ConfigureAwait(false);
                entry.Value = 5;
            }).ConfigureAwait(false);

            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunNestedParallelSynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await ControlledTask.Run(async () =>
            {
                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.CompletedTask;
                    entry.Value = 3;
                }).ConfigureAwait(false);

                entry.Value = 5;
            }).ConfigureAwait(false);

            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestAwaitNestedParallelAsynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await ControlledTask.Run(async () =>
            {
                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.Delay(1).ConfigureAwait(false);
                    entry.Value = 3;
                }).ConfigureAwait(false);

                entry.Value = 5;
            }).ConfigureAwait(false);

            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await ControlledTask.Run(() =>
            {
                entry.Value = 5;
                return entry.Value;
            }).ConfigureAwait(false);

            Assert.Equal(5, value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelSynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await ControlledTask.Run(async () =>
            {
                await ControlledTask.CompletedTask;
                entry.Value = 5;
                return entry.Value;
            }).ConfigureAwait(false);

            Assert.Equal(5, value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunParallelAsynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await ControlledTask.Run(async () =>
            {
                await ControlledTask.Delay(1).ConfigureAwait(false);
                entry.Value = 5;
                return entry.Value;
            }).ConfigureAwait(false);

            Assert.Equal(5, value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunNestedParallelSynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await ControlledTask.Run(async () =>
            {
                return await ControlledTask.Run(async () =>
                {
                    await ControlledTask.CompletedTask;
                    entry.Value = 5;
                    return entry.Value;
                }).ConfigureAwait(false);
            }).ConfigureAwait(false);

            Assert.Equal(5, value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestRunNestedParallelAsynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await ControlledTask.Run(async () =>
            {
                return await ControlledTask.Run(async () =>
                {
                    await ControlledTask.Delay(1).ConfigureAwait(false);
                    entry.Value = 5;
                    return entry.Value;
                }).ConfigureAwait(false);
            }).ConfigureAwait(false);

            Assert.Equal(5, value);
        }
    }
}
