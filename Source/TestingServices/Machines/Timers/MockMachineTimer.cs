// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;

namespace Microsoft.Coyote.TestingServices.Timers
{
    /// <summary>
    /// A mock timer that replaces <see cref="MachineTimer"/> during testing.
    /// It is implemented as a machine.
    /// </summary>
    internal class MockMachineTimer : StateMachine, IMachineTimer
    {
        /// <summary>
        /// Stores information about this timer.
        /// </summary>
        private TimerInfo TimerInfo;

        /// <summary>
        /// Stores information about this timer.
        /// </summary>
        TimerInfo IMachineTimer.Info => this.TimerInfo;

        /// <summary>
        /// The machine that owns this timer.
        /// </summary>
        private StateMachine Owner;

        /// <summary>
        /// The timeout event.
        /// </summary>
        private TimerElapsedEvent TimeoutEvent;

        /// <summary>
        /// Adjusts the probability of firing a timeout event.
        /// </summary>
        private uint Delay;

        [Start]
        [OnEntry(nameof(Setup))]
        [OnEventDoAction(typeof(Default), nameof(HandleTimeout))]
        private class Active : State
        {
        }

        /// <summary>
        /// Initializes the timer with the specified configuration.
        /// </summary>
        private void Setup()
        {
            this.TimerInfo = (this.ReceivedEvent as TimerSetupEvent).Info;
            this.Owner = (this.ReceivedEvent as TimerSetupEvent).Owner;
            this.Delay = (this.ReceivedEvent as TimerSetupEvent).Delay;
            this.TimeoutEvent = new TimerElapsedEvent(this.TimerInfo);
        }

        /// <summary>
        /// Handles the timeout.
        /// </summary>
        private void HandleTimeout()
        {
            // Try to send the next timeout event.
            bool isTimeoutSent = false;
            int delay = (int)this.Delay > 0 ? (int)this.Delay : 1;
            if ((this.RandomInteger(delay) == 0) && this.FairRandom())
            {
                // The probability of sending a timeout event is atmost 1/N.
                this.SendEvent(this.Owner.Id, this.TimeoutEvent);
                isTimeoutSent = true;
            }

            // If non-periodic, and a timeout was successfully sent, then become
            // inactive until disposal. Else retry.
            if (isTimeoutSent && this.TimerInfo.Period.TotalMilliseconds < 0)
            {
                this.Goto<Inactive>();
            }
        }

        private class Inactive : State
        {
        }

        /// <summary>
        /// Determines whether the specified System.Object is equal
        /// to the current System.Object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is MockMachineTimer timer)
            {
                return this.Id == timer.Id;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode() => this.Id.GetHashCode();

        /// <summary>
        /// Returns a string that represents the current instance.
        /// </summary>
        public override string ToString() => this.Id.Name;

        /// <summary>
        /// Indicates whether the specified <see cref="ActorId"/> is equal
        /// to the current <see cref="ActorId"/>.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the other parameter; otherwise, false.</returns>
        public bool Equals(MachineTimer other)
        {
            return this.Equals((object)other);
        }

        /// <summary>
        /// Disposes the resources held by this timer.
        /// </summary>
        public void Dispose()
        {
            this.Runtime.SendEvent(this.Id, new Halt());
        }
    }
}
