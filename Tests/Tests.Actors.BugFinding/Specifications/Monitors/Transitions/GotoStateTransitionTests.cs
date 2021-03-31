// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests.Specifications
{
    public class GotoStateTransitionTests : BaseActorBugFindingTest
    {
        public GotoStateTransitionTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Safety : Monitor
        {
            internal static int MonitorValue;

            [Start]
            [OnEntry(nameof(Init))]
            private class S1 : State
            {
            }

            [OnEntry(nameof(IncrementValue))]
            private class S2 : State
            {
            }

            private void Init() => this.RaiseGotoStateEvent<S2>();

#pragma warning disable CA1822 // Mark members as static
            private void IncrementValue()
#pragma warning restore CA1822 // Mark members as static
            {
                MonitorValue = 101;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestGotoStateTransition()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<Safety>();
            });

            Assert.Equal(101, Safety.MonitorValue);
        }
    }
}
