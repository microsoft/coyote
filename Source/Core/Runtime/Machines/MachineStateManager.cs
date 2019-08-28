// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Manages the state of a machine.
    /// </summary>
    internal class MachineStateManager : IMachineStateManager
    {
        /// <summary>
        /// The runtime that executes the machine being managed.
        /// </summary>
        private readonly MachineRuntime Runtime;

        /// <summary>
        /// The machine being managed.
        /// </summary>
        private readonly Machine Machine;

        /// <summary>
        /// True if the event handler of the machine is running, else false.
        /// </summary>
        public bool IsEventHandlerRunning { get; set; }

        /// <summary>
        /// Id used to identify subsequent operations performed by the machine.
        /// </summary>
        public Guid OperationGroupId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineStateManager"/> class.
        /// </summary>
        internal MachineStateManager(MachineRuntime runtime, Machine machine, Guid operationGroupId)
        {
            this.Runtime = runtime;
            this.Machine = machine;
            this.IsEventHandlerRunning = true;
            this.OperationGroupId = operationGroupId;
        }

        /// <summary>
        /// Returns the cached state of the machine.
        /// </summary>
        public int GetCachedState() => 0;

        /// <summary>
        /// Checks if the specified event is ignored in the current machine state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEventIgnoredInCurrentState(Event e, Guid opGroupId, EventInfo eventInfo) =>
            this.Machine.IsEventIgnoredInCurrentState(e);

        /// <summary>
        /// Checks if the specified event is deferred in the current machine state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEventDeferredInCurrentState(Event e, Guid opGroupId, EventInfo eventInfo) =>
            this.Machine.IsEventDeferredInCurrentState(e);

        /// <summary>
        /// Checks if a default handler is installed in the current machine state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDefaultHandlerInstalledInCurrentState() => this.Machine.IsDefaultHandlerInstalledInCurrentState();

        /// <summary>
        /// Notifies the machine that an event has been enqueued.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnEnqueueEvent(Event e, Guid opGroupId, EventInfo eventInfo) =>
            this.Runtime.Logger.OnEnqueue(this.Machine.Id, e.GetType().FullName);

        /// <summary>
        /// Notifies the machine that an event has been raised.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRaiseEvent(Event e, Guid opGroupId, EventInfo eventInfo) =>
            this.Runtime.NotifyRaisedEvent(this.Machine, e, eventInfo);

        /// <summary>
        /// Notifies the machine that it is waiting to receive an event of one of the specified types.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnWaitEvent(IEnumerable<Type> eventTypes) =>
            this.Runtime.NotifyWaitEvent(this.Machine, eventTypes);

        /// <summary>
        /// Notifies the machine that an event it was waiting to receive has been enqueued.
        /// </summary>
        public void OnReceiveEvent(Event e, Guid opGroupId, EventInfo eventInfo)
        {
            if (opGroupId != Guid.Empty)
            {
                // Inherit the operation group id of the receive operation, if it is non-empty.
                this.OperationGroupId = opGroupId;
            }

            this.Runtime.NotifyReceivedEvent(this.Machine, e, eventInfo);
        }

        /// <summary>
        /// Notifies the machine that an event it was waiting to receive was already in the
        /// event queue when the machine invoked the receive statement.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnReceiveEventWithoutWaiting(Event e, Guid opGroupId, EventInfo eventInfo)
        {
            if (opGroupId != Guid.Empty)
            {
                // Inherit the operation group id of the receive operation, if it is non-empty.
                this.OperationGroupId = opGroupId;
            }

            this.Runtime.NotifyReceivedEventWithoutWaiting(this.Machine, e, eventInfo);
        }

        /// <summary>
        /// Notifies the machine that an event has been dropped.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnDropEvent(Event e, Guid opGroupId, EventInfo eventInfo) =>
            this.Runtime.TryHandleDroppedEvent(e, this.Machine.Id);

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assert(bool predicate, string s, object arg0) => this.Runtime.Assert(predicate, s, arg0);

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assert(bool predicate, string s, object arg0, object arg1) => this.Runtime.Assert(predicate, s, arg0, arg1);

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assert(bool predicate, string s, object arg0, object arg1, object arg2) =>
            this.Runtime.Assert(predicate, s, arg0, arg1, arg2);

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assert(bool predicate, string s, params object[] args) => this.Runtime.Assert(predicate, s, args);
    }
}
