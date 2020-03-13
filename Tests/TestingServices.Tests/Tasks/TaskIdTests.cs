// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Tasks
{
    public class TaskIdTests : BaseTest
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

                var task = ControlledTask.Run(() =>
                {
                    entry.Value = ControlledTask.CurrentId.Value;
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
                var task = ControlledTask.Run(() =>
                {
                    return ControlledTask.CurrentId.Value;
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

                var task = ControlledTask.Run(async () =>
                {
                    await ControlledTask.Delay(1);
                    entry.Value = ControlledTask.CurrentId.Value;
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
                var task = ControlledTask.Run(async () =>
                {
                    await ControlledTask.Delay(1);
                    return ControlledTask.CurrentId.Value;
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
