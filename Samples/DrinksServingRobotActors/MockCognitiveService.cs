// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Samples.Common;

namespace Microsoft.Coyote.Samples.DrinksServingRobot
{
    internal class RecognizeDrinksClientEvent : Event
    {
        public readonly ActorId ClientId;
        public readonly RoomPicture Picture;

        public RecognizeDrinksClientEvent(ActorId clientId, RoomPicture picture)
        {
            this.ClientId = clientId;
            this.Picture = picture;
        }
    }

    internal class DrinksClientDetailsEvent : Event
    {
        public ClientDetails Details { get; private set; }

        public DrinksClientDetailsEvent(ClientDetails details)
        {
            this.Details = details;
        }
    }

    internal class MockCognitiveService : StateMachine
    {
        private const double WorkTime = 1.5;
        private readonly LogWriter Log = LogWriter.Instance;

        internal class RecognitionTimerEvent : TimerElapsedEvent
        {
            public ActorId ClientId;
        }

        [Start]
        [OnEntry(nameof(OnInit))]
        [DeferEvents(typeof(RecognizeDrinksClientEvent))]
        internal class Init : State { }

        private void OnInit()
        {
            this.Log.WriteLine("<CognitiveService> starting.");
            this.RaiseGotoStateEvent<Active>();
        }

        [OnEventDoAction(typeof(RecognizeDrinksClientEvent), nameof(FindADrinksClient))]
        [OnEventDoAction(typeof(RecognitionTimerEvent), nameof(OnTick))]
        internal class Active : State { }

        private void FindADrinksClient(Event e)
        {
            if (e is RecognizeDrinksClientEvent re)
            {
                // Simulate the fact that this service can take some time.
                this.StartTimer(TimeSpan.FromSeconds(WorkTime), new RecognitionTimerEvent() { ClientId = re.ClientId });
            }
        }

        private void OnTick(Event e)
        {
            if (e is RecognitionTimerEvent te)
            {
                var clientId = te.ClientId;
                var clientLocation = Utilities.GetRandomLocation(this.RandomInteger, 2, 2, 30, 30);
                var personType = Utilities.GetRandomPersonType(this.RandomInteger);
                var clientDetailsEvent = new DrinksClientDetailsEvent(new ClientDetails(personType, clientLocation));
                this.SendEvent(clientId, clientDetailsEvent);
            }
        }
    }
}
