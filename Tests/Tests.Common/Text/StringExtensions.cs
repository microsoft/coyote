// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Coyote.Tests.Common
{
    /// <summary>
    /// Provides methods for cleaning up test strings so they can be compared against expected results
    /// after eliminating stuff that is unstable across test runs, test machines or OS platforms.
    /// </summary>
    public static class StringExtensions
    {
        public static string SortLines(this string text)
        {
            var list = new List<string>(text.Split('\n'));
            list.Sort(StringComparer.Ordinal);
            return string.Join("\n", list);
        }

        public static string RemoveInstanceIds(this string actual) => Regex.Replace(actual, @"\([^)]*\)", "()");

        public static string RemoveExcessiveEmptySpace(this string text) => Regex.Replace(text, @"\s+", " ");

        public static string NormalizeNewLines(this string text) => Regex.Replace(text, "[\r\n]+", "\n");

        public static string RemoveNonDeterministicValues(this string text)
        {
            // Match a GUID or other ids (since they can be nondeterministic).
            text = Regex.Replace(text, @"\'[0-9|a-z|A-Z|-]{36}\'|\'[0-9]+\'|\'<unknown>\'", "''");
            text = RemoveInstanceIds(text);
            text = NormalizeNewLines(text);

            // Match a namespace.
            return RemoveNamespaceReferences(text).Trim();
        }

        public static string RemoveNamespaceReferences(this string text)
        {
            text = Regex.Replace(text, @"Microsoft.Coyote.Tests.Common\.", string.Empty);
            return Regex.Replace(text, @"Microsoft\.[^+]*\+", string.Empty);
        }

        public static string RemoveStackTrace(this string text,
            string removeUntilContainsText = "Microsoft.Coyote.SystematicTesting.Tests")
        {
            StringBuilder result = new StringBuilder();
            bool strip = false;
            foreach (var line in text.Split('\n'))
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

        public static string RemoveStackTraceFromXml(this string text)
        {
            StringBuilder result = new StringBuilder();
            bool strip = false;
            foreach (var line in text.Split('\n'))
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
