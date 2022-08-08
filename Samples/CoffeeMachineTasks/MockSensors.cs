// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Random;
using Microsoft.Coyote.Samples.Common;
using Microsoft.Coyote.Specifications;

namespace Microsoft.Coyote.Samples.CoffeeMachineTasks
{
    /// <summary>
    /// This interface represents the state of the sensors in the coffee machine. This is
    /// designed as an async interface to show how one might design a distributed system where
    /// messaging could be cross-process, or cross-device. Perhaps there is a board in the
    /// coffee machine that contains the sensors, and another board somewhere else in the machine
    /// that runs the brain, so each sensor read/write is an async operation. In the case of
    /// a cloud service you would also have async interface for messaging. This async nature is
    /// where interesting bugs can show up and is where Coyote testing can be extremely useful.
    /// </summary>
    internal interface ISensors
    {
        Task<bool> GetPowerSwitchAsync();

        Task SetPowerSwitchAsync(bool value);

        Task<double> GetWaterLevelAsync();

        Task<double> GetHopperLevelAsync();

        Task<double> GetWaterTemperatureAsync();

        Task<double> GetPortaFilterCoffeeLevelAsync();

        Task<bool> GetReadDoorOpenAsync();

        Task SetWaterHeaterButtonAsync(bool value);

        Task SetGrinderButtonAsync(bool value);

        Task SetShotButtonAsync(bool value);

        Task SetDumpGrindsButtonAsync(bool value);

        Task TerminateAsync();

        /// <summary>
        /// An async event can be raised any time the water temperature changes.
        /// </summary>
        event EventHandler<double> WaterTemperatureChanged;

        /// <summary>
        /// An async event can be raised any time the water temperature reaches the right level for making coffee.
        /// </summary>
        event EventHandler<bool> WaterHot;

        /// <summary>
        /// An async event can be raised any time the coffee level changes in the porta filter.
        /// </summary>
        event EventHandler<double> PortaFilterCoffeeLevelChanged;

        /// <summary>
        /// Raised if we run out of coffee beans.
        /// </summary>
        event EventHandler<bool> HopperEmpty;

        /// <summary>
        /// Running a shot takes time, this event is raised when the shot is complete.
        /// </summary>
        event EventHandler<bool> ShotComplete;

        /// <summary>
        /// Raised if we run out of water.
        /// </summary>
        event EventHandler<bool> WaterEmpty;
    }

    /// <summary>
    /// This is a mock implementation of the ISensor interface.
    /// </summary>
    internal class MockSensors : ISensors
    {
        private readonly AsyncLock Lock;
        private bool PowerOn;
        private bool WaterHeaterButton;
        private double WaterLevel;
        private double HopperLevel;
        private double WaterTemperature;
        private bool GrinderButton;
        private double PortaFilterCoffeeLevel;
        private bool ShotButton;
        private readonly bool DoorOpen;
        private readonly Generator RandomGenerator;

        private ControlledTimer WaterHeaterTimer;
        private ControlledTimer CoffeeLevelTimer;
        private ControlledTimer ShotTimer;
        public bool RunSlowly;
        private readonly LogWriter Log = LogWriter.Instance;

        public event EventHandler<double> WaterTemperatureChanged;

        public event EventHandler<bool> WaterHot;

        public event EventHandler<double> PortaFilterCoffeeLevelChanged;

        public event EventHandler<bool> HopperEmpty;

        public event EventHandler<bool> ShotComplete;

        public event EventHandler<bool> WaterEmpty;

        public MockSensors(bool runSlowly)
        {
            this.Lock = new AsyncLock();
            this.RunSlowly = runSlowly;
            this.RandomGenerator = Generator.Create();

            // The use of randomness here makes this mock a more interesting test as it will
            // make sure the coffee machine handles these values correctly.
            this.WaterLevel = this.RandomGenerator.NextInteger(100);
            this.HopperLevel = this.RandomGenerator.NextInteger(100);
            this.WaterHeaterButton = false;
            this.WaterTemperature = this.RandomGenerator.NextInteger(50) + 30;
            this.GrinderButton = false;
            this.PortaFilterCoffeeLevel = 0;
            this.ShotButton = false;
            this.DoorOpen = this.RandomGenerator.NextInteger(5) is 0;
            this.WaterHeaterTimer = new ControlledTimer("WaterHeaterTimer", TimeSpan.FromSeconds(0.1), this.MonitorWaterTemperature);
        }

