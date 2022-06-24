// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Random;
using Microsoft.Coyote.Samples.Common;

namespace Microsoft.Coyote.Samples.CoffeeMachineTasks
{
    /// <summary>
    /// This interface is designed to test how the CoffeeMachine handles "failover" or specifically,
    /// can it correctly "restart after failure" without getting into a bad state. The CoffeeMachine
    /// will be randomly terminated. The only thing the CoffeeMachine can depend on is the persistence
    /// of the state provided by the MockSensors.
    /// </summary>
    internal interface IFailoverDriver
    {
        Task RunTest();
    }

    /// <summary>
    /// This class implements the IFailoverDriver.
    /// </summary>
    internal class FailoverDriver : IFailoverDriver
    {
        private readonly ISensors Sensors;
        private ICoffeeMachine CoffeeMachine;
        private bool IsInitialized;
        private bool RunForever;
        private int Iterations;
        private ControlledTimer HaltTimer;
        private readonly Generator RandomGenerator;
        private readonly LogWriter Log = LogWriter.Instance;

        public FailoverDriver(bool runForever)
        {
            this.RunForever = runForever;
            this.RandomGenerator = Generator.Create();
            this.Sensors = new MockSensors(runForever);
        }

        public async Task RunTest()
        {
            bool halted = true;
            while (this.RunForever || this.Iterations <= 1)
            {
                this.Log.WriteLine("#################################################################");

                // Create a new CoffeeMachine instance.
                string error = null;
                if (halted)
                {
                    this.Log.WriteLine("starting new CoffeeMachine iteration {0}.", this.Iterations);
                    this.IsInitialized = false;
                    this.CoffeeMachine = new CoffeeMachine();
                    halted = false;
                    this.IsInitialized = await this.CoffeeMachine.InitializeAsync(this.Sensors);
                    if (!this.IsInitialized)
                    {
                        error = "init failed";
                    }
                }

                if (error == null)
                {
                    // Setup a timer to randomly kill the coffee machine. When the timer fires we
                    // will restart the coffee machine and this is testing that the machine can
                    // recover gracefully when that happens.
                    this.HaltTimer = new ControlledTimer("HaltTimer", TimeSpan.FromSeconds(this.RandomGenerator.NextInteger(7) + 1), new Action(this.OnStopTest));

                    // Request a coffee!
                    var shots = this.RandomGenerator.NextInteger(3) + 1;
                    error = await this.CoffeeMachine.MakeCoffeeAsync(shots);
                }

                if (string.Compare(error, "<halted>", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // Then OnStopTest did it's thing, so it is time to create new coffee machine.
                    this.Log.WriteWarning("CoffeeMachine is halted.");
                    halted = true;
                }
                else if (!string.IsNullOrEmpty(error))
                {
                    this.Log.WriteWarning("CoffeeMachine reported an error.");
                    // No point trying to make more coffee.
                    this.RunForever = false;
                    this.Iterations = 10;
                }
                else
                {
                    // In this case we let the same CoffeeMachine continue on then.
                    this.Log.WriteLine("CoffeeMachine completed the job.");
                }

                this.Iterations++;
            }

            // Shutdown the sensors because test is now complete.
            this.Log.WriteLine("Test is complete, press ENTER to continue...");
            await this.Sensors.TerminateAsync();
        }

        internal void OnStopTest()
        {
            if (!this.IsInitialized)
            {
                // Not ready!
                return;
            }

            if (this.HaltTimer != null)
            {
                this.HaltTimer.Stop();
                this.HaltTimer = null;
            }

            // Halt the CoffeeMachine. HaltEvent is async and we must ensure the CoffeeMachine
            // is really halted before we create a new one because MockSensors will get confused
            // if two CoffeeMachines are running at the same time. So we've implemented a terminate
            // handshake here. We send event to the CoffeeMachine to terminate, and it sends back
            // a HaltedEvent when it really has been halted.
            this.Log.WriteLine("forcing termination of CoffeeMachine.");
            Task.Run(this.CoffeeMachine.TerminateAsync);
        }
    }
}
