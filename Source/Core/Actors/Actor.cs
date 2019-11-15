// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;
using EventInfo = Microsoft.Coyote.Runtime.EventInfo;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Type that implements an actor. Inherit from this class to declare a custom actor.
    /// </summary>
    public abstract class Actor
    {
        /// <summary>
        /// Cache of actor types to a map of event types to action declarations.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Dictionary<Type, MethodInfo>> ActionCache =
            new ConcurrentDictionary<Type, Dictionary<Type, MethodInfo>>();

        /// <summary>
        /// Checks if the actor type declaration is cached.
        /// </summary>
        private protected static readonly ConcurrentDictionary<Type, bool> IsTypeDeclarationCached =
            new ConcurrentDictionary<Type, bool>();

        /// <summary>
        /// The runtime that executes this actor.
        /// </summary>
        internal CoyoteRuntime Runtime { get; private set; }

        /// <summary>
        /// Unique id that identifies this actor.
        /// </summary>
        protected internal ActorId Id { get; private set; }

        /// <summary>
        /// Manages the actor.
        /// </summary>
        internal IActorManager Manager { get; private set; }

        /// <summary>
        /// The inbox of the actor. Incoming events are enqueued here.
        /// Events are dequeued to be processed.
        /// </summary>
        private protected IEventQueue Inbox;

        /// <summary>
        /// Map from event types to cached action delegates.
        /// </summary>
        private readonly Dictionary<Type, CachedDelegate> ActionMap;

        /// <summary>
        /// Set of currently ignored event types.
        /// </summary>
        private readonly HashSet<Type> IgnoredEvents;

        /// <summary>
        /// Set of currently deferred event types.
        /// </summary>
        private readonly HashSet<Type> DeferredEvents;

        /// <summary>
        /// Map that contains the active timers.
        /// </summary>
        private protected readonly Dictionary<TimerInfo, IActorTimer> Timers;

        /// <summary>
        /// Checks if the actor halted.
        /// </summary>
        internal volatile bool IsHalted;

        /// <summary>
        /// Checks if a default handler is available.
        /// </summary>
        internal bool IsDefaultHandlerAvailable { get; private set; }

        /// <summary>
        /// Checks if <see cref="OnException"/> should suppress the exception
        /// and the actor gracefully halt.
        /// </summary>
        private protected bool IsSuppressingExceptionAndHalting;

        /// <summary>
        /// Gets the latest received <see cref="Event"/>, or null if
        /// no <see cref="Event"/> has been received.
        /// </summary>
        protected internal Event ReceivedEvent { get; private protected set; }

        /// <summary>
        /// Id used to identify subsequent operations performed by this actor. This value
        /// is initially either <see cref="Guid.Empty"/> or the <see cref="Guid"/> specified
        /// upon creation. This value is automatically set to the operation group id of the
        /// last dequeue or receive operation, if it is not <see cref="Guid.Empty"/>. This
        /// value can also be manually set using the property.
        /// </summary>
        protected internal virtual Guid OperationGroupId
        {
            get => this.Manager.OperationGroupId;

            set
            {
                this.Manager.OperationGroupId = value;
            }
        }

        /// <summary>
        /// The installed runtime logger.
        /// </summary>
        protected ILogger Logger => this.Runtime.Logger;

        /// <summary>
        /// User-defined hashed state of the actor. Override to improve the
        /// accuracy of liveness checking when state-caching is enabled.
        /// </summary>
        protected virtual int HashedState => 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="Actor"/> class.
        /// </summary>
        protected Actor()
        {
            this.ActionMap = new Dictionary<Type, CachedDelegate>();
            this.IgnoredEvents = new HashSet<Type>();
            this.DeferredEvents = new HashSet<Type>();
            this.Timers = new Dictionary<TimerInfo, IActorTimer>();
            this.IsHalted = false;
            this.IsDefaultHandlerAvailable = false;
            this.IsSuppressingExceptionAndHalting = false;
        }

        /// <summary>
        /// Configures the actor.
        /// </summary>
        internal void Configure(CoyoteRuntime runtime, ActorId id, IActorManager manager, IEventQueue inbox)
        {
            this.Runtime = runtime;
            this.Id = id;
            this.Manager = manager;
            this.Inbox = inbox;
        }

        /// <summary>
        /// Initializes the actor with the specified optional event.
        /// </summary>
        /// <param name="initialEvent">Optional event used for initialization.</param>
        internal virtual async Task InitializeAsync(Event initialEvent)
        {
            this.ReceivedEvent = initialEvent;

            try
            {
                try
                {
                    // Invoke the custom initializer.
                    Task task = this.OnInitializeAsync(initialEvent);
                    this.Runtime.NotifyWaitTask(this, task);
                    await task;
                }
                catch (Exception ex) when (this.OnExceptionHandler(ex, nameof(this.OnInitializeAsync)))
                {
                    // User handled the exception, return normally.
                }
            }
            catch (Exception ex)
            {
                await this.TryHandleActionInvocationExceptionAsync(ex, nameof(this.OnInitializeAsync));
            }
        }

        /// <summary>
        /// Creates a new actor of the specified type and with the specified optional
        /// <see cref="Event"/>. This <see cref="Event"/> can only be used to access
        /// its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the actor.</param>
        /// <param name="initialEvent">Optional initialization event.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>The unique actor id.</returns>
        protected ActorId CreateActor(Type type, Event initialEvent = null, Guid opGroupId = default) =>
            this.Runtime.CreateActor(null, type, null, initialEvent, this, opGroupId);

        /// <summary>
        /// Creates a new actor of the specified type and name, and with the specified
        /// optional <see cref="Event"/>. This <see cref="Event"/> can only be used to
        /// access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the actor.</param>
        /// <param name="name">Optional name used for logging.</param>
        /// <param name="initialEvent">Optional initialization event.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>The unique actor id.</returns>
        protected ActorId CreateActor(Type type, string name, Event initialEvent = null, Guid opGroupId = default) =>
            this.Runtime.CreateActor(null, type, name, initialEvent, this, opGroupId);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, using the specified
        /// unbound actor id, and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="id">Unbound actor id.</param>
        /// <param name="type">Type of the actor.</param>
        /// <param name="name">Optional name used for logging.</param>
        /// <param name="initialEvent">Optional initialization event.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        protected void CreateActor(ActorId id, Type type, string name, Event initialEvent = null, Guid opGroupId = default) =>
            this.Runtime.CreateActor(id, type, name, initialEvent, this, opGroupId);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a target.
        /// </summary>
        /// <param name="id">The id of the target.</param>
        /// <param name="e">The event to send.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <param name="options">Optional configuration of a send operation.</param>
        protected void SendEvent(ActorId id, Event e, Guid opGroupId = default, SendOptions options = null) =>
            this.Runtime.SendEvent(id, e, this, opGroupId, options);

        /// <summary>
        /// Raises an <see cref="Event"/> internally at the end of the current action.
        /// </summary>
        /// <param name="e">The event to raise.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        protected void RaiseEvent(Event e, Guid opGroupId = default)
        {
            this.Assert(!this.IsHalted, "'{0}' invoked RaiseEvent while halted.", this.Id);
            this.Assert(e != null, "'{0}' is raising a null event.", this.Id);

            // The operation group id of this operation is set using the following precedence:
            // (1) To the specified raise operation group id, if it is non-empty.
            // (2) To the operation group id of this actor.
            this.Inbox.RaiseEvent(e, opGroupId != Guid.Empty ? opGroupId : this.OperationGroupId);
        }

        /// <summary>
        /// Waits to receive an <see cref="Event"/> of the specified type
        /// that satisfies an optional predicate.
        /// </summary>
        /// <param name="eventType">The event type.</param>
        /// <param name="predicate">The optional predicate.</param>
        /// <returns>The received event.</returns>
        protected internal Task<Event> ReceiveEventAsync(Type eventType, Func<Event, bool> predicate = null)
        {
            this.Assert(!this.IsHalted, "'{0}' invoked ReceiveEventAsync while halted.", this.Id);
            this.Runtime.NotifyReceiveCalled(this);
            return this.Inbox.ReceiveEventAsync(eventType, predicate);
        }

        /// <summary>
        /// Waits to receive an <see cref="Event"/> of the specified types.
        /// </summary>
        /// <param name="eventTypes">The event types to wait for.</param>
        /// <returns>The received event.</returns>
        protected internal Task<Event> ReceiveEventAsync(params Type[] eventTypes)
        {
            this.Assert(!this.IsHalted, "'{0}' invoked ReceiveEventAsync while halted.", this.Id);
            this.Runtime.NotifyReceiveCalled(this);
            return this.Inbox.ReceiveEventAsync(eventTypes);
        }

        /// <summary>
        /// Waits to receive an <see cref="Event"/> of the specified types
        /// that satisfy the specified predicates.
        /// </summary>
        /// <param name="events">Event types and predicates.</param>
        /// <returns>The received event.</returns>
        protected internal Task<Event> ReceiveEventAsync(params Tuple<Type, Func<Event, bool>>[] events)
        {
            this.Assert(!this.IsHalted, "'{0}' invoked ReceiveEventAsync while halted.", this.Id);
            this.Runtime.NotifyReceiveCalled(this);
            return this.Inbox.ReceiveEventAsync(events);
        }

        /// <summary>
        /// Sets the actor to ignore all events of the the specified type. This can be reverted
        /// by setting the <paramref name="ignore"/> parameter to false.
        /// Ignoring of the
        /// </summary>
        /// <param name="eventType">The event type to ignore.</param>
        /// <param name="ignore">True to ignore events of the specified type, else false.</param>
        protected void IgnoreEvent(Type eventType, bool ignore = true)
        {
            this.Assert(eventType != null, "'{0}' is ignoring a null event type.", this.Id);

            if (ignore)
            {
                this.IgnoredEvents.Add(eventType);
            }
            else
            {
                this.IgnoredEvents.Remove(eventType);
            }
        }

        /// <summary>
        /// Sets the actor to defer all events of the the specified type. This can be reverted
        /// by setting the <paramref name="defer"/> parameter to false.
        /// Ignoring of the
        /// </summary>
        /// <param name="eventType">The event type to defer.</param>
        /// <param name="defer">True to defer events of the specified type, else false.</param>
        protected void DeferEvent(Type eventType, bool defer = true)
        {
            this.Assert(eventType != null, "'{0}' is deferring a null event type.", this.Id);

            if (defer)
            {
                this.DeferredEvents.Add(eventType);
            }
            else
            {
                this.DeferredEvents.Remove(eventType);
            }
        }

        /// <summary>
        /// Starts a timer that sends a <see cref="TimerElapsedEvent"/> to this actor after the
        /// specified due time. The timer accepts an optional payload to be used during timeout.
        /// The timer is automatically disposed after it timeouts. To manually stop and dispose
        /// the timer, invoke the <see cref="StopTimer"/> method.
        /// </summary>
        /// <param name="dueTime">The amount of time to wait before sending the first timeout event.</param>
        /// <param name="payload">Optional payload of the timeout event.</param>
        /// <returns>Handle that contains information about the timer.</returns>
        protected TimerInfo StartTimer(TimeSpan dueTime, object payload = null)
        {
            // The specified due time and period must be valid.
            this.Assert(dueTime.TotalMilliseconds >= 0, "'{0}' registered a timer with a negative due time.", this.Id);
            return this.RegisterTimer(dueTime, Timeout.InfiniteTimeSpan, payload);
        }

        /// <summary>
        /// Starts a periodic timer that sends a <see cref="TimerElapsedEvent"/> to this actor after
        /// the specified due time, and then repeats after each specified period. The timer accepts
        /// an optional payload to be used during timeout. The timer can be stopped by invoking the
        /// <see cref="StopTimer"/> method.
        /// </summary>
        /// <param name="dueTime">The amount of time to wait before sending the first timeout event.</param>
        /// <param name="period">The time interval between timeout events.</param>
        /// <param name="payload">Optional payload of the timeout event.</param>
        /// <returns>Handle that contains information about the timer.</returns>
        protected TimerInfo StartPeriodicTimer(TimeSpan dueTime, TimeSpan period, object payload = null)
        {
            // The specified due time and period must be valid.
            this.Assert(dueTime.TotalMilliseconds >= 0, "'{0}' registered a periodic timer with a negative due time.", this.Id);
            this.Assert(period.TotalMilliseconds >= 0, "'{0}' registered a periodic timer with a negative period.", this.Id);
            return this.RegisterTimer(dueTime, period, payload);
        }

        /// <summary>
        /// Stops and disposes the specified timer.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        protected void StopTimer(TimerInfo info)
        {
            this.Assert(info.OwnerId == this.Id, "'{0}' is not allowed to dispose timer '{1}', which is owned by '{2}'.",
                this.Id, info, info.OwnerId);
            this.UnregisterTimer(info);
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>The controlled nondeterministic choice.</returns>
        protected bool Random()
        {
            return this.Runtime.GetNondeterministicBooleanChoice(this, 2);
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing. The value is used
        /// to generate a number in the range [0..maxValue), where 0
        /// triggers true.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <returns>The controlled nondeterministic choice.</returns>
        protected bool Random(int maxValue)
        {
            return this.Runtime.GetNondeterministicBooleanChoice(this, maxValue);
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>The controlled nondeterministic choice.</returns>
        protected bool FairRandom(
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            var havocId = this.FormatFairRandom(callerMemberName, callerFilePath, callerLineNumber);
            return this.Runtime.GetFairNondeterministicBooleanChoice(this, havocId);
        }

        /// <summary>
        /// Returns a nondeterministic integer, that can be controlled during
        /// analysis or testing. The value is used to generate an integer in
        /// the range [0..maxValue).
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <returns>The controlled nondeterministic integer.</returns>
        protected int RandomInteger(int maxValue)
        {
            return this.Runtime.GetNondeterministicIntegerChoice(this, maxValue);
        }

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        /// <typeparam name="T">Type of the monitor.</typeparam>
        /// <param name="e">The event to send.</param>
        protected void Monitor<T>(Event e)
        {
            this.Monitor(typeof(T), e);
        }

        /// <summary>
        /// Invokes the specified monitor with the specified event.
        /// </summary>
        /// <param name="type">Type of the monitor.</param>
        /// <param name="e">The event to send.</param>
        protected void Monitor(Type type, Event e)
        {
            this.Assert(e != null, "'{0}' is sending a null event.", this.Id);
            this.Runtime.Monitor(type, this, e);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        protected void Assert(bool predicate)
        {
            this.Runtime.Assert(predicate);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        protected void Assert(bool predicate, string s, object arg0)
        {
            this.Runtime.Assert(predicate, s, arg0);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        protected void Assert(bool predicate, string s, object arg0, object arg1)
        {
            this.Runtime.Assert(predicate, s, arg0, arg1);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        protected void Assert(bool predicate, string s, object arg0, object arg1, object arg2)
        {
            this.Runtime.Assert(predicate, s, arg0, arg1, arg2);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        protected void Assert(bool predicate, string s, params object[] args)
        {
            this.Runtime.Assert(predicate, s, args);
        }

        /// <summary>
        /// Enqueues the specified event and its metadata.
        /// </summary>
        internal EnqueueStatus Enqueue(Event e, Guid opGroupId, EventInfo info)
        {
            if (this.IsHalted)
            {
                return EnqueueStatus.Dropped;
            }

            return this.Inbox.Enqueue(e, opGroupId, info);
        }

        /// <summary>
        /// Runs the event handler. The handler terminates if there is no next
        /// event to process or if the actor has halted.
        /// </summary>
        internal async Task RunEventHandlerAsync()
        {
            if (this.IsHalted)
            {
                return;
            }

            Event lastDequeuedEvent = null;
            while (!this.IsHalted && this.Runtime.IsRunning)
            {
                (DequeueStatus status, Event e, Guid opGroupId, EventInfo info) = this.Inbox.Dequeue();
                if (opGroupId != Guid.Empty)
                {
                    // Inherit the operation group id of the dequeue or raise operation, if it is non-empty.
                    this.Manager.OperationGroupId = opGroupId;
                }

                if (status is DequeueStatus.Success)
                {
                    // Notify the runtime for a new event to handle. This is only used
                    // during bug-finding and operation bounding, because the runtime
                    // has to schedule a actor when a new operation is dequeued.
                    this.Runtime.NotifyDequeuedEvent(this, e, info);
                }
                else if (status is DequeueStatus.Raised)
                {
                    this.Runtime.NotifyHandleRaisedEvent(this, e);
                }
                else if (status is DequeueStatus.Default)
                {
                    this.Runtime.LogWriter.OnDefaultEventHandler(this.Id,
                        this is StateMachine stateMachine ? stateMachine.CurrentStateName : default);

                    // If the default event was dequeued, then notify the runtime.
                    // This is only used during bug-finding, because the runtime must
                    // instrument a scheduling point between default event handlers.
                    this.Runtime.NotifyDefaultEventDequeued(this);
                }
                else if (status is DequeueStatus.NotAvailable)
                {
                    break;
                }

                // Assigns the received event.
                this.ReceivedEvent = e;

                if (status is DequeueStatus.Success)
                {
                    // Inform the user of a successful dequeue.
                    lastDequeuedEvent = e;
                    await this.InvokeUserCallbackAsync(EventHandlerStatus.EventDequeued, lastDequeuedEvent);
                }

                if (e is TimerElapsedEvent timeoutEvent &&
                    timeoutEvent.Info.Period.TotalMilliseconds < 0)
                {
                    // If the timer is not periodic, then dispose it.
                    this.UnregisterTimer(timeoutEvent.Info);
                }

                if (!this.IsHalted)
                {
                    // Handles the next event, if the actor is not halted.
                    await this.HandleEventAsync(e);
                }

                if (!this.Inbox.IsEventRaised && lastDequeuedEvent != null && !this.IsHalted)
                {
                    // Inform the user that the actor is done handling the current event.
                    // The actor will either go idle or dequeue its next event.
                    await this.InvokeUserCallbackAsync(EventHandlerStatus.EventHandled, lastDequeuedEvent);
                    lastDequeuedEvent = null;
                }
            }
        }

        /// <summary>
        /// Handles the specified <see cref="Event"/>.
        /// </summary>
        private protected virtual async Task HandleEventAsync(Event e)
        {
            if (this.ActionMap.ContainsKey(e.GetType()))
            {
                await this.InvokeAction(e.GetType());
            }
            else if (this.ActionMap.ContainsKey(typeof(WildCardEvent)))
            {
                await this.InvokeAction(typeof(WildCardEvent));
            }
            else if (e is HaltEvent)
            {
                // If the event is the halt event, then terminate the actor.
                await this.HaltAsync();
                return;
            }
            else
            {
                await this.InvokeUserCallbackAsync(EventHandlerStatus.EventUnhandled, e);
                if (this.IsHalted)
                {
                    // Invoking a user callback caused the actor to halt.
                    return;
                }

                var unhandledEx = new UnhandledEventException(e, default, "Unhandled Event");
                if (this.OnUnhandledEventExceptionHandler(nameof(this.HandleEventAsync), unhandledEx))
                {
                    await this.HaltAsync();
                    return;
                }
                else
                {
                    // If the event cannot be handled then report an error and exit.
                    this.Assert(false, "'{0}' received event '{1}' that cannot be handled.",
                        this.Id, e.GetType().FullName);
                }
            }
        }

        /// <summary>
        /// Invokes the action for the specified event type.
        /// </summary>
        private async Task InvokeAction(Type eventType)
        {
            CachedDelegate cachedAction = this.ActionMap[eventType];
            this.Runtime.NotifyInvokedAction(this, cachedAction.MethodInfo, this.ReceivedEvent);
            await this.InvokeActionAsync(cachedAction);
            this.Runtime.NotifyCompletedAction(this, cachedAction.MethodInfo, this.ReceivedEvent);
        }

        /// <summary>
        /// Invokes the specified action delegate.
        /// </summary>
        private protected async Task InvokeActionAsync(CachedDelegate cachedAction)
        {
            try
            {
                if (cachedAction.Handler is Action action)
                {
                    // Use an exception filter to call OnFailure before the stack has been unwound.
                    try
                    {
                        action();
                    }
                    catch (Exception ex) when (this.OnExceptionHandler(ex, cachedAction.MethodInfo.Name))
                    {
                        // User handled the exception, return normally.
                    }
                    catch (Exception ex) when (!this.IsSuppressingExceptionAndHalting && this.InvokeOnFailureExceptionFilter(cachedAction, ex))
                    {
                        // If the exception filter does not fail-fast, it returns
                        // false to process the exception normally.
                    }
                }
                else if (cachedAction.Handler is Func<Task> taskFunc)
                {
                    try
                    {
                        // We have no reliable stack for awaited operations.
                        Task task = taskFunc();
                        this.Runtime.NotifyWaitTask(this, task);
                        await task;
                    }
                    catch (Exception ex) when (this.OnExceptionHandler(ex, cachedAction.MethodInfo.Name))
                    {
                        // User handled the exception, return normally.
                    }
                }
            }
            catch (Exception ex)
            {
                await this.TryHandleActionInvocationExceptionAsync(ex, cachedAction.MethodInfo.Name);
            }
        }

        /// <summary>
        /// Invokes the specified event handler user callback.
        /// </summary>
        private protected async Task InvokeUserCallbackAsync(EventHandlerStatus eventHandlerStatus,
            Event lastDequeuedEvent, string currentState = default)
        {
            try
            {
                if (eventHandlerStatus is EventHandlerStatus.EventDequeued)
                {
                    try
                    {
                        Task task = this.OnEventDequeueAsync(lastDequeuedEvent);
                        this.Runtime.NotifyWaitTask(this, task);
                        await task;
                    }
                    catch (Exception ex) when (this.OnExceptionHandler(ex, nameof(this.OnEventDequeueAsync)))
                    {
                        // User handled the exception, return normally.
                    }
                }
                else if (eventHandlerStatus is EventHandlerStatus.EventHandled)
                {
                    try
                    {
                        Task task = this.OnEventHandledAsync(lastDequeuedEvent);
                        this.Runtime.NotifyWaitTask(this, task);
                        await task;
                    }
                    catch (Exception ex) when (this.OnExceptionHandler(ex, nameof(this.OnEventHandledAsync)))
                    {
                        // User handled the exception, return normally.
                    }
                }
                else if (eventHandlerStatus is EventHandlerStatus.EventUnhandled)
                {
                    try
                    {
                        Task task = this.OnEventUnhandledAsync(lastDequeuedEvent, currentState);
                        this.Runtime.NotifyWaitTask(this, task);
                        await task;
                    }
                    catch (Exception ex) when (this.OnExceptionHandler(ex, nameof(this.OnEventUnhandledAsync)))
                    {
                        // User handled the exception, return normally.
                    }
                }
            }
            catch (Exception ex)
            {
                // Reports the unhandled exception.
                if (eventHandlerStatus is EventHandlerStatus.EventDequeued)
                {
                    await this.TryHandleActionInvocationExceptionAsync(ex, nameof(this.OnEventDequeueAsync));
                }
                else if (eventHandlerStatus is EventHandlerStatus.EventHandled)
                {
                    await this.TryHandleActionInvocationExceptionAsync(ex, nameof(this.OnEventHandledAsync));
                }
                else if (eventHandlerStatus is EventHandlerStatus.EventUnhandled)
                {
                    await this.TryHandleActionInvocationExceptionAsync(ex, nameof(this.OnEventUnhandledAsync));
                }
            }
        }

        /// <summary>
        /// An exception filter that calls <see cref="CoyoteRuntime.OnFailure"/>,
        /// which can choose to fast-fail the app to get a full dump.
        /// </summary>
        /// <param name="action">The action being executed when the failure occurred.</param>
        /// <param name="ex">The exception being tested.</param>
        private bool InvokeOnFailureExceptionFilter(CachedDelegate action, Exception ex)
        {
            // This is called within the exception filter so the stack has not yet been unwound.
            // If the call does not fail-fast, return false to process the exception normally.
            this.Runtime.RaiseOnFailureEvent(new MachineActionExceptionFilterException(action.MethodInfo.Name, ex));
            return false;
        }

        /// <summary>
        /// Tries to handle an exception thrown during an action invocation.
        /// </summary>
        private Task TryHandleActionInvocationExceptionAsync(Exception ex, string actionName)
        {
            Exception innerException = ex;
            while (innerException is TargetInvocationException)
            {
                innerException = innerException.InnerException;
            }

            if (innerException is AggregateException)
            {
                innerException = innerException.InnerException;
            }

            if (innerException is ExecutionCanceledException)
            {
                this.IsHalted = true;
                Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from '{this.Id}'.");
            }
            else if (innerException is TaskSchedulerException)
            {
                this.IsHalted = true;
                Debug.WriteLine($"<Exception> TaskSchedulerException was thrown from '{this.Id}'.");
            }
            else if (this.IsSuppressingExceptionAndHalting)
            {
                // Gracefully halt.
                return this.HaltAsync();
            }
            else
            {
                // Reports the unhandled exception.
                this.ReportUnhandledException(innerException, actionName);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Checks if the specified event is ignored.
        /// </summary>
        internal bool IsEventIgnored(Event e)
        {
            if (e is TimerElapsedEvent timeoutEvent && !this.Timers.ContainsKey(timeoutEvent.Info))
            {
                // The timer that created this timeout event is not active.
                return true;
            }

            Type eventType = e.GetType();
            if (this.IgnoredEvents.Contains(eventType))
            {
                return true;
            }
            else if (this.ActionMap.ContainsKey(eventType))
            {
                return false;
            }
            else if (this.IgnoredEvents.Contains(typeof(WildCardEvent)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the specified event is deferred.
        /// </summary>
        internal bool IsEventDeferred(Event e)
        {
            Type eventType = e.GetType();
            if (this.DeferredEvents.Contains(eventType))
            {
                return true;
            }
            else if (this.ActionMap.ContainsKey(eventType))
            {
                return false;
            }
            else if (this.DeferredEvents.Contains(typeof(WildCardEvent)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the cached state of this actor.
        /// </summary>
        internal virtual int GetCachedState()
        {
            unchecked
            {
                var hash = 19;
                hash = (hash * 31) + this.GetType().GetHashCode();
                hash = (hash * 31) + this.Id.Value.GetHashCode();
                hash = (hash * 31) + this.IsHalted.GetHashCode();

                hash = (hash * 31) + this.Manager.GetCachedState();

                hash = (hash * 31) + this.Inbox.GetCachedState();

                if (this.Runtime.Configuration.EnableUserDefinedStateHashing)
                {
                    // Adds the user-defined hashed state.
                    hash = (hash * 31) + this.HashedState;
                }

                return hash;
            }
        }

        /// <summary>
        /// Registers a new timer using the specified configuration.
        /// </summary>
        private protected TimerInfo RegisterTimer(TimeSpan dueTime, TimeSpan period, object payload)
        {
            var info = new TimerInfo(this.Id, dueTime, period, payload);
            var timer = this.Runtime.CreateActorTimer(info, this);
            this.Runtime.LogWriter.OnCreateTimer(info);
            this.Timers.Add(info, timer);
            return info;
        }

        /// <summary>
        /// Unregisters the specified timer.
        /// </summary>
        private protected void UnregisterTimer(TimerInfo info)
        {
            if (!this.Timers.TryGetValue(info, out IActorTimer timer))
            {
                this.Assert(info.OwnerId == this.Id, "Timer '{0}' is already disposed.", info);
            }

            this.Runtime.LogWriter.OnStopTimer(info);
            this.Timers.Remove(info);
            timer.Dispose();
        }

        /// <summary>
        /// Extracts user declarations and setups the event handlers.
        /// </summary>
        internal virtual void SetupEventHandlers()
        {
            Type actorType = this.GetType();
            if (IsTypeDeclarationCached.TryAdd(actorType, false))
            {
                // Events with already declared handlers.
                var handledEvents = new HashSet<Type>();

                // Map containing all action bindings.
                var actionBindings = new Dictionary<Type, ActionEventHandlerDeclaration>();
                var doAttributes = this.GetType().GetCustomAttributes(typeof(OnEventDoActionAttribute), false)
                    as OnEventDoActionAttribute[];

                foreach (var attr in doAttributes)
                {
                    this.Assert(!handledEvents.Contains(attr.Event),
                        "'{0}' declared multiple handlers for event '{1}'.",
                        this.Id, attr.Event);
                    actionBindings.Add(attr.Event, new ActionEventHandlerDeclaration(attr.Action));
                    handledEvents.Add(attr.Event);
                }

                // Caches the action declarations for this actor type.
                if (ActionCache.TryAdd(actorType, new Dictionary<Type, MethodInfo>()))
                {
                    foreach (var action in actionBindings)
                    {
                        if (!ActionCache[actorType].ContainsKey(action.Key))
                        {
                            ActionCache[actorType].Add(
                                action.Key,
                                this.GetActionWithName(action.Value.Name));
                        }
                    }
                }

                // This actor type has been cached.
                lock (IsTypeDeclarationCached)
                {
                    IsTypeDeclarationCached[actorType] = true;
                    System.Threading.Monitor.PulseAll(IsTypeDeclarationCached);
                }
            }
            else if (!IsTypeDeclarationCached[actorType])
            {
                lock (IsTypeDeclarationCached)
                {
                    while (!IsTypeDeclarationCached[actorType])
                    {
                        System.Threading.Monitor.Wait(IsTypeDeclarationCached);
                    }
                }
            }

            // Populates the map of event handlers for this actor instance.
            foreach (var kvp in ActionCache[actorType])
            {
                this.ActionMap.Add(kvp.Key, new CachedDelegate(kvp.Value, this));
            }
        }

        /// <summary>
        /// Returns the action with the specified name.
        /// </summary>
        private protected MethodInfo GetActionWithName(string actionName)
        {
            MethodInfo method;
            Type actorType = this.GetType();

            do
            {
                method = actorType.GetMethod(actionName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                    Type.DefaultBinder, Array.Empty<Type>(), null);
                actorType = actorType.BaseType;
            }
            while (method is null && actorType != typeof(StateMachine) && actorType != typeof(Actor));

            this.Assert(method != null, "Cannot detect action declaration '{0}' in '{1}'.", actionName, this.GetType().Name);
            this.Assert(method.GetParameters().Length is 0, "Action '{0}' in '{1}' must have 0 formal parameters.",
                method.Name, this.GetType().Name);

            // Check if the action is an 'async' method.
            if (method.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null)
            {
                this.Assert(method.ReturnType == typeof(Task),
                    "Async action '{0}' in '{1}' must have 'Task' return type.",
                    method.Name, this.GetType().Name);
            }
            else
            {
                this.Assert(method.ReturnType == typeof(void), "Action '{0}' in '{1}' must have 'void' return type.",
                    method.Name, this.GetType().Name);
            }

            return method;
        }

        /// <summary>
        /// Returns the formatted strint to be used with a fair nondeterministic boolean choice.
        /// </summary>
        private protected virtual string FormatFairRandom(string callerMemberName, string callerFilePath, int callerLineNumber) =>
            string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}_{3}",
                this.Id.Name, callerMemberName, callerFilePath, callerLineNumber.ToString());

        /// <summary>
        /// Wraps the unhandled exception inside an <see cref="AssertionFailureException"/>
        /// exception, and throws it to the user.
        /// </summary>
        private protected virtual void ReportUnhandledException(Exception ex, string actionName)
        {
            this.Runtime.WrapAndThrowException(ex, $"Exception '{ex.GetType()}' was thrown " +
                $"in '{this.Id}', action '{actionName}', " +
                $"'{ex.Source}':\n" +
                $"   {ex.Message}\n" +
                $"The stack trace is:\n{ex.StackTrace}");
        }

        /// <summary>
        /// User callback that is invoked when the actor is initialized with an optional event.
        /// </summary>
        /// <param name="initialEvent">Optional event used for initialization.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        protected virtual Task OnInitializeAsync(Event initialEvent) => Task.CompletedTask;

        /// <summary>
        /// Invokes user callback when a actor throws an exception.
        /// </summary>
        /// <param name="ex">The exception thrown by the actor.</param>
        /// <param name="methodName">The handler (outermost) that threw the exception.</param>
        /// <returns>False if the exception should continue to get thrown, true if it was handled in this method.</returns>
        private protected bool OnExceptionHandler(Exception ex, string methodName)
        {
            if (ex is ExecutionCanceledException)
            {
                // Internal exception, used during testing.
                return false;
            }

            string stateName = this is StateMachine stateMachine ? stateMachine.CurrentStateName : default;
            this.Runtime.LogWriter.OnExceptionThrown(this.Id, stateName, methodName, ex);

            var ret = this.OnException(nameof(this.OnEventHandledAsync), ex);
            this.IsSuppressingExceptionAndHalting = false;

            switch (ret)
            {
                case OnExceptionOutcome.ThrowException:
                    return false;
                case OnExceptionOutcome.HandledException:
                    this.Runtime.LogWriter.OnExceptionHandled(this.Id, stateName, methodName, ex);
                    return true;
                case OnExceptionOutcome.Halt:
                    this.IsSuppressingExceptionAndHalting = true;
                    return false;
            }

            return false;
        }

        /// <summary>
        /// Invokes user callback when the actor receives an event that it cannot handle.
        /// </summary>
        /// <param name="methodName">The handler (outermost) that threw the exception.</param>
        /// <param name="ex">The exception thrown by the actor.</param>
        /// <returns>False if the exception should continue to get thrown, true if the actir should gracefully halt.</returns>
        private protected bool OnUnhandledEventExceptionHandler(string methodName, UnhandledEventException ex)
        {
            this.Runtime.LogWriter.OnExceptionThrown(this.Id, ex.CurrentStateName, methodName, ex);

            var ret = this.OnException(methodName, ex);
            this.IsSuppressingExceptionAndHalting = false;
            switch (ret)
            {
                case OnExceptionOutcome.Halt:
                case OnExceptionOutcome.HandledException:
                    this.Runtime.LogWriter.OnExceptionHandled(this.Id, ex.CurrentStateName, methodName, ex);
                    this.IsSuppressingExceptionAndHalting = true;
                    return true;
                case OnExceptionOutcome.ThrowException:
                    return false;
            }

            return false;
        }

        /// <summary>
        /// User callback when a actor throws an exception.
        /// </summary>
        /// <param name="methodName">The handler (outermost) that threw the exception.</param>
        /// <param name="ex">The exception thrown by the actor.</param>
        /// <returns>The action that the runtime should take.</returns>
        protected virtual OnExceptionOutcome OnException(string methodName, Exception ex)
        {
            return OnExceptionOutcome.ThrowException;
        }

        /// <summary>
        /// User callback that is invoked when the actor successfully dequeues
        /// an event from its inbox. This method is not called when the dequeue happens
        /// via a receive statement.
        /// </summary>
        /// <param name="e">The event that was dequeued.</param>
        protected virtual Task OnEventDequeueAsync(Event e) => Task.CompletedTask;

        /// <summary>
        /// User callback that is invoked when the actor finishes handling a dequeued event,
        /// unless the handler of the dequeued event raised an event or caused the actor to
        /// halt (either normally or due to an exception). Unless this callback raises an event, the
        /// actor will either become idle or dequeue the next event from its inbox.
        /// </summary>
        /// <param name="e">The event that was handled.</param>
        protected virtual Task OnEventHandledAsync(Event e) => Task.CompletedTask;

        /// <summary>
        /// User callback that is invoked when the actor receives an event that it is not prepared
        /// to handle. The callback is invoked first, after which the actor will necessarily throw
        /// an <see cref="UnhandledEventException"/>
        /// </summary>
        /// <param name="e">The event that was unhandled.</param>
        /// <param name="state">The state when the event was dequeued.</param>
        protected virtual Task OnEventUnhandledAsync(Event e, string state) => Task.CompletedTask;

        /// <summary>
        /// User callback that is invoked when the actor halts.
        /// </summary>
        /// <returns>Task that represents the asynchronous operation.</returns>
        protected virtual Task OnHaltAsync() => Task.CompletedTask;

        /// <summary>
        /// Halts the actor.
        /// </summary>
        private protected Task HaltAsync()
        {
            this.IsHalted = true;
            this.ReceivedEvent = null;

            // Close the inbox, which will stop any subsequent enqueues.
            this.Inbox.Close();

            this.Runtime.LogWriter.OnHalt(this.Id, this.Inbox.Size);
            this.Runtime.NotifyHalted(this);

            // Dispose any held resources.
            this.Inbox.Dispose();
            foreach (var timer in this.Timers.Keys.ToList())
            {
                this.UnregisterTimer(timer);
            }

            // Invoke user callback.
            return this.OnHaltAsync();
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is Actor m &&
                this.GetType() == m.GetType())
            {
                return this.Id.Value == m.Id.Value;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            return this.Id.Value.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current actor.
        /// </summary>
        public override string ToString()
        {
            return this.Id.Name;
        }

        /// <summary>
        /// Attribute for declaring which action should be invoked
        /// to handle a dequeued event of the specified type.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        protected sealed class OnEventDoActionAttribute : Attribute
        {
            /// <summary>
            /// The type of the dequeued event.
            /// </summary>
            internal Type Event;

            /// <summary>
            /// The name of the action to invoke.
            /// </summary>
            internal string Action;

            /// <summary>
            /// Initializes a new instance of the <see cref="OnEventDoActionAttribute"/> class.
            /// </summary>
            /// <param name="eventType">The type of the dequeued event.</param>
            /// <param name="actionName">The name of the action to invoke.</param>
            public OnEventDoActionAttribute(Type eventType, string actionName)
            {
                this.Event = eventType;
                this.Action = actionName;
            }
        }
    }
}
