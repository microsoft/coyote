// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Samples;
using Microsoft.Coyote.SystematicTesting;

namespace Microsoft.Coyote.Samples.TestDriver
{
    public static class Program
    {
        public static void Main()
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            // AccountManager tests.
            var configuration = Configuration.Create().WithTestingIterations(100).
                WithSystematicFuzzingFallbackEnabled(false);
            RunTest(Samples.AccountManager.Program.TestAccountCreation, configuration,
                "AccountManager.TestAccountCreation");
            RunTest(Samples.AccountManager.Program.TestConcurrentAccountCreation, configuration,
                "AccountManager.TestConcurrentAccountCreation",
                "Microsoft.Coyote.Samples.AccountManager.RowAlreadyExistsException");
            RunTest(Samples.AccountManager.Program.TestConcurrentAccountDeletion, configuration,
                "AccountManager.TestConcurrentAccountDeletion",
                "Microsoft.Coyote.Samples.AccountManager.RowNotFoundException");
            RunTest(Samples.AccountManager.Program.TestConcurrentAccountCreationAndDeletion, configuration,
                "AccountManager.TestConcurrentAccountCreationAndDeletion");
            RunTest(Samples.AccountManager.ETags.Program.TestAccountUpdate, configuration,
                "AccountManager.ETags.TestAccountCreation");
            RunTest(Samples.AccountManager.ETags.Program.TestConcurrentAccountUpdate, configuration,
                "AccountManager.ETags.TestConcurrentAccountCreation");
            RunTest(Samples.AccountManager.ETags.Program.TestGetAccountAfterConcurrentUpdate, configuration,
                "AccountManager.ETags.TestConcurrentAccountDeletion");

            // BoundedBuffer tests.
            configuration = Configuration.Create().WithTestingIterations(100).
                WithSystematicFuzzingFallbackEnabled(false);
            RunTest(Samples.BoundedBuffer.Program.TestBoundedBufferNoDeadlock, configuration,
                "BoundedBuffer.TestBoundedBufferNoDeadlock");
            RunTest(Samples.BoundedBuffer.Program.TestBoundedBufferMinimalDeadlock, configuration,
                "BoundedBuffer.TestBoundedBufferMinimalDeadlock",
                "Deadlock detected.");

            // CloudMessaging tests.
            // configuration = Configuration.Create().WithTestingIterations(1000)
            //     .WithMaxSchedulingSteps(500);
            // RunTest(Samples.CloudMessaging.Mocking.Program.Execute, configuration,
            //     "CloudMessaging.TestWithMocking");
            // RunTest(Samples.CloudMessaging.Nondeterminism.Program.Execute, configuration,
            //     "CloudMessaging.TestWithNondeterminism");

            // CoffeeMachineActors tests.
            configuration = Configuration.Create().WithTestingIterations(1000)
                .WithMaxSchedulingSteps(500).WithPrioritizationStrategy(true).
                WithSystematicFuzzingFallbackEnabled(false);
            RunTest(Samples.CoffeeMachineActors.Program.Execute, configuration,
                "CoffeeMachineActors.Test",
                "Please do not turn on grinder if there are no beans in the hopper",
                "detected liveness bug in hot state 'Busy'");

            // CoffeeMachineTasks tests.
            configuration = Configuration.Create().WithTestingIterations(1000)
                .WithMaxSchedulingSteps(500).WithPrioritizationStrategy(true).
                WithSystematicFuzzingFallbackEnabled(false);
            RunTest(Samples.CoffeeMachineTasks.Program.Execute, configuration,
                "CoffeeMachineTasks.Test",
                "Please do not turn on grinder if there are no beans in the hopper");

            // DrinksServingRobotActors tests.
            configuration = Configuration.Create().WithTestingIterations(1000)
                .WithMaxSchedulingSteps(2000).WithPrioritizationStrategy(true).
                WithSystematicFuzzingFallbackEnabled(false);
            RunTest(Samples.DrinksServingRobot.Program.Execute, configuration,
                "DrinksServingRobotActors.Test",
                "detected liveness bug in hot state 'Busy'");

            // HelloWorldActors tests.
            configuration = Configuration.Create().WithTestingIterations(100).
                WithSystematicFuzzingFallbackEnabled(false);
            RunTest(Samples.HelloWorldActors.Program.Execute, configuration,
                "HelloWorldActors.Test",
                "Too many greetings returned!");

