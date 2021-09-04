// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// The synchronization context where controlled operations are executed.
    /// </summary>
    internal sealed class OperationSynchronizationContext : SynchronizationContext, IDisposable
    {
        /// <summary>
        /// Responsible for controlling the execution of operations during systematic testing.
        /// </summary>
        private CoyoteRuntime Runtime;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationSynchronizationContext"/> class.
        /// </summary>
        internal OperationSynchronizationContext(CoyoteRuntime runtime)
        {
            this.Runtime = runtime;
        }

        /// <inheritdoc/>
        public override void Post(SendOrPostCallback d, object state)
        {
            try
            {
                this.Runtime?.Schedule(() => d(state));
            }
            catch (ThreadInterruptedException)
            {
                // Ignore the thread interruption.
            }
        }

        /// <inheritdoc/>
        public override SynchronizationContext CreateCopy() => this;

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Runtime = null;
        }
    }
}
