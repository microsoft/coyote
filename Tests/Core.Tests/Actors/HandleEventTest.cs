// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.TestingServices;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests.Actors
{
    public class HandleEventTest : BaseTest
    {
        public HandleEventTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Result
        {
            public int Value = 0;
        }

        private class SetupEvent : Event
        {
            public Result Result;

            public SetupEvent(Result result)
            {
                this.Result = result;
            }
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        private class E3 : Event
        {
        }

        private class M1 : StateMachine
        {
            private Result Result;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.Result = (e as SetupEvent).Result;
            }

            private void HandleE1()
            {
                this.Result.Value += 1;
            }
        }

        private class M2 : StateMachine
        {
            private Result Result;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            [OnEventDoAction(typeof(E3), nameof(HandleE3))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.Result = (e as SetupEvent).Result;
            }

            private void HandleE1()
            {
                this.Result.Value += 1;
            }

            private void HandleE2()
            {
                this.Result.Value += 2;
            }

            private void HandleE3()
            {
                this.Result.Value += 3;
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestHandleEventInStateMachine()
        {
            var result = new Result();

            var configuration = GetConfiguration();
            var test = new StateMachineTestKit<M1>(configuration: configuration);

            await test.StartMachineAsync(new SetupEvent(result));

            await test.SendEventAsync(new E1());

            test.AssertInboxSize(0);
            test.Assert(result.Value == 1, $"Incorrect result '{result.Value}'");
        }

        [Fact(Timeout = 5000)]
        public async Task TestHandleMultipleEventsInStateMachine()
        {
            var result = new Result();

            var configuration = GetConfiguration();
            var test = new StateMachineTestKit<M2>(configuration: configuration);

            await test.StartMachineAsync(new SetupEvent(result));

            await test.SendEventAsync(new E1());
            await test.SendEventAsync(new E2());
            await test.SendEventAsync(new E3());

            test.AssertInboxSize(0);
            test.Assert(result.Value == 6, $"Incorrect result '{result.Value}'");
        }
    }
}
