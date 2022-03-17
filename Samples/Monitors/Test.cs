// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Samples.Monitors
{
    /// <summary>
    /// A sample application written using C# and the Coyote library.
    ///
    /// This program implements a failure detection protocol. A failure detector state
    /// machine is given a list of machines, each of which represents a daemon running
    /// at a computing node in a distributed system. The failure detector sends each
    /// machine in the list a 'Ping' event and determines whether the machine has failed
    /// if it does not respond with a 'Pong' event within a certain time period.
    ///
    /// Note: this is an abstract implementation aimed primarily to showcase the testing
    /// capabilities of Coyote.
    /// </summary>
    public static class Program
    {
        public static void Main()
        {
            // Optional: increases verbosity level to see the Coyote runtime log.
            var configuration = Configuration.Create().WithVerbosityEnabled();

            // Creates a new Coyote runtime instance, and passes an optional configuration.
            var runtime = RuntimeFactory.Create(configuration);

            // Executes the Coyote program.
            Execute(runtime);

            // The Coyote runtime executes asynchronously, so we wait
            // to not terminate the process.
            Console.ReadLine();
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static void Execute(IActorRuntime runtime)
        {
            // Monitors must be registered before the first Coyote machine
            // gets created (which will kickstart the runtime).
            runtime.RegisterMonitor<Safety>();
            runtime.RegisterMonitor<Liveness>();
            runtime.CreateActor(typeof(Driver), new Driver.Config(2));
        }
    }
}
