// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
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

        private class Configure : Event
        {
            public ErrorType ErrorTypeVal;

            public Configure(ErrorType errorTypeVal)
            {
                this.ErrorTypeVal = errorTypeVal;
            }
        }

        private class E : Event
        {
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
                var errorType = (this.ReceivedEvent as Configure).ErrorTypeVal;
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
                        this.RaiseEvent(new E());
                        break;
                    case ErrorType.CallSend:
                        this.SendEvent(this.Id, new E());
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
            var expectedError = "Machine 'M()' has called multiple raise, goto, push or pop in the same action.";
            this.TestWithError(r =>
            {
                r.CreateStateMachine(typeof(M), new Configure(ErrorType.CallGoto));
            },
            expectedError: expectedError,
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestGotoStateTopLevelActionFail2()
        {
            var expectedError = "Machine 'M()' has called multiple raise, goto, push or pop in the same action.";
            this.TestWithError(r =>
            {
                r.CreateStateMachine(typeof(M), new Configure(ErrorType.CallRaise));
            },
            expectedError: expectedError,
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestGotoStateTopLevelActionFail3()
        {
            var expectedError = "Machine 'M()' cannot send an event after calling raise, goto, push or pop in the same action.";
            this.TestWithError(r =>
            {
                r.CreateStateMachine(typeof(M), new Configure(ErrorType.CallSend));
            },
            expectedError: expectedError,
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestGotoStateTopLevelActionFail4()
        {
            var expectedError = "Machine 'M()' has called raise, goto, push or pop inside an OnExit method.";
            this.TestWithError(r =>
            {
                r.CreateStateMachine(typeof(M), new Configure(ErrorType.OnExit));
            },
            expectedError: expectedError,
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestGotoStateTopLevelActionFail5()
        {
            var expectedError = "Machine 'M()' has called multiple raise, goto, push or pop in the same action.";
            this.TestWithError(r =>
            {
                r.CreateStateMachine(typeof(M), new Configure(ErrorType.CallPush));
            },
            expectedError: expectedError,
            replay: true);
        }
    }
}
