// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Samples.Common;
using Microsoft.Coyote.Specifications;

namespace Microsoft.Coyote.Samples.CoffeeMachineActors
{
    /// <summary>
    /// This safety monitor ensure nothing bad happens while a door is open on the coffee machine.
    /// </summary>
    internal class DoorSafetyMonitor : Monitor
    {
        internal class DoorOpenEvent : Event
        {
            internal bool Open;

            internal DoorOpenEvent(bool value) { this.Open = value; }
        }

        internal class BusyEvent : Event { }

        [Start]
        [OnEventGotoState(typeof(DoorOpenEvent), typeof(Error))]
        [IgnoreEvents(typeof(BusyEvent))]
        private class Init : State { }

        [OnEventDoAction(typeof(BusyEvent), nameof(OnBusy))]
        private class Error : State { }

        private void OnBusy()
        {
            this.Assert(false, "Should not be doing anything while door is open");
        }
    }

    /// <summary>
    /// This Actor models is a sensor that detects whether any doors on the coffee machine are open.
    /// For safe operation, all doors must be closed before machine will do anything.
    /// </summary>
    [OnEventDoAction(typeof(ReadDoorOpenEvent), nameof(OnReadDoorOpen))]
    [OnEventDoAction(typeof(RegisterClientEvent), nameof(OnRegisterClient))]
    internal class MockDoorSensor : Actor
    {
        private bool DoorOpen;
        private ActorId Client;

        protected override Task OnInitializeAsync(Event initialEvent)
        {
            // Since this is a mock, we randomly it to false with one chance out of 5 just
            // to test this error condition, if the door is open, the machine should not
            // agree to do anything for you.
            this.DoorOpen = this.RandomInteger(5) is 0;
            if (this.DoorOpen)
            {
                this.Monitor<DoorSafetyMonitor>(new DoorSafetyMonitor.DoorOpenEvent(this.DoorOpen));
            }

            return base.OnInitializeAsync(initialEvent);
        }

        private void OnRegisterClient(Event e)
        {
            this.Client = ((RegisterClientEvent)e).Caller;
        }

        private void OnReadDoorOpen()
        {
            if (this.Client != null)
            {
                this.SendEvent(this.Client, new DoorOpenEvent(this.DoorOpen));
            }
        }
    }

    /// <summary>
    /// This Actor models is a mock implementation of a the water tank inside the coffee machine.
    /// It can heat the water, and run a water pump which runs pressurized water through the
    /// porta filter when making an espresso shot.
    /// </summary>
    [OnEventDoAction(typeof(RegisterClientEvent), nameof(OnRegisterClient))]
    [OnEventDoAction(typeof(ReadWaterLevelEvent), nameof(OnReadWaterLevel))]
    [OnEventDoAction(typeof(ReadWaterTemperatureEvent), nameof(OnReadWaterTemperature))]
    [OnEventDoAction(typeof(WaterHeaterButtonEvent), nameof(OnWaterHeaterButton))]
    [OnEventDoAction(typeof(HeaterTimerEvent), nameof(MonitorWaterTemperature))]
    [OnEventDoAction(typeof(PumpWaterEvent), nameof(OnPumpWater))]
    [OnEventDoAction(typeof(WaterPumpTimerEvent), nameof(MonitorWaterPump))]
    internal class MockWaterTank : Actor
    {
        private ActorId Client;
        private bool RunSlowly;
        private double WaterLevel;
        private double WaterTemperature;
        private bool WaterHeaterButton;
        private TimerInfo WaterHeaterTimer;
        private bool WaterPump;
        private TimerInfo WaterPumpTimer;
        private readonly LogWriter Log = LogWriter.Instance;

        internal class HeaterTimerEvent : TimerElapsedEvent
        {
        }

        internal class WaterPumpTimerEvent : TimerElapsedEvent
        {
        }

        public MockWaterTank()
        {
            // Assume heater is off by default.
            this.WaterHeaterButton = false;
            this.WaterPump = false;
        }

        protected override Task OnInitializeAsync(Event initialEvent)
        {
            if (initialEvent is ConfigEvent ce)
            {
                this.RunSlowly = ce.RunSlowly;
            }

            // Since this is a mock, we randomly initialize the water temperature to
            // some sort of room temperature between 20 and 50 degrees celsius.
            this.WaterTemperature = this.RandomInteger(30) + 20;
            // Since this is a mock, we randomly initialize the water level to some value
            // between 0 and 100% full.
            this.WaterLevel = this.RandomInteger(100);

            return base.OnInitializeAsync(initialEvent);
        }

        private void OnRegisterClient(Event e)
        {
            this.Client = ((RegisterClientEvent)e).Caller;
        }

