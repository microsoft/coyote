// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.TestingServices.Runtime
{
    /// <summary>
    /// Implements a state machine manager that is used during testing.
    /// </summary>
    internal sealed class MockStateMachineManager : IActorManager
    {
        /// <summary>
        /// The runtime that executes the state machine being managed.
        /// </summary>
        private readonly SystematicTestingRuntime Runtime;

        /// <summary>
        /// The state machine being managed.
        /// </summary>
        private readonly StateMachine Instance;

        /// <summary>
        /// True if the event handler of the state machine is running, else false.
        /// </summary>
        public bool IsEventHandlerRunning { get; set; }

        /// <summary>
        /// Id used to identify subsequent operations performed by the state machine.
        /// </summary>
        public Guid OperationGroupId { get; set; }

        /// <summary>
        /// Program counter used for state-caching. Distinguishes
        /// scheduling from non-deterministic choices.
        /// </summary>
        internal int ProgramCounter;

        /// <summary>
        /// True if a transition statement was called in the current action, else false.
        /// </summary>
        internal bool IsTransitionStatementCalledInCurrentAction;

        /// <summary>
        /// True if the state machine is executing an on exit action, else false.
        /// </summary>
        internal bool IsInsideOnExit;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockStateMachineManager"/> class.
        /// </summary>
        internal MockStateMachineManager(SystematicTestingRuntime runtime, StateMachine instance, Guid operationGroupId)
        {
            this.Runtime = runtime;
            this.Instance = instance;
            this.IsEventHandlerRunning = true;
            this.OperationGroupId = operationGroupId;
            this.ProgramCounter = 0;
            this.IsTransitionStatementCalledInCurrentAction = false;
            this.IsInsideOnExit = false;
        }

        /// <summary>
        /// Returns the cached state of the state machine.
        /// </summary>
        public int GetCachedState()
        {
            unchecked
            {
                var hash = 19;
                hash = (hash * 31) + this.IsEventHandlerRunning.GetHashCode();
                hash = (hash * 31) + this.ProgramCounter;
                return hash;
            }
        }

        /// <summary>
        /// Checks if the specified event is ignored in the current state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEventIgnored(Event e, Guid opGroupId, EventInfo eventInfo) =>
            this.Instance.IsEventIgnoredInCurrentState(e);

        /// <summary>
        /// Checks if the specified event is deferred in the current state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEventDeferred(Event e, Guid opGroupId, EventInfo eventInfo) =>
            this.Instance.IsEventDeferredInCurrentState(e);

        /// <summary>
        /// Checks if a default handler is installed in the current state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDefaultHandlerAvailable() => this.Instance.IsDefaultHandlerInstalledInCurrentState();

        /// <summary>
        /// Notifies the state machine that an event has been enqueued.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnEnqueueEvent(Event e, Guid opGroupId, EventInfo eventInfo) =>
            this.Runtime.LogWriter.LogEnqueueEvent(this.Instance.Id, e.GetType().FullName);

        /// <summary>
        /// Notifies the state machine that an event has been raised.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRaiseEvent(Event e, Guid opGroupId, EventInfo eventInfo) =>
            this.Runtime.NotifyRaisedEvent(this.Instance, e, eventInfo);

        /// <summary>
        /// Notifies the state machine that it is waiting to receive an event of one of the specified types.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnWaitEvent(IEnumerable<Type> eventTypes) =>
            this.Runtime.NotifyWaitEvent(this.Instance, eventTypes);

        /// <summary>
        /// Notifies the state machine that an event it was waiting to receive has been enqueued.
        /// </summary>
        public void OnReceiveEvent(Event e, Guid opGroupId, EventInfo eventInfo)
        {
            if (opGroupId != Guid.Empty)
            {
                // Inherit the operation group id of the receive operation, if it is non-empty.
                this.OperationGroupId = opGroupId;
            }

            this.Runtime.NotifyReceivedEvent(this.Instance, e, eventInfo);
        }

        /// <summary>
        /// Notifies the state machine that an event it was waiting to receive was already in the
        /// event queue when the state machine invoked the receive statement.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnReceiveEventWithoutWaiting(Event e, Guid opGroupId, EventInfo eventInfo)
        {
            if (opGroupId != Guid.Empty)
            {
                // Inherit the operation group id of the receive operation, if it is non-empty.
                this.OperationGroupId = opGroupId;
            }

            this.Runtime.NotifyReceivedEventWithoutWaiting(this.Instance, e, eventInfo);
        }

        /// <summary>
        /// Notifies the state machine that an event has been dropped.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnDropEvent(Event e, Guid opGroupId, EventInfo eventInfo)
        {
            this.Runtime.Assert(!eventInfo.MustHandle, "'{0}' halted before dequeueing must-handle event '{1}'.",
                this.Instance.Id, e.GetType().FullName);
            this.Runtime.TryHandleDroppedEvent(e, this.Instance.Id);
        }

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
