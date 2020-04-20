// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class TaskInterleavingsTests : Microsoft.Coyote.Production.Tests.Tasks.TaskInterleavingsTests
    {
        public TaskInterleavingsTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool SystematicTest => true;
    }
}
