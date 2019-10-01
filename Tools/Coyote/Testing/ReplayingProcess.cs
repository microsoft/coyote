// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.TestingServices;

namespace Microsoft.Coyote
{
    /// <summary>
    /// A replaying process.
    /// </summary>
    internal sealed class ReplayingProcess
    {
        /// <summary>
        /// Configuration.
        /// </summary>
        private readonly Configuration Configuration;

        /// <summary>
        /// Creates a Coyote replaying process.
        /// </summary>
        public static ReplayingProcess Create(Configuration configuration)
        {
            return new ReplayingProcess(configuration);
        }

        /// <summary>
        /// Starts the Coyote replaying process.
        /// </summary>
        public void Start()
        {
            Console.WriteLine(". Reproducing trace in " + this.Configuration.AssemblyToBeAnalyzed);

            // Creates a new replay engine to reproduce a bug.
            ITestingEngine engine = TestingEngineFactory.CreateReplayEngine(this.Configuration);

            engine.Run();
            Console.WriteLine(engine.GetReport());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayingProcess"/> class.
        /// </summary>
        private ReplayingProcess(Configuration configuration)
        {
            configuration.EnableColoredConsoleOutput = true;
            configuration.DisableEnvironmentExit = false;
            this.Configuration = configuration;
        }
    }
}
