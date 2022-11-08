// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using SyncMonitor = System.Threading.Monitor;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Resource that can be used to synchronize asynchronous operations.
    /// </summary>
    internal struct SynchronizedSection : IDisposable
    {
        /// <summary>
        /// Per-thread variable that specifies whether the current thread has
        /// entered the critical section or not.
        /// </summary>
        [ThreadStatic]
        private static bool IsEntered;

        /// <summary>
        /// Object that is used to synchronize access to the section.
        /// </summary>
        private readonly object SyncObject;

        /// <summary>
        /// The locking action to perform.
        /// </summary>
        private readonly LockingAction Action;

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizedSection"/> struct.
        /// </summary>
        private SynchronizedSection(object syncObject, bool isEntering)
        {
            this.SyncObject = syncObject;
            if (isEntering)
            {
                this.Action = IsEntered ? LockingAction.None : LockingAction.Acquire;
            }
            else
            {
                this.Action = IsEntered ? LockingAction.Release :
                    throw new InvalidOperationException("Cannot release a runtime lock that is not acquired.");
            }
        }

        /// <summary>
        /// Enters the synchronized section that is guarded by the specified synchronization object.
        /// When the synchronized section gets disposed, the thread will automatically exit it.
        /// </summary>
        internal static SynchronizedSection Enter(object syncObject) =>
            new SynchronizedSection(syncObject, true).InvokeAction();

        /// <summary>
        /// Exits the synchronized section that is guarded by the specified synchronization object.
        /// When the synchronized section gets disposed, the thread will automatically enter it.
        /// </summary>
        internal static SynchronizedSection Exit(object syncObject) =>
            new SynchronizedSection(syncObject, false).InvokeAction();

        /// <summary>
        /// Invokes the locking action on the synchronized section.
        /// </summary>
        private SynchronizedSection InvokeAction()
        {
            if (this.Action is LockingAction.Acquire)
            {
                this.Enter();
            }
            else if (this.Action is LockingAction.Release)
            {
                this.Exit();
            }

            return this;
        }

        /// <summary>
        /// Enters the synchronized section.
        /// </summary>
        private void Enter()
        {
            SyncMonitor.Enter(this.SyncObject);
            IsEntered = true;
        }

        /// <summary>
        /// Exits the synchronized section.
        /// </summary>
        private void Exit()
        {
            IsEntered = false;
            SyncMonitor.Exit(this.SyncObject);
        }

        /// <summary>
        /// Releases any held resources.
        /// </summary>
        public void Dispose()
        {
            if (this.Action is LockingAction.Acquire)
            {
                this.Exit();
            }
            else if (this.Action is LockingAction.Release)
            {
                this.Enter();
            }
        }

        /// <summary>
        /// The locking action to perform.
        /// </summary>
        private enum LockingAction
        {
            None,
            Acquire,
            Release
        }
    }
}
