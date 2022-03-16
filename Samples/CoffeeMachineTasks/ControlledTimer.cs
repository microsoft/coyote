// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Samples.CoffeeMachineTasks
{
    /// <summary>
    /// This class provides a Timer that is similar to how the Actor model timers work that
    /// has delays that can be controlled by Coyote tester.
    /// </summary>
    internal class ControlledTimer
    {
        private readonly CancellationTokenSource Source = new CancellationTokenSource();
        private readonly TimeSpan StartDelay;
        private readonly TimeSpan? Interval;
        private readonly Action Handler;
        private bool Stopped;
        private readonly string Name;

        public ControlledTimer(string name, TimeSpan startDelay, TimeSpan interval, Action handler)
        {
            this.Name = name;
            this.StartDelay = startDelay;
            this.Interval = interval;
            this.Handler = handler;
            this.StartTimer(startDelay);
        }

        public ControlledTimer(string name, TimeSpan dueTime, Action handler)
        {
            this.Name = name;
            this.StartDelay = dueTime;
            this.Handler = handler;
            this.StartTimer(dueTime);
        }

        private void StartTimer(TimeSpan dueTime)
        {
            Task.Run(async () =>
            {
                await Task.Delay(dueTime, this.Source.Token);
                this.OnTick();
            });
        }

        private void OnTick()
        {
            if (!this.Stopped)
            {
                this.Handler();
                if (this.Interval.HasValue)
                {
                    this.StartTimer(this.Interval.Value);
                }
            }
        }

        public void Stop()
        {
            this.Stopped = true;
            this.Source.Cancel();
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
