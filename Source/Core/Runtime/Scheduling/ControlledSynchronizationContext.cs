// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// The synchronization context where controlled operations are executed.
    /// </summary>
    internal sealed class ControlledSynchronizationContext : SynchronizationContext, IDisposable
    {
        /// <summary>
        /// Responsible for controlling the execution of operations during systematic testing.
        /// </summary>
        internal CoyoteRuntime Runtime { get; private set; }

        /// <summary>
        /// The operation scheduling policy used by the runtime.
        /// </summary>
        internal SchedulingPolicy SchedulingPolicy => this.Runtime?.SchedulingPolicy ?? SchedulingPolicy.None;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledSynchronizationContext"/> class.
        /// </summary>
        internal ControlledSynchronizationContext(CoyoteRuntime runtime)
        {
            this.Runtime = runtime;
        }

        /// <inheritdoc/>
        public override void Post(SendOrPostCallback d, object state)
        {
            try
            {
                IO.Debug.WriteLine("<CoyoteDebug> Posting callback from thread '{0}'.",
                    Thread.CurrentThread.ManagedThreadId);
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
