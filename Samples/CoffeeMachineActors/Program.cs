// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Samples.Common;

namespace Microsoft.Coyote.Samples.CoffeeMachineActors
{
    public static class Program
    {
        private static bool RunForever = false;

        public static void Main()
        {
            // Optional: increases verbosity level to see the Coyote runtime log and sets it to output to the console.
            // var configuration = Configuration.Create();
            var configuration = Configuration.Create().WithVerbosityEnabled().WithConsoleLoggingEnabled();
            RunForever = true;

            IActorRuntime runtime = RuntimeFactory.Create(configuration);
            runtime.OnFailure += OnRuntimeFailure;
            Execute(runtime);
            Console.ReadLine();
            Console.WriteLine("User cancelled the test by pressing ENTER");
        }

        private static void OnRuntimeFailure(Exception ex)
        {
            Console.WriteLine("Unhandled exception: {0}", ex.Message);
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static void Execute(IActorRuntime runtime)
        {
            LogWriter.Initialize(runtime.Logger);
            runtime.RegisterMonitor<LivenessMonitor>();
            runtime.RegisterMonitor<DoorSafetyMonitor>();
            ActorId driver = runtime.CreateActor(typeof(FailoverDriver), new ConfigEvent(RunForever));
            runtime.SendEvent(driver, new FailoverDriver.StartTestEvent());
        }
    }
}
