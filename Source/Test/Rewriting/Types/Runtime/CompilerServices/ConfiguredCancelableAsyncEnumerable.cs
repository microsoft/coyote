// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Coyote.Rewriting.Types.Runtime.CompilerServices
{
    /// <summary>
    /// Provides an async enumerable whose configured awaits can be controlled during testing.
    /// </summary>
    /// <typeparam name="T">The type of each element.</typeparam>
    /// <remarks>This type is intended for compiler use only.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public readonly struct ConfiguredCancelableAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> Enumerable;
        private readonly CancellationToken CancellationToken;
        private readonly bool ContinueOnCapturedContext;

        internal ConfiguredCancelableAsyncEnumerable(IAsyncEnumerable<T> enumerable,
            bool continueOnCapturedContext, CancellationToken cancellationToken)
        {
            this.Enumerable = enumerable;
            this.CancellationToken = cancellationToken;
            this.ContinueOnCapturedContext = continueOnCapturedContext;
        }

        /// <summary>
        /// Configures continuation context capture for iteration and disposal.
        /// </summary>
        public ConfiguredCancelableAsyncEnumerable<T> ConfigureAwait(bool continueOnCapturedContext) =>
            new ConfiguredCancelableAsyncEnumerable<T>(this.Enumerable, continueOnCapturedContext, this.CancellationToken);

        /// <summary>
        /// Replaces the cancellation token passed to the underlying enumerable.
        /// </summary>
        public ConfiguredCancelableAsyncEnumerable<T> WithCancellation(CancellationToken cancellationToken) =>
            new ConfiguredCancelableAsyncEnumerable<T>(this.Enumerable, this.ContinueOnCapturedContext, cancellationToken);

        /// <summary>
        /// Creates the configured enumerator.
        /// </summary>
        public Enumerator GetAsyncEnumerator() =>
            new Enumerator(this.Enumerable.GetAsyncEnumerator(this.CancellationToken), this.ContinueOnCapturedContext);

        /// <summary>
        /// Wraps iteration and disposal in controllable configured awaitables.
        /// </summary>
        public readonly struct Enumerator
        {
            private readonly IAsyncEnumerator<T> AsyncEnumerator;
            private readonly bool ContinueOnCapturedContext;

            internal Enumerator(IAsyncEnumerator<T> enumerator, bool continueOnCapturedContext)
            {
                this.AsyncEnumerator = enumerator;
                this.ContinueOnCapturedContext = continueOnCapturedContext;
            }

            /// <summary>
            /// Gets the current element.
            /// </summary>
            public T Current => this.AsyncEnumerator.Current;

            /// <summary>
            /// Advances the underlying enumerator.
            /// </summary>
            public ConfiguredValueTaskAwaitable<bool> MoveNextAsync()
            {
                var task = this.AsyncEnumerator.MoveNextAsync();
                return new ConfiguredValueTaskAwaitable<bool>(ref task, this.ContinueOnCapturedContext);
            }

            /// <summary>
            /// Disposes the underlying enumerator.
            /// </summary>
            public ConfiguredValueTaskAwaitable DisposeAsync()
            {
                var task = this.AsyncEnumerator.DisposeAsync();
                return new ConfiguredValueTaskAwaitable(ref task, this.ContinueOnCapturedContext);
            }
        }
    }
}
#endif
