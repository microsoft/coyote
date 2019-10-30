// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Utilities;

namespace Microsoft.Coyote.TestingServices.Runtime
{
    /// <summary>
    /// Implements a queue of events that is used by a serialized machine during testing.
    /// </summary>
    internal sealed class SerializedMachineEventQueue : IEventQueue
    {
        /// <summary>
        /// Manages the state of the machine that owns this queue.
        /// </summary>
        private readonly IMachineStateManager MachineStateManager;

        /// <summary>
        /// The machine that owns this queue.
        /// </summary>
        private readonly StateMachine Machine;

        /// <summary>
        /// The internal queue that contains events with their metadata.
        /// </summary>
        private readonly LinkedList<(Event e, Guid opGroupId, EventInfo info)> Queue;

        /// <summary>
        /// The raised event and its metadata, or null if no event has been raised.
        /// </summary>
        private (Event e, Guid opGroupId, EventInfo info) RaisedEvent;

        /// <summary>
        /// Map from the types of events that the owner of the queue is waiting to receive
        /// to an optional predicate. If an event of one of these types is enqueued, then
        /// if there is no predicate, or if there is a predicate and evaluates to true, then
        /// the event is received, else the event is deferred.
        /// </summary>
        private Dictionary<Type, Func<Event, bool>> EventWaitTypes;

        /// <summary>
        /// Task completion source that contains the event obtained using an explicit receive.
        /// </summary>
        private TaskCompletionSource<Event> ReceiveCompletionSource;

        /// <summary>
        /// Checks if the queue is accepting new events.
        /// </summary>
        private bool IsClosed;

        /// <summary>
        /// The size of the queue.
        /// </summary>
        public int Size => this.Queue.Count;

        /// <summary>
        /// Checks if an event has been raised.
        /// </summary>
        public bool IsEventRaised => this.RaisedEvent != default;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializedMachineEventQueue"/> class.
        /// </summary>
        internal SerializedMachineEventQueue(IMachineStateManager machineStateManager, StateMachine machine)
        {
            this.MachineStateManager = machineStateManager;
            this.Machine = machine;
            this.Queue = new LinkedList<(Event, Guid, EventInfo)>();
            this.EventWaitTypes = new Dictionary<Type, Func<Event, bool>>();
            this.IsClosed = false;
        }

        /// <summary>
        /// Enqueues the specified event and its optional metadata.
        /// </summary>
        public EnqueueStatus Enqueue(Event e, Guid opGroupId, EventInfo info)
        {
            if (this.IsClosed)
            {
                return EnqueueStatus.Dropped;
            }

            if (this.EventWaitTypes.TryGetValue(e.GetType(), out Func<Event, bool> predicate) &&
                (predicate is null || predicate(e)))
            {
                this.EventWaitTypes.Clear();
                this.MachineStateManager.OnReceiveEvent(e, opGroupId, info);
                this.ReceiveCompletionSource.SetResult(e);
                return EnqueueStatus.EventHandlerRunning;
            }

            this.MachineStateManager.OnEnqueueEvent(e, opGroupId, info);
            this.Queue.AddLast((e, opGroupId, info));

            if (info.Assert >= 0)
            {
                var eventCount = this.Queue.Count(val => val.e.GetType().Equals(e.GetType()));
                this.MachineStateManager.Assert(eventCount <= info.Assert,
                    "There are more than {0} instances of '{1}' in the input queue of machine '{2}'.",
                    info.Assert, info.EventName, this.Machine.Id);
            }

            if (info.Assume >= 0)
            {
                var eventCount = this.Queue.Count(val => val.e.GetType().Equals(e.GetType()));
                this.MachineStateManager.Assert(eventCount <= info.Assume,
                    "There are more than {0} instances of '{1}' in the input queue of machine '{2}'.",
                    info.Assume, info.EventName, this.Machine.Id);
            }

            if (!this.MachineStateManager.IsEventHandlerRunning)
            {
                if (this.TryDequeueEvent(true).e is null)
                {
                    return EnqueueStatus.NextEventUnavailable;
                }
                else
                {
                    this.MachineStateManager.IsEventHandlerRunning = true;
                    return EnqueueStatus.EventHandlerNotRunning;
                }
            }

            return EnqueueStatus.EventHandlerRunning;
        }

