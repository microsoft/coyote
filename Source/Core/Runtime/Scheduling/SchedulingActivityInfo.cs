// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Defines scheduling activity information that can be used to monitor test execution.
    /// </summary>
    internal class SchedulingActivityInfo
    {
        /// <summary>
        /// Number of created operations since last activity check.
        /// </summary>
        public int OperationCount { get; set; }

        /// <summary>
        /// Number of scheduling steps since last activity check.
        /// </summary>
        public int StepCount { get; set; }
    }
}
