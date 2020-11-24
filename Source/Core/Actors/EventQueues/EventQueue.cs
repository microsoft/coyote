// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Implements a queue of events.
    /// </summary>
    internal class EventQueue : IEventQueue
    {
        /// <summary>
        /// The actor that owns this queue.
        /// </summary>
        private readonly Actor Owner;

        /// <summary>
        /// The backing queue.
        /// </summary>
        private readonly LinkedList<(Event e, EventGroup eventGroup)> Queue;

        /// <summary>
        /// The raised event and its metadata, or null if no event has been raised.
        /// </summary>
        private (Event e, EventGroup eventGroup) RaisedEvent;

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

        /// <inheritdoc/>
        public int Size => this.Queue.Count;

        /// <inheritdoc/>
        public bool IsEventRaised => this.RaisedEvent != default;

        /// <summary>
        /// True if the event handler is currently running, else false.
        /// </summary>
        protected virtual bool IsEventHandlerRunning
        {
            get => this.Owner.IsEventHandlerRunning;

            set
            {
                this.Owner.IsEventHandlerRunning = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventQueue"/> class.
        /// </summary>
        internal EventQueue(Actor owner)
        {
            this.Owner = owner;
            this.Queue = new LinkedList<(Event, EventGroup)>();
            this.IsClosed = false;
        }

        /// <inheritdoc/>
        public EnqueueStatus Enqueue(Event e, EventGroup eventGroup, EventInfo info)
        {
            EnqueueStatus enqueueStatus = EnqueueStatus.EventHandlerRunning;
            lock (this.Queue)
            {
                if (this.IsClosed)
                {
                    return EnqueueStatus.Dropped;
                }

                if (this.EventWaitTypes != null &&
                    this.EventWaitTypes.TryGetValue(e.GetType(), out Func<Event, bool> predicate) &&
                    (predicate is null || predicate(e)))
                {
                    this.EventWaitTypes = null;
                    enqueueStatus = EnqueueStatus.Received;
                }
                else
                {
                    this.Queue.AddLast((e, eventGroup));
                    if (!this.IsEventHandlerRunning)
                    {
                        this.IsEventHandlerRunning = true;
                        enqueueStatus = EnqueueStatus.EventHandlerNotRunning;
                    }
                }
            }

            if (enqueueStatus is EnqueueStatus.Received)
            {
                this.OnReceiveEvent(e, eventGroup, info);
                this.ReceiveCompletionSource.SetResult(e);
                return enqueueStatus;
            }
            else
            {
                this.OnEnqueueEvent(e, eventGroup, info);
            }

            return enqueueStatus;
        }

        /// <inheritdoc/>
        public (DequeueStatus status, Event e, EventGroup eventGroup, EventInfo info) Dequeue()
        {
            // Try to get the raised event, if there is one. Raised events
            // have priority over the events in the inbox.
            if (this.RaisedEvent != default)
            {
                if (this.IsEventIgnored(this.RaisedEvent.e))
                {
                    // TODO: should the user be able to raise an ignored event?
                    // The raised event is ignored in the current state.
                    this.OnIgnoreEvent(this.RaisedEvent.e, this.RaisedEvent.eventGroup, null);
                    this.RaisedEvent = default;
                }
                else
                {
                    (Event e, EventGroup eventGroup) = this.RaisedEvent;
                    this.RaisedEvent = default;
                    return (DequeueStatus.Raised, e, eventGroup, null);
                }
            }

            lock (this.Queue)
            {
                // Try to dequeue the next event, if there is one.
                var node = this.Queue.First;
                while (node != null)
                {
                    // Iterates through the events in the inbox.
                    if (this.IsEventIgnored(node.Value.e))
                    {
                        // Removes an ignored event.
                        var nextNode = node.Next;
                        this.Queue.Remove(node);
                        this.OnIgnoreEvent(node.Value.e, node.Value.eventGroup, null);
                        node = nextNode;
                        continue;
                    }
                    else if (this.IsEventDeferred(node.Value.e))
                    {
                        // Skips a deferred event.
                        this.OnDeferEvent(node.Value.e, node.Value.eventGroup, null);
                        node = node.Next;
                        continue;
                    }

                    // Found next event that can be dequeued.
                    this.Queue.Remove(node);
                    return (DequeueStatus.Success, node.Value.e, node.Value.eventGroup, null);
                }

                // No event can be dequeued, so check if there is a default event handler.
                if (!this.IsDefaultHandlerAvailable())
                {
                    // There is no default event handler installed, so do not return an event.
                    // Setting IsEventHandlerRunning must happen inside the lock as it needs
                    // to be synchronized with the enqueue and starting a new event handler.
                    this.IsEventHandlerRunning = false;
                    return (DequeueStatus.NotAvailable, null, null, null);
                }
            }

            // TODO: check op-id of default event.
            // A default event handler exists.
            return (DequeueStatus.Default, DefaultEvent.Instance, null, null);
        }

        /// <inheritdoc/>
        public void RaiseEvent(Event e, EventGroup eventGroup = null)
        {
            this.RaisedEvent = (e, eventGroup);
            this.OnRaiseEvent(e, eventGroup, null);
        }

        //// <inheritdoc/>
        public Task<Event> ReceiveEventAsync(Type eventType, Func<Event, bool> predicate = null)
        {
            var eventWaitTypes = new Dictionary<Type, Func<Event, bool>>
            {
                { eventType, predicate }
            };

            return this.ReceiveEventAsync(eventWaitTypes);
        }

        /// <inheritdoc/>
        public Task<Event> ReceiveEventAsync(params Type[] eventTypes)
        {
            var eventWaitTypes = new Dictionary<Type, Func<Event, bool>>();
            foreach (var type in eventTypes)
            {
                eventWaitTypes.Add(type, null);
            }

            return this.ReceiveEventAsync(eventWaitTypes);
        }

        /// <inheritdoc/>
        public Task<Event> ReceiveEventAsync(params Tuple<Type, Func<Event, bool>>[] events)
        {
            var eventWaitTypes = new Dictionary<Type, Func<Event, bool>>();
            foreach (var e in events)
            {
                eventWaitTypes.Add(e.Item1, e.Item2);
            }

            return this.ReceiveEventAsync(eventWaitTypes);
        }

        /// <summary>
        /// Waits for an event to be enqueued based on the conditions defined in the event wait types.
        /// </summary>
        private Task<Event> ReceiveEventAsync(Dictionary<Type, Func<Event, bool>> eventWaitTypes)
        {
            (Event e, EventGroup eventGroup) receivedEvent = default;
            lock (this.Queue)
            {
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
                }
            }

            if (receivedEvent == default)
            {
                // Note that EventWaitTypes is racy, so should not be accessed outside
                // the lock, this is why we access eventWaitTypes instead.
                this.OnWaitEvent(eventWaitTypes.Keys);
                return this.ReceiveCompletionSource.Task;
            }

            this.OnReceiveEventWithoutWaiting(receivedEvent.e, receivedEvent.eventGroup, null);
            return Task.FromResult(receivedEvent.e);
        }

        /// <summary>
        /// Checks if the specified event is currently ignored.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual bool IsEventIgnored(Event e) => this.Owner.IsEventIgnored(e);

        /// <summary>
        /// Checks if the specified event is currently deferred.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual bool IsEventDeferred(Event e) => this.Owner.IsEventDeferred(e);

        /// <summary>
        /// Checks if a default handler is currently available.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual bool IsDefaultHandlerAvailable() => this.Owner.IsDefaultHandlerInstalled();

        /// <summary>
        /// Notifies the actor that an event has been enqueued.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void OnEnqueueEvent(Event e, EventGroup eventGroup, EventInfo eventInfo) =>
            this.Owner.OnEnqueueEvent(e, eventGroup, eventInfo);

        /// <summary>
        /// Notifies the actor that an event has been raised.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void OnRaiseEvent(Event e, EventGroup eventGroup, EventInfo eventInfo) =>
            this.Owner.OnRaiseEvent(e, eventGroup, eventInfo);

        /// <summary>
        /// Notifies the actor that it is waiting to receive an event of one of the specified types.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void OnWaitEvent(IEnumerable<Type> eventTypes) => this.Owner.OnWaitEvent(eventTypes);

        /// <summary>
        /// Notifies the actor that an event it was waiting to receive has been enqueued.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void OnReceiveEvent(Event e, EventGroup eventGroup, EventInfo eventInfo) =>
            this.Owner.OnReceiveEvent(e, eventGroup, eventInfo);

        /// <summary>
        /// Notifies the actor that an event it was waiting to receive was already in the
        /// event queue when the actor invoked the receive statement.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void OnReceiveEventWithoutWaiting(Event e, EventGroup eventGroup, EventInfo eventInfo) =>
            this.Owner.OnReceiveEventWithoutWaiting(e, eventGroup, eventInfo);

        /// <summary>
        /// Notifies the actor that an event has been ignored.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void OnIgnoreEvent(Event e, EventGroup eventGroup, EventInfo eventInfo) => this.Owner.OnIgnoreEvent(e);

        /// <summary>
        /// Notifies the actor that an event has been deferred.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void OnDeferEvent(Event e, EventGroup eventGroup, EventInfo eventInfo) => this.Owner.OnDeferEvent(e);

        /// <summary>
        /// Notifies the actor that an event has been dropped.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void OnDropEvent(Event e, EventGroup eventGroup, EventInfo eventInfo) =>
            this.Owner.OnDropEvent(e, eventInfo);

        //// <inheritdoc/>
        public int GetCachedState() => 0;

        /// <inheritdoc/>
        public void Close()
        {
            lock (this.Queue)
            {
                this.IsClosed = true;
            }
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

            foreach (var (e, g) in this.Queue)
            {
                this.OnDropEvent(e, g, null);
            }

            this.Queue.Clear();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
