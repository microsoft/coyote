// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Samples.Common;
using Microsoft.Coyote.Specifications;

namespace Microsoft.Coyote.Samples.DrinksServingRobot
{
    internal class Robot : StateMachine
    {
        internal ActorId CreatorId; // the Id of the Actor who created this instance.

        internal ActorId NavigatorId { get; set; }

        private readonly LogWriter Log = LogWriter.Instance;
        internal bool RunForever;

        private static readonly Location StartingLocation = new Location(1, 1);
        private Location Coordinates = StartingLocation;
        private List<Location> Route;

        private DrinkOrder CurrentOrder;
        private bool DrinkOrderPending;

        internal const double MoveDuration = 0.5;
        internal const int ServingDuration = 2;
        internal const int RetreatingDuration = 1;

        private readonly Dictionary<string, TimerInfo> Timers = new Dictionary<string, TimerInfo>();

        internal class ConfigEvent : Event
        {
            internal readonly bool RunForever;
            internal readonly ActorId CreatorId;

            public ConfigEvent(bool runForever, ActorId creatorId)
            {
                this.RunForever = runForever;
                this.CreatorId = creatorId;
            }
        }

        internal class RobotReadyEvent : Event
        {
        }

        internal class NavigatorResetEvent : Event { }

        internal class MoveTimerElapsedEvent : TimerElapsedEvent { }

        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventDoAction(typeof(Navigator.RegisterNavigatorEvent), nameof(OnSetNavigator))]
        [DeferEvents(typeof(Navigator.DrinkOrderProducedEvent))]
        internal class Init : State { }

        internal void OnInit(Event e)
        {
            if (e is ConfigEvent ce)
            {
                this.RunForever = ce.RunForever;
                this.CreatorId = ce.CreatorId;
            }
        }

        private void OnSetNavigator(Event e)
        {
            if (e is Navigator.RegisterNavigatorEvent sne)
            {
                // Note: the whole point of this sample is to test failover of the Navigator.
                // The Robot is designed to be robust in the face of failover, and that means
                // it needs to continue on with the new navigator object.
                if (this.NavigatorId == null)
                {
                    this.NavigatorId = sne.NewNavigatorId;
                    this.RaisePushStateEvent<Active>();
                }
                else
                {
                    this.Log.WriteLine("<Robot> received a new Navigator, and pending drink order={0}!!!", this.DrinkOrderPending);

                    // continue on with the new navigator.
                    this.NavigatorId = sne.NewNavigatorId;

                    if (this.DrinkOrderPending)
                    {
                        // stop any current driving and wait for DrinkOrderProducedEvent from new navigator
                        // as it restarts the previous drink order request.
                        this.StopMoving();
                        this.RaiseGotoStateEvent<Active>();
                        this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
                    }
                }

                this.SendEvent(this.CreatorId, new NavigatorResetEvent());
            }
        }

        [OnEntry(nameof(OnInitActive))]
        [OnEventGotoState(typeof(Navigator.DrinkOrderProducedEvent), typeof(ExecutingOrder))]
        [OnEventDoAction(typeof(Navigator.DrinkOrderConfirmedEvent), nameof(OnDrinkOrderConfirmed))]
        internal class Active : State { }

        private void OnInitActive()
        {
            if (!this.DrinkOrderPending)
            {
                this.SendEvent(this.NavigatorId, new Navigator.GetDrinkOrderEvent(this.GetPicture()));
                this.Log.WriteLine("<Robot> Asked for a new Drink Order");
            }

            this.Monitor<LivenessMonitor>(new LivenessMonitor.BusyEvent());
        }

        private void OnDrinkOrderConfirmed()
        {
            this.DrinkOrderPending = true;
            this.SendEvent(this.CreatorId, new RobotReadyEvent());
        }

        public RoomPicture GetPicture()
        {
            var now = DateTime.UtcNow;
            this.Log.WriteLine($"<Robot> Obtained a Room Picture at {now} UTC");
            return new RoomPicture() { TimeTaken = now, Image = ReadCamera() };
        }

        private static byte[] ReadCamera()
        {
            return new byte[1]; // todo: plug in real camera code here.
        }

        [OnEntry(nameof(OnInitExecutingOrder))]
        [OnEventGotoState(typeof(DrivingInstructionsEvent), typeof(ReachingClient))]
        internal class ExecutingOrder : State { }

        private void OnInitExecutingOrder(Event e)
        {
            this.CurrentOrder = (e as Navigator.DrinkOrderProducedEvent)?.DrinkOrder;

            if (this.CurrentOrder != null)
            {
                this.Log.WriteLine("<Robot> Received new Drink Order. Executing ...");
                this.ExecuteOrder();
            }
        }

