// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Samples.Common;
using Microsoft.Coyote.Specifications;

namespace Microsoft.Coyote.Samples.CoffeeMachineTasks
{
    /// <summary>
    /// This async interface is provided by the CoffeeMachine.
    /// </summary>
    internal interface ICoffeeMachine
    {
        /// <summary>
        /// Initialize the coffee machine, checking the Sensors to make sure we're good to go.
        /// </summary>
        /// <param name="sensors">The persistent sensor state.</param>
        /// <returns>True if everything looks good, false if we cannot make coffee at this time.</returns>
        Task<bool> InitializeAsync(ISensors sensors);

        /// <summary>
        /// Make a coffee.  This is a long running async operation.
        /// </summary>
        /// <param name="shots">The number of espresso shots.</param>
        /// <returns>An async task that can be controlled by Coyote tester containing an optional string error message.</returns>
        Task<string> MakeCoffeeAsync(int shots);

        /// <summary>
        /// Reboot the coffee machine!
        /// </summary>
        /// <returns>An async task.</returns>
        Task TerminateAsync();
    }

    /// <summary>
    /// Implementation of the ICoffeeMachine interface.
    /// </summary>
    /// </summary>
    internal class CoffeeMachine : ICoffeeMachine
    {
        // TaskYieldInjector taskYieldInjector = new TaskYieldInjector();
        private readonly object SyncObject = new object();
        private bool Initialized;
        private ISensors Sensors;
        private bool Heating;
        private double? WaterLevel;
        private double? HopperLevel;
        private bool? DoorOpen;
        private double? PortaFilterCoffeeLevel;
        private double? WaterTemperature;
        private int ShotsRequested;
        private double PreviousShotCount;
        private bool RefillRequired;
        private string Error;
        private bool Halted;
        private TaskCompletionSource<bool> ShotCompleteSource;
        private readonly LogWriter Log = LogWriter.Instance;

        public async Task<bool> InitializeAsync(ISensors sensors)
        {
            await TaskYieldInjector.InjectYieldsAtMethodStart();
            this.Log.WriteLine("initializing...");

            lock (this.SyncObject)
            {
                this.Sensors = sensors;
                this.RegisterSensorEvents(false);
                this.RegisterSensorEvents(true);
            }

            await TaskYieldInjector.InjectYieldsAtMethodMiddle();
            await this.CheckSensors();

            this.Initialized = !this.RefillRequired && string.IsNullOrEmpty(this.Error);
            await TaskYieldInjector.InjectYieldsAtMethodEnd();
            return this.Initialized;
        }

        public async Task<string> MakeCoffeeAsync(int shots)
        {
            await TaskYieldInjector.InjectYieldsAtMethodStart();
            if (!this.Initialized)
            {
                throw new Exception("Please make sure InitializeAsync returns true.");
            }

            if (this.Halted)
            {
                return "Ignoring MakeCoffeeAsync on halted Coffee machine";
            }

            Specification.Monitor<LivenessMonitor>(new LivenessMonitor.BusyEvent());

            if (!this.RefillRequired && !this.Halted)
            {
                // Make sure water is hot enough.
                await this.StartHeatingWater();
            }

            this.Log.WriteLine($"Coffee requested, shots={shots}");
            this.ShotsRequested = shots;
            await TaskYieldInjector.InjectYieldsAtMethodMiddle();

            // Grind beans until porta filter is full. Turn on shot button for desired time dump the
            // grinds, while checking for error conditions, e.g. out of water or coffee beans.
            if (!this.RefillRequired && !this.Halted)
            {
                await this.GrindBeans();
            }

            if (!this.RefillRequired && !this.Halted)
            {
                await this.MakeShotsAsync();
            }

            await this.CleanupAsync();

            if (this.Halted)
            {
                return "<halted>";
            }

            await TaskYieldInjector.InjectYieldsAtMethodEnd();
            return this.Error;
        }

