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
            ICoyoteRuntime runtime = RuntimeProvider.Create();
            _ = Execute(runtime);
            Console.ReadLine();
            Console.WriteLine("User cancelled the test by pressing ENTER");
        }

        private static void OnRuntimeFailure(Exception ex)
        {
            Console.WriteLine("### Failure: " + ex.Message);
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static async Task Execute(ICoyoteRuntime runtime)
        {
            LogWriter.Initialize(runtime.Logger, RunForever);
            runtime.OnFailure += OnRuntimeFailure;
            Specification.RegisterMonitor<LivenessMonitor>();
            IFailoverDriver driver = new FailoverDriver(RunForever);
            await driver.RunTest();
        }
    }
}
