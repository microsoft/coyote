// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Samples.Common;

namespace Coyote.Examples.Timers
{
    public static class Program
    {
        public static void Main()
        {
            // Optional: increases verbosity level to see the Coyote runtime log and sets it to output to the console.
            // var configuration = Configuration.Create();
            var configuration = Configuration.Create().WithVerbosityEnabled().WithConsoleLoggingEnabled();

            // Creates a new Coyote runtime instance, and passes an optional configuration.
            var runtime = RuntimeFactory.Create(configuration);

            // Executes the Coyote program.
            Execute(runtime);

            // The Coyote runtime executes asynchronously, so we wait
            // here until user presses ENTER key before terminating the program.
            Console.ReadLine();
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static void Execute(IActorRuntime runtime)
        {
            LogWriter.Initialize(runtime.Logger);
            runtime.CreateActor(typeof(TimerSample));
        }
    }
}
