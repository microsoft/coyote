﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Runtime.Tests.Logging
{
    public class ConsoleLoggerTests : BaseRuntimeTest
    {
        public ConsoleLoggerTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private string WriteAllSeverityMessages(VerbosityLevel level)
        {
            using var stream = new MemoryStream();
            using var logger = new ConsoleLogger(level);
            using (var interceptor = new ConsoleOutputInterceptor(stream))
            {
                logger.WriteLine(LogSeverity.Debug, VerbosityMessages.DebugMessage);
                logger.WriteLine(LogSeverity.Info, VerbosityMessages.InfoMessage);
                logger.WriteLine(LogSeverity.Warning, VerbosityMessages.WarningMessage);
                logger.WriteLine(LogSeverity.Error, VerbosityMessages.ErrorMessage);
                logger.WriteLine(LogSeverity.Important, VerbosityMessages.ImportantMessage);
            }

            string result = Encoding.UTF8.GetString(stream.ToArray()).NormalizeNewLines();
            this.TestOutput.WriteLine($"Result (length: {result.Length}):");
            this.TestOutput.WriteLine(result);
            return result;
        }

        [Fact(Timeout = 5000)]
        public void TestConsoleLoggerNoneVerbosity()
        {
            string result = this.WriteAllSeverityMessages(VerbosityLevel.None);
            string expected = StringExtensions.FormatLines(
                VerbosityMessages.ImportantMessage);
            Assert.Equal(expected, result);
        }

        [Fact(Timeout = 5000)]
        public void TestConsoleLoggerErrorVerbosity()
        {
            string result = this.WriteAllSeverityMessages(VerbosityLevel.Error);
            string expected = StringExtensions.FormatLines(
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage);
            Assert.Equal(expected, result);
        }

        [Fact(Timeout = 5000)]
        public void TestConsoleLoggerWarningVerbosity()
        {
            string result = this.WriteAllSeverityMessages(VerbosityLevel.Warning);
            string expected = StringExtensions.FormatLines(
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage);
            Assert.Equal(expected, result);
        }

        [Fact(Timeout = 5000)]
        public void TestConsoleLoggerInfoVerbosity()
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
        public void TestConsoleLoggerDebugVerbosity()
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
        public void TestConsoleLoggerExhaustiveVerbosity()
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
    }
}
