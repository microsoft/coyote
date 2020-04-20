// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class TaskCompletionSourceTests : Microsoft.Coyote.Production.Tests.Tasks.TaskCompletionSourceTests
    {
        public TaskCompletionSourceTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool SystematicTest => true;
    }
}
