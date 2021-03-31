// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests.ConcurrencyFuzzing
{
    public class TaskExceptionTests : Tests.TaskExceptionTests
    {
        public TaskExceptionTests(ITestOutputHelper output)
            : base(output)
        {
        }

        protected override Configuration GetConfiguration()
        {
            return base.GetConfiguration().WithConcurrencyFuzzingEnabled();
        }
    }
}
