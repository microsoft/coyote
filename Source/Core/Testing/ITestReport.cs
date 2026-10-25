// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Coyote.Testing
{
    /// <summary>
    /// Interface of a test report.
    /// </summary>
    internal interface ITestReport
    {
        /// <summary>
        /// Sets the specified scheduling statistics.
        /// </summary>
        void SetSchedulingStatistics(bool isBugFound, string bugReport, int numOperations, int concurrencyDegree,
            int groupingDegree, int scheduledSteps, bool isMaxScheduledStepsBoundReached, bool isScheduleFair);

        /// <summary>
        /// Sets the specified unhandled exception.
        /// </summary>
        void SetUnhandledException(Exception exception);

        /// <summary>
        /// Sets the specified uncontrolled invocations.
        /// </summary>
        void SetUncontrolledInvocations(HashSet<string> invocations);

        /// <summary>
        /// Records uncontrolled concurrency, including when its source is unknown.
        /// </summary>
        void SetUncontrolledConcurrencyDetected(bool isDetected);

        /// <summary>
        /// Records an execution path that was interrupted without being completed.
        /// </summary>
        void SetExecutionInterrupted(bool isInterrupted);

        /// <summary>
        /// Records an error reported by the scheduling strategy.
        /// </summary>
        void SetInternalError(string error);
    }
}
