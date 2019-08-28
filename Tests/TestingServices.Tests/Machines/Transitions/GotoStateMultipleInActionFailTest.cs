// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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

        private class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitMethod))]
            private class Init : MachineState
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
                        this.Raise(new E());
                        break;
                    case ErrorType.CallSend:
                        this.Send(this.Id, new E());
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

            private class Done : MachineState
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestGotoStateTopLevelActionFail1()
        {
            var expectedError = "Machine 'M()' has called multiple raise, goto, push or pop in the same action.";
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M), new Configure(ErrorType.CallGoto));
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
                r.CreateMachine(typeof(M), new Configure(ErrorType.CallRaise));
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
                r.CreateMachine(typeof(M), new Configure(ErrorType.CallSend));
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
                r.CreateMachine(typeof(M), new Configure(ErrorType.OnExit));
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
                r.CreateMachine(typeof(M), new Configure(ErrorType.CallPush));
            },
            expectedError: expectedError,
            replay: true);
        }
    }
}
