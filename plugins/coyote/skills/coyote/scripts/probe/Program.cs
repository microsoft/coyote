// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Coyote;
using Microsoft.Coyote.Rewriting;
using Microsoft.Coyote.SystematicTesting;

namespace Probe
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            bool selfTest = args.Length == 1 && args[0] == "--self-test";
            uint iterations = 100;
            if (!selfTest && (args.Length > 1 ||
                (args.Length == 1 && (!uint.TryParse(args[0], out iterations) || iterations == 0 || iterations > 10000))))
            {
                Console.Error.WriteLine("Usage: Probe.dll [iterations: 1..10000 | --self-test]");
                return 3;
            }

            if (!RewritingEngine.IsAssemblyRewritten(typeof(Scenario).Assembly))
            {
                Console.Error.WriteLine("ERROR: probe assembly is not rewritten. Use run.ps1.");
                return 3;
            }

            Scenario.CompletedIterations = 0;
            var (report, _) = Run(Scenario.Run, iterations);
            if (!selfTest)
            {
                if (report.NumOfFoundBugs > 0)
                {
                    Console.WriteLine("RESULT: FAILURE. Diagnose the report; this is not an API-support verdict.");
                    return 1;
                }

                RequireComplete(report, iterations);
                Console.WriteLine("RESULT: No control failure observed on the explored paths.");
                Console.WriteLine("This does not establish support for all uses of the probed API.");
                return 0;
            }

            RequireComplete(report, iterations);
            Console.WriteLine("PASS: controlled delays, atomic updates, and iteration completion.");

            var (race, trace) = Run(RegressionScenarios.LostUpdate, iterations);
            RequireExpectedBug(race, RegressionScenarios.LostUpdateMessage);
            Require(!string.IsNullOrWhiteSpace(trace), "The lost update did not produce a replay trace.");
            Console.WriteLine("PASS: intentional lost update detected with the expected invariant.");

            var (replay, _) = Run(RegressionScenarios.LostUpdate, 1, trace);
            RequireExpectedBug(replay, RegressionScenarios.LostUpdateMessage);
            Require(replay.BugReports.SetEquals(race.BugReports), "Replay did not reproduce the same failure.");
            Console.WriteLine("PASS: recorded schedule reproduced the lost-update failure.");

            var (concurrency, _) = Run(RegressionScenarios.UncontrolledConcurrency, 1);
            RequireExpectedBug(concurrency, "System.Threading.Timer", exact: false);
            Console.WriteLine("PASS: strict mode rejected the known uncontrolled timer.");

            var (data, _) = Run(RegressionScenarios.UncontrolledData, 1);
            RequireExpectedBug(data, "System.Guid.NewGuid", exact: false);
            Console.WriteLine("PASS: strict mode rejected data nondeterminism.");
            return 0;
        }

        private static (TestReport Report, string Trace) Run(Func<Task> scenario, uint iterations, string? trace = null)
        {
            var configuration = Configuration.Create()
                .WithTestingIterations(iterations)
                .WithRandomStrategy()
                .WithRandomGeneratorSeed(0)
                .WithMaxSchedulingSteps(1000)
                .WithFailureOnMaxStepsBoundEnabled()
                .WithPartiallyControlledConcurrencyAllowed(false)
                .WithPartiallyControlledDataNondeterminismAllowed(false)
                .WithTelemetryEnabled(false);
            if (trace != null)
            {
                configuration.WithReproducibleTrace(trace);
            }

            using var engine = TestingEngine.Create(configuration, scenario);
            engine.Run();
            Console.WriteLine(engine.TestReport.GetText(configuration, "[probe]"));
            Require(engine.TestReport.InternalErrors.Count == 0,
                string.Join(Environment.NewLine, engine.TestReport.InternalErrors));
            Require(engine.TestReport.NumOfExploredFairPaths + engine.TestReport.NumOfExploredUnfairPaths > 0,
                "No schedules were explored.");
            return (engine.TestReport, engine.ReproducibleTrace);
        }

        private static void RequireComplete(TestReport report, uint iterations)
        {
            Require(report.NumOfFoundBugs == 0, string.Join(Environment.NewLine, report.BugReports));
            Require(report.UncontrolledInvocations.Count == 0, "The passing scenario reported uncontrolled calls.");
            Require(report.NumOfExploredFairPaths + report.NumOfExploredUnfairPaths == iterations,
                "The requested number of schedules was not explored.");
            Require(Scenario.CompletedIterations == iterations, "A schedule did not reach the final assertion.");
        }

        private static void RequireExpectedBug(TestReport report, string message, bool exact = true)
        {
            Require(report.NumOfFoundBugs == 1 && report.BugReports.Count == 1 &&
                report.BugReports.Any(bug => exact ? bug == message : bug.Contains(message, StringComparison.Ordinal)),
                $"Expected one failure naming '{message}', got: {string.Join(" | ", report.BugReports)}");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
