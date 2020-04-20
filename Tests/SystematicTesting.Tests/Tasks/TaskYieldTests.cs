// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class TaskYieldTests : Microsoft.Coyote.Production.Tests.Tasks.TaskYieldTests
    {
        public TaskYieldTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool SystematicTest => true;
    }
}
