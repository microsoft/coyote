// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote.Machines;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class AmbiguousEventHandlerTest : BaseTest
    {
        public AmbiguousEventHandlerTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
        }

        private class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(HandleE))]
            public class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
            }

            private void HandleE()
            {
            }

#pragma warning disable CA1801 // Parameter not used
            private void HandleE(int k)
            {
            }
#pragma warning restore CA1801 // Parameter not used
        }

        private class Safety : Monitor
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(HandleE))]
            public class Init : MonitorState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new E());
            }

            private void HandleE()
            {
            }

#pragma warning disable CA1801 // Parameter not used
            private void HandleE(int k)
            {
            }
#pragma warning restore CA1801 // Parameter not used
        }

        [Fact(Timeout=5000)]
        public void TestAmbiguousMachineEventHandler()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M));
            });
        }

        [Fact(Timeout=5000)]
        public void TestAmbiguousMonitorEventHandler()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(Safety));
            });
        }
    }
}
