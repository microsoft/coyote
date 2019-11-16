// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Manages an actor in production.
    /// </summary>
    internal class ActorManager : IActorManager
    {
        /// <summary>
        /// The runtime that executes the actor being managed.
        /// </summary>
        private readonly CoyoteRuntime Runtime;

        /// <summary>
        /// The actor being managed.
        /// </summary>
        private readonly Actor Instance;

        /// <summary>
        /// True if the event handler of the actor is running, else false.
        /// </summary>
        public bool IsEventHandlerRunning { get; set; }

        /// <summary>
        /// Id used to identify subsequent operations performed by the actor.
        /// </summary>
        public Guid OperationGroupId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorManager"/> class.
        /// </summary>
        internal ActorManager(CoyoteRuntime runtime, Actor instance, Guid operationGroupId)
        {
            this.Runtime = runtime;
            this.Instance = instance;
            this.IsEventHandlerRunning = true;
            this.OperationGroupId = operationGroupId;
        }

        /// <summary>
        /// Returns the cached state of the actor.
        /// </summary>
        public int GetCachedState() => 0;

        /// <summary>
        /// Checks if the specified event is currently ignored.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEventIgnored(Event e, Guid opGroupId, EventInfo eventInfo) => this.Instance.IsEventIgnored(e);

        /// <summary>
        /// Checks if the specified event is currently deferred.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEventDeferred(Event e, Guid opGroupId, EventInfo eventInfo) => this.Instance.IsEventDeferred(e);

        /// <summary>
        /// Checks if a default handler is currently available.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDefaultHandlerAvailable() => this.Instance.IsDefaultHandlerAvailable;

        /// <summary>
        /// Notifies the actor that an event has been enqueued.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnEnqueueEvent(Event e, Guid opGroupId, EventInfo eventInfo) =>
            this.Runtime.LogWriter.LogEnqueueEvent(this.Instance.Id, e.GetType().FullName);

        /// <summary>
        /// Notifies the actor that an event has been raised.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRaiseEvent(Event e, Guid opGroupId, EventInfo eventInfo) =>
            this.Runtime.LogWriter.LogRaiseEvent(this.Instance.Id, default, e.GetType().FullName);

        /// <summary>
        /// Notifies the actor that it is waiting to receive an event of one of the specified types.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnWaitEvent(IEnumerable<Type> eventTypes) =>
            this.Runtime.NotifyWaitEvent(this.Instance, eventTypes);

        /// <summary>
        /// Notifies the actor that an event it was waiting to receive has been enqueued.
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
        /// Notifies the actor that an event it was waiting to receive was already in the
        /// event queue when the actor invoked the receive statement.
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
        /// Notifies the actor that an event has been dropped.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnDropEvent(Event e, Guid opGroupId, EventInfo eventInfo) =>
            this.Runtime.TryHandleDroppedEvent(e, this.Instance.Id);

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
