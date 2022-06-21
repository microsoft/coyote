// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Coyote;
using Microsoft.Coyote.SystematicTesting;
using PetImages;
using PetImages.Contracts;
using PetImages.Tests.MessagingMocks;
using PetImages.Tests.StorageMocks;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable SA1005
namespace PetImages.Tests
{
    public class Tests
    {
        public static async Task InjectYieldsAtMethodStart()
        {
            string envYiledLoop = Environment.GetEnvironmentVariable("YIELDS_METHOD_START");
            int envYiledLoopInt = 0;
            if (envYiledLoop != null)
            {
#pragma warning disable CA1305 // Specify IFormatProvider
                envYiledLoopInt = int.Parse(envYiledLoop);
#pragma warning restore CA1305 // Specify IFormatProvider
            }

            for (int i = 0; i < envYiledLoopInt; i++)
            {
                await Task.Yield();
            }
        }

        [Fact]
        public async Task TestFirstScenario()
        {
            await InjectYieldsAtMethodStart();

            // Initialize the in-memory service factory.
            using var factory = new ServiceFactory();
            await factory.InitializeAccountContainerAsync();
            await factory.InitializeImageContainerAsync();

            using var client = new ServiceClient(factory);

            // Create an account request payload.
            var account = new Account()
            {
                Name = "MyAccount"
            };

            // Call 'CreateAccount' twice without awaiting, which makes both methods run
            // asynchronously with each other.
            var task1 = client.CreateAccountAsync(account);
            var task2 = client.CreateAccountAsync(account);

            // Then wait both requests to complete.
            await Task.WhenAll(task1, task2);

            // Finally, assert that only one of the two requests succeeded and the other
            // failed. Note that we do not know which one of the two succeeded as the
            // requests ran concurrently (this is why we use an exclusive OR).
            Assert.True(
               (task1.Result == HttpStatusCode.OK && task2.Result == HttpStatusCode.Conflict) ||
               (task1.Result == HttpStatusCode.Conflict && task2.Result == HttpStatusCode.OK));
        }

        [Fact]
        public async Task TestSecondScenario()
        {
            await InjectYieldsAtMethodStart();

            // Initialize the in-memory service factory.
            using var factory = new ServiceFactory();
            await factory.InitializeAccountContainerAsync();
            var imageContainer = await factory.InitializeImageContainerAsync();

            using var client = new ServiceClient(factory);

            string accountName = "MyAccount";
            string imageName = "pet.jpg";

            // Create an account request payload.
            var account = new Account()
            {
                Name = accountName
            };

            var accountResult = await client.CreateAccountAsync(account);
            Assert.True(accountResult == HttpStatusCode.OK);

            imageContainer.EnableRandomizedFaults();

            var task1 = client.CreateImageAsync(accountName,
                new Image() { Name = imageName, Content = GetDogImageBytes() });
            var task2 = client.CreateImageAsync(accountName,
                new Image() { Name = imageName, Content = GetDogImageBytes() });
            await Task.WhenAll(task1, task2);

            imageContainer.DisableRandomizedFaults();

            Assert.True(task1.Result == HttpStatusCode.OK || task1.Result == HttpStatusCode.Conflict ||
                task1.Result == HttpStatusCode.ServiceUnavailable);
            Assert.True(task2.Result == HttpStatusCode.OK || task2.Result == HttpStatusCode.Conflict ||
                task2.Result == HttpStatusCode.ServiceUnavailable);

            if (task1.Result == HttpStatusCode.OK || task2.Result == HttpStatusCode.OK)
            {
                var (statusCode, content) = await client.GetImageAsync(accountName, imageName);
                Assert.True(statusCode == HttpStatusCode.OK,
                    $"Status is '{statusCode}', but expected '{HttpStatusCode.OK}'.");
                Assert.True(IsDogImage(content), "The image is not a dog image.");
            }
        }

