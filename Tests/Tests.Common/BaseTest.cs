// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Tests.Common
{
    public abstract class BaseTest
    {
        protected readonly ITestOutputHelper TestOutput;

        public BaseTest(ITestOutputHelper output)
        {
            this.TestOutput = output;
        }

        protected static string RemoveNonDeterministicValuesFromReport(string report)
        {
            // Match a GUID or other ids (since they can be nondeterministic).
            report = Regex.Replace(report, @"\'[0-9|a-z|A-Z|-]{36}\'|\'[0-9]+\'|\'<unknown>\'", "''");
            report = Regex.Replace(report, @"\([^)]*\)", "()");
            report = Regex.Replace(report, @"\[[^)]*\]", "[]");

            // Match a namespace.
            return RemoveNamespaceReferencesFromReport(report);
        }

        protected static string SortLines(string text)
        {
            var list = new List<string>(text.Split('\n'));
            list.Sort();
            return string.Join("\n", list);
        }

        protected static string RemoveNamespaceReferencesFromReport(string report)
        {
            report = Regex.Replace(report, @"Microsoft.Coyote.Tests.Common\.", string.Empty);
            return Regex.Replace(report, @"Microsoft\.[^+]*\+", string.Empty);
        }

        protected static string RemoveExcessiveEmptySpaceFromReport(string report)
        {
            return Regex.Replace(report, @"\s+", " ");
        }

        protected static string RemoveStackTraceFromReport(string report,
            string removeUntilContainsText = "Microsoft.Coyote.SystematicTesting.Tests")
        {
            StringBuilder result = new StringBuilder();
            bool strip = false;
            foreach (var line in report.Split('\n'))
            {
                string trimmed = line.Trim('\r');
                string nows = trimmed.Trim();
                if (nows.StartsWith("<StackTrace>"))
                {
                    result.AppendLine("<StackTrace> ");
                    strip = true;
                }
                else if (strip && string.IsNullOrEmpty(nows))
                {
                    strip = false;
                    continue;
                }

                if (!strip)
                {
                    result.AppendLine(trimmed);
                }
                else if (strip && trimmed.Contains(removeUntilContainsText))
                {
                    result.AppendLine(trimmed);
                }
            }

            return result.ToString();
        }

        protected static string RemoveStackTraceFromXmlReport(string report)
        {
            StringBuilder result = new StringBuilder();
            bool strip = false;
            foreach (var line in report.Split('\n'))
            {
                string trimmed = line.Trim('\r');
                string nows = trimmed.Trim();
                if (nows.StartsWith("<AssertionFailure>&lt;StackTrace&gt;"))
                {
                    result.AppendLine("  <AssertionFailure>StackTrace:");
                    strip = true;
                }
                else if (strip && nows.StartsWith("</AssertionFailure>"))
                {
                    result.AppendLine("  </AssertionFailure>");
                    strip = false;
                    continue;
                }

                if (!strip)
                {
                    result.AppendLine(trimmed);
                }
                else if (strip && trimmed.Contains("Microsoft.Coyote.SystematicTesting.Tests"))
                {
                    result.AppendLine(trimmed);
                }
            }

            return result.ToString();
        }
    }
}
