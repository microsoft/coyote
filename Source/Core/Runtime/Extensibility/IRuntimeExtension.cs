// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Coverage;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Interface for a Coyote runtime extension.
    /// </summary>
    public interface IRuntimeExtension : IDisposable
    {
        /// <summary>
        /// Runs the specified test entry point delegate and returns
        /// a task that completes when the test is completed.
        /// </summary>
        /// <param name="test">The test entry point delegate.</param>
        /// <param name="task">A task that completes when the test is completed.</param>
        /// <returns>True if the extension can execute the test, else false.</returns>
        bool RunTest(Delegate test, out Task task);

        /// <summary>
        /// Builds the extended <see cref="CoverageInfo"/>.
        /// </summary>
        /// <remarks>
        /// This information is only available when <see cref="Configuration.IsActivityCoverageReported"/> is enabled.
        /// </remarks>
        CoverageInfo BuildCoverageInfo();

        /// <summary>
        /// Returns the extended <see cref="CoverageInfo"/>.
        /// </summary>
        CoverageInfo GetCoverageInfo();

        /// <summary>
        /// Returns the <see cref="CoverageGraph"/> of the current execution.
        /// </summary>
        CoverageGraph GetCoverageGraph();

        /// <summary>
        /// Returns a task that completes once all operations managed by the extension reach quiescence.
        /// </summary>
        Task WaitUntilQuiescenceAsync();
    }
}
