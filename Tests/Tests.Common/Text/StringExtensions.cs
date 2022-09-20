// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.Coyote.Tests.Common
{
    /// <summary>
    /// Provides methods for cleaning up test strings so they can be compared against expected results
    /// after eliminating stuff that is unstable across test runs, test machines or OS platforms.
    /// </summary>
    public static class StringExtensions
    {
        public static string FormatNewLine(this string text) => text + "\n";
        public static string FormatLines(params string[] args) => string.Join("\n", args) + "\n";

        public static string SortLines(this string text)
        {
            var list = new List<string>(text.Split('\n'));
            list.Sort(StringComparer.Ordinal);
            list.RemoveAll(string.IsNullOrEmpty);
            return string.Join("\n", list);
        }

        public static string RemoveInstanceIds(this string text) => Regex.Replace(text, @"\([^)]*\)", "()");
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

        public static string RemoveDebugLines(this string text) =>
            Regex.Replace(text, @"^\[coyote::debug\].*\n", string.Empty, RegexOptions.Multiline);
    }
}