        public Task TerminateAsync()
        {
            StopTimer(this.WaterHeaterTimer);
            StopTimer(this.CoffeeLevelTimer);
            StopTimer(this.ShotTimer);
            return Task.CompletedTask;
        }

        public async Task<bool> GetPowerSwitchAsync()
        {
            // to model real async behavior we insert a delay here.
            await Task.Delay(1);
            return this.PowerOn;
        }

        public async Task<double> GetWaterLevelAsync()
        {
            await Task.Delay(1);
            return this.WaterLevel;
        }

        public async Task<double> GetHopperLevelAsync()
        {
            await Task.Delay(1);
            return this.HopperLevel;
        }

        public async Task<double> GetWaterTemperatureAsync()
        {
            await Task.Delay(1);
            return this.WaterTemperature;
        }

        public async Task<double> GetPortaFilterCoffeeLevelAsync()
        {
            await Task.Delay(1);
            return this.PortaFilterCoffeeLevel;
        }

        public async Task<bool> GetReadDoorOpenAsync()
        {
            await Task.Delay(1);
            return this.DoorOpen;
        }

        public async Task SetPowerSwitchAsync(bool value)
        {
            await Task.Delay(1);

            // NOTE: you should not use C# locks that interact with Tasks (like Task.Run) because
            // it can result in deadlocks, instead use the Coyote AsyncLock as follows.
            using (await this.Lock.AcquireAsync())
            {
                this.PowerOn = value;
                if (!this.PowerOn)
                {
                    // Master power override then also turns everything else off for safety!
                    this.WaterHeaterButton = false;
                    this.GrinderButton = false;
                    this.ShotButton = false;

                    StopTimer(this.CoffeeLevelTimer);
                    this.CoffeeLevelTimer = null;

                    StopTimer(this.ShotTimer);
                    this.ShotTimer = null;
                }
            }
        }

        public async Task SetWaterHeaterButtonAsync(bool value)
        {
            await Task.Delay(1);

            using (await this.Lock.AcquireAsync())
            {
                this.WaterHeaterButton = value;

                // Should never turn on the heater when there is no water to heat.
                if (this.WaterHeaterButton && this.WaterLevel <= 0)
                {
                    Specification.Assert(false, "Please do not turn on heater if there is no water");
                }
            }
        }

        public async Task SetGrinderButtonAsync(bool value)
        {
            await Task.Delay(1);
            await this.OnGrinderButtonChanged(value);
        }

        private async Task OnGrinderButtonChanged(bool value)
        {
            using (await this.Lock.AcquireAsync())
            {
                this.GrinderButton = value;
                if (this.GrinderButton)
                {
                    // Should never turn on the grinder when there is no coffee to grind.
                    if (this.HopperLevel <= 0)
                    {
                        Specification.Assert(false, "Please do not turn on grinder if there are no beans in the hopper");
                    }
                }

                if (value && this.CoffeeLevelTimer == null)
                {
                    // Start monitoring the coffee level.
                    this.CoffeeLevelTimer = new ControlledTimer("CoffeeLevelTimer", TimeSpan.FromSeconds(0.1), this.MonitorGrinder);
                }
                else if (!value && this.CoffeeLevelTimer != null)
                {
                    StopTimer(this.CoffeeLevelTimer);
                    this.CoffeeLevelTimer = null;
                }
            }
        }

