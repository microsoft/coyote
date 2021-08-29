// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// The synchronization context where controlled operations are executed.
    /// </summary>
    internal sealed class OperationSynchronizationContext : SynchronizationContext, IDisposable
    {
        /// <summary>
        /// Responsible for controlling the execution of tasks during systematic testing.
        /// </summary>
        private CoyoteRuntime Runtime;

        /// <summary>
        /// The original synchronization context.
        /// </summary>
        private readonly SynchronizationContext OriginalSynchronizationContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationSynchronizationContext"/> class.
        /// </summary>
        internal OperationSynchronizationContext(CoyoteRuntime runtime)
        {
            Console.WriteLine($"      SC: New OperationSynchronizationContext: thread-id: {Thread.CurrentThread.ManagedThreadId}; task-id: {Task.CurrentId}");
            this.Runtime = runtime;
            this.OriginalSynchronizationContext = Current;
            this.SetWaitNotificationRequired();
        }

        /// <inheritdoc/>
        public override void Send(SendOrPostCallback d, object state)
        {
            Console.WriteLine($"      SC: Send: thread-id: {Thread.CurrentThread.ManagedThreadId}; task-id: {Task.CurrentId}");
            base.Send((object s) =>
            {
                this.OperationStarted();
                d(s);
                this.OperationCompleted();
            }, state);
        }

        /// <inheritdoc/>
        public override void Post(SendOrPostCallback d, object state)
        {
            try
            {
                this.Runtime?.Schedule(d, state);
            }
            catch (ThreadInterruptedException)
            {
                // Ignore the thread interruption.
                Console.WriteLine($">>>>>>>>>>>> SC: ThreadInterrupted: thread-id: {Thread.CurrentThread.ManagedThreadId}; task-id: {Task.CurrentId}");
            }
        }

        /// <inheritdoc/>
        public override void OperationStarted()
        {
            Console.WriteLine($"      SC: OperationStarted: thread-id: {Thread.CurrentThread.ManagedThreadId}; task-id: {Task.CurrentId}");
        }

        /// <inheritdoc/>
        public override void OperationCompleted()
        {
            Console.WriteLine($"      SC: OperationCompleted: thread-id: {Thread.CurrentThread.ManagedThreadId}; task-id: {Task.CurrentId}");
        }

        // /// <inheritdoc/>
        // public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        // {
        //     SetSynchronizationContext(this.OriginalSynchronizationContext);
        //     Console.WriteLine($"      SC: Wait: thread-id: {Thread.CurrentThread.ManagedThreadId}; task-id: {Task.CurrentId}");
        //     // Console.WriteLine($"      SC:   |_ waitHandles: {waitHandles.Length}");
        //
        //     // Console.WriteLine($"      SC: Waiting ... thread-id: {Thread.CurrentThread.ManagedThreadId}; task-id: {Task.CurrentId}");
        //
        //     // if (this.Runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
        //     // {
        //     //     // var op = this.Runtime.GetExecutingOperation<Operation>();
        //     //     // Console.WriteLine($"      SC: Wait: op: {op?.Name}");
        //     //     // op?.BlockUntilWaitHandlesComplete(waitHandles, waitAll);
        //     // }
        //     // else if (this.Runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing)
        //     // {
        //     //     this.Runtime.DelayOperation();
        //     // }
        //
        //     // Console.WriteLine($"      SC: Done waiting ... thread-id: {Thread.CurrentThread.ManagedThreadId}; task-id: {Task.CurrentId}");
        //
        //     var result = base.Wait(waitHandles, waitAll, millisecondsTimeout);
        //     SetSynchronizationContext(this);
        //     return result;
        // }

        /// <inheritdoc/>
        public override SynchronizationContext CreateCopy()
        {
            Console.WriteLine($"      SC: CreateCopy: thread-id: {Thread.CurrentThread.ManagedThreadId}; task-id: {Task.CurrentId}");
            return this;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Console.WriteLine($"      SC: Disposing: thread-id: {Thread.CurrentThread.ManagedThreadId}; task-id: {Task.CurrentId}");
            this.Runtime = null;
        }
    }
}