        private void OnReadWaterLevel()
        {
            if (this.Client != null)
            {
                this.SendEvent(this.Client, new WaterLevelEvent(this.WaterLevel));
            }
        }

        private void OnReadWaterTemperature()
        {
            if (this.Client != null)
            {
                this.SendEvent(this.Client, new WaterTemperatureEvent(this.WaterTemperature));
            }
        }

        private void OnWaterHeaterButton(Event e)
        {
            var evt = e as WaterHeaterButtonEvent;
            this.WaterHeaterButton = evt.PowerOn;

            // Should never turn on the heater when there is no water to heat.
            if (this.WaterHeaterButton && this.WaterLevel <= 0)
            {
                this.Assert(false, "Please do not turn on heater if there is no water");
            }

            if (this.WaterHeaterButton)
            {
                this.Monitor<DoorSafetyMonitor>(new DoorSafetyMonitor.BusyEvent());
                this.WaterHeaterTimer = this.StartPeriodicTimer(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1), new HeaterTimerEvent());
            }
            else if (this.WaterHeaterTimer != null)
            {
                this.StopTimer(this.WaterHeaterTimer);
                this.WaterHeaterTimer = null;
            }
        }

        private void MonitorWaterTemperature()
        {
            double temp = this.WaterTemperature;
            if (this.WaterHeaterButton)
            {
                // Note: when running in production mode we run forever, and it is fun to
                // watch the water heat up and cool down. But in test mode this creates too
                // many async events to explore which makes the test slow. So in test mode
                // we short circuit this process and jump straight to the boundary conditions.
                if (!this.RunSlowly && temp < 99)
                {
                    temp = 99;
                }

                // Every time interval the temperature increases by 10 degrees up to 100 degrees.
                if (temp < 100)
                {
                    temp = (int)temp + 10;
                    this.WaterTemperature = temp;
                    if (this.Client != null)
                    {
                        this.SendEvent(this.Client, new WaterTemperatureEvent(this.WaterTemperature));
                    }
                }
                else
                {
                    if (this.Client != null)
                    {
                        this.SendEvent(this.Client, new WaterHotEvent());
                    }
                }
            }
            else
            {
                // Then it is cooling down to room temperature, more slowly.
                if (temp > 70)
                {
                    temp -= 0.1;
                    this.WaterTemperature = temp;
                }
            }
        }