        public async Task SetShotButtonAsync(bool value)
        {
            await Task.Delay(1);

            using (await this.Lock.AcquireAsync())
            {
                this.ShotButton = value;

                if (this.ShotButton)
                {
                    // Should never turn on the make shots button when there is no water.
                    if (this.WaterLevel <= 0)
                    {
                        Specification.Assert(false, "Please do not turn on shot maker if there is no water");
                    }
                }

                if (value && this.ShotTimer == null)
                {
                    // Start monitoring the coffee level.
                    this.ShotTimer = new ControlledTimer("ShotTimer", TimeSpan.FromSeconds(1), this.MonitorShot);
                }
                else if (!value && this.ShotTimer != null)
                {
                    StopTimer(this.ShotTimer);
                    this.ShotTimer = null;
                }
            }
        }

        public async Task SetDumpGrindsButtonAsync(bool value)
        {
            await Task.Delay(1);
            if (value)
            {
                // This is a toggle button, in no time grinds are dumped (just for simplicity).
                this.PortaFilterCoffeeLevel = 0;
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
                    this.WaterTemperatureChanged?.Invoke(this, this.WaterTemperature);
                }
                else
                {
                    this.WaterHot?.Invoke(this, true);
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

            // Start another callback.
            this.WaterHeaterTimer = new ControlledTimer("WaterHeaterTimer", TimeSpan.FromSeconds(0.1), this.MonitorWaterTemperature);
        }

        private void MonitorGrinder()
        {
            // Every time interval the porta filter fills 10%. When it's full the grinder turns off
            // automatically, unless the hopper is empty in which case grinding does nothing!

            Task.Run(async () =>
            {
                bool changed = false;
                bool notifyEmpty = false;
                bool turnOffGrinder = false;

                using (await this.Lock.AcquireAsync())
                {
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
                            this.Log.WriteLine("### HopperLevel: RunSlowly = {0}, level = {1}", this.RunSlowly, hopperLevel);
                            level = 99;
                        }

                        if (level < 100)
                        {
                            level += 10;
                            this.PortaFilterCoffeeLevel = level;
                            changed = true;
                            if (level >= 100)
                            {
                                turnOffGrinder = true;
                            }
                        }

                        // And the hopper level drops by 0.1 percent.
                        hopperLevel -= 1;

                        this.HopperLevel = hopperLevel;
                    }

                    if (this.HopperLevel <= 0)
                    {
                        hopperLevel = 0;
                        notifyEmpty = true;

                        StopTimer(this.CoffeeLevelTimer);
                        this.CoffeeLevelTimer = null;
                    }
                }

                if (turnOffGrinder)
                {
                    // Turning off the grinder is automatic.
                    await this.OnGrinderButtonChanged(false);
                }

                // Event callbacks should not be inside the lock otherwise we could get deadlocks.
                if (notifyEmpty && this.HopperEmpty != null)
                {
                    this.HopperEmpty(this, true);
                }

                if (changed && this.PortaFilterCoffeeLevelChanged != null)
                {
                    this.PortaFilterCoffeeLevelChanged(this, this.PortaFilterCoffeeLevel);
                }

                if (this.HopperLevel <= 0 && this.HopperEmpty != null)
                {
                    this.HopperEmpty(this, true);
                }

                // Start another callback.
                this.CoffeeLevelTimer = new ControlledTimer("WaterHeaterTimer", TimeSpan.FromSeconds(0.1), this.MonitorGrinder);
            });
        }

        private void MonitorShot()
        {
            Task.Run(async () =>
            {
                // One second of running water completes the shot.
                using (await this.Lock.AcquireAsync())
                {
                    this.WaterLevel -= 1;
                    // Turn off the water.
                    this.ShotButton = false;
                    this.ShotTimer = null;
                }

                // Event callbacks should not be inside the lock otherwise we could get deadlocks.
                if (this.WaterLevel > 0)
                {
                    this.ShotComplete?.Invoke(this, true);
                }
                else
                {
                    this.WaterEmpty?.Invoke(this, true);
                }
            });
        }

        private static void StopTimer(ControlledTimer timer)
        {
            if (timer != null)
            {
                timer.Stop();
            }
        }
    }
}
