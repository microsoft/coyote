// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class MonitorStateInheritanceTest : BaseTest
    {
        public MonitorStateInheritanceTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
        }

        private class M1 : Monitor
        {
            [Start]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(E), nameof(Check))]
            private abstract class BaseState : State
            {
            }

            private void Check()
            {
                this.Assert(false, "Error reached.");
            }
        }

        private class M2 : Monitor
        {
            [Start]
            private class Init : BaseState
            {
            }

            [Start]
            private class BaseState : State
            {
            }
        }

        private class M3 : Monitor
        {
            [Start]
            private class Init : BaseState
            {
            }

            [OnEntry(nameof(BaseOnEntry))]
            private class BaseState : State
            {
            }

            private void BaseOnEntry()
            {
                this.Assert(false, "Error reached.");
            }
        }

        private class M4 : Monitor
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEntry(nameof(BaseOnEntry))]
            private class BaseState : State
            {
            }

            private void InitOnEntry()
            {
            }

            private void BaseOnEntry()
            {
                this.Assert(false, "Error reached.");
            }
        }

        private class M5 : Monitor
        {
            [Start]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(E), nameof(Check))]
            private class BaseState : State
            {
            }

            private void Check()
            {
                this.Assert(false, "Error reached.");
            }
        }

        private class M6 : Monitor
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Check))]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(E), nameof(BaseCheck))]
            private class BaseState : State
            {
            }

            private void Check()
            {
            }

            private void BaseCheck()
            {
                this.Assert(false, "Error reached.");
            }
        }

        private class M7 : Monitor
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Check))]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(E), nameof(BaseCheck))]
            private class BaseState : BaseBaseState
            {
            }

            [OnEventDoAction(typeof(E), nameof(BaseBaseCheck))]
            private class BaseBaseState : State
            {
            }

            private void Check()
            {
            }

            private void BaseCheck()
            {
                this.Assert(false, "Error reached.");
            }

            private void BaseBaseCheck()
            {
                this.Assert(false, "Error reached.");
            }
        }

        private class M8 : Monitor
        {
            [Start]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(E), nameof(BaseCheck))]
            private class BaseState : BaseBaseState
            {
            }

            [OnEventDoAction(typeof(E), nameof(BaseBaseCheck))]
            private class BaseBaseState : State
            {
            }

            private void BaseCheck()
            {
            }

            private void BaseBaseCheck()
            {
                this.Assert(false, "Error reached.");
            }
        }

        private class M9 : Monitor
        {
            [Start]
            private class Init : BaseState
            {
            }

            [OnEventGotoState(typeof(E), typeof(Done))]
            private class BaseState : State
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : State
            {
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Done reached.");
            }
        }

        private class M10 : Monitor
        {
            [Start]
            [OnEventGotoState(typeof(E), typeof(Done))]
            private class Init : BaseState
            {
            }

            [OnEventGotoState(typeof(E), typeof(Error))]
            private class BaseState : State
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : State
            {
            }

            [OnEntry(nameof(ErrorOnEntry))]
            private class Error : State
            {
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Done reached.");
            }

            private void ErrorOnEntry()
            {
                this.Assert(false, "Error reached.");
            }
        }

        private class M11 : Monitor
        {
            [Start]
            [OnEventGotoState(typeof(E), typeof(Done))]
            private class Init : BaseState
            {
            }

            [OnEventGotoState(typeof(E), typeof(Error))]
            private class BaseState : BaseBaseState
            {
            }

            [OnEventGotoState(typeof(E), typeof(Error))]
            private class BaseBaseState : State
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : State
            {
            }

            [OnEntry(nameof(ErrorOnEntry))]
            private class Error : State
            {
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Done reached.");
            }

            private void ErrorOnEntry()
            {
                this.Assert(false, "Error reached.");
            }
        }

        private class M12 : Monitor
        {
            [Start]
            private class Init : BaseState
            {
            }

            [OnEventGotoState(typeof(E), typeof(Done))]
            private class BaseState : BaseBaseState
            {
            }

            [OnEventGotoState(typeof(E), typeof(Error))]
            private class BaseBaseState : State
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : State
            {
            }

            [OnEntry(nameof(ErrorOnEntry))]
            private class Error : State
            {
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Done reached.");
            }

            private void ErrorOnEntry()
            {
                this.Assert(false, "Error reached.");
            }
        }

        [Fact(Timeout=5000)]
        public void TestMonitorStateInheritingAbstractState()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(M1));
                r.InvokeMonitor<M1>(new E());
            },
            expectedError: "Error reached.");
        }

        [Fact(Timeout=5000)]
        public void TestMonitorStateInheritingStateDuplicateStart()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(M2));
            },
            expectedError: "Monitor 'M2' can not declare more than one start states.");
        }

        [Fact(Timeout=5000)]
        public void TestMonitorStateInheritingStateOnEntry()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(M3));
            },
            expectedError: "Error reached.");
        }

        [Fact(Timeout=5000)]
        public void TestMonitorStateOverridingStateOnEntry()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(M4));
            });
        }

        [Fact(Timeout=5000)]
        public void TestMonitorStateInheritingStateOnEventDoAction()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(M5));
                r.InvokeMonitor<M5>(new E());
            },
            expectedError: "Error reached.");
        }

        [Fact(Timeout=5000)]
        public void TestMonitorStateOverridingStateOnEventDoAction()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(M6));
                r.InvokeMonitor<M6>(new E());
            });
        }

        [Fact(Timeout=5000)]
        public void TestMonitorStateOverridingTwoStatesOnEventDoAction()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(M7));
                r.InvokeMonitor<M7>(new E());
            });
        }

        [Fact(Timeout=5000)]
        public void TestMonitorStateOverridingDeepStateOnEventDoAction()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(M8));
                r.InvokeMonitor<M8>(new E());
            });
        }

        [Fact(Timeout=5000)]
        public void TestMonitorStateInheritingStateOnEventGotoState()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(M9));
                r.InvokeMonitor<M9>(new E());
            },
            expectedError: "Done reached.");
        }

        [Fact(Timeout=5000)]
        public void TestMonitorStateOverridingStateOnEventGotoState()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(M10));
                r.InvokeMonitor<M10>(new E());
            },
            expectedError: "Done reached.");
        }

        [Fact(Timeout=5000)]
        public void TestMonitorStateOverridingTwoStatesOnEventGotoState()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(M11));
                r.InvokeMonitor<M11>(new E());
            },
            expectedError: "Done reached.");
        }

        [Fact(Timeout=5000)]
        public void TestMonitorStateOverridingDeepStateOnEventGotoState()
        {
            this.TestWithError(r =>
            {
                r.RegisterMonitor(typeof(M12));
                r.InvokeMonitor<M12>(new E());
            },
            expectedError: "Done reached.");
        }
    }
}
