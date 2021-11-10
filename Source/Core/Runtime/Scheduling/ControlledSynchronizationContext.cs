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
                IO.Debug.WriteLine("<ScheduleDebug> Posting callback from thread '{0}'.",
                    Thread.CurrentThread.ManagedThreadId);
                this.Runtime?.Schedule(() => d(state));
            }
            catch (ThreadInterruptedException)
            {
                // Ignore the thread interruption.
            }
        }

        /// <inheritdoc/>
        public override void Send(SendOrPostCallback d, object state)
        {
            try
            {
                IO.Debug.WriteLine("\n\n\n\n>>>>> <ScheduleDebug> Sending callback from thread '{0}'\n\n\n\n.",
                    Thread.CurrentThread.ManagedThreadId);
                // this.Runtime?.Schedule(() => d(state));
                base.Send(d, state);
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