        private void OnPumpWater(Event e)
        {
            var evt = e as PumpWaterEvent;
            this.WaterPump = evt.PowerOn;

            if (this.WaterPump)
            {
                this.Monitor<DoorSafetyMonitor>(new DoorSafetyMonitor.BusyEvent());
                // Should never turn on the make shots button when there is no water.
                if (this.WaterLevel <= 0)
                {
                    this.Assert(false, "Please do not turn on shot maker if there is no water");
                }

                // Time the shot then send shot complete event.
                this.WaterPumpTimer = this.StartPeriodicTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), new WaterPumpTimerEvent());
            }
            else if (this.WaterPumpTimer != null)
            {
                this.StopTimer(this.WaterPumpTimer);
                this.WaterPumpTimer = null;
            }
        }

        private void MonitorWaterPump()
        {
            // One second of running water completes the shot.
            this.WaterLevel -= 1;
            if (this.WaterLevel > 0)
            {
                this.SendEvent(this.Client, new ShotCompleteEvent());
            }
            else
            {
                this.SendEvent(this.Client, new WaterEmptyEvent());
            }

            // Automatically stop the water when shot is completed.
            if (this.WaterPumpTimer != null)
            {
                this.StopTimer(this.WaterPumpTimer);
                this.WaterPumpTimer = null;
            }

            // Turn off the water.
            this.WaterPump = false;
        }

        protected override Task OnEventUnhandledAsync(Event e, string state)
        {
            this.Log.WriteLine("### Unhandled event {0} in state {1}", e.GetType().FullName, state);
            return base.OnEventUnhandledAsync(e, state);
        }
    }

    /// <summary>
    /// This Actor models is a mock implementation of the coffee grinder in the coffee machine.
    /// This is connected to the hopper containing beans, and the porta filter that stores the ground
    /// coffee before pouring a shot.
    /// </summary>
    [OnEventDoAction(typeof(RegisterClientEvent), nameof(OnRegisterClient))]
    [OnEventDoAction(typeof(ReadPortaFilterCoffeeLevelEvent), nameof(OnReadPortaFilterCoffeeLevel))]
    [OnEventDoAction(typeof(ReadHopperLevelEvent), nameof(OnReadHopperLevel))]
    [OnEventDoAction(typeof(GrinderButtonEvent), nameof(OnGrinderButton))]
    [OnEventDoAction(typeof(GrinderTimerEvent), nameof(MonitorGrinder))]
    [OnEventDoAction(typeof(DumpGrindsButtonEvent), nameof(OnDumpGrindsButton))]
    internal class MockCoffeeGrinder : Actor
    {
        private ActorId Client;
        private bool RunSlowly;
        private double PortaFilterCoffeeLevel;
        private double HopperLevel;
        private bool GrinderButton;
        private TimerInfo GrinderTimer;
        private readonly LogWriter Log = LogWriter.Instance;

        internal class GrinderTimerEvent : TimerElapsedEvent
        {
        }

        protected override Task OnInitializeAsync(Event initialEvent)
        {
            if (initialEvent is ConfigEvent ce)
            {
                this.RunSlowly = ce.RunSlowly;
            }

            // Since this is a mock, we randomly initialize the hopper level to some value
            // between 0 and 100% full.
            this.HopperLevel = this.RandomInteger(100);

            return base.OnInitializeAsync(initialEvent);
        }

        private void OnRegisterClient(Event e)
        {
            this.Client = ((RegisterClientEvent)e).Caller;
        }

        private void OnReadPortaFilterCoffeeLevel()
        {
            if (this.Client != null)
            {
                this.SendEvent(this.Client, new PortaFilterCoffeeLevelEvent(this.PortaFilterCoffeeLevel));
            }
        }

        private void OnGrinderButton(Event e)
        {
            var evt = e as GrinderButtonEvent;
            this.GrinderButton = evt.PowerOn;
            this.OnGrinderButtonChanged();
        }

        private void OnReadHopperLevel()
        {
            if (this.Client != null)
            {
                this.SendEvent(this.Client, new HopperLevelEvent(this.HopperLevel));
            }
        }

        private void OnGrinderButtonChanged()
        {
            if (this.GrinderButton)
            {
                this.Monitor<DoorSafetyMonitor>(new DoorSafetyMonitor.BusyEvent());
                // Should never turn on the grinder when there is no coffee to grind.
                if (this.HopperLevel <= 0)
                {
                    this.Assert(false, "Please do not turn on grinder if there are no beans in the hopper");
                }

                // Start monitoring the coffee level.
                this.GrinderTimer = this.StartPeriodicTimer(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1), new GrinderTimerEvent());
            }
            else if (this.GrinderTimer != null)
            {
                this.StopTimer(this.GrinderTimer);
                this.GrinderTimer = null;
            }
        }

        private void MonitorGrinder()
        {
            // In each time interval the porta filter fills 10%. When it's full the grinder turns
            // off automatically, unless the hopper is empty in which case grinding does nothing!
            double hopperLevel = this.HopperLevel;
            if (hopperLevel > 0)
            {
                double level = this.PortaFilterCoffeeLevel;

                // Note: when running in production mode we run in real time, and it is fun
                // to watch the porta filter filling up. But in test mode this creates too
                // many async events to explore which makes the test slow. So in test mode
                // we short circuit this process and jump straight to the boundary conditions.
                if (!this.RunSlowly && level < 99)
                {
                    hopperLevel -= 98 - (int)level;
                    level = 99;
                }

                if (level < 100)
                {
                    level += 10;
                    this.PortaFilterCoffeeLevel = level;
                    if (this.Client != null)
                    {
                        this.SendEvent(this.Client, new PortaFilterCoffeeLevelEvent(this.PortaFilterCoffeeLevel));
                    }

                    if (level == 100)
                    {
                        // Turning off the grinder is automatic.
                        this.GrinderButton = false;
                        this.OnGrinderButtonChanged();
                    }
                }

                // And the hopper level drops by 0.1 percent.
                hopperLevel -= 1;

                this.HopperLevel = hopperLevel;
            }

            if (this.HopperLevel <= 0)
            {
                hopperLevel = 0;
                if (this.Client != null)
                {
                    this.SendEvent(this.Client, new HopperEmptyEvent());
                }

                if (this.GrinderTimer != null)
                {
                    this.StopTimer(this.GrinderTimer);
                    this.GrinderTimer = null;
                }
            }
        }

        protected override Task OnEventUnhandledAsync(Event e, string state)
        {
            this.Log.WriteLine("### Unhandled event {0} in state {1}", e.GetType().FullName, state);
            return base.OnEventUnhandledAsync(e, state);
        }

        private void OnDumpGrindsButton(Event e)
        {
            var evt = e as DumpGrindsButtonEvent;
            if (evt.PowerOn)
            {
                this.Monitor<DoorSafetyMonitor>(new DoorSafetyMonitor.BusyEvent());
                // This is a toggle button, in no time grinds are dumped (just for simplicity).
                this.PortaFilterCoffeeLevel = 0;
            }
        }
    }
}