        private void ExecuteOrder()
        {
            var clientLocation = this.CurrentOrder.ClientDetails.Coordinates;
            this.Log.WriteLine($"<Robot> Asked for driving instructions from {this.Coordinates} to {clientLocation}");

            this.SendEvent(this.NavigatorId, new Navigator.GetDrivingInstructionsEvent(this.Coordinates, clientLocation));
            this.Monitor<LivenessMonitor>(new LivenessMonitor.BusyEvent());
        }

        [OnEntry(nameof(ReachClient))]
        internal class ReachingClient : State { }

        private void ReachClient(Event e)
        {
            var route = (e as DrivingInstructionsEvent)?.Route;
            if (route != null)
            {
                this.Route = route;
                // this.DrinkOrderPending = false; // this is where it really belongs.
                this.Timers["MoveTimer"] = this.StartTimer(TimeSpan.FromSeconds(MoveDuration), new MoveTimerElapsedEvent());
            }

            this.RaiseGotoStateEvent<MovingOnRoute>();
        }

        [OnEventDoAction(typeof(MoveTimerElapsedEvent), nameof(NextMove))]
        [IgnoreEvents(typeof(Navigator.DrinkOrderProducedEvent))]
        internal class MovingOnRoute : State { }

        private void NextMove()
        {
            this.DrinkOrderPending = false;

            if (this.Route == null)
            {
                return;
            }

            if (!this.Route.Any())
            {
                this.StopMoving();
                this.RaiseGotoStateEvent<ServingClient>();

                this.Log.WriteLine("<Robot> Reached Client.");
                Specification.Assert(
                    this.Coordinates == this.CurrentOrder.ClientDetails.Coordinates,
                    "Having reached the Client the Robot's coordinates must be the same as the Client's, but they aren't");
            }
            else
            {
                var nextDestination = this.Route[0];
                this.Route.RemoveAt(0);
                this.MoveTo(nextDestination);
                this.Timers["MoveTimer"] = this.StartTimer(TimeSpan.FromSeconds(MoveDuration), new MoveTimerElapsedEvent());
            }
        }

        private void StopMoving()
        {
            this.Route = null;
            this.DestroyTimer("MoveTimer");
        }

        private void DestroyTimer(string name)
        {
            if (this.Timers.TryGetValue(name,  out TimerInfo info))
            {
                this.StopTimer(info);
                this.Timers.Remove(name);
            }
        }

        private void MoveTo(Location there)
        {
            this.Log.WriteLine($"<Robot> Moving from {this.Coordinates} to {there}");
            this.Coordinates = there;
        }

        [OnEntry(nameof(ServeClient))]
        internal class ServingClient : State { }

        private void ServeClient()
        {
            this.Log.WriteLine("<Robot> Serving order");
            var drinkType = this.SelectDrink();
            var glassOfDrink = this.GetFullFlass(drinkType);

            this.FinishOrder();
        }

        private void FinishOrder()
        {
            this.Log.WriteLine("<Robot> Finished serving the order. Retreating.");
            this.Log.WriteLine("==================================================");
            this.Log.WriteLine(string.Empty);
            this.MoveTo(StartingLocation);
            this.CurrentOrder = null;
            this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
            if (this.RunForever)
            {
                this.RaiseGotoStateEvent<Active>();
            }
            else
            {
                this.RaiseGotoStateEvent<FinishState>();
            }
        }

        private DrinkType SelectDrink()
        {
            var clientType = this.CurrentOrder.ClientDetails.PersonType;
            var selectedDrink = this.GetRandomDrink(clientType);
            this.Log.WriteLine($"<Robot> Selected \"{selectedDrink}\" for {clientType} client");
            return selectedDrink;
        }

        private Glass GetFullFlass(DrinkType drinkType)
        {
            var fillLevel = 100;
            this.Log.WriteLine($"<Robot> Filled a new glass of {drinkType} to {fillLevel}% level");
            return new Glass(drinkType, fillLevel);
        }

        private DrinkType GetRandomDrink(PersonType drinkerType)
        {
            var appropriateDrinks = drinkerType == PersonType.Adult
                ? Drinks.ForAdults
                : Drinks.ForMinors;
            return appropriateDrinks[this.RandomInteger(appropriateDrinks.Count)];
        }

        [OnEntry(nameof(Finish))]
        internal class FinishState : State { }

        private void Finish()
        {
            this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
            this.SendEvent(this.Id, HaltEvent.Instance);
        }

        protected override Task OnEventUnhandledAsync(Event e, string state)
        {
            // this can be handy for debugging.
            return base.OnEventUnhandledAsync(e, state);
        }
    }
}
