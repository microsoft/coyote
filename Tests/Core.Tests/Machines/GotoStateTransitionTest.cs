// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Machines;
using Microsoft.Coyote.TestingServices;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests
{
    public class GotoStateTransitionTest : BaseTest
    {
        public GotoStateTransitionTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Message : Event
        {
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Goto<Final>();
            }

            private class Final : MachineState
            {
            }
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEventGotoState(typeof(Message), typeof(Final))]
            private class Init : MachineState
            {
            }

            private class Final : MachineState
            {
            }
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(Message), typeof(Final))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new Message());
            }

            private class Final : MachineState
            {
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestGotoStateTransition()
        {
            var configuration = GetConfiguration();
            var test = new StateMachineTestKit<M1>(configuration: configuration);
            await test.StartMachineAsync();
            test.AssertStateTransition("Final");
        }

        [Fact(Timeout = 5000)]
        public async Task TestGotoStateTransitionAfterSend()
        {
            var configuration = GetConfiguration();
            var test = new StateMachineTestKit<M2>(configuration: configuration);
            await test.StartMachineAsync();
            test.AssertStateTransition("Init");

            await test.SendEventAsync(new Message());
            test.AssertStateTransition("Final");
        }

        [Fact(Timeout = 5000)]
        public async Task TestGotoStateTransitionAfterRaise()
        {
            var configuration = GetConfiguration();
            var test = new StateMachineTestKit<M3>(configuration: configuration);
            await test.StartMachineAsync();
            test.AssertStateTransition("Final");
        }
    }
}
