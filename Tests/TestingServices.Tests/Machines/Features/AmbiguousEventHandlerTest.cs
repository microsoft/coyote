﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
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

        private class M : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(HandleE))]
            public class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, new E());
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
            public class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(new E());
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
                r.CreateStateMachine(typeof(M));
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
