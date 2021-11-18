// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Coyote.IO;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Implements a report with uncontrolled invocations.
    /// </summary>
    internal class UncontrolledInvocationsReport
    {
        /// <summary>
        /// Converts the specified set of uncontrolled invocations to JSON.
        /// </summary>
        internal static string ToJSON(HashSet<string> uncontrolledInvocations)
        {
            var report = new UncontrolledInvocationsJsonReport();
            report.UncontrolledInvocations = new List<string>(uncontrolledInvocations);

            // TODO: replace with the new 'System.Text.Json' when .NET 5 comes out.
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(UncontrolledInvocationsJsonReport));
                var stream = new MemoryStream();
                serializer.WriteObject(stream, report);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw new InvalidOperationException($"Unexpected JSON format.\n{ex.Message}");
            }
        }

        /// <summary>
        /// Implements an uncontrolled invocations JSON report object.
        /// </summary>
        [DataContract]
        private class UncontrolledInvocationsJsonReport
        {
            [DataMember(Name = "UncontrolledInvocations")]
            internal List<string> UncontrolledInvocations { get; set; }
        }
    }
}
