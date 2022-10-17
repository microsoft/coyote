// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.Tests
{
    public class TimerTests : BaseActorTest
    {
        public TimerTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SetupEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public SetupEvent(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        private class TransferTimerEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;
            public TimerInfo Timer;

            public TransferTimerEvent(TaskCompletionSource<bool> tcs, TimerInfo timer)
            {
                this.Tcs = tcs;
                this.Timer = timer;
            }
        }

        private class T1 : StateMachine
        {
            private TaskCompletionSource<bool> Tcs;

            private int Count;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.Tcs = (e as SetupEvent).Tcs;
                this.Count = 0;

                // Start a regular timer.
                this.StartTimer(TimeSpan.FromMilliseconds(10));
            }

            private void HandleTimeout()
            {
                this.Count++;
                if (this.Count is 1)
                {
                    this.Tcs.SetResult(true);
                }
                else
                {
                    this.Tcs.SetResult(false);
                }

                this.RaiseHaltEvent();
            }
        }

        [Fact(Timeout = 10000)]
        public async Task TestBasicTimerOperationInStateMachine()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateActor(typeof(T1), new SetupEvent(tcs));

                var result = await this.GetResultAsync(tcs);
                Assert.True(result);
            });
        }

        private class T2 : StateMachine
        {
            private TaskCompletionSource<bool> Tcs;

            private TimerInfo Timer;
            private int Count;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.Tcs = (e as SetupEvent).Tcs;
                this.Count = 0;

                // Start a periodic timer.
                this.Timer = this.StartPeriodicTimer(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10));
            }

            private void HandleTimeout()
            {
                this.Count++;
                if (this.Count is 10)
                {
                    this.StopTimer(this.Timer);
                    this.Tcs.SetResult(true);
                    this.RaiseHaltEvent();
                }
            }
        }

        [Fact(Timeout = 10000)]
        public async Task TestBasicPeriodicTimerOperationInStateMachine()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateActor(typeof(T2), new SetupEvent(tcs));

                var result = await this.GetResultAsync(tcs);
                Assert.True(result);
            });
        }

        private class T3 : StateMachine
        {
            private TaskCompletionSource<bool> Tcs;

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

            private async Task DoPing(Event e)
            {
                this.Tcs = (e as SetupEvent).Tcs;
                this.PingTimer = this.StartPeriodicTimer(TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(5));
                await Task.Delay(100);
                this.StopTimer(this.PingTimer);
                this.RaiseGotoStateEvent<Pong>();
            }

            private void DoPong()
            {
                this.PongTimer = this.StartPeriodicTimer(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50));
            }

            private void HandleTimeout(Event e)
            {
                var timeout = e as TimerElapsedEvent;
                if (timeout.Info == this.PongTimer)
                {
                    this.Tcs.SetResult(true);
                }
                else
                {
                    this.Tcs.SetResult(false);
                }

                this.RaiseHaltEvent();
            }
        }

        [Fact(Timeout = 10000)]
        public async Task TestDropTimeoutsAfterTimerDisposalInStateMachine()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateActor(typeof(T3), new SetupEvent(tcs));

                var result = await this.GetResultAsync(tcs);
                Assert.True(result);
            });
        }

        private class T4 : StateMachine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            private class Init : State
            {
            }

            private void Initialize(Event e)
            {
                var tcs = (e as SetupEvent).Tcs;
                try
                {
                    this.StartTimer(TimeSpan.FromSeconds(-1));
                }
                catch (AssertionFailureException ex)
                {
                    this.Logger.WriteLine(LogSeverity.Error, ex.Message);
                    tcs.SetResult(true);
                    this.RaiseHaltEvent();
                    return;
                }

                tcs.SetResult(false);
                this.RaiseHaltEvent();
            }
        }

        [Fact(Timeout = 10000)]
        public async Task TestIllegalDueTimeSpecificationInStateMachine()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateActor(typeof(T4), new SetupEvent(tcs));

                var result = await this.GetResultAsync(tcs);
                Assert.True(result);
            });
        }

        private class T5 : StateMachine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            private class Init : State
            {
            }

            private void Initialize(Event e)
            {
                var tcs = (e as SetupEvent).Tcs;
                try
                {
                    this.StartPeriodicTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(-1));
                }
                catch (AssertionFailureException ex)
                {
                    this.Logger.WriteLine(LogSeverity.Error, ex.Message);
                    tcs.SetResult(true);
                    this.RaiseHaltEvent();
                    return;
                }

                tcs.SetResult(false);
                this.RaiseHaltEvent();
            }
        }

        [Fact(Timeout = 10000)]
        public async Task TestIllegalPeriodSpecificationInStateMachine()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateActor(typeof(T5), new SetupEvent(tcs));

                var result = await this.GetResultAsync(tcs);
                Assert.True(result);
            });
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
                public TaskCompletionSource<bool> Tcs;
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
                bool expectError = false;
                try
                {
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
                catch (AssertionFailureException ex)
                {
                    this.Logger.WriteLine(LogSeverity.Error, ex.Message);
                    ce.Tcs.SetResult(expectError is true);
                    this.RaiseHaltEvent();
                }
            }

            private void OnMyTimeout(Event e)
            {
                if (e is MyTimeoutEvent)
                {
                    this.Config.Tcs.SetResult(true);
                }
                else
                {
                    this.Logger.WriteLine(LogSeverity.Error, "Unexpected event type {0}", e.GetType().FullName);
                    this.Config.Tcs.SetResult(false);
                }

                this.RaiseHaltEvent();
            }
        }

        [Fact(Timeout = 10000)]
        public async Task TestCustomTimerEventInStateMachine()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateActor(typeof(T6), new T6.ConfigEvent { Tcs = tcs, Test = T6.TestType.CustomTimer });

                var result = await this.GetResultAsync(tcs);
                Assert.True(result);
            });
        }

        [Fact(Timeout = 10000)]
        public async Task TestCustomPeriodicTimerEventInStateMachine()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateActor(typeof(T6), new T6.ConfigEvent { Tcs = tcs, Test = T6.TestType.CustomPeriodicTimer });

                var result = await this.GetResultAsync(tcs);
                Assert.True(result);
            });
        }
    }
}
