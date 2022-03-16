// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Samples.DrinksServingRobot
{
    internal class GetRouteEvent : Event
    {
        public readonly ActorId ClientId;
        public readonly Location Start;
        public readonly Location End;

        public GetRouteEvent(ActorId clientId, Location start, Location end)
        {
            this.ClientId = clientId;
            this.Start = start;
            this.End = end;
        }
    }

    internal class DrivingInstructionsEvent : Event
    {
        public readonly List<Location> Route;

        public DrivingInstructionsEvent(List<Location> route)
        {
            this.Route = route;
        }
    }

    internal class MockRoutePlanner : StateMachine
    {
        [Start]
        [OnEventDoAction(typeof(GetRouteEvent), nameof(GenerateRoute))]
        internal class Active : State { }

        private void GenerateRoute(Event e)
        {
            if (e is GetRouteEvent getRouteEvent)
            {
                var clientId = getRouteEvent.ClientId;
                var start = getRouteEvent.Start;
                var destination = getRouteEvent.End;
                var hopsCount = this.RandomInteger(3) + 1;

                var route = new List<Location> { };
                for (var i = 1; i < hopsCount; i++)
                {
                    route.Add(Utilities.GetRandomLocation(this.RandomInteger, 2, 2, 30, 30));
                }

                route.Add(destination);
                this.SendEvent(clientId, new DrivingInstructionsEvent(route));
            }
        }
    }
}
