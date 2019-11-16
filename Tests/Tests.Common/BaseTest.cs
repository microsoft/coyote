// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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

        protected static string RemoveNamespaceReferencesFromReport(string report)
        {
            report = Regex.Replace(report, @"Microsoft.Coyote.Tests.Common\.", string.Empty);
            return Regex.Replace(report, @"Microsoft\.[^+]*\+", string.Empty);
        }

        protected static string RemoveExcessiveEmptySpaceFromReport(string report)
        {
            return Regex.Replace(report, @"\s+", " ");
        }
    }
}