        /// <summary>
        /// Dequeues the next event, if there is one available.
        /// </summary>
        public (DequeueStatus status, Event e, Guid opGroupId, EventInfo info) Dequeue()
        {
            // Try to get the raised event, if there is one. Raised events
            // have priority over the events in the inbox.
            if (this.RaisedEvent != default)
            {
                if (this.MachineStateManager.IsEventIgnoredInCurrentState(this.RaisedEvent.e, this.RaisedEvent.opGroupId, this.RaisedEvent.info))
                {
                    // TODO: should the user be able to raise an ignored event?
                    // The raised event is ignored in the current state.
                    this.RaisedEvent = default;
                }
                else
                {
                    (Event e, Guid opGroupId, EventInfo info) raisedEvent = this.RaisedEvent;
                    this.RaisedEvent = default;
                    return (DequeueStatus.Raised, raisedEvent.e, raisedEvent.opGroupId, raisedEvent.info);
                }
            }

            var hasDefaultHandler = this.MachineStateManager.IsDefaultHandlerInstalledInCurrentState();
            if (hasDefaultHandler)
            {
                this.Machine.Runtime.NotifyDefaultEventHandlerCheck(this.Machine);
            }

            // Try to dequeue the next event, if there is one.
            var (e, opGroupId, info) = this.TryDequeueEvent();
            if (e != null)
            {
                // Found next event that can be dequeued.
                return (DequeueStatus.Success, e, opGroupId, info);
            }

            // No event can be dequeued, so check if there is a default event handler.
            if (!hasDefaultHandler)
            {
                // There is no default event handler installed, so do not return an event.
                this.MachineStateManager.IsEventHandlerRunning = false;
                return (DequeueStatus.NotAvailable, null, Guid.Empty, null);
            }

            // TODO: check op-id of default event.
            // A default event handler exists.
            var eventOrigin = new EventOriginInfo(this.Machine.Id, this.Machine.GetType().FullName,
                NameResolver.GetStateNameForLogging(this.Machine.CurrentState));
            return (DequeueStatus.Default, Default.Event, Guid.Empty, new EventInfo(Default.Event, eventOrigin));
        }

        /// <summary>
        /// Dequeues the next event and its metadata, if there is one available, else returns null.
        /// </summary>
        private (Event e, Guid opGroupId, EventInfo info) TryDequeueEvent(bool checkOnly = false)
        {
            (Event, Guid, EventInfo) nextAvailableEvent = default;

            // Iterates through the events and metadata in the inbox.
            var node = this.Queue.First;
            while (node != null)
            {
                var nextNode = node.Next;
                var currentEvent = node.Value;

                if (this.MachineStateManager.IsEventIgnoredInCurrentState(currentEvent.e, currentEvent.opGroupId, currentEvent.info))
                {
                    if (!checkOnly)
                    {
                        // Removes an ignored event.
                        this.Queue.Remove(node);
                    }

                    node = nextNode;
                    continue;
                }

                // Skips a deferred event.
                if (!this.MachineStateManager.IsEventDeferredInCurrentState(currentEvent.e, currentEvent.opGroupId, currentEvent.info))
                {
                    nextAvailableEvent = currentEvent;
                    if (!checkOnly)
                    {
                        this.Queue.Remove(node);
                    }

                    break;
                }

                node = nextNode;
            }

            return nextAvailableEvent;
        }

