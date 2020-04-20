// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class TaskRandomBooleanTests : Microsoft.Coyote.Production.Tests.Tasks.TaskRandomBooleanTests
    {
        public TaskRandomBooleanTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool SystematicTest => true;
    }
}
