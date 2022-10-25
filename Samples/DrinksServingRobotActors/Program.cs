// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Samples.Common;

namespace Microsoft.Coyote.Samples.DrinksServingRobot
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

            IActorRuntime runtime = Actors.RuntimeFactory.Create(configuration);
            runtime.OnFailure += OnRuntimeFailure;
            Execute(runtime);
            Console.ReadLine();
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static void Execute(IActorRuntime runtime)
        {
            LogWriter.Initialize(runtime.Logger);
            runtime.RegisterMonitor<LivenessMonitor>();
            runtime.CreateActor(typeof(FailoverDriver), new FailoverDriver.ConfigEvent(RunForever));
        }

        private static void OnRuntimeFailure(Exception ex)
        {
            LogWriter.Instance.WriteError("### Error: {0}", ex.Message);
        }
    }
}
