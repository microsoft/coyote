// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Samples.Common;

namespace Coyote.Examples.Timers
{
    [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
    [OnEventDoAction(typeof(CustomTimerEvent), nameof(HandlePeriodicTimeout))]
    internal class TimerSample : Actor
    {
        /// <summary>
        /// Timer used in a periodic timer.
        /// </summary>
        private TimerInfo PeriodicTimer;

        /// <summary>
        /// The log to write output to
        /// </summary>
        private readonly LogWriter Log = LogWriter.Instance;

        /// <summary>
        /// A custom timer event
        /// </summary>
        internal class CustomTimerEvent : TimerElapsedEvent
        {
            /// <summary>
            /// Count of timeout events processed.
            /// </summary>
            internal int Count;
        }

        protected override Task OnInitializeAsync(Event initialEvent)
        {
            this.Log.WriteWarning("<Client> Starting a non-periodic timer");
            this.StartTimer(TimeSpan.FromSeconds(1));
            return base.OnInitializeAsync(initialEvent);
        }

        private void HandleTimeout(Event e)
        {
            TimerElapsedEvent te = (TimerElapsedEvent)e;

            this.Log.WriteWarning("<Client> Handling timeout from timer");

            this.Log.WriteWarning("<Client> Starting a period timer");
            this.PeriodicTimer = this.StartPeriodicTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), new CustomTimerEvent());
        }

        private void HandlePeriodicTimeout(Event e)
        {
            this.Log.WriteWarning("<Client> Handling timeout from periodic timer");
            if (e is CustomTimerEvent ce)
            {
                ce.Count++;
                if (ce.Count == 3)
                {
                    this.Log.WriteWarning("<Client> Stopping the periodic timer");
                    this.Log.WriteWarning("<Client> Press ENTER to terminate.");
                    this.StopTimer(this.PeriodicTimer);
                }
            }
        }
    }
}
