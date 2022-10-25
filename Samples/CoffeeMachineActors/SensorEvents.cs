// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Samples.CoffeeMachineActors
{
    // This file contains the events you can use to talk read/write sensor values
    // Think of this as the "async interface" to the sensors, where the client could
    // be talking to the real machine or just a mock implementation of the machine.

    /// <summary>
    /// Internal mock sensor flag, essentially tells the sensor if we are
    /// running in production mode (RunSlowly=true) or test mode (RunSlowly=false).
    /// </summary>
    internal class ConfigEvent : Event
    {
        internal bool RunSlowly;

        internal ConfigEvent(bool runSlowly)
        {
            this.RunSlowly = runSlowly;
        }
    }

    /// <summary>
    /// Pass this caller ActorId to each sensor so it knows how to call you back.
    /// </summary>
    internal class RegisterClientEvent : Event
    {
        internal ActorId Caller;

        internal RegisterClientEvent(ActorId caller) { this.Caller = caller; }
    }

    /// <summary>
    /// The result of ReadWaterLevelEvent, caller cannot set the water level.
    /// </summary>
    internal class WaterLevelEvent : Event
    {
        // Starts at 100% full and drops when shot button is on.
        internal double WaterLevel;

        internal WaterLevelEvent(double value) { this.WaterLevel = value; }
    }

    /// <summary>
    /// Read the current water level, returns a WaterLevelEvent to the caller.
    /// </summary>
    internal class ReadWaterLevelEvent : Event { }

    /// <summary>
    /// Result from ReadHopperLevelEvent, caller cannot set the hopper level.
    /// </summary>
    internal class HopperLevelEvent : Event
    {
        // Starts at 100% full of beans, and drops when grinder is on.
        internal double HopperLevel;

        internal HopperLevelEvent(double value) { this.HopperLevel = value; }
    }

    /// <summary>
    /// Read the current coffee level in the hopper. Returns a HopperLevelEvent to the caller.
    /// </summary>
    internal class ReadHopperLevelEvent : Event { }

    /// <summary>
    /// Event is returned from ReadWaterTemperatureEvent, cannot set this value.
    /// </summary>
    internal class WaterTemperatureEvent : Event
    {
        // Starts at room temp, heats up to 100 when water heater is on.
        internal double WaterTemperature;

        internal WaterTemperatureEvent(double value) { this.WaterTemperature = value; }
    }

    /// <summary>
    /// Read the current water temperature. Returns a WaterTemperatureEvent to the caller.
    /// </summary>
    internal class ReadWaterTemperatureEvent : Event { }

    /// <summary>
    /// Event is returned from ReadPortaFilterCoffeeLevelEvent, cannot set this value.
    /// </summary>
    internal class PortaFilterCoffeeLevelEvent : Event
    {
        // Starts out empty=0, it gets filled to 100% with ground coffee while grinder is on.
        internal double CoffeeLevel;

        internal PortaFilterCoffeeLevelEvent(double value) { this.CoffeeLevel = value; }
    }

    /// <summary>
    /// Read the current coffee level in the porta filter. Returns a PortaFilterCoffeeLevelEvent to the caller.
    /// </summary>
    internal class ReadPortaFilterCoffeeLevelEvent : Event { }

    /// <summary>
    /// Returned from ReadDoorOpenEvent, cannot set this value.
    /// </summary>
    internal class DoorOpenEvent : Event
    {
        // True if open, a safety check to make sure machine is buttoned up properly before use.
        internal bool Open;

        internal DoorOpenEvent(bool value) { this.Open = value; }
    }

    internal class ReadDoorOpenEvent : Event { }

    /// <summary>
    /// Set the water heater power button state.
    /// </summary>
    internal class WaterHeaterButtonEvent : Event
    {
        // True means the power is on.
        internal bool PowerOn;

        internal WaterHeaterButtonEvent(bool value) { this.PowerOn = value; }
    }

    /// <summary>
    /// Turn on/off the coffee grinder.
    /// </summary>
    internal class GrinderButtonEvent : Event
    {
        // True means the power is on.
        internal bool PowerOn;

        internal GrinderButtonEvent(bool value) { this.PowerOn = value; }
    }

    internal class HopperEmptyEvent : Event
    {
    }

    internal class WaterEmptyEvent : Event
    {
    }

    internal class WaterHotEvent : Event { }

    internal class ShotCompleteEvent : Event { }

    internal class PumpWaterEvent : Event
    {
        // True means the power is on, shot button produces 1 shot of espresso and turns off automatically,
        // raising a ShowCompleteEvent press it multiple times to get multiple shots.
        internal bool PowerOn;

        internal PumpWaterEvent(bool value) { this.PowerOn = value; }
    }

    internal class DumpGrindsButtonEvent : Event
    {
        // True means the power is on, empties the PortaFilter and turns off automatically.
        internal bool PowerOn;

        internal DumpGrindsButtonEvent(bool value) { this.PowerOn = value; }
    }
}
