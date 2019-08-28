// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Timers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests
{
    public class TimerTest : BaseTest
    {
        public TimerTest(ITestOutputHelper output)
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

        private class T1 : Machine
        {
            private TaskCompletionSource<bool> Tcs;

            private TimerInfo Timer;
            private int Count;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Count = 0;

                // Start a regular timer.
                this.Timer = this.StartTimer(TimeSpan.FromMilliseconds(10));
            }

            private void HandleTimeout()
            {
                this.Count++;
                if (this.Count == 1)
                {
                    this.Tcs.SetResult(true);
                    this.Raise(new Halt());
                    return;
                }

                this.Tcs.SetResult(false);
                this.Raise(new Halt());
            }
        }

        private class T2 : Machine
        {
            private TaskCompletionSource<bool> Tcs;

            private TimerInfo Timer;
            private int Count;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;
                this.Count = 0;

                // Start a periodic timer.
                this.Timer = this.StartPeriodicTimer(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10));
            }

            private void HandleTimeout()
            {
                this.Count++;
                if (this.Count == 10)
                {
                    this.StopTimer(this.Timer);
                    this.Tcs.SetResult(true);
                    this.Raise(new Halt());
                }
            }
        }

        private class T3 : Machine
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
            private class Ping : MachineState
            {
            }

            /// <summary>
            /// Start the PongTimer and start handling the timeout events from it.
            /// After handling 10 events, stop the timer and move to the Ping state.
            /// </summary>
            [OnEntry(nameof(DoPong))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            private class Pong : MachineState
            {
            }

            private async Task DoPing()
            {
                this.Tcs = (this.ReceivedEvent as SetupEvent).Tcs;

                this.PingTimer = this.StartPeriodicTimer(TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(5));
                await Task.Delay(100);
                this.StopTimer(this.PingTimer);

                this.Goto<Pong>();
            }

            private void DoPong()
            {
                this.PongTimer = this.StartPeriodicTimer(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50));
            }

            private void HandleTimeout()
            {
                var timeout = this.ReceivedEvent as TimerElapsedEvent;
                if (timeout.Info == this.PongTimer)
                {
                    this.Tcs.SetResult(true);
                    this.Raise(new Halt());
                }
                else
                {
                    this.Tcs.SetResult(false);
                    this.Raise(new Halt());
                }
            }
        }

        private class T4 : Machine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            private class Init : MachineState
            {
            }

            private void Initialize()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;

                try
                {
                    this.StartTimer(TimeSpan.FromSeconds(-1));
                }
                catch (AssertionFailureException ex)
                {
                    this.Logger.WriteLine(ex.Message);
                    tcs.SetResult(true);
                    this.Raise(new Halt());
                    return;
                }

                tcs.SetResult(false);
                this.Raise(new Halt());
            }
        }

        private class T5 : Machine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            private class Init : MachineState
            {
            }

            private void Initialize()
            {
                var tcs = (this.ReceivedEvent as SetupEvent).Tcs;

                try
                {
                    this.StartPeriodicTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(-1));
                }
                catch (AssertionFailureException ex)
                {
                    this.Logger.WriteLine(ex.Message);
                    tcs.SetResult(true);
                    this.Raise(new Halt());
                    return;
                }

                tcs.SetResult(false);
                this.Raise(new Halt());
            }
        }

        [Fact(Timeout=5000)]
        public async Task TestBasicTimerOperation()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(T1), new SetupEvent(tcs));

                var result = await GetResultAsync(tcs.Task);
                Assert.True(result);
            });
        }

        [Fact(Timeout=5000)]
        public async Task TestBasicPeriodicTimerOperation()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(T2), new SetupEvent(tcs));

                var result = await GetResultAsync(tcs.Task);
                Assert.True(result);
            });
        }

        [Fact(Timeout=5000)]
        public async Task TestDropTimeoutsAfterTimerDisposal()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(T3), new SetupEvent(tcs));

                var result = await GetResultAsync(tcs.Task);
                Assert.True(result);
            });
        }

        [Fact(Timeout=5000)]
        public async Task TestIllegalDueTimeSpecification()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(T4), new SetupEvent(tcs));

                var result = await GetResultAsync(tcs.Task);
                Assert.True(result);
            });
        }

        [Fact(Timeout=5000)]
        public async Task TestIllegalPeriodSpecification()
        {
            await this.RunAsync(async r =>
            {
                var tcs = new TaskCompletionSource<bool>();
                r.CreateMachine(typeof(T5), new SetupEvent(tcs));

                var result = await GetResultAsync(tcs.Task);
                Assert.True(result);
            });
        }
    }
}
