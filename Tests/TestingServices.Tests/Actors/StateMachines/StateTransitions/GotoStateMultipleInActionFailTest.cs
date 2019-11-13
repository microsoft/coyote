// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors.StateMachines
{
    public class GotoStateTopLevelActionFailTest : BaseTest
    {
        public GotoStateTopLevelActionFailTest(ITestOutputHelper output)
            : base(output)
        {
        }

        public enum ErrorType
        {
            CallGoto,
            CallPush,
            CallRaise,
            CallSend,
            OnExit
        }

        private class SetupEvent : Event
        {
            public ErrorType ErrorTypeVal;

            public SetupEvent(ErrorType errorTypeVal)
            {
                this.ErrorTypeVal = errorTypeVal;
            }
        }

        private class M : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitMethod))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                var errorType = (this.ReceivedEvent as SetupEvent).ErrorTypeVal;
                this.Foo();
                switch (errorType)
                {
                    case ErrorType.CallGoto:
                        this.Goto<Done>();
                        break;
                    case ErrorType.CallPush:
                        this.Push<Done>();
                        break;
                    case ErrorType.CallRaise:
                        this.RaiseEvent(new UnitEvent());
                        break;
                    case ErrorType.CallSend:
                        this.SendEvent(this.Id, new UnitEvent());
                        break;
                    case ErrorType.OnExit:
                        break;
                    default:
                        break;
                }
            }

            private void ExitMethod()
            {
                this.Pop();
            }

            private void Foo()
            {
                this.Goto<Done>();
            }

            private class Done : State
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestGotoStateTopLevelActionFail1()
        {
            var expectedError = "'M()' has called multiple raise, goto, push or pop in the same action.";
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M), new SetupEvent(ErrorType.CallGoto));
            },
            expectedError: expectedError,
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestGotoStateTopLevelActionFail2()
        {
            var expectedError = "'M()' has called multiple raise, goto, push or pop in the same action.";
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M), new SetupEvent(ErrorType.CallRaise));
            },
            expectedError: expectedError,
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestGotoStateTopLevelActionFail3()
        {
            var expectedError = "'M()' cannot send an event after calling raise, goto, push or pop in the same action.";
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M), new SetupEvent(ErrorType.CallSend));
            },
            expectedError: expectedError,
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestGotoStateTopLevelActionFail4()
        {
            var expectedError = "'M()' has called raise, goto, push or pop inside an OnExit method.";
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M), new SetupEvent(ErrorType.OnExit));
            },
            expectedError: expectedError,
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestGotoStateTopLevelActionFail5()
        {
            var expectedError = "'M()' has called multiple raise, goto, push or pop in the same action.";
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M), new SetupEvent(ErrorType.CallPush));
            },
            expectedError: expectedError,
            replay: true);
        }
    }
}