        public async Task CheckSensors()
        {
            await TaskYieldInjector.InjectYieldsAtMethodStart();
            this.Log.WriteLine("checking initial state of sensors...");

            // When this state machine starts it has to figure out the state of the sensors.
            if (!await this.Sensors.GetPowerSwitchAsync())
            {
                // Coffee machine was off, so this is the easy case, simply turn it on!
                await this.Sensors.SetPowerSwitchAsync(true);
            }

            // Make sure grinder, shot maker and water heater are off.
            await this.Sensors.SetGrinderButtonAsync(false);
            await this.Sensors.SetShotButtonAsync(false);
            await this.Sensors.SetWaterHeaterButtonAsync(false);
            await TaskYieldInjector.InjectYieldsAtMethodMiddle();

            // Need to check water and hopper levels and if the porta filter
            // has coffee in it we need to dump those grinds.
            await this.CheckWaterLevelAsync();
            await this.CheckHopperLevelAsync();
            await this.CheckPortaFilterCoffeeLevelAsync();
            await this.CheckDoorOpenAsync();
            await TaskYieldInjector.InjectYieldsAtMethodEnd();
        }

        private async Task CheckWaterLevelAsync()
        {
            await TaskYieldInjector.InjectYieldsAtMethodStart();
            this.WaterLevel = await this.Sensors.GetWaterLevelAsync();
            this.Log.WriteLine("Water level is {0} %", (int)this.WaterLevel.Value);
            await TaskYieldInjector.InjectYieldsAtMethodMiddle();
            if ((int)this.WaterLevel.Value <= 0)
            {
                this.OnRefillRequired("is out of water");
            }

            await TaskYieldInjector.InjectYieldsAtMethodEnd();
        }

        private async Task CheckHopperLevelAsync()
        {
            await TaskYieldInjector.InjectYieldsAtMethodStart();
            this.HopperLevel = await this.Sensors.GetHopperLevelAsync();
            this.Log.WriteLine("Hopper level is {0} %", (int)this.HopperLevel.Value);
            await TaskYieldInjector.InjectYieldsAtMethodMiddle();
            if ((int)this.HopperLevel.Value == 0)
            {
                this.OnRefillRequired("out of coffee beans");
            }

            await TaskYieldInjector.InjectYieldsAtMethodEnd();
        }

        private async Task CheckPortaFilterCoffeeLevelAsync()
        {
            await TaskYieldInjector.InjectYieldsAtMethodStart();
            this.PortaFilterCoffeeLevel = await this.Sensors.GetPortaFilterCoffeeLevelAsync();
            await TaskYieldInjector.InjectYieldsAtMethodMiddle();
            if (this.PortaFilterCoffeeLevel > 0)
            {
                // Dump these grinds because they could be old, we have no idea how long
                // the coffee machine was off (no real time clock sensor).
                this.Log.WriteLine("Dumping old smelly grinds!");
                await this.Sensors.SetDumpGrindsButtonAsync(true);
            }

            await TaskYieldInjector.InjectYieldsAtMethodEnd();
        }

        private async Task CheckDoorOpenAsync()
        {
            await TaskYieldInjector.InjectYieldsAtMethodStart();
            this.DoorOpen = await this.Sensors.GetReadDoorOpenAsync();
            await TaskYieldInjector.InjectYieldsAtMethodMiddle();
            if (this.DoorOpen.Value != false)
            {
                this.Log.WriteLine("Cannot safely operate coffee machine with the door open!");
                this.OnError();
            }

            await TaskYieldInjector.InjectYieldsAtMethodEnd();
        }

        private async Task StartHeatingWater()
        {
            await TaskYieldInjector.InjectYieldsAtMethodStart();
            if (!this.Halted)
            {
                // Start heater and keep monitoring the water temp till it reaches 100!
                this.Log.WriteLine("Warming the water to 100 degrees");
                Specification.Monitor<LivenessMonitor>(new LivenessMonitor.BusyEvent());
                await this.MonitorWaterTemperature();
            }
            else
            {
                this.Log.WriteLine("Ignoring StartHeatingWater on a Halted Coffee machine");
            }

            await TaskYieldInjector.InjectYieldsAtMethodMiddle();

            await TaskYieldInjector.InjectYieldsAtMethodEnd();
        }

        private async Task OnWaterHot()
        {
            await TaskYieldInjector.InjectYieldsAtMethodStart();
            this.Log.WriteLine("Coffee machine water temperature is now 100");
            if (this.Heating)
            {
                this.Heating = false;
                // Turn off the heater so we don't overheat it!
                await this.Sensors.SetWaterHeaterButtonAsync(false);
                this.Log.WriteLine("Turning off the water heater");
            }

            await TaskYieldInjector.InjectYieldsAtMethodMiddle();
            this.OnReady();
            await TaskYieldInjector.InjectYieldsAtMethodEnd();
        }

