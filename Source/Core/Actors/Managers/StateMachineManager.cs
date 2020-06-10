﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Manages a state machine in production.
    /// </summary>
    internal class StateMachineManager : IActorManager
    {
        /// <summary>
        /// The runtime that executes the state machine being managed.
        /// </summary>
        private readonly ActorRuntime Runtime;

        /// <summary>
        /// The state machine being managed.
        /// </summary>
        private readonly StateMachine Instance;

        /// <inheritdoc/>
        public bool IsEventHandlerRunning { get; set; }

        private Operation op;

        /// <inheritdoc/>
        public Operation CurrentOperation
        {
            get
            {
                return this.op;
            }

            set
            {
                if (value == null)
                {
                    Console.WriteLine("debug me!");
                }

                this.op = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StateMachineManager"/> class.
        /// </summary>
        internal StateMachineManager(ActorRuntime runtime, StateMachine instance, Operation op)
        {
            this.Runtime = runtime;
            this.Instance = instance;
            this.IsEventHandlerRunning = true;
            this.CurrentOperation = op;
        }

        /// <inheritdoc/>
        public int GetCachedState() => 0;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEventIgnored(Event e, EventInfo eventInfo) =>
            this.Instance.IsEventIgnoredInCurrentState(e);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEventDeferred(Event e, EventInfo eventInfo) =>
            this.Instance.IsEventDeferredInCurrentState(e);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDefaultHandlerAvailable() => this.Instance.IsDefaultHandlerInstalledInCurrentState();

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnEnqueueEvent(Event e, Operation op, EventInfo eventInfo) =>
            this.Runtime.LogWriter.LogEnqueueEvent(this.Instance.Id, e);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRaiseEvent(Event e, Operation op, EventInfo eventInfo) =>
            this.Runtime.NotifyRaisedEvent(this.Instance, e, eventInfo);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnWaitEvent(IEnumerable<Type> eventTypes) =>
            this.Runtime.NotifyWaitEvent(this.Instance, eventTypes);

        /// <inheritdoc/>
        public void OnReceiveEvent(Event e, Operation op, EventInfo eventInfo)
        {
            // Inherit the operation group id of the receive operation, if it is non-empty.
            this.CurrentOperation = op;
            this.Runtime.NotifyReceivedEvent(this.Instance, e, eventInfo);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnReceiveEventWithoutWaiting(Event e, Operation op, EventInfo eventInfo)
        {
            // Inherit the operation group id of the receive operation, if it is non-empty.
            this.CurrentOperation = op;
            this.Runtime.NotifyReceivedEventWithoutWaiting(this.Instance, e, eventInfo);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnDropEvent(Event e, Operation op, EventInfo eventInfo) =>
            this.Runtime.TryHandleDroppedEvent(e, this.Instance.Id);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assert(bool predicate, string s, object arg0) => this.Runtime.Assert(predicate, s, arg0);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assert(bool predicate, string s, object arg0, object arg1) => this.Runtime.Assert(predicate, s, arg0, arg1);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assert(bool predicate, string s, object arg0, object arg1, object arg2) =>
            this.Runtime.Assert(predicate, s, arg0, arg1, arg2);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assert(bool predicate, string s, params object[] args) => this.Runtime.Assert(predicate, s, args);
    }
}
