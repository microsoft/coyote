// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Machines;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class GenericMonitorTest : BaseTest
    {
        public GenericMonitorTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Program<T> : StateMachine
        {
            private T Item;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Item = default;
                this.Goto<Active>();
            }

            [OnEntry(nameof(ActiveInit))]
            private class Active : MachineState
            {
            }

            private void ActiveInit()
            {
                this.Assert(this.Item is int);
            }
        }

        private class E : Event
        {
        }

        private class M<T> : Monitor
        {
            [Start]
            [OnEntry(nameof(Init))]
            private class S1 : MonitorState
            {
            }

            private class S2 : MonitorState
            {
            }

            private void Init()
            {
                this.Goto<S2>();
            }
        }

        [Fact(Timeout=5000)]
        public void TestGenericMonitor()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(M<int>));
                r.CreateMachine(typeof(Program<int>));
            });
        }
    }
}
