// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class TaskRandomIntegerTests : Microsoft.Coyote.Production.Tests.Tasks.TaskRandomIntegerTests
    {
        public TaskRandomIntegerTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool SystematicTest => true;
    }
}