        private async Task MonitorWaterTemperature()
        {
            await TaskYieldInjector.InjectYieldsAtMethodStart();
            while (!this.IsBroken)
            {
                this.WaterTemperature = await this.Sensors.GetWaterTemperatureAsync();

                if (this.WaterTemperature.Value >= 100)
                {
                    await this.OnWaterHot();
                    break;
                }
                else
                {
                    if (!this.Heating)
                    {
                        this.Heating = true;
                        // Turn on the heater and wait for WaterHotEvent.
                        this.Log.WriteLine("Turning on the water heater");
                        await this.Sensors.SetWaterHeaterButtonAsync(true);
                    }
                }

                this.Log.WriteLine("Coffee machine is warming up ({0} degrees)...", this.WaterTemperature);

                await Task.Delay(TimeSpan.FromSeconds(0.1));
            }

            await TaskYieldInjector.InjectYieldsAtMethodMiddle();
            await TaskYieldInjector.InjectYieldsAtMethodEnd();
        }

        private void OnReady()
        {
            Specification.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
            this.Log.WriteLine("Coffee machine is ready to make coffee (green light is on)");
        }

        private async Task GrindBeans()
        {
            await TaskYieldInjector.InjectYieldsAtMethodStart();
            // Grind beans until porta filter is full.
            this.Log.WriteLine("Grinding beans...");

            // Turn on the grinder!
            await this.Sensors.SetGrinderButtonAsync(true);
            await TaskYieldInjector.InjectYieldsAtMethodMiddle();

            // We now receive a stream of PortaFilterCoffeeLevelChanged events so we keep monitoring
            // the porta filter till it is full, and the bean level in case we get empty.
            await this.MonitorPortaFilter();
            await TaskYieldInjector.InjectYieldsAtMethodEnd();
        }

        private async Task MonitorPortaFilter()
        {
            await TaskYieldInjector.InjectYieldsAtMethodStart();
            while (this.PortaFilterCoffeeLevel < 100 && !this.RefillRequired && !this.IsBroken)
            {
                await Task.Delay(TimeSpan.FromSeconds(0.1));
            }

            await TaskYieldInjector.InjectYieldsAtMethodMiddle();
            await TaskYieldInjector.InjectYieldsAtMethodEnd();
        }

        private async Task OnHopperEmpty()
        {
            await TaskYieldInjector.InjectYieldsAtMethodStart();
            await this.Sensors.SetGrinderButtonAsync(false);
            await TaskYieldInjector.InjectYieldsAtMethodMiddle();
            this.OnRefillRequired("out of coffee beans");
            await TaskYieldInjector.InjectYieldsAtMethodEnd();
        }

        private Task MakeShotsAsync()
        {
            // Pour the shots.
            this.Log.WriteLine("Making shots...");

            // First we assume user placed a new cup in the machine, and so the shot count is zero.
            this.PreviousShotCount = 0;

            // Wait for shots to be completed.
            return this.MonitorShotsAsync();
        }

