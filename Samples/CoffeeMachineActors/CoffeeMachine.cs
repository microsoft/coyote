// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Samples.Common;

namespace Microsoft.Coyote.Samples.CoffeeMachineActors
{
    [OnEventDoAction(typeof(TerminateEvent), nameof(OnTerminate))]
    internal class CoffeeMachine : StateMachine
    {
        private ActorId Client;
        private ActorId WaterTank;
        private ActorId CoffeeGrinder;
        private ActorId DoorSensor;
        private bool Heating;
        private double? WaterLevel;
        private double? HopperLevel;
        private bool? DoorOpen;
        private double? PortaFilterCoffeeLevel;
        private double? WaterTemperature;
        private int ShotsRequested;
        private double PreviousCoffeeLevel;
        private double PreviousShotCount;
        private readonly LogWriter Log = LogWriter.Instance;

        internal class ConfigEvent : Event
        {
            public ActorId WaterTank;
            public ActorId CoffeeGrinder;
            public ActorId Client;
            public ActorId DoorSensor;

            public ConfigEvent(ActorId waterTank, ActorId coffeeGrinder, ActorId doorSensor, ActorId client)
            {
                this.WaterTank = waterTank;
                this.CoffeeGrinder = coffeeGrinder;
                this.Client = client;
                this.DoorSensor = doorSensor;
            }
        }

        internal class MakeCoffeeEvent : Event
        {
            public int Shots;

            public MakeCoffeeEvent(int shots)
            {
                this.Shots = shots;
            }
        }

        internal class CoffeeCompletedEvent : Event
        {
            public bool Error;
        }

        internal class TerminateEvent : Event { }

        internal class HaltedEvent : Event { }

        [Start]
        [OnEntry(nameof(OnInit))]
        [DeferEvents(typeof(MakeCoffeeEvent))]
        private class Init : State { }

        private void OnInit(Event e)
        {
            var evt = e as ConfigEvent;
            this.Log.WriteLine("initializing...");
            this.Client = evt.Client;
            this.WaterTank = evt.WaterTank;
            this.CoffeeGrinder = evt.CoffeeGrinder;
            this.DoorSensor = evt.DoorSensor;
            // Register this class as a client of the sensors.
            this.SendEvent(this.WaterTank, new RegisterClientEvent(this.Id));
            this.SendEvent(this.CoffeeGrinder, new RegisterClientEvent(this.Id));
            this.SendEvent(this.DoorSensor, new RegisterClientEvent(this.Id));
            this.RaiseGotoStateEvent<CheckSensors>();
        }

        [OnEntry(nameof(OnCheckSensors))]
        [DeferEvents(typeof(MakeCoffeeEvent))]
        [OnEventDoAction(typeof(WaterLevelEvent), nameof(OnWaterLevel))]
        [OnEventDoAction(typeof(HopperLevelEvent), nameof(OnHopperLevel))]
        [OnEventDoAction(typeof(DoorOpenEvent), nameof(OnDoorOpen))]
        [OnEventDoAction(typeof(PortaFilterCoffeeLevelEvent), nameof(OnPortaFilterCoffeeLevel))]
        private class CheckSensors : State { }

        private void OnCheckSensors()
        {
            this.Log.WriteLine("checking initial state of sensors...");

            // Make sure grinder, shot maker and water heater are off.
            // Notice how easy it is to queue up a whole bunch of async work!
            this.SendEvent(this.CoffeeGrinder, new GrinderButtonEvent(false));
            this.SendEvent(this.WaterTank, new PumpWaterEvent(false));
            this.SendEvent(this.WaterTank, new WaterHeaterButtonEvent(false));

            // Need to check water and hopper levels and if the porta filter has
            // coffee in it we need to dump those grinds.
            this.SendEvent(this.WaterTank, new ReadWaterLevelEvent());
            this.SendEvent(this.CoffeeGrinder, new ReadHopperLevelEvent());
            this.SendEvent(this.DoorSensor, new ReadDoorOpenEvent());
            this.SendEvent(this.CoffeeGrinder, new ReadPortaFilterCoffeeLevelEvent());
        }