        /// <summary>
        /// Enqueues the specified raised event.
        /// </summary>
        public void Raise(Event e, Guid opGroupId)
        {
            var eventOrigin = new EventOriginInfo(this.Machine.Id, this.Machine.GetType().FullName,
                NameResolver.GetStateNameForLogging(this.Machine.CurrentState));
            var info = new EventInfo(e, eventOrigin);
            this.RaisedEvent = (e, opGroupId, info);
            this.MachineStateManager.OnRaiseEvent(e, opGroupId, info);
        }

        /// <summary>
        /// Waits to receive an event of the specified type that satisfies an optional predicate.
        /// </summary>
        public Task<Event> ReceiveAsync(Type eventType, Func<Event, bool> predicate = null)
        {
            var eventWaitTypes = new Dictionary<Type, Func<Event, bool>>
            {
                { eventType, predicate }
            };

            return this.ReceiveAsync(eventWaitTypes);
        }

        /// <summary>
        /// Waits to receive an event of the specified types.
        /// </summary>
        public Task<Event> ReceiveAsync(params Type[] eventTypes)
        {
            var eventWaitTypes = new Dictionary<Type, Func<Event, bool>>();
            foreach (var type in eventTypes)
            {
                eventWaitTypes.Add(type, null);
            }

            return this.ReceiveAsync(eventWaitTypes);
        }

        /// <summary>
        /// Waits to receive an event of the specified types that satisfy the specified predicates.
        /// </summary>
        public Task<Event> ReceiveAsync(params Tuple<Type, Func<Event, bool>>[] events)
        {
            var eventWaitTypes = new Dictionary<Type, Func<Event, bool>>();
            foreach (var e in events)
            {
                eventWaitTypes.Add(e.Item1, e.Item2);
            }

            return this.ReceiveAsync(eventWaitTypes);
        }

        /// <summary>
        /// Waits for an event to be enqueued.
        /// </summary>
        private Task<Event> ReceiveAsync(Dictionary<Type, Func<Event, bool>> eventWaitTypes)
        {
            this.Machine.Runtime.NotifyReceiveCalled(this.Machine);

            (Event e, Guid opGroupId, EventInfo info) receivedEvent = default;
            var node = this.Queue.First;
            while (node != null)
            {
                // Dequeue the first event that the caller waits to receive, if there is one in the queue.
                if (eventWaitTypes.TryGetValue(node.Value.e.GetType(), out Func<Event, bool> predicate) &&
                    (predicate is null || predicate(node.Value.e)))
                {
                    receivedEvent = node.Value;
                    this.Queue.Remove(node);
                    break;
                }

                node = node.Next;
            }

            if (receivedEvent == default)
            {
                this.ReceiveCompletionSource = new TaskCompletionSource<Event>();
                this.EventWaitTypes = eventWaitTypes;
                this.MachineStateManager.OnWaitEvent(this.EventWaitTypes.Keys);
                return this.ReceiveCompletionSource.Task;
            }

            this.MachineStateManager.OnReceiveEventWithoutWaiting(receivedEvent.e, receivedEvent.opGroupId, receivedEvent.info);
            return Task.FromResult(receivedEvent.e);
        }

        /// <summary>
        /// Returns the cached state of the queue.
        /// </summary>
        public int GetCachedState()
        {
            unchecked
            {
                var hash = 19;
                foreach (var (_, _, info) in this.Queue)
                {
                    hash = (hash * 31) + info.EventName.GetHashCode();
                    if (info.HashedState != 0)
                    {
                        // Adds the user-defined hashed event state.
                        hash = (hash * 31) + info.HashedState;
                    }
                }

                return hash;
            }
        }

        /// <summary>
        /// Closes the queue, which stops any further event enqueues.
        /// </summary>
        public void Close()
        {
            this.IsClosed = true;
        }

        /// <summary>
        /// Disposes the queue resources.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            foreach (var (e, opGroupId, info) in this.Queue)
            {
                this.MachineStateManager.OnDropEvent(e, opGroupId, info);
            }

            this.Queue.Clear();
        }

        /// <summary>
        /// Disposes the queue resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
