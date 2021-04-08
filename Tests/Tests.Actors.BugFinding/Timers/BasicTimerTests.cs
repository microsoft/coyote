// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors.Timers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests
{
    public class BasicTimerTests : BaseActorBugFindingTest
    {
        public BasicTimerTests(ITestOutputHelper output)
            : base(output)
        {
        }

        internal class TimerCountEvent : Event
        {
            public int Count;
        }

        [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
        private class A1 : Actor
        {
            private TimerCountEvent Config;

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.Config = (TimerCountEvent)initialEvent;
                this.Config.Count = 0;

                // Start a regular timer.
                this.StartTimer(TimeSpan.FromMilliseconds(10));
                return Task.CompletedTask;
            }

            private void HandleTimeout()
            {
                this.Config.Count++;
                this.Assert(this.Config.Count is 1);
            }
        }

        [Fact(Timeout = 10000)]
        public void TestBasicTimerOperationInActor()
        {
            var config = new TimerCountEvent();
            this.Test(r =>
            {
                r.CreateActor(typeof(A1), config);
            },
            configuration: Configuration.Create().WithTestingIterations(200).WithMaxSchedulingSteps(200).WithTimeoutDelay(1));
            Assert.True(config.Count > 0, "Timer never fired?");
        }

        private class M1 : StateMachine
        {
            private TimerCountEvent Config;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.Config = (TimerCountEvent)e;
                this.Config.Count = 0;

                // Start a regular timer.
                this.StartTimer(TimeSpan.FromMilliseconds(10));
            }

            private void HandleTimeout()
            {
                this.Config.Count++;
                this.Assert(this.Config.Count is 1);
            }
        }

        [Fact(Timeout = 10000)]
        public void TestBasicTimerOperationInStateMachine()
        {
            var config = new TimerCountEvent();
            this.Test(r =>
            {
                r.CreateActor(typeof(M1), config);
            },
            configuration: Configuration.Create().WithTestingIterations(200).WithMaxSchedulingSteps(200).WithTimeoutDelay(1));
            Assert.True(config.Count > 0, "Timer never fired?");
        }

        [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
        private class A2 : Actor
        {
            private TimerInfo Timer;
            private TimerCountEvent Config;

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.Config = (TimerCountEvent)initialEvent;
                this.Config.Count = 0;

                // Start a periodic timer.
                this.Timer = this.StartPeriodicTimer(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10));
                return Task.CompletedTask;
            }

            private void HandleTimeout()
            {
                this.Config.Count++;
                this.Assert(this.Config.Count <= 10);

                if (this.Config.Count == 10)
                {
                    this.StopTimer(this.Timer);
                }
            }
        }

        [Fact(Timeout = 10000)]
        public void TestBasicPeriodicTimerOperationInActor()
        {
            var config = new TimerCountEvent();
            this.Test(r =>
            {
                r.CreateActor(typeof(A2), config);
            },
            configuration: Configuration.Create().WithTestingIterations(200).WithTimeoutDelay(1));
            Assert.True(config.Count > 0, "Timer never fired?");
        }

        private class M2 : StateMachine
        {
            private TimerInfo Timer;
            private TimerCountEvent Config;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.Config = (TimerCountEvent)e;
                this.Config.Count = 0;

                // Start a periodic timer.
                this.Timer = this.StartPeriodicTimer(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10));
            }

            private void HandleTimeout()
            {
                this.Config.Count++;
                this.Assert(this.Config.Count <= 10);

                if (this.Config.Count == 10)
                {
                    this.StopTimer(this.Timer);
                }
            }
        }

        [Fact(Timeout = 10000)]
        public void TestBasicPeriodicTimerOperationInStateMachine()
        {
            var config = new TimerCountEvent();
            this.Test(r =>
            {
                r.CreateActor(typeof(M2), config);
            },
            configuration: Configuration.Create().WithTestingIterations(200).WithTimeoutDelay(1));
            Assert.True(config.Count > 0, "Timer never fired?");
        }

        private class M3 : StateMachine
        {
            private TimerInfo PingTimer;
            private TimerInfo PongTimer;
            private TimerCountEvent Config;

            /// <summary>
            /// Start the PingTimer and start handling the timeout events from it.
            /// After handling 10 events, stop the timer and move to the Pong state.
            /// </summary>
            [Start]
            [OnEntry(nameof(DoPing))]
            [IgnoreEvents(typeof(TimerElapsedEvent))]
            private class Ping : State
            {
            }

            /// <summary>
            /// Start the PongTimer and start handling the timeout events from it.
            /// After handling 10 events, stop the timer and move to the Ping state.
            /// </summary>
            [OnEntry(nameof(DoPong))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            private class Pong : State
            {
            }

            private void DoPing(Event e)
            {
                this.Config = (TimerCountEvent)e;
                this.Config.Count = 0;
                this.PingTimer = this.StartPeriodicTimer(TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(5));
                this.StopTimer(this.PingTimer);
                this.RaiseGotoStateEvent<Pong>();
            }

            private void DoPong()
            {
                this.PongTimer = this.StartPeriodicTimer(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50));
            }

            private void HandleTimeout(Event e)
            {
                this.Config.Count++;
                var timeout = e as TimerElapsedEvent;
                this.Assert(timeout.Info == this.PongTimer);
            }
        }

        [Fact(Timeout = 10000)]
        public void TestDropTimeoutsAfterTimerDisposal()
        {
            var config = new TimerCountEvent();
            this.Test(r =>
            {
                r.CreateActor(typeof(M3), config);
            },
            configuration: Configuration.Create().WithTestingIterations(200).WithMaxSchedulingSteps(200).WithTimeoutDelay(1));
            Assert.True(config.Count > 0, "Timer never fired?");
        }

        private class M4 : StateMachine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            private class Init : State
            {
            }

            private void Initialize()
            {
                this.StartTimer(TimeSpan.FromSeconds(-1));
            }
        }

        [Fact(Timeout = 10000)]
        public void TestIllegalDueTimeSpecification()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M4));
            },
            configuration: Configuration.Create().WithTestingIterations(200).WithMaxSchedulingSteps(200).WithTimeoutDelay(1),
            expectedError: "M4() registered a timer with a negative due time.",
            replay: true);
        }

        private class M5 : StateMachine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            private class Init : State
            {
            }

            private void Initialize()
            {
                this.StartPeriodicTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(-1));
            }
        }

        [Fact(Timeout = 10000)]
        public void TestIllegalPeriodSpecification()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M5));
            },
            configuration: Configuration.Create().WithTestingIterations(200).WithMaxSchedulingSteps(200).WithTimeoutDelay(1),
            expectedError: "M5() registered a periodic timer with a negative period.",
            replay: true);
        }

        private class TransferTimerEvent : Event
        {
            public TimerInfo Timer;

            public TransferTimerEvent(TimerInfo timer)
            {
                this.Timer = timer;
            }
        }

        private class M6 : StateMachine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            [IgnoreEvents(typeof(TimerElapsedEvent))]
            private class Init : State
            {
            }

            private void Initialize()
            {
                var timer = this.StartPeriodicTimer(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10));
                this.CreateActor(typeof(M7), new TransferTimerEvent(timer));
            }
        }

        private class M7 : StateMachine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            private class Init : State
            {
            }

            private void Initialize(Event e)
            {
                this.StopTimer((e as TransferTimerEvent).Timer);
            }
        }

        [Fact(Timeout = 10000)]
        public void TestTimerDisposedByNonOwner()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M6));
            },
            configuration: Configuration.Create().WithTestingIterations(200).WithMaxSchedulingSteps(200).WithTimeoutDelay(1),
            expectedError: "M7() is not allowed to dispose timer '', which is owned by M6().",
            replay: true);
        }

        private class M8 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(TimerElapsedEvent))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                // Start a regular timer.
                this.StartTimer(TimeSpan.FromMilliseconds(10));
                this.RaiseGotoStateEvent<Final>();
            }

            [OnEntry(nameof(FinalOnEntry))]
            [IgnoreEvents(typeof(TimerElapsedEvent))]
            private class Final : State
            {
            }

            private void FinalOnEntry() => this.RaiseHaltEvent();
        }

        [Fact(Timeout = 10000)]
        public void TestExplicitHaltWithTimer()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M8));
            },
            configuration: Configuration.Create().WithTestingIterations(200).WithMaxSchedulingSteps(200).WithTimeoutDelay(1));
        }

        private class T6 : StateMachine
        {
            private ConfigEvent Config;

            internal class MyTimeoutEvent : TimerElapsedEvent
            {
            }

            internal enum TestType
            {
                CustomTimer,
                CustomPeriodicTimer
            }

            internal class ConfigEvent : Event
            {
                public TestType Test;
                public int Count;
            }

            [Start]
            [OnEntry(nameof(Initialize))]
            [OnEventDoAction(typeof(MyTimeoutEvent), nameof(OnMyTimeout))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(OnMyTimeout))]
            private class Init : State
            {
            }

            private void Initialize(Event e)
            {
                var ce = e as ConfigEvent;
                this.Config = ce;
                this.Config.Count = 0;
                switch (ce.Test)
                {
                    case TestType.CustomTimer:
                        this.StartTimer(TimeSpan.FromMilliseconds(1), customEvent: new MyTimeoutEvent());
                        break;
                    case TestType.CustomPeriodicTimer:
                        this.StartPeriodicTimer(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), customEvent: new MyTimeoutEvent());
                        break;
                    default:
                        break;
                }
            }

            private void OnMyTimeout(Event e)
            {
                if (e is MyTimeoutEvent)
                {
                    this.Config.Count++;
                    this.Assert(this.Config.Count is 1 || this.Config.Test == TestType.CustomPeriodicTimer);
                }
                else
                {
                    this.Assert(false, "Unexpected event type {0}", e.GetType().FullName);
                }
            }
        }

        [Fact(Timeout = 10000)]
        public void TestCustomTimerEvent()
        {
            var config = new T6.ConfigEvent { Test = T6.TestType.CustomTimer };
            this.Test(r =>
            {
                r.CreateActor(typeof(T6), config);
            },
            configuration: Configuration.Create().WithTestingIterations(200).WithMaxSchedulingSteps(200).WithTimeoutDelay(1));
            Assert.True(config.Count > 0, "Timer never fired?");
        }

        [Fact(Timeout = 10000)]
        public void TestCustomPeriodicTimerEvent()
        {
            var config = new T6.ConfigEvent { Test = T6.TestType.CustomPeriodicTimer };
            this.Test(r =>
            {
                r.CreateActor(typeof(T6), config);
            },
            configuration: Configuration.Create().WithTestingIterations(200).WithMaxSchedulingSteps(200).WithTimeoutDelay(1));
            Assert.True(config.Count > 0, "Timer never fired?");
        }
    }
}
