// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote;
using Microsoft.Coyote.SystematicTesting;

namespace PetImages.Tests
{
    public static class Program
    {
        public static void Main()
        {
            var tests = new Tests();
            var config = Configuration.Create()
                .WithTestingIterations(1)
                // .WithTestIterationsRunToCompletion();
                // .WithRandomGeneratorSeed(2156141611)
                .WithDebugLoggingEnabled()
                .WithVerbosityEnabled();
            var engine = TestingEngine.Create(config, tests.TestThirdScenario);
            engine.Run();
            Console.WriteLine($"Bugs found: {engine.TestReport.NumOfFoundBugs}");
        }
    }
}
