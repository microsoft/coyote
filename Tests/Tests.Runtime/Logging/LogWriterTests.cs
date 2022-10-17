// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Runtime.Tests.Logging
{
    public class LogWriterTests : BaseRuntimeTest
    {
        public LogWriterTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private string WriteAllSeverityMessages(VerbosityLevel level)
        {
            Configuration config = this.GetConfiguration().WithVerbosityEnabled(level);
            using var logger = new MemoryLogger(config.VerbosityLevel);
            using var logWriter = new LogWriter(config);
            logWriter.SetLogger(logger);
            Assert.IsType<MemoryLogger>(logWriter.Logger);

            logWriter.LogDebug(VerbosityMessages.DebugMessage);
            logWriter.LogInfo(VerbosityMessages.InfoMessage);
            logWriter.LogWarning(VerbosityMessages.WarningMessage);
            logWriter.LogError(VerbosityMessages.ErrorMessage);
            logWriter.LogImportant(VerbosityMessages.ImportantMessage);

            string result = logger.ToString().NormalizeNewLines();
            this.TestOutput.WriteLine($"Result (length: {result.Length}):");
            this.TestOutput.WriteLine(result);
            return result;
        }

        [Fact(Timeout = 5000)]
        public void TestLogWriterNoneVerbosity()
        {
            string result = this.WriteAllSeverityMessages(VerbosityLevel.None);
            string expected = StringExtensions.FormatLines(
                VerbosityMessages.ImportantMessage);
            Assert.Equal(expected, result);
        }

        [Fact(Timeout = 5000)]
        public void TestLogWriterErrorVerbosity()
        {
            string result = this.WriteAllSeverityMessages(VerbosityLevel.Error);
            string expected = StringExtensions.FormatLines(
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage);
            Assert.Equal(expected, result);
        }

        [Fact(Timeout = 5000)]
        public void TestLogWriterWarningVerbosity()
        {
            string result = this.WriteAllSeverityMessages(VerbosityLevel.Warning);
            string expected = StringExtensions.FormatLines(
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage);
            Assert.Equal(expected, result);
        }

        [Fact(Timeout = 5000)]
        public void TestLogWriterInfoVerbosity()
        {
            string result = this.WriteAllSeverityMessages(VerbosityLevel.Info);
            string expected = StringExtensions.FormatLines(
                VerbosityMessages.InfoMessage,
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage);
            Assert.Equal(expected, result);
        }

        [Fact(Timeout = 5000)]
        public void TestLogWriterDebugVerbosity()
        {
            string result = this.WriteAllSeverityMessages(VerbosityLevel.Debug);
            string expected = StringExtensions.FormatLines(
                VerbosityMessages.DebugMessage,
                VerbosityMessages.InfoMessage,
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage);
            Assert.Equal(expected, result);
        }

        [Fact(Timeout = 5000)]
        public void TestLogWriterExhaustiveVerbosity()
        {
            string result = this.WriteAllSeverityMessages(VerbosityLevel.Exhaustive);
            string expected = StringExtensions.FormatLines(
                VerbosityMessages.DebugMessage,
                VerbosityMessages.InfoMessage,
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage);
            Assert.Equal(expected, result);
        }

        private string WriteAllSeverityMessages(LogWriter logWriter)
        {
            using var stream = new MemoryStream();
            using (var interceptor = new ConsoleOutputInterceptor(stream))
            {
                logWriter.LogDebug(VerbosityMessages.DebugMessage);
                logWriter.LogInfo(VerbosityMessages.InfoMessage);
                logWriter.LogWarning(VerbosityMessages.WarningMessage);
                logWriter.LogError(VerbosityMessages.ErrorMessage);
                logWriter.LogImportant(VerbosityMessages.ImportantMessage);
            }

            string result = Encoding.UTF8.GetString(stream.ToArray()).NormalizeNewLines();
            this.TestOutput.WriteLine($"Result (length: {result.Length}):");
            this.TestOutput.WriteLine(result);
            return result;
        }

        [Fact(Timeout = 5000)]
        public void TestLogWriterConsoleOutput()
        {
            Configuration config = this.GetConfiguration().WithVerbosityEnabled().WithConsoleLoggingEnabled();
            using var logWriter = new LogWriter(config);
            Assert.IsType<ConsoleLogger>(logWriter.Logger);
            string result = this.WriteAllSeverityMessages(logWriter);
            string expected = StringExtensions.FormatLines(
                VerbosityMessages.InfoMessage,
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage);
            Assert.Equal(expected, result);
        }

        [Fact(Timeout = 5000)]
        public void TestLogWriterForceConsoleOutput()
        {
            Configuration config = this.GetConfiguration().WithVerbosityEnabled().WithConsoleLoggingEnabled(false);
            using var logWriter = new LogWriter(config, true);
            Assert.IsType<ConsoleLogger>(logWriter.Logger);
            string result = this.WriteAllSeverityMessages(logWriter);
            string expected = StringExtensions.FormatLines(
                VerbosityMessages.InfoMessage,
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage);
            Assert.Equal(expected, result);
        }

        [Fact(Timeout = 5000)]
        public void TestLogWriterNullOutput()
        {
            Configuration config = this.GetConfiguration().WithVerbosityEnabled();
            using var logWriter = new LogWriter(config);
            Assert.IsType<NullLogger>(logWriter.Logger);
            string result = this.WriteAllSeverityMessages(logWriter);
            Assert.Equal(string.Empty, result);
        }
    }
}