        [Fact]
        public async Task TestThirdScenario()
        {
           await InjectYieldsAtMethodStart();

           // Initialize the in-memory service factory.
           using var factory = new ServiceFactory();
           await factory.InitializeAccountContainerAsync();
           var imageContainer = await factory.InitializeImageContainerAsync();

           using var client = new ServiceClient(factory);

           string accountName = "MyAccount";
           string imageName = "pet.jpg";

           // Create an account request payload.
           var account = new Account()
           {
               Name = accountName
           };

           var accountResult = await client.CreateAccountAsync(account);
           Assert.True(accountResult == HttpStatusCode.OK);

           var task1 = client.CreateOrUpdateImageAsync(accountName,
               new Image() { Name = imageName, Content = GetDogImageBytes() });
           var task2 = client.CreateOrUpdateImageAsync(accountName,
               new Image() { Name = imageName, Content = GetCatImageBytes() });
           await Task.WhenAll(task1, task2);

           Assert.True(task1.Result.Item1 == HttpStatusCode.OK);
           Assert.True(task2.Result.Item1 == HttpStatusCode.OK);

           var (imageStatusCode, imageContent) = await client.GetImageAsync(accountName, imageName);
           Assert.True(imageStatusCode == HttpStatusCode.OK);
           byte[] image = imageContent;

           byte[] thumbnail;
           while (true)
           {
               var (thumbnailStatusCode, thumbnailContent) = await client.GetImageThumbnailAsync(accountName, imageName);
               if (thumbnailStatusCode == HttpStatusCode.OK)
               {
                   thumbnail = thumbnailContent;
                   break;
               }
           }

           Assert.True(
               (IsDogImage(image) && IsDogThumbnail(thumbnail)) ||
               (IsCatImage(image) && IsCatThumbnail(thumbnail)),
               "Found a thumbnail that does not correspond to its image.");
        }

        [Fact]
        public void CoyoteTestFirstScenario()
        {
            RunCoyoteTest(TestFirstScenario);
        }

        [Fact]
        public void CoyoteTestSecondScenario()
        {
           RunCoyoteTest(this.TestSecondScenario);
        }

        [Fact]
        public void CoyoteTestThirdScenario()
        {
           RunCoyoteTest(this.TestThirdScenario);
        }

