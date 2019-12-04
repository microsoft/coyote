// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Specifications
{
    public class GotoStateTransitionTests : BaseTest
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

            private Transition Init() => this.GotoState<S2>();

            private void IncrementValue()
            {
                MonitorValue = 101;
            }
        }

        [Fact(Timeout=5000)]
        public void TestGotoStateTransition()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(Safety));
            });

            Assert.Equal(101, Safety.MonitorValue);
        }
    }
}
