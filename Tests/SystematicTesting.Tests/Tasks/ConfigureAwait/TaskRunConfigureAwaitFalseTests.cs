// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class TaskRunConfigureAwaitFalseTests : Production.Tests.Tasks.TaskRunConfigureAwaitFalseTests
    {
        public TaskRunConfigureAwaitFalseTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool IsSystematicTest => true;
    }
}