        /// <summary>
        /// Invoke the Coyote systematic testing engine to run the specified test multiple iterations,
        /// each iteration exploring potentially different interleavings using some underlying program
        /// exploration strategy (by default a uniform probabilistic strategy).
        /// </summary>
        /// <remarks>
        /// Learn more in our documentation: https://microsoft.github.io/coyote/how-to/unit-testing
        /// </remarks>
        private static void RunCoyoteTest(Func<Task> test, string reproducibleScheduleFilePath = null)
        {
            // Configuration for how to run a concurrency unit test with Coyote.
            // This configuration will run the test 1000 times exploring different paths each time.
            var config = Configuration.Create().WithTestingIterations(1000);

            if (reproducibleScheduleFilePath != null)
            {
                var trace = File.ReadAllText(reproducibleScheduleFilePath);
                config = config.WithReplayStrategy(trace);
            }

            // var config = Configuration.Create();

            // --------------------------------------------START [PCT BOUND]-------------------------------------------------------------------------------------------------------------------------------------------
            string envMaxPCTSwitchPoints = Environment.GetEnvironmentVariable("OLP_TEST_PCT_SWITCHES"); // NOTE: OLP_TEST_PCT_SWITCHES muse be a positive integer.
            uint envMaxPCTSwitchPointsInt = 10;
            if (envMaxPCTSwitchPoints != null)
            {
#pragma warning disable CA1305 // Specify IFormatProvider
                envMaxPCTSwitchPointsInt = uint.Parse(envMaxPCTSwitchPoints);
#pragma warning restore CA1305 // Specify IFormatProvider
            }

            // --------------------------------------------END [PCT BOUND]---------------------------------------------------------------------------------------------------------------------------------------------

            // --------------------------------------------START [STRATEGY]--------------------------------------------------------------------------------------------------------------------------------------------
            string envScheduler = Environment.GetEnvironmentVariable("OLP_TEST_SCHEDULER"); // NOTE: OLP_TEST_SCHEDULER muse be a string, either "PCT", "FAIRPCT" or "RANDOM".
            if (envScheduler != null)
            {
                if (envScheduler == "PCT")
                {
                    // config = config.WithPCTStrategy(false, envMaxPCTSwitchPointsInt);
                    config = config.WithPrioritizationStrategy(false, envMaxPCTSwitchPointsInt);
                }
                else if (envScheduler == "FAIRPCT")
                {
                    // config = config.WithPCTStrategy(true, (uint)envMaxPCTSwitchPointsInt);
                    config = config.WithPrioritizationStrategy(true, envMaxPCTSwitchPointsInt);
                }

                // else if (envScheduler == "TASKPCT")
                // {
                //     config = config.WithTaskPCTStrategy(false, (uint)envMaxPCTSwitchPointsInt);
                // }
                // else if (envScheduler == "FAIRTASKPCT")
                // {
                //     config = config.WithTaskPCTStrategy(true, (uint)envMaxPCTSwitchPointsInt);
                // }
                else if (envScheduler == "RL")
                {
                    config = config.WithRLStrategy();
                }
                else if (envScheduler == "PROBABILISTIC")
                {
                    config = config.WithProbabilisticStrategy();
                }

                // else if (envScheduler == "DFS")
                // {
                //     config = config.WithDFSStrategy();
                // }
                else
                {
                    envScheduler = "RANDOM";
                }
            }
            else
            {
                envScheduler = "RANDOM";
            }

            // --------------------------------------------END [STRATEGY]---------------------------------------------------------------------------------------------------------------------------------------------

            // --------------------------------------------START [DEBUGGER]-------------------------------------------------------------------------------------------------------------------------------------------
            string envDebugger = Environment.GetEnvironmentVariable("OLP_TEST_DEBUGGER");
            if (envDebugger != null)
            {
                bool envDebuggerBool = bool.Parse(envDebugger);
                if (envDebuggerBool)
                {
                    System.Diagnostics.Debugger.Launch();
                }
            }

            // --------------------------------------------END [DEBUGGER]---------------------------------------------------------------------------------------------------------------------------------------------

            // --------------------------------------------START [ITERATION]------------------------------------------------------------------------------------------------------------------------------------------
            string envIterations = Environment.GetEnvironmentVariable("OLP_TEST_ITERATIONS");
            uint envIterationsInt = 100;
            if (envIterations != null)
            {
#pragma warning disable CA1305 // Specify IFormatProvider
                envIterationsInt = uint.Parse(envIterations);
#pragma warning restore CA1305 // Specify IFormatProvider
            }

            // --------------------------------------------END [ITERATION]---------------------------------------------------------------------------------------------------------------------------------------------

            // --------------------------------------------START [VERBOSITY]---------------------------------------------------------------------------------------------------------------------------------------------
            string envVerbosity = Environment.GetEnvironmentVariable("OLP_TEST_VERBOSITY");
            bool envVerbosityBool = false;
            if (envVerbosity != null)
            {
                envVerbosityBool = bool.Parse(envVerbosity);
            }

            // --------------------------------------------END [VERBOSITY]---------------------------------------------------------------------------------------------------------------------------------------------

            // --------------------------------------------START [EXPLORE]---------------------------------------------------------------------------------------------------------------------------------------------
            string envExplore = Environment.GetEnvironmentVariable("OLP_TEST_EXPLORE");
            bool envExploreBool = false;
            if (envExplore != null)
            {
                #pragma warning disable CA1305 // Specify IFormatProvider
                envExploreBool = bool.Parse(envExplore);
                #pragma warning restore CA1305 // Specify IFormatProvider
            }

            // --------------------------------------------END [EXPLORE]---------------------------------------------------------------------------------------------------------------------------------------------

            // --------------------------------------------START [SEED]---------------------------------------------------------------------------------------------------------------------------------------------
            string envSeed = Environment.GetEnvironmentVariable("OLP_TEST_SEED");
            if (envSeed != null)
            {
                #pragma warning disable CA1305 // Specify IFormatProvider
                uint envSeedInt = uint.Parse(envSeed);
                #pragma warning restore CA1305 // Specify IFormatProvider
                // config = config.WithRandomGeneratorSeed(envSeedInt);
            }

            // --------------------------------------------END [SEED]---------------------------------------------------------------------------------------------------------------------------------------------

            // =============================================================START [NON IMP env vars]=======================================================================================================================================
            string envMaxSteps = Environment.GetEnvironmentVariable("OLP_TEST_MAXSTEPS");
            if (envMaxSteps != null)
            {
                #pragma warning disable CA1305 // Specify IFormatProvider
                uint envMaxStepsInt = uint.Parse(envMaxSteps);
                #pragma warning restore CA1305 // Specify IFormatProvider
                config = config.WithMaxSchedulingSteps(envMaxStepsInt);
            }

            string envTimeout = Environment.GetEnvironmentVariable("OLP_TEST_TIMEOUT");
            if (envTimeout != null)
            {
                #pragma warning disable CA1305 // Specify IFormatProvider
                int envTimeoutInt = int.Parse(envTimeout);
                #pragma warning restore CA1305 // Specify IFormatProvider
                config = config.WithTestingTimeout(envTimeoutInt);
            }

            string envXmlLog = Environment.GetEnvironmentVariable("OLP_TEST_XMLLOG");
            if (envXmlLog != null)
            {
                #pragma warning disable CA1305 // Specify IFormatProvider
                bool envXmlLogBool = bool.Parse(envXmlLog);
                #pragma warning restore CA1305 // Specify IFormatProvider
                config = config.WithXmlLogEnabled(envXmlLogBool);
            }

            // =============================================================END [NON IMP env vars]=======================================================================================================================================

            Console.WriteLine($"--------------------COYOTE TESTING STARTING, ENV VAR VALUE: envMaxPCTSwitchPoints: {envMaxPCTSwitchPoints}");
            Console.WriteLine($"--------------------COYOTE TESTING STARTING, ENV VAR VALUE: envScheduler: {envScheduler}");
            Console.WriteLine($"--------------------COYOTE TESTING STARTING, ENV VARS VALUE: envDebugger: {envDebugger}");
            Console.WriteLine($"--------------------COYOTE TESTING STARTING, ENV VARS VALUE: envIterations: {envIterations}");
            Console.WriteLine($"--------------------COYOTE TESTING STARTING, ENV VARS VALUE: envVerbosity: {envVerbosity}");
            Console.WriteLine($"--------------------COYOTE TESTING STARTING, ENV VARS VALUE: envSeed: {envSeed}");
            Console.WriteLine($"--------------------COYOTE TESTING STARTING, ENV VARS VALUE: envMaxSteps: {envMaxSteps}");
            Console.WriteLine($"--------------------COYOTE TESTING STARTING, ENV VARS VALUE: envTimeout: {envTimeout}");
            Console.WriteLine($"--------------------COYOTE TESTING STARTING, ENV VARS VALUE: envXmlLog: {envXmlLog}");
            Console.WriteLine($"--------------------COYOTE TESTING STARTING, ENV VARS VALUE: envExploreBool: {envExploreBool}");

            config = config.WithTestingIterations(envIterationsInt);
            config = config.WithVerbosityEnabled(envVerbosityBool);
            config = config.WithTestIterationsRunToCompletion(envExploreBool);

            // maybe from coyote version 1.4.1
            // config = config.WithConcurrencyFuzzingFallbackEnabled(false);
            // config = config.WithConcurrencyFuzzingEnabled(false);
            // config = config.WithPartiallyControlledConcurrencyEnabled(false);

            config = config.WithPartiallyControlledConcurrencyAllowed(false);
            config = config.WithSystematicFuzzingEnabled(false);
            config = config.WithSystematicFuzzingFallbackEnabled(false);
            config = config.WithSharedStateReductionEnabled(false);
            config = config.WithActivityCoverageReported(false);
            config = config.WithPotentialDeadlocksReportedAsBugs(false);

            // Added to ignore the error: task not intercepted and controlled during testing, so it can interfere with the ability to reproduce bug traces.
            config = config.WithNoBugTraceRepro();
            
            // FN_DOUBT:
            // config = config.WithLivenessTemperatureThreshold()// doubt.
            // config = config.WithReplayStrategy() // doubt: how to do this schedule trace business.
            // config = config.WithDebugLoggingEnabled(true); // in older versions of coyote only
            // config = config.WithDeadlockTimeout();
            // config = config.WithTraceVisualizationEnabled();
            // config = config.WithPotentialDeadlocksReportedAsBugs();
            // config = config.WithUncontrolledConcurrencyResolutionTimeout();
            // config = config.WithTimeoutDelay(); // doubt.
            // config = config.WithIncrementalSeedGenerationEnabled();
            // config = config.WithProductionMonitorEnabled();
            var testingEngine = TestingEngine.Create(config, test);

            try
            {
                testingEngine.Run();

                string assertionText = testingEngine.TestReport.GetText(config);
                // assertionText +=
                //     $"{Environment.NewLine} Random Generator Seed: " +
                //     $"{testingEngine.TestReport.Configuration.RandomGeneratorSeed}{Environment.NewLine}";
                foreach (var bugReport in testingEngine.TestReport.BugReports)
                {
                    assertionText +=
                    $"{Environment.NewLine}" +
                    "Bug Report: " + bugReport.ToString(CultureInfo.InvariantCulture);
                }

                if (testingEngine.TestReport.NumOfFoundBugs > 0)
                {
                    var timeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ", CultureInfo.InvariantCulture);
                    var reproducibleTraceFileName = $"buggy-{timeStamp}.schedule";
                    assertionText += Environment.NewLine + "Reproducible trace which leads to the bug can be found at " +
                        $"{Path.Combine(Directory.GetCurrentDirectory(), reproducibleTraceFileName)}";

                    File.WriteAllText(reproducibleTraceFileName, testingEngine.ReproducibleTrace);
                }

                Assert.True(testingEngine.TestReport.NumOfFoundBugs == 0, assertionText);
            }
            finally
            {
                testingEngine.Stop();
            }
        }

        private static byte[] GetDogImageBytes() => new byte[] { 1, 2, 3 };

        private static byte[] GetCatImageBytes() => new byte[] { 4, 5, 6 };

        private static bool IsDogImage(byte[] imageBytes) => imageBytes.SequenceEqual(GetDogImageBytes());

        private static bool IsCatImage(byte[] imageBytes) => imageBytes.SequenceEqual(GetCatImageBytes());

        private static bool IsDogThumbnail(byte[] thumbnailBytes) => thumbnailBytes.SequenceEqual(GetDogImageBytes());

        private static bool IsCatThumbnail(byte[] thumbnailBytes) => thumbnailBytes.SequenceEqual(GetCatImageBytes());
    }
}
