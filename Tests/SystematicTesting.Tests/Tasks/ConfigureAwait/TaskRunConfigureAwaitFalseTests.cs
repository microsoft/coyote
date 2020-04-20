// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class TaskRunConfigureAwaitFalseTests : Microsoft.Coyote.Production.Tests.Tasks.TaskRunConfigureAwaitFalseTests
    {
        public TaskRunConfigureAwaitFalseTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool SystematicTest => true;
    }
}
