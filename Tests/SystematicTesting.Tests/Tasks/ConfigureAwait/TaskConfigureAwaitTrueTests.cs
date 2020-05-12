// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class TaskConfigureAwaitTrueTests : Production.Tests.Tasks.TaskConfigureAwaitTrueTests
    {
        public TaskConfigureAwaitTrueTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool SystematicTest => true;
    }
}
