// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;

namespace Microsoft.Coyote.Samples.DrinksServingRobot
{
    /// <summary>
    /// This monitors the Robot and the Navigator to make sure the Robot always finishes the job,
    /// by serving a Drink.
    /// </summary>
    internal class LivenessMonitor : Monitor
    {
        public class BusyEvent : Event { }

        public class IdleEvent : Event { }

        [Start]
        [Cold]
        [OnEventGotoState(typeof(BusyEvent), typeof(Busy))]
        [IgnoreEvents(typeof(IdleEvent))]
        private class Idle : State { }

        [Hot]
        [OnEventGotoState(typeof(IdleEvent), typeof(Idle))]
        [IgnoreEvents(typeof(BusyEvent))]
        private class Busy : State { }
    }
}