        private void OnWaterLevel(Event e)
        {
            var evt = e as WaterLevelEvent;
            this.WaterLevel = evt.WaterLevel;
            this.Log.WriteLine("Water level is {0} %", (int)this.WaterLevel.Value);
            if ((int)this.WaterLevel.Value <= 0)
            {
                this.Log.WriteLine("Coffee machine is out of water");
                this.RaiseGotoStateEvent<RefillRequired>();
                return;
            }

            this.CheckInitialState();
        }

        private void OnHopperLevel(Event e)
        {
            var evt = e as HopperLevelEvent;
            this.HopperLevel = evt.HopperLevel;
            this.Log.WriteLine("Hopper level is {0} %", (int)this.HopperLevel.Value);
            if ((int)this.HopperLevel.Value == 0)
            {
                this.Log.WriteError("Coffee machine is out of coffee beans");
                this.RaiseGotoStateEvent<RefillRequired>();
                return;
            }

            this.CheckInitialState();
        }

        private void OnDoorOpen(Event e)
        {
            var evt = e as DoorOpenEvent;
            this.DoorOpen = evt.Open;
            if (this.DoorOpen.Value != false)
            {
                this.Log.WriteError("Cannot safely operate coffee machine with the door open!");
                this.RaiseGotoStateEvent<Error>();
                return;
            }

            this.CheckInitialState();
        }

        private void OnPortaFilterCoffeeLevel(Event e)
        {
            var evt = e as PortaFilterCoffeeLevelEvent;
            this.PortaFilterCoffeeLevel = evt.CoffeeLevel;
            if (evt.CoffeeLevel > 0)
            {
                // Dump these grinds because they could be old, we have no idea how long
                // the coffee machine was off (no real time clock sensor).
                this.Log.WriteLine("Dumping old smelly grinds!");
                this.SendEvent(this.CoffeeGrinder, new DumpGrindsButtonEvent(true));
            }

            this.CheckInitialState();
        }

        private void CheckInitialState()
        {
            if (this.WaterLevel.HasValue && this.HopperLevel.HasValue &&
                this.DoorOpen.HasValue && this.PortaFilterCoffeeLevel.HasValue)
            {
                this.RaiseGotoStateEvent<HeatingWater>();
            }
        }

        [OnEntry(nameof(OnStartHeating))]
        [DeferEvents(typeof(MakeCoffeeEvent))]
        [OnEventDoAction(typeof(WaterTemperatureEvent), nameof(MonitorWaterTemperature))]
        [OnEventDoAction(typeof(WaterHotEvent), nameof(OnWaterHot))]
        private class HeatingWater : State { }

        private void OnStartHeating()
        {
            // Start heater and keep monitoring the water temp till it reaches 100!
            this.Log.WriteLine("Warming the water to 100 degrees");
            this.Monitor<LivenessMonitor>(new LivenessMonitor.BusyEvent());
            this.SendEvent(this.WaterTank, new ReadWaterTemperatureEvent());
        }

        private void OnWaterHot()
        {
            this.Log.WriteLine("Coffee machine water temperature is now 100");
            if (this.Heating)
            {
                this.Heating = false;
                // Turn off the heater so we don't overheat it!
                this.Log.WriteLine("Turning off the water heater");
                this.SendEvent(this.WaterTank, new WaterHeaterButtonEvent(false));
            }

            this.RaiseGotoStateEvent<Ready>();
        }

        private void MonitorWaterTemperature(Event e)
        {
            var evt = e as WaterTemperatureEvent;
            this.WaterTemperature = evt.WaterTemperature;

            if (this.WaterTemperature.Value >= 100)
            {
                this.OnWaterHot();
            }
            else
            {
                if (!this.Heating)
                {
                    this.Heating = true;
                    // Turn on the heater and wait for WaterHotEvent.
                    this.Log.WriteLine("Turning on the water heater");
                    this.SendEvent(this.WaterTank, new WaterHeaterButtonEvent(true));
                }
            }

            this.Log.WriteLine("Coffee machine is warming up ({0} degrees)...", (int)this.WaterTemperature);
        }

