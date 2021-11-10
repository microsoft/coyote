// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Actors.Mocks
{
    /// <summary>
    /// Implements a queue of events that can be used during systematic testing.
    /// </summary>
    /// <remarks>
    /// This is not a thread-safe queue.
    /// </remarks>
    internal class MockEventQueue : IEventQueue
    {
        /// <summary>
        /// The actor that owns this queue.
        /// </summary>
        private readonly Actor Owner;

        /// <summary>
        /// The backing queue that contains events with their metadata.
        /// </summary>
        private readonly LinkedList<(Event e, EventGroup eventGroup, EventInfo info)> Queue;

        /// <summary>
        /// The raised event and its metadata, or null if no event has been raised.
        /// </summary>
        private (Event e, EventGroup eventGroup, EventInfo info) RaisedEvent;

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
        /// A lock that synchronizes accesses to the queue.
        /// </summary>
        private readonly SemaphoreSlim Lock;

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
        /// Initializes a new instance of the <see cref="MockEventQueue"/> class.
        /// </summary>
        internal MockEventQueue(Actor owner)
        {
            this.Owner = owner;
            this.Queue = new LinkedList<(Event, EventGroup, EventInfo)>();
            this.Lock = new SemaphoreSlim(1, 1);
            this.IsClosed = false;
        }

        /// <inheritdoc/>
        public EnqueueStatus Enqueue(Event e, EventGroup eventGroup, EventInfo info)
        {
            Console.WriteLine($"Try Enqueue: {e}");
            EnqueueStatus enqueueStatus = EnqueueStatus.EventHandlerRunning;
            this.Lock.Wait();
            try
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
                    this.Queue.AddLast((e, eventGroup, info));
                    if (info.Assert >= 0)
                    {
                        var eventCount = this.Queue.Count(val => val.e.GetType().Equals(e.GetType()));
                        this.Assert(eventCount <= info.Assert,
                            "There are more than {0} instances of '{1}' in the input queue of {2}.",
                            info.Assert, info.EventName, this.Owner.Id);
                    }

                    if (!this.IsEventHandlerRunning)
                    {
                        if (this.TryDequeueEvent(true).e is null)
                        {
                            enqueueStatus = EnqueueStatus.NextEventUnavailable;
                        }
                        else
                        {
                            this.IsEventHandlerRunning = true;
                            enqueueStatus = EnqueueStatus.EventHandlerNotRunning;
                        }
                    }
                }
            }
            finally
            {
                this.Lock.Release();
            }

            if (enqueueStatus is EnqueueStatus.Received)
            {
                this.OnReceiveEvent(e, eventGroup, info);
                this.ReceiveCompletionSource.SetResult(e);
                enqueueStatus = EnqueueStatus.EventHandlerRunning;
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
                    this.OnIgnoreEvent(this.RaisedEvent.e, this.RaisedEvent.eventGroup, this.RaisedEvent.info);
                    this.RaisedEvent = default;
                }
                else
                {
                    (Event e, EventGroup eventGroup, EventInfo info) raisedEvent = this.RaisedEvent;
                    this.RaisedEvent = default;
                    return (DequeueStatus.Raised, raisedEvent.e, raisedEvent.eventGroup, raisedEvent.info);
                }
            }

            // Make sure this happens before a potential dequeue.
            var hasDefaultHandler = this.IsDefaultHandlerAvailable();
            Console.WriteLine($"Try Dequeue");
            this.Lock.Wait();
            try
            {
                // Try to dequeue the next event, if there is one.
                var (e, eventGroup, info) = this.TryDequeueEvent();
                if (e != null)
                {
                    // Found next event that can be dequeued.
                    return (DequeueStatus.Success, e, eventGroup, info);
                }

                // No event can be dequeued, so check if there is a default event handler.
                if (!hasDefaultHandler)
                {
                    // There is no default event handler installed, so do not return an event. Setting the
                    // IsEventHandlerRunning field must happen inside the lock as it needs to be synchronized
                    // with the enqueue and starting a new event handler.
                    this.IsEventHandlerRunning = false;
                    return (DequeueStatus.Unavailable, null, null, null);
                }
            }
            finally
            {
                this.Lock.Release();
            }

            // TODO: check op-id of default event.
            // A default event handler exists.
            string stateName = this.Owner is StateMachine stateMachine ?
                NameResolver.GetStateNameForLogging(stateMachine.CurrentState) : string.Empty;
            var eventOrigin = new EventOriginInfo(this.Owner.Id, this.Owner.GetType().FullName, stateName);
            return (DequeueStatus.Default, DefaultEvent.Instance, null, new EventInfo(DefaultEvent.Instance, eventOrigin));
        }

        /// <summary>
        /// Dequeues the next event and its metadata, if there is one available, else returns null.
        /// </summary>
        private (Event e, EventGroup eventGroup, EventInfo info) TryDequeueEvent(bool checkOnly = false)
        {
            // Try to dequeue the next event, if there is one.
            var node = this.Queue.First;
            while (node != null)
            {
                // Iterates through the events and metadata in the inbox.
                var currentEvent = node.Value;
                if (this.IsEventIgnored(currentEvent.e))
                {
                    var nextNode = node.Next;
                    if (!checkOnly)
                    {
                        // Removes an ignored event.
                        this.Queue.Remove(node);
                        this.OnIgnoreEvent(currentEvent.e, currentEvent.eventGroup, currentEvent.info);
                    }

                    node = nextNode;
                    continue;
                }
                else if (this.IsEventDeferred(currentEvent.e))
                {
                    // Skips a deferred event.
                    this.OnDeferEvent(currentEvent.e, currentEvent.eventGroup, currentEvent.info);
                    node = node.Next;
                    continue;
                }

                if (!checkOnly)
                {
                    // Found next event that can be dequeued.
                    this.Queue.Remove(node);
                }

                return currentEvent;
            }

            return default;
        }

        /// <inheritdoc/>
        public void RaiseEvent(Event e, EventGroup eventGroup)
        {
            string stateName = this.Owner is StateMachine stateMachine ?
                NameResolver.GetStateNameForLogging(stateMachine.CurrentState) : string.Empty;
            var eventOrigin = new EventOriginInfo(this.Owner.Id, this.Owner.GetType().FullName, stateName);
            var info = new EventInfo(e, eventOrigin);
            this.RaisedEvent = (e, eventGroup, info);
            this.OnRaiseEvent(e, eventGroup, info);
        }

        /// <inheritdoc/>
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
            this.OnReceiveInvoked();

            (Event e, EventGroup eventGroup, EventInfo info) receivedEvent = default;
            this.Lock.Wait();
            try
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
            finally
            {
                this.Lock.Release();
            }

            if (receivedEvent == default)
            {
                // Note that the EventWaitTypes field is racy, so should not be accessed outside
                // the lock, this is why we access eventWaitTypes instead.
                this.OnWaitEvent(eventWaitTypes.Keys);
                return this.ReceiveCompletionSource.Task;
            }

            this.OnReceiveEventWithoutWaiting(receivedEvent.e, receivedEvent.eventGroup, receivedEvent.info);
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
        protected virtual bool IsDefaultHandlerAvailable()
        {
            bool result = this.Owner.IsDefaultHandlerInstalled();
            if (result)
            {
                this.Owner.Context.Runtime.ScheduleNextOperation(Runtime.AsyncOperationType.Default);
            }

            return result;
        }

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
        /// Notifies the actor that <see cref="ReceiveEventAsync(Type[])"/> or one of its overloaded methods was invoked.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void OnReceiveInvoked() => this.Owner.OnReceiveInvoked();

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

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an exception.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void Assert(bool predicate, string s, object arg0, object arg1, object arg2) =>
            this.Owner.Context.Assert(predicate, s, arg0, arg1, arg2);

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void Close()
        {
            this.Lock.Wait();
            try
            {
                this.IsClosed = true;
            }
            finally
            {
                this.Lock.Release();
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

            foreach (var (e, g, info) in this.Queue)
            {
                this.OnDropEvent(e, g, info);
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
