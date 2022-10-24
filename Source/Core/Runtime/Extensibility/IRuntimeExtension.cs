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
        /// Returns the extended <see cref="CoverageInfo"/> data structure, if there is any.
        /// </summary>
        CoverageInfo GetCoverageInfo();

        /// <summary>
        /// Returns a task that completes once all operations managed by the extension reach quiescence.
        /// </summary>
        Task WaitUntilQuiescenceAsync();
    }
}
