// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Coyote.Rewriting.Types.Runtime.CompilerServices;

namespace Microsoft.Coyote.Rewriting.Types.Threading.Tasks
{
    /// <summary>
    /// Configures async enumeration and disposal using controllable awaitables.
    /// </summary>
    /// <remarks>This type is intended for compiler use only.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class TaskAsyncEnumerableExtensions
    {
        /// <summary>
        /// Configures context capture when disposing an async resource.
        /// </summary>
        public static ConfiguredAsyncDisposable ConfigureAwait(IAsyncDisposable source, bool continueOnCapturedContext) =>
            new ConfiguredAsyncDisposable(source, continueOnCapturedContext);

        /// <summary>
        /// Configures context capture when iterating an async enumerable.
        /// </summary>
        public static ConfiguredCancelableAsyncEnumerable<T> ConfigureAwait<T>(
            IAsyncEnumerable<T> source, bool continueOnCapturedContext) =>
            new ConfiguredCancelableAsyncEnumerable<T>(source, continueOnCapturedContext, default);

        /// <summary>
        /// Configures the cancellation token used when iterating an async enumerable.
        /// </summary>
        public static ConfiguredCancelableAsyncEnumerable<T> WithCancellation<T>(
            IAsyncEnumerable<T> source, CancellationToken cancellationToken) =>
            new ConfiguredCancelableAsyncEnumerable<T>(source, true, cancellationToken);
    }
}
#endif
