// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class TaskConfigureAwaitFalseTests : Production.Tests.Tasks.TaskConfigureAwaitFalseTests
    {
        public TaskConfigureAwaitFalseTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool IsSystematicTest => true;
    }
}
