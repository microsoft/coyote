// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Production.Tests.Tasks
{
    public class TaskRunTests : BaseProductionTest
    {
        public TaskRunTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestRunParallelTask()
        {
            SharedEntry entry = new SharedEntry();
            await Task.Run(() =>
            {
                entry.Value = 5;
            });

            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestRunParallelSynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await Task.Run(async () =>
            {
                await Task.CompletedTask;
                entry.Value = 5;
            });

            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestRunParallelAsynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await Task.Run(async () =>
            {
                await Task.Delay(1);
                entry.Value = 5;
            });

            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestRunNestedParallelSynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await Task.Run(async () =>
            {
                await Task.Run(async () =>
                {
                    await Task.CompletedTask;
                    entry.Value = 3;
                });

                entry.Value = 5;
            });

            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestAwaitNestedParallelAsynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await Task.Run(async () =>
            {
                await Task.Run(async () =>
                {
                    await Task.Delay(1);
                    entry.Value = 3;
                });

                entry.Value = 5;
            });

            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestRunParallelTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await Task.Run(() =>
            {
                entry.Value = 5;
                return entry.Value;
            });

            Assert.Equal(5, value);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestRunParallelSynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await Task.Run(async () =>
            {
                await Task.CompletedTask;
                entry.Value = 5;
                return entry.Value;
            });

            Assert.Equal(5, value);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestRunParallelAsynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await Task.Run(async () =>
            {
                await Task.Delay(1);
                entry.Value = 5;
                return entry.Value;
            });

            Assert.Equal(5, value);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestRunNestedParallelSynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await Task.Run(async () =>
            {
                return await Task.Run(async () =>
                {
                    await Task.CompletedTask;
                    entry.Value = 5;
                    return entry.Value;
                });
            });

            Assert.Equal(5, value);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestRunNestedParallelAsynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await Task.Run(async () =>
            {
                return await Task.Run(async () =>
                {
                    await Task.Delay(1);
                    entry.Value = 5;
                    return entry.Value;
                });
            });

            Assert.Equal(5, value);
        }
    }
}
