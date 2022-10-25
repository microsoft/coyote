// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Samples.Common;
using Microsoft.Coyote.Specifications;

namespace Microsoft.Coyote.Samples.CoffeeMachineTasks
{
    public static class Program
    {
        private static bool RunForever = false;

        public static void Main()
        {
            RunForever = true;

            LogWriter.Initialize();
            _ = RunTest();

            Console.ReadLine();
            Console.WriteLine("User cancelled the test by pressing ENTER");
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static async Task Execute(ICoyoteRuntime runtime)
        {
            LogWriter.Initialize(runtime.Logger);
            Specification.RegisterMonitor<LivenessMonitor>();
            await RunTest();
        }

        private static async Task RunTest()
        {
            IFailoverDriver driver = new FailoverDriver(RunForever);
            await driver.RunTest();
        }
    }
}
