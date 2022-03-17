// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting;

namespace Microsoft.Coyote.Samples.CloudMessaging.Mocking
{
    public static class Program
    {
        [Test]
        public static void Execute(IActorRuntime runtime)
        {
            var testScenario = new RaftTestScenario();
            testScenario.RunTest(runtime, 5, 2);
        }
    }
}