        [OnEntry(nameof(OnReady))]
        [IgnoreEvents(typeof(WaterLevelEvent), typeof(WaterHotEvent), typeof(HopperLevelEvent))]
        [OnEventGotoState(typeof(MakeCoffeeEvent), typeof(MakingCoffee))]
        [OnEventDoAction(typeof(HopperEmptyEvent), nameof(OnHopperEmpty))]
        private class Ready : State { }

        private void OnReady()
        {
            this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
            this.Log.WriteLine("Coffee machine is ready to make coffee (green light is on)");
        }

        [OnEntry(nameof(OnMakeCoffee))]
        private class MakingCoffee : State { }

        private void OnMakeCoffee(Event e)
        {
            var evt = e as MakeCoffeeEvent;
            this.Monitor<LivenessMonitor>(new LivenessMonitor.BusyEvent());
            this.Log.WriteLine($"Coffee requested, shots={evt.Shots}");
            this.ShotsRequested = evt.Shots;

            // First we assume user placed a new cup in the machine, and so the shot count is zero.
            this.PreviousShotCount = 0;

            // Grind beans until porta filter is full. Turn on shot button for desired time dump the
            // grinds, while checking for error conditions, e.g. out of water or coffee beans.
            this.RaiseGotoStateEvent<GrindingBeans>();
        }

        [OnEntry(nameof(OnGrindingBeans))]
        [OnEventDoAction(typeof(PortaFilterCoffeeLevelEvent), nameof(MonitorPortaFilter))]
        [OnEventDoAction(typeof(HopperLevelEvent), nameof(MonitorHopperLevel))]
        [OnEventDoAction(typeof(HopperEmptyEvent), nameof(OnHopperEmpty))]
        [IgnoreEvents(typeof(WaterHotEvent))]
        private class GrindingBeans : State { }

        private void OnGrindingBeans()
        {
            // Grind beans until porta filter is full.
            this.Log.WriteLine("Grinding beans...");
            // Turn on the grinder!
            this.SendEvent(this.CoffeeGrinder, new GrinderButtonEvent(true));
            // And keep monitoring the porta filter till it is full, and the bean level in case we get empty.
            this.SendEvent(this.CoffeeGrinder, new ReadHopperLevelEvent());
        }

        private void MonitorPortaFilter(Event e)
        {
            var evt = e as PortaFilterCoffeeLevelEvent;
            if (evt.CoffeeLevel >= 100)
            {
                this.Log.WriteLine("PortaFilter is full");
                this.SendEvent(this.CoffeeGrinder, new GrinderButtonEvent(false));
                this.RaiseGotoStateEvent<MakingShots>();
            }
            else
            {
                if (evt.CoffeeLevel != this.PreviousCoffeeLevel)
                {
                    this.PreviousCoffeeLevel = evt.CoffeeLevel;
                    this.Log.WriteLine("PortaFilter is {0} % full", evt.CoffeeLevel);
                }
            }
        }

        private void MonitorHopperLevel(Event e)
        {
            var evt = e as HopperLevelEvent;
            if (evt.HopperLevel == 0)
            {
                this.OnHopperEmpty();
            }
            else
            {
                this.SendEvent(this.CoffeeGrinder, new ReadHopperLevelEvent());
            }
        }

        private void OnHopperEmpty()
        {
            this.Log.WriteError("hopper is empty!");
            this.SendEvent(this.CoffeeGrinder, new GrinderButtonEvent(false));
            this.RaiseGotoStateEvent<RefillRequired>();
        }

        [OnEntry(nameof(OnMakingShots))]
        [OnEventDoAction(typeof(WaterLevelEvent), nameof(OnMonitorWaterLevel))]
        [OnEventDoAction(typeof(ShotCompleteEvent), nameof(OnShotComplete))]
        [OnEventDoAction(typeof(WaterEmptyEvent), nameof(OnWaterEmpty))]
        [IgnoreEvents(typeof(WaterHotEvent), typeof(HopperLevelEvent), typeof(HopperEmptyEvent))]
        private class MakingShots : State { }

        private void OnMakingShots()
        {
            // Pour the shots.
            this.Log.WriteLine("Making shots...");
            // Turn on the grinder!
            this.SendEvent(this.WaterTank, new PumpWaterEvent(true));
            // And keep monitoring the water is empty while we wait for ShotCompleteEvent.
            this.SendEvent(this.WaterTank, new ReadWaterLevelEvent());
        }

