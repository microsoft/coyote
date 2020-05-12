// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class TaskRunConfigureAwaitTrueTests : Production.Tests.Tasks.TaskRunConfigureAwaitTrueTests
    {
        public TaskRunConfigureAwaitTrueTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool SystematicTest => true;
    }
}
