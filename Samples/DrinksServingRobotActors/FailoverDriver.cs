// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Samples.Common;

namespace Microsoft.Coyote.Samples.DrinksServingRobot
{
    internal class FailoverDriver : StateMachine
    {
        private ActorId StorageId;
        private ActorId RobotId;
        private ActorId NavigatorId;

        private bool RunForever;
        private readonly LogWriter Log = LogWriter.Instance;

        private TimerInfo HaltTimer;
        private int Iterations;
        private const int NavigatorTimeToLive = 500;  // milliseconds

        internal class ConfigEvent : Event
        {
            public bool RunForever;

            public ConfigEvent(bool runForever)
            {
                this.RunForever = runForever;
            }
        }

        [Start]
        [OnEntry(nameof(OnInit))]
        [DeferEvents(typeof(TimerElapsedEvent), typeof(Robot.RobotReadyEvent))]
        [IgnoreEvents(typeof(Robot.NavigatorResetEvent))]
        internal class Init : State { }

        internal void OnInit(Event e)
        {
            if (e is ConfigEvent ce)
            {
                this.RunForever = ce.RunForever;
            }

            this.Log.WriteLine("<FailoverDriver> #################################################################");
            this.Log.WriteLine("<FailoverDriver> Starting the Robot.");
            this.StorageId = this.CreateActor(typeof(MockStorage));
            this.NavigatorId = this.CreateNavigator();

            // Create the Robot.
            this.RobotId = this.CreateActor(typeof(Robot), new Robot.ConfigEvent(this.RunForever, this.Id));

            // Wake up the Navigator.
            this.SendEvent(this.NavigatorId, new Navigator.WakeUpEvent(this.RobotId));
            this.RaiseGotoStateEvent<Active>();
        }

        [OnEventGotoState(typeof(TimerElapsedEvent), typeof(TerminatingNavigator))]
        [OnEventDoAction(typeof(Robot.RobotReadyEvent), nameof(OnRobotReady))]
        [IgnoreEvents(typeof(Robot.NavigatorResetEvent))]
        internal class Active : State { }

        private void OnRobotReady()
        {
            // We have to wait for the robot to be ready before we test killing the Navigator otherwise
            // we end up killing the Navigator before the Robot has anything to do which is a waste of time.
            // Setup a timer to randomly kill the Navigator.   When the timer fires we will restart the
            // Navigator and this is testing that the Navigator and Robot can recover gracefully when that happens.

            int duration = this.RandomInteger(NavigatorTimeToLive) + NavigatorTimeToLive;
            this.HaltTimer = this.StartTimer(TimeSpan.FromMilliseconds(duration));
        }

        private void StopTimer()
        {
            if (this.HaltTimer != null)
            {
                this.StopTimer(this.HaltTimer);
                this.HaltTimer = null;
            }
        }

        private ActorId CreateNavigator()
        {
            var cognitiveServiceId = this.CreateActor(typeof(MockCognitiveService));
            var routePlannerServiceId = this.CreateActor(typeof(MockRoutePlanner));
            return this.CreateActor(typeof(Navigator), new Navigator.NavigatorConfigEvent(this.Id, this.StorageId, cognitiveServiceId, routePlannerServiceId));
        }

        [OnEntry(nameof(OnTerminateNavigator))]
        [OnEventDoAction(typeof(Navigator.HaltedEvent), nameof(OnHalted))]
        [OnEventDoAction(typeof(Robot.NavigatorResetEvent), nameof(OnNavigatorReset))]
        [IgnoreEvents(typeof(TimerElapsedEvent))]
        internal class TerminatingNavigator : State { }

        private void OnTerminateNavigator()
        {
            this.StopTimer();
            this.Log.WriteLine("<FailoverDriver> #################################################################");
            this.Log.WriteLine("<FailoverDriver> #       Starting the fail over of the Navigator                 #");
            this.Log.WriteLine("<FailoverDriver> #################################################################");
            this.SendEvent(this.NavigatorId, new Navigator.TerminateEvent());
        }

        private void OnHalted()
        {
            this.Log.WriteLine("<FailoverDriver> *****  The Navigator confirmed that it has terminated ***** ");

            // Create a new Navigator.
            this.Log.WriteLine("<FailoverDriver> *****   Created a new Navigator -- paused *****");
            this.NavigatorId = this.CreateNavigator();

            this.Log.WriteLine("<FailoverDriver> *****   Waking up the new Navigator *****");
            this.SendEvent(this.NavigatorId, new Navigator.WakeUpEvent(this.RobotId));
        }

        private void OnNavigatorReset()
        {
            this.Log.WriteLine("*****   Robot confirmed it reset to the new Navigator *****");

            this.Iterations++;
            if (this.Iterations == 1 || this.RunForever)
            {
                // Continue on, we expect the WakeUpEvent to RegisterNavigator on the Robot which
                // will cause the Robot to raise another RobotReady event.
            }
            else
            {
                this.HaltSystem();
            }

            this.RaiseGotoStateEvent<Active>();
        }

        private void HaltSystem()
        {
            this.KillActors(this.RobotId, this.NavigatorId, this.StorageId);
            this.RaiseHaltEvent();
        }

        private void KillActors(params ActorId[] actors)
        {
            foreach (var actor in actors.Where(ac => ac != null))
            {
                this.SendEvent(actor, HaltEvent.Instance);
            }
        }

        private void WriteLine(string s)
        {
            this.Log.WriteLine(s);
        }
    }
}
