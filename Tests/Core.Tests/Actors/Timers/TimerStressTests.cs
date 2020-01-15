// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests.Actors
{
    public class TimerStressTests : BaseTest
    {
        public TimerStressTests(ITestOutputHelper output)
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

        private class T1 : StateMachine
        {
            private TaskCompletionSource<bool> Tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.Tcs = (e as SetupEvent).Tcs;

                // Start a regular timer.
                this.StartTimer(TimeSpan.FromTicks(1));
            }

            private Transition HandleTimeout()
            {
                this.Tcs.SetResult(true);
                return this.Halt();
            }
        }

        [Fact(Timeout = 6000)]
        public async Task TestTimerLifetime()
        {
            await this.RunAsync(async r =>
            {
                int numTimers = 1000;
                var awaiters = new Task[numTimers];
                for (int i = 0; i < numTimers; i++)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    r.CreateActor(typeof(T1), new SetupEvent(tcs));
                    awaiters[i] = tcs.Task;
                }

                Task task = Task.WhenAll(awaiters);
                await WaitAsync(task);
            });
        }

        private class T2 : StateMachine
        {
            private TaskCompletionSource<bool> Tcs;
            private int Counter;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.Tcs = (e as SetupEvent).Tcs;
                this.Counter = 0;

                // Start a periodic timer.
                this.StartPeriodicTimer(TimeSpan.FromTicks(1), TimeSpan.FromTicks(1));
            }

            private Transition HandleTimeout()
            {
                this.Counter++;
                if (this.Counter == 10)
                {
                    this.Tcs.SetResult(true);
                    return this.Halt();
                }

                return default;
            }
        }

        [Fact(Timeout = 6000)]
        public async Task TestPeriodicTimerLifetime()
        {
            await this.RunAsync(async r =>
            {
                int numTimers = 1000;
                var awaiters = new Task[numTimers];
                for (int i = 0; i < numTimers; i++)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    r.CreateActor(typeof(T2), new SetupEvent(tcs));
                    awaiters[i] = tcs.Task;
                }

                Task task = Task.WhenAll(awaiters);
                await WaitAsync(task);
            });
        }
    }
}
