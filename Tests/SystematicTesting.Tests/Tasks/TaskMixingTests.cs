// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    /// <summary>
    /// This tests that we can mix production tests in with systematic tests in the same test run
    /// which is important onramp for customers adopting Microsoft.Coyote.Task where they want to
    /// be able to run new Coyote tests and their old traditional unit tests in the same test run.
    /// </summary>
    public class TaskMixingTests : Production.Tests.Tasks.TaskYieldTests
    {
        public TaskMixingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool SystematicTest => false;
    }
}
