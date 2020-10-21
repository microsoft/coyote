// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
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

        [Fact(Skip = "[Low priority] Requires binary rewriting support.", Timeout = 5000)]
        public void TestExpectedIdInTaskWithAction()
        {
            // TODO: this scenario can be enabled with binary rewriting, but its low priority as .NET
            // does not recommend using task ids besides logging (as there can be duplicates).
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
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Skip = "[Low priority] Requires binary rewriting support.", Timeout = 5000)]
        public void TestExpectedIdInTaskWithFunction()
        {
            // TODO: this scenario can be enabled with binary rewriting, but its low priority as .NET
            // does not recommend using task ids besides logging (as there can be duplicates).
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
            configuration: GetConfiguration().WithTestingIterations(200),
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
                });

                await task;

                Specification.Assert(entry.Value != task.Id, "Unexpected task id.");
                Specification.Assert(entry.Value == task.Id, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
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
                    int id = Task.CurrentId.Value;
                    await Task.Delay(1);
                    return id;
                });

                await task;

                Specification.Assert(task.Result != task.Id, "Unexpected task id.");
                Specification.Assert(task.Result == task.Id, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }
    }
}
