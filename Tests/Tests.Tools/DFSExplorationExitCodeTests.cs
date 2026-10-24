// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Reflection;
using Microsoft.Coyote.SystematicTesting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Tools.Tests
{
    public class DFSExplorationExitCodeTests : BaseToolsTest
    {
        public DFSExplorationExitCodeTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Theory(Timeout = 5000)]
        [InlineData(true, true, false, false, 0)]
        [InlineData(true, false, false, false, 0)]
        [InlineData(false, true, false, false, 4)]
        [InlineData(false, false, false, false, 0)]
        [InlineData(true, true, true, false, 2)]
        [InlineData(false, true, true, false, 2)]
        [InlineData(true, true, true, true, 3)]
        [InlineData(false, true, true, true, 3)]
        public void TestExitCodeForExhaustionEvidence(bool exhausted, bool isStrict, bool hasBug,
            bool hasInternalError, int expectedExitCode)
        {
            var configuration = this.GetConfiguration().WithTestingIterations(100).WithStrictExplorationEnabled(isStrict);
            var report = new TestReport(configuration)
            {
                NumOfExploredFairPaths = 3,
                MaxExploredFairSteps = 1,
                NumOfFoundBugs = hasBug ? 1 : 0
            };
            if (exhausted)
            {
                report.RecordExplorationExhaustion();
            }

            if (hasInternalError)
            {
                report.InternalErrors.Add("Fixture internal error.");
            }

            // The CLI does not expose DFS, so exercise the same exit-code helper used by its test command.
            var framework = new DirectoryInfo(AppContext.BaseDirectory).Name;
            var cliPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../bin", framework, "coyote.dll"));
            var program = Assembly.LoadFrom(cliPath).GetType("Microsoft.Coyote.Program", throwOnError: true);
            var getExitCode = program.GetMethod("GetExitCodeFromTestReport", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(getExitCode);
            Assert.Equal(expectedExitCode, Convert.ToInt32(getExitCode.Invoke(null, new object[] { report, configuration })));
        }
    }
}
