// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Samples.Common;
using Microsoft.Coyote.Specifications;

namespace Microsoft.Coyote.Samples.DrinksServingRobot
{
    /// <summary>
    /// The Navigator state machine manages the handling of a long running request to serve a drink
    /// to someone in a camera picture.  The idea is the robot sees someone who wants a drink, and
    /// goes about making this happen.  The Navigator uses MockStorage to persist this operation
    /// so that it can be fault tolerant in the light of "failover" of the Navigator.
    /// </summary>
    internal class Navigator : StateMachine
    {
        private ActorId CreatorId;
        private ActorId RobotId;
        private ActorId StorageId;
        private ActorId CognitiveServiceId;
        private ActorId RoutePlannerServiceId;
        private bool Terminating;
        private readonly LogWriter Log = LogWriter.Instance;

        private const string DrinkOrderStorageKey = "DrinkOrderStorageKey";

        internal class NavigatorConfigEvent : Event
        {
            public ActorId CreatorId;
            public ActorId StorageId;
            public ActorId CognitiveServiceId;
            public ActorId RoutePlannerId;

            public NavigatorConfigEvent(ActorId creatorId, ActorId storageId, ActorId cognitiveServiceId, ActorId routePlannerId)
            {
                this.CreatorId = creatorId;
                this.StorageId = storageId;
                this.CognitiveServiceId = cognitiveServiceId;
                this.RoutePlannerId = routePlannerId;
            }
        }

        internal class TerminateEvent : Event { }

        internal class GetDrinkOrderEvent : Event
        {
            public RoomPicture Picture;

            public GetDrinkOrderEvent(RoomPicture picture)
            {
                this.Picture = picture;
            }
        }

        internal class DrinkOrderConfirmedEvent : Event
        {
        }

        internal class DrinkOrderProducedEvent : Event
        {
            public DrinkOrder DrinkOrder;

            public DrinkOrderProducedEvent(DrinkOrder drinkOrder)
            {
                this.DrinkOrder = drinkOrder;
            }
        }

        internal class GetDrivingInstructionsEvent : Event
        {
            public readonly Location StartPoint;
            public readonly Location EndPoint;

            public GetDrivingInstructionsEvent(Location startPoint, Location endPoint)
            {
                this.StartPoint = startPoint;
                this.EndPoint = endPoint;
            }
        }

        internal class HaltedEvent : Event { }

        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventDoAction(typeof(TerminateEvent), nameof(OnTerminate))]
        [DeferEvents(typeof(WakeUpEvent), typeof(GetDrinkOrderEvent), typeof(GetDrivingInstructionsEvent))]
        internal class Init : State { }

        internal void OnInit(Event e)
        {
            if (e is NavigatorConfigEvent configEvent)
            {
                this.CreatorId = configEvent.CreatorId;
                this.StorageId = configEvent.StorageId;
                this.CognitiveServiceId = configEvent.CognitiveServiceId;
                this.RoutePlannerServiceId = configEvent.RoutePlannerId;
            }

            this.RaisePushStateEvent<Paused>();
        }

        private void SaveGetDrinkOrderEvent(GetDrinkOrderEvent e)
        {
            this.SendEvent(this.StorageId, new KeyValueEvent(this.Id, DrinkOrderStorageKey, e));
        }

        internal class WakeUpEvent : Event
        {
            internal readonly ActorId ClientId;

            public WakeUpEvent(ActorId clientId)
            {
                this.ClientId = clientId;
            }
        }

        internal class RegisterNavigatorEvent : Event
        {
            internal ActorId NewNavigatorId;

            public RegisterNavigatorEvent(ActorId newNavigatorId)
            {
                this.NewNavigatorId = newNavigatorId;
            }
        }

        [OnEventDoAction(typeof(WakeUpEvent), nameof(OnWakeUp))]
        [OnEventDoAction(typeof(KeyValueEvent), nameof(RestartPendingJob))]
        [DeferEvents(typeof(TerminateEvent), typeof(GetDrinkOrderEvent), typeof(GetDrivingInstructionsEvent))]
        internal class Paused : State { }

        private void OnWakeUp(Event e)
        {
            this.Log.WriteLine("<Navigator> starting");
            if (e is WakeUpEvent wpe)
            {
                this.Log.WriteLine("<Navigator> Got RobotId");
                this.RobotId = wpe.ClientId;

                // tell this client robot about this new navigator.  During failover testing
                // of the Navigator, this can be swapping out the Navigator that the robot is using.
                this.SendEvent(this.RobotId, new RegisterNavigatorEvent(this.Id));
            }

            // Check storage to see if we have a pending request already.
            this.SendEvent(this.StorageId, new ReadKeyEvent(this.Id, DrinkOrderStorageKey));
        }

        internal void RestartPendingJob(Event e)
        {
            if (e is KeyValueEvent kve)
            {
                var key = kve.Key;
                object value = kve.Value;
                Specification.Assert(key != null, $"Error: KeyValueEvent contains a null key");
                if (key == DrinkOrderStorageKey)
                {
                    this.RestartPendingGetDrinkOrderRequest(value as GetDrinkOrderEvent);
                }

                this.RaiseGotoStateEvent<Active>();
            }
        }

