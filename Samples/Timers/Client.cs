using System;

using Microsoft.Coyote;
using Microsoft.Coyote.Timers;

namespace Timers
{
    class Client : Machine
    {
        /// <summary>
        /// Count of timeout events processed per state.
        /// </summary>
        int Count;

        /// <summary>
        /// Timer used in the Ping state.
        /// </summary>
        TimerInfo PingTimerInfo;

        /// <summary>
        /// Timer used in the Pong state.
        /// </summary>
        TimerInfo PongTimerInfo;

        /// <summary>
        /// Start the ping timer and start handling the timeout events from it.
        /// After handling 10 events, stop ping timer and move to the Pong state.
        /// </summary>
        [Start]
        [OnEntry(nameof(DoPing))]
        [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeoutForPing))]
        class Ping : MachineState { }

        /// <summary>
        /// Start the pong timer and start handling the timeout events from it.
        /// After handling 10 events, stop pong timer and move to the Ping state.
        /// </summary>
        [OnEntry(nameof(DoPong))]
        [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeoutForPong))]
        class Pong : MachineState { }

        private void DoPing()
        {
            // Reset the count.
            this.Count = 1;

            // Start a periodic timer with timeout interval of 1sec.
            // The timer generates TimerElapsedEvent with 'm' as payload.
            this.PingTimerInfo = this.StartPeriodicTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), payload: new object());
        }

        /// <summary>
        /// Handle timeout events from the ping timer.
        /// </summary>
        private void HandleTimeoutForPing()
        {
            var timeout = (this.ReceivedEvent as TimerElapsedEvent);

            // Ensure that we are handling a valid timeout event.
            this.Assert(timeout.Info == this.PingTimerInfo, "Handling timeout event from an invalid timer.");
            this.Logger.WriteLine("Ping count: {0}", this.Count);

            // Extract the payload.
            object payload = timeout.Info.Payload;

            // Increment the count.
            this.Count++;
            if (this.Count == 10)
            {
                // Stop the ping timer after handling 10 timeout events.
                // This will cause any enqueued events from this timer to be ignored.
                this.StopTimer(this.PingTimerInfo);
                this.Goto<Pong>();
            }
        }

        /// <summary>
        /// Handle timeout events from the pong timer.
        /// </summary>
        private void DoPong()
        {
            // Reset the count.
            this.Count = 1;

            // Start a periodic timer with timeout interval of 0.5sec.
            // The timer generates a TimerElapsedEvent with 'm' as payload.
            this.PongTimerInfo = this.StartPeriodicTimer(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500), payload: new object());
        }

        private void HandleTimeoutForPong()
        {
            var timeout = (this.ReceivedEvent as TimerElapsedEvent);

            // Ensure that we are handling a valid timeout event.
            this.Assert(timeout.Info == this.PongTimerInfo, "Handling timeout event from an invalid timer.");
            this.Logger.WriteLine("Pong count: {0}", this.Count);

            // Extract the payload.
            object payload = timeout.Info.Payload;

            // Increment the count.
            this.Count++;
            if (this.Count == 10)
            {
                // Stop the pong timer after handling 10 timeout events.
                // This will cause any enqueued events from this timer to be ignored.
                this.StopTimer(this.PongTimerInfo);
                this.Goto<Ping>();
            }
        }
    }
}