        private async Task MonitorShotsAsync()
        {
            await TaskYieldInjector.InjectYieldsAtMethodStart();
            try
            {
                while (!this.IsBroken)
                {
                    this.Log.WriteLine("Shot count is {0}", this.PreviousShotCount);

                    // So we can wait for async event to come back from the sensors.
                    var completion = new TaskCompletionSource<bool>();
                    this.ShotCompleteSource = completion;

                    // Request another shot!
                    await this.Sensors.SetShotButtonAsync(true);

                    if (!this.IsBroken)
                    {
                        await completion.Task;

                        if (!this.IsBroken)
                        {
                            this.PreviousShotCount++;
                            if (this.PreviousShotCount >= this.ShotsRequested && !this.IsBroken)
                            {
                                this.Log.WriteLine("{0} shots completed and {1} shots requested!", this.PreviousShotCount, this.ShotsRequested);
                                if (this.PreviousShotCount > this.ShotsRequested)
                                {
                                    Specification.Assert(false, "Made the wrong number of shots");
                                }

                                break;
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelled.
            }

            await TaskYieldInjector.InjectYieldsAtMethodMiddle();
            await TaskYieldInjector.InjectYieldsAtMethodEnd();
        }

        private Task CleanupAsync()
        {
            // Dump the grinds.
            this.Log.WriteLine("Dumping the grinds!");
            return this.Sensors.SetDumpGrindsButtonAsync(true);
        }

        private void OnRefillRequired(string message)
        {
            this.Error = message;
            this.RefillRequired = true;
            Specification.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
            this.Log.WriteError(message);
        }

        private void OnError()
        {
            this.Error = "Coffee machine needs fixing!";
            Specification.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
            this.Log.WriteError(this.Error);
        }

        public async Task TerminateAsync()
        {
            await TaskYieldInjector.InjectYieldsAtMethodStart();
            this.Halted = true;
            this.Log.WriteLine("Coffee Machine Terminating...");
            var sensors = this.Sensors;
            if (sensors != null)
            {
                await sensors.SetPowerSwitchAsync(false);
            }

            var src = this.ShotCompleteSource;
            if (src != null)
            {
                src.TrySetCanceled();
            }

            await TaskYieldInjector.InjectYieldsAtMethodMiddle();
            // Stop listening to the sensors.
            this.RegisterSensorEvents(false);

            Specification.Monitor<LivenessMonitor>(new LivenessMonitor.IdleEvent());
            this.Log.WriteWarning("#################################################################");
            this.Log.WriteWarning("# Coffee Machine Halted                                         #");
            this.Log.WriteWarning("#################################################################");
            this.Log.WriteLine(string.Empty);
            await TaskYieldInjector.InjectYieldsAtMethodEnd();
        }

        private void RegisterSensorEvents(bool register)
        {
            if (register)
            {
                this.Sensors.HopperEmpty += this.OnHopperEmpty;
                this.Sensors.PortaFilterCoffeeLevelChanged += this.OnPortaFilterCoffeeLevelChanged;
                this.Sensors.ShotComplete += this.OnShotComplete;
                this.Sensors.WaterEmpty += this.OnWaterEmpty;
                this.Sensors.WaterHot += this.OnWaterHot;
                this.Sensors.WaterTemperatureChanged += this.OnWaterTemperatureChanged;
            }
            else
            {
                this.Sensors.HopperEmpty -= this.OnHopperEmpty;
                this.Sensors.PortaFilterCoffeeLevelChanged -= this.OnPortaFilterCoffeeLevelChanged;
                this.Sensors.ShotComplete -= this.OnShotComplete;
                this.Sensors.WaterEmpty -= this.OnWaterEmpty;
                this.Sensors.WaterHot -= this.OnWaterHot;
                this.Sensors.WaterTemperatureChanged -= this.OnWaterTemperatureChanged;
            }
        }

        private void OnWaterTemperatureChanged(object sender, double level)
        {
        }

        private void OnWaterHot(object sender, bool value)
        {
            if (!this.IsBroken)
            {
                Task.Run(this.OnWaterHot);
            }
        }

        private void OnWaterEmpty(object sender, bool e)
        {
            if (!this.IsBroken)
            {
                // Turn off the water pump.
                Task.Run(async () =>
                {
                    await this.Sensors.SetShotButtonAsync(false);
                });
                this.OnRefillRequired("Water is empty!");
            }
        }

        private void OnShotComplete(object sender, bool value)
        {
            if (!this.IsBroken && this.ShotCompleteSource != null)
            {
                try
                {
                    this.ShotCompleteSource.SetResult(value);
                }
                catch (InvalidOperationException)
                {
                    // Cancelled.
                }
            }
        }

        private void OnPortaFilterCoffeeLevelChanged(object sender, double level)
        {
            if (!this.IsBroken)
            {
                if (level >= 100)
                {
                    this.PortaFilterCoffeeLevel = level;
                    this.Log.WriteLine("PortaFilter is full");
                }
                else
                {
                    if (level != this.PortaFilterCoffeeLevel)
                    {
                        this.PortaFilterCoffeeLevel = level;
                        this.Log.WriteLine("PortaFilter is {0} % full", (int)level);
                    }
                }
            }

            Task.Run(this.UpdatePortaFilterLevelAsync);
        }

        private Task UpdatePortaFilterLevelAsync()
        {
            if (this.PortaFilterCoffeeLevel >= 100)
            {
                return this.Sensors.SetGrinderButtonAsync(false);
            }

            return Task.CompletedTask;
        }

        private void OnHopperEmpty(object sender, bool value)
        {
            if (!this.IsBroken)
            {
                var nowait = this.OnHopperEmpty();
            }
        }

        private bool IsBroken
        {
            get { return this.Halted || !string.IsNullOrEmpty(this.Error); }
        }
    }
}
