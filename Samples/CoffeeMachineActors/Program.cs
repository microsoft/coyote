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
            RunForever = true;
            IActorRuntime runtime = RuntimeFactory.Create();
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
            LogWriter.Initialize(runtime.Logger, RunForever);

            runtime.OnFailure += OnRuntimeFailure;
            runtime.RegisterMonitor<LivenessMonitor>();
            runtime.RegisterMonitor<DoorSafetyMonitor>();
            ActorId driver = runtime.CreateActor(typeof(FailoverDriver), new ConfigEvent(RunForever));
            runtime.SendEvent(driver, new FailoverDriver.StartTestEvent());
        }
    }
}