        private void OnShotComplete()
        {
            this.PreviousShotCount++;
            if (this.PreviousShotCount >= this.ShotsRequested)
            {
                this.Log.WriteLine("{0} shots completed and {1} shots requested!", this.PreviousShotCount, this.ShotsRequested);
                if (this.PreviousShotCount > this.ShotsRequested)
                {
                    this.Log.WriteError("Made the wrong number of shots!");
                    this.Assert(false, "Made the wrong number of shots");
                }

                this.RaiseGotoStateEvent<Cleanup>();
            }
            else
            {
                this.Log.WriteLine("Shot count is {0}", this.PreviousShotCount);

                // request another shot!
                this.SendEvent(this.WaterTank, new PumpWaterEvent(true));
            }
        }

        private void OnWaterEmpty()
        {
            this.Log.WriteError("Water is empty!");
            // Turn off the water pump.
            this.SendEvent(this.WaterTank, new PumpWaterEvent(false));
            this.RaiseGotoStateEvent<RefillRequired>();
        }

        private void OnMonitorWaterLevel(Event e)
        {
            var evt = e as WaterLevelEvent;
            if (evt.WaterLevel <= 0)
            {
                this.OnWaterEmpty();
            }
        }

        [OnEntry(nameof(OnCleanup))]
        [IgnoreEvents(typeof(WaterLevelEvent))]
        private class Cleanup : State { }

        private void OnCleanup()
        {
            // Dump the grinds.
            this.Log.WriteLine("Dumping the grinds!");
            this.SendEvent(this.CoffeeGrinder, new DumpGrindsButtonEvent(true));
            if (this.Client != null)
            {
                this.SendEvent(this.Client, new CoffeeCompletedEvent());
            }

            this.RaiseGotoStateEvent<Ready>();
        }

        [OnEntry(nameof(OnRefillRequired))]
        [IgnoreEvents(typeof(MakeCoffeeEvent), typeof(WaterLevelEvent), typeof(HopperLevelEvent),
            typeof(DoorOpenEvent), typeof(PortaFilterCoffeeLevelEvent))]
        private class RefillRequired : State { }

        private void OnRefillRequired()
        {
            if (this.Client != null)
            {
                this.SendEvent(this.Client, new CoffeeCompletedEvent() { Error = true });
            }

            this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
            this.Log.WriteLine("Coffee machine needs manual refilling of water and/or coffee beans!");
        }

        [OnEntry(nameof(OnError))]
        [IgnoreEvents(typeof(MakeCoffeeEvent), typeof(WaterLevelEvent), typeof(PortaFilterCoffeeLevelEvent),
            typeof(HopperLevelEvent))]
        private class Error : State { }

        private void OnError()
        {
            if (this.Client != null)
            {
                this.SendEvent(this.Client, new CoffeeCompletedEvent() { Error = true });
            }

            this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
            this.Log.WriteError("Coffee machine needs fixing!");
        }

        private void OnTerminate()
        {
            this.Log.WriteLine("Coffee Machine Terminating...");
            // Better turn everything off then!
            this.SendEvent(this.CoffeeGrinder, new GrinderButtonEvent(false));
            this.SendEvent(this.WaterTank, new PumpWaterEvent(false));
            this.SendEvent(this.WaterTank, new WaterHeaterButtonEvent(false));
            this.RaiseHaltEvent();
        }

        protected override Task OnHaltAsync(Event e)
        {
            this.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
            this.Log.WriteWarning("#################################################################");
            this.Log.WriteWarning("# Coffee Machine Halted                                         #");
            this.Log.WriteWarning("#################################################################");
            this.Log.WriteLine(string.Empty);
            if (this.Client != null)
            {
                this.SendEvent(this.Client, new HaltedEvent());
            }

            return base.OnHaltAsync(e);
        }

        protected override Task OnEventUnhandledAsync(Event e, string state)
        {
            this.Log.WriteError("### Unhandled event {0} in state {1}", e.GetType().FullName, state);
            return base.OnEventUnhandledAsync(e, state);
        }
    }
}
