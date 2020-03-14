// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Production.Tests.Tasks
{
    public class TaskRunConfigureAwaitFalseTests : BaseProductionTest
    {
        public TaskRunConfigureAwaitFalseTests(ITestOutputHelper output)
            : base(output)
        {
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
