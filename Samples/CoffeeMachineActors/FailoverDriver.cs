// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Samples.Common;

namespace Microsoft.Coyote.Samples.CoffeeMachineActors
{
    /// <summary>
    /// This class is designed to test how the CoffeeMachine handles "failover" or specifically, can it
    /// correctly "restart after failure" without getting into a bad state. The CoffeeMachine will be
    /// randomly terminated. The only thing the CoffeeMachine can depend on is the persistence of the
    /// state provided by the MockSensors.
    /// </summary>
    internal class FailoverDriver : StateMachine
    {
        private ActorId WaterTankId;
        private ActorId CoffeeGrinderId;
        private ActorId DoorSensorId;
        private ActorId CoffeeMachineId;
        private bool RunForever;
        private int Iterations;
        private TimerInfo HaltTimer;
        private readonly LogWriter Log = LogWriter.Instance;

        internal class StartTestEvent : Event { }

        [Start]
        [OnEntry(nameof(OnInit))]
        [OnEventGotoState(typeof(StartTestEvent), typeof(Test))]
        internal class Init : State { }

        internal void OnInit(Event e)
        {
            var evt = e as ConfigEvent;
            this.RunForever = evt.RunSlowly;

            // Create the persistent sensor state.
            this.WaterTankId = this.CreateActor(typeof(MockWaterTank), new ConfigEvent(this.RunForever));
            this.CoffeeGrinderId = this.CreateActor(typeof(MockCoffeeGrinder), new ConfigEvent(this.RunForever));
            this.DoorSensorId = this.CreateActor(typeof(MockDoorSensor), new ConfigEvent(this.RunForever));
        }

        [OnEntry(nameof(OnStartTest))]
        [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimer))]
        [OnEventGotoState(typeof(CoffeeMachine.CoffeeCompletedEvent), typeof(Stop))]
        internal class Test : State { }

        internal void OnStartTest()
        {
            this.Log.WriteLine("#################################################################");
            this.Log.WriteLine("starting new CoffeeMachine.");
            // Create a new CoffeeMachine instance
            this.CoffeeMachineId = this.CreateActor(typeof(CoffeeMachine), new CoffeeMachine.ConfigEvent(this.WaterTankId,
                this.CoffeeGrinderId, this.DoorSensorId, this.Id));

            // Request a coffee!
            var shots = this.RandomInteger(3) + 1;
            this.SendEvent(this.CoffeeMachineId, new CoffeeMachine.MakeCoffeeEvent(shots));

            // Setup a timer to randomly kill the coffee machine. When the timer fires we
            // will restart the coffee machine and this is testing that the machine can
            // recover gracefully when that happens.
            this.HaltTimer = this.StartTimer(TimeSpan.FromSeconds(this.RandomInteger(7) + 1));
        }

        private void HandleTimer()
        {
            this.RaiseGotoStateEvent<Stop>();
        }

        internal void OnStopTest(Event e)
        {
            if (this.HaltTimer != null)
            {
                this.StopTimer(this.HaltTimer);
                this.HaltTimer = null;
            }

            if (e is CoffeeMachine.CoffeeCompletedEvent ce)
            {
                if (ce.Error)
                {
                    this.Log.WriteWarning("CoffeeMachine reported an error.");
                    this.Log.WriteWarning("Test is complete, press ENTER to continue...");
                    // No point trying to make more coffee.
                    this.RunForever = false;
                }
                else
                {
                    this.Log.WriteLine("CoffeeMachine completed the job.");
                }

                this.RaiseGotoStateEvent<Stopped>();
            }
            else
            {
                // Halt the CoffeeMachine. HaltEvent is async and we must ensure the CoffeeMachine
                // is really halted before we create a new one because MockSensors will get confused
                // if two CoffeeMachines are running at the same time. So we've implemented a terminate
                // handshake here. We send event to the CoffeeMachine to terminate, and it sends back
                // a HaltedEvent when it really has been halted.
                this.Log.WriteLine("forcing termination of CoffeeMachine.");
                this.SendEvent(this.CoffeeMachineId, new CoffeeMachine.TerminateEvent());
            }
        }

        [OnEntry(nameof(OnStopTest))]
        [OnEventDoAction(typeof(CoffeeMachine.HaltedEvent), nameof(OnCoffeeMachineHalted))]
        [IgnoreEvents(typeof(CoffeeMachine.CoffeeCompletedEvent))]
        internal class Stop : State { }

        internal void OnCoffeeMachineHalted()
        {
            // OK, the CoffeeMachine really is halted now, so we can go to the stopped state.
            this.RaiseGotoStateEvent<Stopped>();
        }

        [OnEntry(nameof(OnStopped))]
        internal class Stopped : State { }

        private void OnStopped()
        {
            if (this.RunForever || this.Iterations == 0)
            {
                this.Iterations += 1;
                // Run another CoffeeMachine instance!
                this.RaiseGotoStateEvent<Test>();
            }
            else
            {
                // Test is done, halt the mock sensors.
                this.SendEvent(this.DoorSensorId, HaltEvent.Instance);
                this.SendEvent(this.WaterTankId, HaltEvent.Instance);
                this.SendEvent(this.CoffeeGrinderId, HaltEvent.Instance);
                this.RaiseHaltEvent();
            }
        }

        protected override Task OnEventUnhandledAsync(Event e, string state)
        {
            this.Log.WriteLine("### Unhandled event {0} in state {1}", e.GetType().FullName, state);
            return base.OnEventUnhandledAsync(e, state);
        }
    }
}
