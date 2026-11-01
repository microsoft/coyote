// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET
using System;

namespace Microsoft.Coyote.Rewriting.Types.Runtime.CompilerServices
{
    /// <summary>
    /// Provides an async disposable whose configured await can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use only.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public readonly struct ConfiguredAsyncDisposable
    {
        private readonly IAsyncDisposable Disposable;
        private readonly bool ContinueOnCapturedContext;

        internal ConfiguredAsyncDisposable(IAsyncDisposable disposable, bool continueOnCapturedContext)
        {
            this.Disposable = disposable;
            this.ContinueOnCapturedContext = continueOnCapturedContext;
        }

        /// <summary>
        /// Disposes the underlying resource.
        /// </summary>
        public ConfiguredValueTaskAwaitable DisposeAsync()
        {
            var task = this.Disposable.DisposeAsync();
            return new ConfiguredValueTaskAwaitable(ref task, this.ContinueOnCapturedContext);
        }
    }
}
#endif
