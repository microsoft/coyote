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
            var conf = null as Configuration;
            // var conf = Configuration.Create().WithVerbosityEnabled();

            RunForever = true;
            IActorRuntime runtime = Actors.RuntimeFactory.Create(conf);
            runtime.OnFailure += OnRuntimeFailure;
            Execute(runtime);
            Console.ReadLine();
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static void Execute(IActorRuntime runtime)
        {
            LogWriter.Initialize(runtime.Logger, RunForever);
            runtime.RegisterMonitor<LivenessMonitor>();
            ActorId driver = runtime.CreateActor(typeof(FailoverDriver), new FailoverDriver.ConfigEvent(RunForever));
        }

        private static void OnRuntimeFailure(Exception ex)
        {
            LogWriter.Instance.WriteError("### Error: {0}", ex.Message);
        }
    }
}
