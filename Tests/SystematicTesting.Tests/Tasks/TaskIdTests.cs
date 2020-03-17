// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class TaskIdTests : BaseSystematicTest
    {
        public TaskIdTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestExpectedIdInTaskWithAction()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                var task = Task.Run(() =>
                {
                    entry.Value = Task.CurrentId.Value;
                });

                await task;

                Specification.Assert(entry.Value == task.Id, "Unexpected task id.");
                Specification.Assert(entry.Value != task.Id, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestExpectedIdInTaskWithFunction()
        {
            this.TestWithError(async () =>
            {
                var task = Task.Run(() =>
                {
                    return Task.CurrentId.Value;
                });

                await task;

                Specification.Assert(task.Result == task.Id, "Unexpected task id.");
                Specification.Assert(task.Result != task.Id, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestExpectedIdInTaskWithAsynchronousFunction()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                var task = Task.Run(async () =>
                {
                    await Task.Delay(1);
                    entry.Value = Task.CurrentId.Value;
                });

                await task;

                Specification.Assert(entry.Value != task.Id, "Unexpected task id.");
                Specification.Assert(entry.Value == task.Id, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestExpectedIdInTaskWithGenericAsynchronousFunction()
        {
            this.TestWithError(async () =>
            {
                var task = Task.Run(async () =>
                {
                    await Task.Delay(1);
                    return Task.CurrentId.Value;
                });

                await task;

                Specification.Assert(task.Result != task.Id, "Unexpected task id.");
                Specification.Assert(task.Result == task.Id, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }
    }
}
