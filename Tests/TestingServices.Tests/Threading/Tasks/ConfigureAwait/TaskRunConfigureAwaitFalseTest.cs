// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class TaskRunConfigureAwaitFalseTest : BaseTest
    {
        public TaskRunConfigureAwaitFalseTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SharedEntry
        {
            public int Value = 0;
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await ControlledTask.Run(() =>
                {
                    entry.Value = 5;
                }).ConfigureAwait(false);

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await ControlledTask.Run(() =>
                {
                    entry.Value = 3;
                }).ConfigureAwait(false);

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelSynchronousTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.CompletedTask;
                    entry.Value = 5;
                }).ConfigureAwait(false);

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelSynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.CompletedTask;
                    entry.Value = 3;
                }).ConfigureAwait(false);

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelAsynchronousTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.Delay(1).ConfigureAwait(false);
                    entry.Value = 5;
                }).ConfigureAwait(false);

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelAsynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.Delay(1).ConfigureAwait(false);
                    entry.Value = 3;
                }).ConfigureAwait(false);

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunNestedParallelSynchronousTask()
        {
            this.Test(async () =>
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

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedParallelSynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.Run(async () =>
                    {
                        await ControlledTask.CompletedTask;
                        entry.Value = 5;
                    }).ConfigureAwait(false);

                    entry.Value = 3;
                }).ConfigureAwait(false);

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedParallelAsynchronousTask()
        {
            this.Test(async () =>
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

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedParallelAsynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.Run(async () =>
                    {
                        await ControlledTask.Delay(1).ConfigureAwait(false);
                        entry.Value = 5;
                    }).ConfigureAwait(false);

                    entry.Value = 3;
                }).ConfigureAwait(false);

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await ControlledTask.Run(() =>
                {
                    entry.Value = 5;
                    return entry.Value;
                }).ConfigureAwait(false);

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await ControlledTask.Run(() =>
                {
                    entry.Value = 3;
                    return entry.Value;
                }).ConfigureAwait(false);

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelSynchronousTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await ControlledTask.Run(async () =>
                {
                    await ControlledTask.CompletedTask;
                    entry.Value = 5;
                    return entry.Value;
                }).ConfigureAwait(false);

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelSynchronousTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await ControlledTask.Run(async () =>
                {
                    await ControlledTask.CompletedTask;
                    entry.Value = 3;
                    return entry.Value;
                }).ConfigureAwait(false);

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelAsynchronousTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await ControlledTask.Run(async () =>
                {
                    await ControlledTask.Delay(1).ConfigureAwait(false);
                    entry.Value = 5;
                    return entry.Value;
                }).ConfigureAwait(false);

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestRunParallelAsynchronousTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await ControlledTask.Run(async () =>
                {
                    await ControlledTask.Delay(1).ConfigureAwait(false);
                    entry.Value = 3;
                    return entry.Value;
                }).ConfigureAwait(false);

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunNestedParallelSynchronousTaskWithResult()
        {
            this.Test(async () =>
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

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestRunNestedParallelSynchronousTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await ControlledTask.Run(async () =>
                {
                    return await ControlledTask.Run(async () =>
                    {
                        await ControlledTask.CompletedTask;
                        entry.Value = 3;
                        return entry.Value;
                    }).ConfigureAwait(false);
                }).ConfigureAwait(false);

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRunNestedParallelAsynchronousTaskWithResult()
        {
            this.Test(async () =>
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

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestRunNestedParallelAsynchronousTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await ControlledTask.Run(async () =>
                {
                    return await ControlledTask.Run(async () =>
                    {
                        await ControlledTask.Delay(1).ConfigureAwait(false);
                        entry.Value = 3;
                        return entry.Value;
                    }).ConfigureAwait(false);
                }).ConfigureAwait(false);

                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
    }
}
