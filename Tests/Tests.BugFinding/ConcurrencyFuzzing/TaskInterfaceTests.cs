// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests.ConcurrencyFuzzing
{
    public class TaskInterfaceTests : Tests.TaskInterfaceTests
    {
        public TaskInterfaceTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private protected override SchedulingPolicy SchedulingPolicy => SchedulingPolicy.Fuzzing;

        protected override Configuration GetConfiguration()
        {
            return base.GetConfiguration().WithConcurrencyFuzzingEnabled();
        }
    }
}
