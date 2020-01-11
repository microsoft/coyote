// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class BasicTimerTests : BaseTest
    {
        public BasicTimerTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
        private class A1 : Actor
        {
            private int Count;

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.Count = 0;

                // Start a regular timer.
                this.StartTimer(TimeSpan.FromMilliseconds(10));
                return Task.CompletedTask;
            }

            private void HandleTimeout()
            {
                this.Count++;
                this.Assert(this.Count == 1);
            }
        }

        [Fact(Timeout = 10000)]
        public void TestBasicTimerOperationInActor()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(A1));
            },
            configuration: Configuration.Create().WithNumberOfIterations(200).WithMaxSteps(200));
        }

        private class M1 : StateMachine
        {
            private int Count;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Count = 0;

                // Start a regular timer.
                this.StartTimer(TimeSpan.FromMilliseconds(10));
            }

            private void HandleTimeout()
            {
                this.Count++;
                this.Assert(this.Count == 1);
            }
        }

        [Fact(Timeout = 10000)]
        public void TestBasicTimerOperationInStateMachine()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M1));
            },
            configuration: Configuration.Create().WithNumberOfIterations(200).WithMaxSteps(200));
        }

        [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
        private class A2 : Actor
        {
            private TimerInfo Timer;
            private int Count;

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.Count = 0;

                // Start a periodic timer.
                this.Timer = this.StartPeriodicTimer(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10));
                return Task.CompletedTask;
            }

            private void HandleTimeout()
            {
                this.Count++;
                this.Assert(this.Count <= 10);

                if (this.Count == 10)
                {
                    this.StopTimer(this.Timer);
                }
            }
        }

        [Fact(Timeout = 10000)]
        public void TestBasicPeriodicTimerOperationInActor()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(A2));
            },
            configuration: Configuration.Create().WithNumberOfIterations(200));
        }

        private class M2 : StateMachine
        {
            private TimerInfo Timer;
            private int Count;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Count = 0;

                // Start a periodic timer.
                this.Timer = this.StartPeriodicTimer(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10));
            }

            private void HandleTimeout()
            {
                this.Count++;
                this.Assert(this.Count <= 10);

                if (this.Count == 10)
                {
                    this.StopTimer(this.Timer);
                }
            }
        }

        [Fact(Timeout = 10000)]
        public void TestBasicPeriodicTimerOperationInStateMachine()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M2));
            },
            configuration: Configuration.Create().WithNumberOfIterations(200));
        }

        private class M3 : StateMachine
        {
            private TimerInfo PingTimer;
            private TimerInfo PongTimer;

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

            private Transition DoPing()
            {
                this.PingTimer = this.StartPeriodicTimer(TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(5));
                this.StopTimer(this.PingTimer);
                return this.GotoState<Pong>();
            }

            private void DoPong()
            {
                this.PongTimer = this.StartPeriodicTimer(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50));
            }

            private void HandleTimeout(Event e)
            {
                var timeout = e as TimerElapsedEvent;
                this.Assert(timeout.Info == this.PongTimer);
            }
        }

        [Fact(Timeout = 10000)]
        public void TestDropTimeoutsAfterTimerDisposal()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M3));
            },
            configuration: Configuration.Create().WithNumberOfIterations(200).WithMaxSteps(200));
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
            configuration: Configuration.Create().WithNumberOfIterations(200).WithMaxSteps(200),
            expectedError: "'M4()' registered a timer with a negative due time.",
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
            configuration: Configuration.Create().WithNumberOfIterations(200).WithMaxSteps(200),
            expectedError: "'M5()' registered a periodic timer with a negative period.",
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
            configuration: Configuration.Create().WithNumberOfIterations(200).WithMaxSteps(200),
            expectedError: "'M7()' is not allowed to dispose timer '', which is owned by 'M6()'.",
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

            private Transition InitOnEntry()
            {
                // Start a regular timer.
                this.StartTimer(TimeSpan.FromMilliseconds(10));
                return this.GotoState<Final>();
            }

            [OnEntry(nameof(FinalOnEntry))]
            [IgnoreEvents(typeof(TimerElapsedEvent))]
            private class Final : State
            {
            }

            private Transition FinalOnEntry() => this.Halt();
        }

        [Fact(Timeout = 10000)]
        public void TestExplicitHaltWithTimer()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M8));
            },
            configuration: Configuration.Create().WithNumberOfIterations(200).WithMaxSteps(200));
        }
    }
}