            // Monitors tests.
            configuration = Configuration.Create().WithTestingIterations(10000)
                .WithMaxSchedulingSteps(200).WithPrioritizationStrategy(false).
                WithSystematicFuzzingFallbackEnabled(false);
            RunTest(Samples.Monitors.Program.Execute, configuration,
                "Monitors.Test",
                "ping count must be <= 3");

            // ImageGallery tests.
            // configuration = Configuration.Create().WithTestingIterations(1000);
            // var imageGalleryTests = new ImageGallery.Tests.UnitTests();
            // RunTest(imageGalleryTests.TestConcurrentAccountRequestsAsync, configuration,
            //     "ImageGallery.TestConcurrentAccountRequests",
            //     "Found unexpected error code: ServiceUnavailable");
            // RunTest(imageGalleryTests.TestConcurrentAccountAndImageRequestsAsync, configuration,
            //     "ImageGallery.TestConcurrentAccountAndImageRequests",
            //     "The given key 'gallery-0' was not present in the dictionary",
            //     "The image was not deleted from Azure Blob Storage");

            // PetImages tests.
            // configuration = Configuration.Create().WithTestingIterations(1000);
            // var petImagesTests = new PetImages.Tests.Tests();
            // RunTest(petImagesTests.TestFirstScenario, configuration,
            //     "PetImages.TestFirstScenario",
            //     "PetImages.Exceptions.DatabaseItemAlreadyExistsException",
            //     "Assert.IsTrue failed");
            // RunTest(petImagesTests.TestSecondScenario, configuration,
            //     "PetImages.TestSecondScenario",
            //     "Assert.IsTrue failed");
            // RunTest(petImagesTests.TestThirdScenario, configuration,
            //     "PetImages.TestThirdScenario",
            //     "Assert.IsTrue failed");

            stopWatch.Stop();
            Console.WriteLine($"Done testing in {stopWatch.ElapsedMilliseconds}ms. All expected bugs found.");
        }

        private static void RunTest(Action test, Configuration configuration, string testName,
            params string[] expectedBugs)
        {
            var engine = TestingEngine.Create(configuration, test);
            RunTest(engine, testName, expectedBugs);
        }

        private static void RunTest(Func<Task> test, Configuration configuration, string testName,
            params string[] expectedBugs)
        {
            var engine = TestingEngine.Create(configuration, test);
            RunTest(engine, testName, expectedBugs);
        }

        private static void RunTest(Func<ICoyoteRuntime, Task> test, Configuration configuration, string testName,
            params string[] expectedBugs)
        {
            var engine = TestingEngine.Create(configuration, test);
            RunTest(engine, testName, expectedBugs);
        }

        private static void RunTest(Action<IActorRuntime> test, Configuration configuration, string testName,
            params string[] expectedBugs)
        {
            var engine = TestingEngine.Create(configuration, test);
            RunTest(engine, testName, expectedBugs);
        }

        private static void RunTest(TestingEngine engine, string testName, string[] expectedBugs)
        {
            Console.WriteLine($"Starting to test '{testName}'.");
            engine.Run();
            Console.WriteLine($"Done testing '{testName}'. Found {engine.TestReport.NumOfFoundBugs} bugs.");
            if (expectedBugs.Length > 0 && engine.TestReport.NumOfFoundBugs == 0)
            {
                foreach (var expectedBug in expectedBugs)
                {
                    Console.WriteLine($"Expected bug '{expectedBug}' not found.");
                }

                Environment.Exit(1);
            }
            else if (expectedBugs.Length > 0 && engine.TestReport.NumOfFoundBugs > 0)
            {
                bool isFound = false;
                var actualBug = engine.TestReport.BugReports.First();
                foreach (var expectedBug in expectedBugs)
                {
                    if (actualBug.Contains(expectedBug))
                    {
                        isFound = true;
                        break;
                    }
                }

                if (!isFound)
                {
                    foreach (var expectedBug in expectedBugs)
                    {
                        Console.WriteLine($"Found '{actualBug}' bug instead of the expected bug '{expectedBug}'.");
                    }

                    Environment.Exit(1);
                }

                Console.WriteLine($"Found expected '{actualBug}' bug.");
            }
            else if (engine.TestReport.NumOfFoundBugs > 0)
            {
                Console.WriteLine($"Unexpected '{engine.TestReport.BugReports.First()}' bug found.");
                Environment.Exit(1);
            }
        }
    }
}
