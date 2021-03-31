// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests.ConcurrencyFuzzing
{
    public class TaskDelayTests : Tests.TaskDelayTests
    {
        public TaskDelayTests(ITestOutputHelper output)
            : base(output)
        {
        }

        protected override Configuration GetConfiguration()
        {
            return base.GetConfiguration().WithConcurrencyFuzzingEnabled();
        }
    }
}
