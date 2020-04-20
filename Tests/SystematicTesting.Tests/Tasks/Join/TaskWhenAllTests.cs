// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class TaskWhenAllTests : Microsoft.Coyote.Production.Tests.Tasks.TaskWhenAllTests
    {
        public TaskWhenAllTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool SystematicTest => true;
    }
}
