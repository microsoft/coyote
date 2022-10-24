// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Coverage;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Implementation of a no-op runtime extension.
    /// </summary>
    internal class NullRuntimeExtension : IRuntimeExtension
    {
        /// <summary>
        /// Gets a cached <see cref="NullRuntimeExtension"/> instance.
        /// </summary>
        internal static NullRuntimeExtension Instance { get; } = new NullRuntimeExtension();

        /// <summary>
        /// Initializes a new instance of the <see cref="NullRuntimeExtension"/> class.
        /// </summary>
        private NullRuntimeExtension()
        {
        }

        /// <inheritdoc/>
        CoverageInfo IRuntimeExtension.GetCoverageInfo() => null;

        /// <inheritdoc/>
        Task IRuntimeExtension.WaitUntilQuiescenceAsync() => Task.CompletedTask;

        /// <inheritdoc/>
        public void Dispose()
        {
            // This should never be called.
            throw new InvalidOperationException("Dispose should never be called on the null runtime extension.");
        }
    }
}