        private void RestartPendingGetDrinkOrderRequest(GetDrinkOrderEvent e)
        {
            if (e != null)
            {
                this.ProcessDrinkOrder(e);
                this.Log.WriteLine("<Navigator> Restarting the pending Robot's request to find drink clients ...");
            }
            else
            {
                this.Log.WriteLine("<Navigator> There was no prior pending request to find drink clients ...");
            }
        }

        [OnEntry(nameof(InitActive))]
        [OnEventDoAction(typeof(GetDrinkOrderEvent), nameof(GetDrinkOrder))]
        [OnEventDoAction(typeof(ConfirmedEvent), nameof(OnStorageConfirmed))]
        [OnEventDoAction(typeof(GetDrivingInstructionsEvent), nameof(GetDrivingInstructions))]
        [OnEventDoAction(typeof(DrinksClientDetailsEvent), nameof(SendClientDetailsToRobot))]
        [OnEventDoAction(typeof(DrivingInstructionsEvent), nameof(SendDrivingInstructionsToRobot))]
        [IgnoreEvents(typeof(KeyValueEvent))]
        internal class Active : State { }

        private void InitActive()
        {
            this.Log.WriteLine("<Navigator> initialized.");
        }

        private void GetDrinkOrder(Event e)
        {
            if (e is GetDrinkOrderEvent getDrinkOrderEvent)
            {
                this.SaveGetDrinkOrderEvent(getDrinkOrderEvent);
            }
        }

        private void OnStorageConfirmed(Event e)
        {
            if (e is ConfirmedEvent ce && ce.Key == DrinkOrderStorageKey)
            {
                Specification.Assert(
                    !ce.Existing,
                    $"Error: The storage `{DrinkOrderStorageKey}` was already set which means we lost a GetDrinkOrderEvent");

                this.SendEvent(this.RobotId, new DrinkOrderConfirmedEvent());
                this.ProcessDrinkOrder(ce.Value as GetDrinkOrderEvent);
            }
        }

        private void ProcessDrinkOrder(GetDrinkOrderEvent e)
        {
            // continue on...
            var picture = e.Picture;
            this.SendEvent(this.CognitiveServiceId, new RecognizeDrinksClientEvent(this.Id, picture));
        }

        private void SendClientDetailsToRobot(Event e)
        {
            // When the cognitive service recognizes someone in the picture it sends us a
            // DrinksClientDetailsEvent containing information about who is in the picture and where
            // they are located.
            if (e is DrinksClientDetailsEvent drinksClientDetailsEvent)
            {
                var details = drinksClientDetailsEvent.Details;
                this.SendEvent(this.RobotId, new DrinkOrderProducedEvent(new DrinkOrder(details)));
            }
        }

        private void GetDrivingInstructions(Event e)
        {
            // When the DrinkOrderProducedEvent is received by the Robot it calls back with
            // this event to request driving instructions.  This operation is not restartable.  Instead,
            // during failover of the navigator the robot will re-request any driving instructions.
            if (e is GetDrivingInstructionsEvent getDrivingInstructionsEvent)
            {
                this.ProcessDrivingInstructions(getDrivingInstructionsEvent);
            }
        }

        private void SendDrivingInstructionsToRobot(Event e)
        {
            if (e is DrivingInstructionsEvent drivingInstructionsEvent)
            {
                this.SendEvent(this.RobotId, drivingInstructionsEvent);

                // The drink order is now completed, so we can delete the persistent job.
                this.Log.WriteLine("<Navigator> drink order is complete, deleting the job record.");
                this.SendEvent(this.StorageId, new DeleteKeyEvent(this.Id, DrinkOrderStorageKey));
            }
        }

        private void ProcessDrivingInstructions(GetDrivingInstructionsEvent e)
        {
            this.SendEvent(this.RoutePlannerServiceId, new GetRouteEvent(this.Id, e.StartPoint, e.EndPoint));
        }

        private void OnTerminate(Event e)
        {
            if (e is TerminateEvent)
            {
                this.TerminateMyself();
            }
        }

        private void TerminateMyself()
        {
            if (!this.Terminating)
            {
                this.Terminating = true;
                this.Log.WriteLine("<Navigator> Terminating as previously ordered ...");

                this.SendEvent(this.CognitiveServiceId, HaltEvent.Instance);
                this.SendEvent(this.RoutePlannerServiceId, HaltEvent.Instance);
                this.CognitiveServiceId = this.RoutePlannerServiceId = null;

                this.Log.WriteLine("<Navigator> Sent Termination Confirmation to my Creator ...");
                this.SendEvent(this.CreatorId, new HaltedEvent());
                this.Log.WriteLine("<Navigator> Halting now ...");

                this.RaiseHaltEvent();
            }
        }

        protected override Task OnEventUnhandledAsync(Event e, string state)
        {
            // this can be handy for debugging.
            return base.OnEventUnhandledAsync(e, state);
        }
    }
}
