// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;

namespace Microsoft.Coyote.Samples.CoffeeMachineActors
{
    /// <summary>
    /// This monitors the coffee machine to make sure it always finishes the job,
    /// either by making the requested coffee or by requesting a refill.
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
