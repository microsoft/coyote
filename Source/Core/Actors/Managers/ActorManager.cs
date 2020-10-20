// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Coverage;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Responsible for managing the lifetime of an actor.
    /// </summary>
    internal class ActorManager : IActorRuntime
    {
        /// <summary>
        /// The execution context of the actor being managed.
        /// </summary>
        protected readonly ActorExecutionContext Context;

        /// <summary>
        /// The actor being managed.
        /// </summary>
        protected readonly Actor Instance;

        /// <summary>
        /// The configuration used by the runtime.
        /// </summary>
        internal Configuration Configuration => this.Context.Configuration;

        /// <summary>
        /// Responsible for checking specifications.
        /// </summary>
        internal SpecificationEngine SpecificationEngine => this.Context.SpecificationEngine;

        /// <summary>
        /// True if the event handler of the actor is running, else false.
        /// </summary>
        internal bool IsEventHandlerRunning { get; set; }

        /// <summary>
        /// An optional event group associated with the Actor.
        /// </summary>
        internal EventGroup CurrentEventGroup { get; set; }

        /// <summary>
        /// Responsible for writing to all registered <see cref="IActorRuntimeLog"/> objects.
        /// </summary>
        internal LogWriter LogWriter => this.Context.LogWriter;

        /// <summary>
        /// Used to log text messages. Use <see cref="ICoyoteRuntime.SetLogger"/>
        /// to replace the logger with a custom one.
        /// </summary>
        public virtual ILogger Logger
        {
            get => this.Context.LogWriter.Logger;

            set
            {
                using var v = this.Context.LogWriter.SetLogger(value);
            }
        }

        /// <summary>
        /// True if the actor is executing, else false.
        /// </summary>
        internal bool IsRunning => this.Context.Scheduler.IsProgramExecuting;

        /// <summary>
        /// If true, the actor execution is controlled, else false.
        /// </summary>
        protected virtual bool IsExecutionControlled => false;

        /// <summary>
        /// Callback that is fired when a Coyote event is dropped. This happens when
        /// <see cref="IActorRuntime.SendEvent"/> is called with an ActorId that has no matching
        /// actor defined or the actor is halted.
        /// </summary>
        public event OnEventDroppedHandler OnEventDropped
        {
            add
            {
                lock (this.Context)
                {
                    this.Context.OnEventDropped += value;
                }
            }

            remove
            {
                lock (this.Context)
                {
                    this.Context.OnEventDropped -= value;
                }
            }
        }

        /// <summary>
        /// Callback that is fired when an exception is thrown that includes failed assertions.
        /// </summary>
        public event OnFailureHandler OnFailure
        {
            add
            {
                lock (this.Context)
                {
                    this.Context.Runtime.OnFailure += value;
                }
            }

            remove
            {
                lock (this.Context)
                {
                    this.Context.Runtime.OnFailure -= value;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorManager"/> class.
        /// </summary>
        internal ActorManager(ActorExecutionContext context, Actor instance, EventGroup group)
        {
            this.Context = context;
            this.Instance = instance;
            this.CurrentEventGroup = group;
            this.IsEventHandlerRunning = true;
        }

        /// <summary>
        /// Creates a fresh actor id that has not yet been bound to any actor.
        /// </summary>
        public ActorId CreateActorId(Type type, string name = null) => new ActorId(type, this.Context.GetNextOperationId(), name, this);

        /// <summary>
        /// Creates a actor id that is uniquely tied to the specified unique name. The
        /// returned actor id can either be a fresh id (not yet bound to any actor), or
        /// it can be bound to a previously created actor. In the second case, this actor
        /// id can be directly used to communicate with the corresponding actor.
        /// </summary>
        public virtual ActorId CreateActorIdFromName(Type type, string name) => new ActorId(type, 0, name, this, true);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and with the specified
        /// optional <see cref="Event"/>. This event is given to the <see cref="Actor.InitializeAsync"/>
        /// method on the new actor.
        /// </summary>
        public virtual ActorId CreateActor(Type type, Event initialEvent = null, EventGroup group = null) =>
            this.CreateActor(null, type, null, initialEvent, null, group);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, and with the
        /// specified optional <see cref="Event"/>. This event is given to the <see cref="Actor.InitializeAsync"/>
        /// method on the new actor.
        /// </summary>
        public virtual ActorId CreateActor(Type type, string name, Event initialEvent = null, EventGroup group = null) =>
            this.CreateActor(null, type, name, initialEvent, null, group);

        /// <summary>
        /// Creates a new actor of the specified type, using the specified <see cref="ActorId"/>.
        /// This method optionally passes an <see cref="Event"/>. This event is given to the
        /// InitializeAsync method on the new actor.
        /// </summary>
        public virtual ActorId CreateActor(ActorId id, Type type, Event initialEvent = null, EventGroup group = null) =>
            this.CreateActor(id, type, null, initialEvent, null, group);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and with the specified
        /// optional <see cref="Event"/>. This event is given to the <see cref="Actor.InitializeAsync"/>
        /// method on the new actor. The method returns only when the actor is initialized and
        /// the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public virtual Task<ActorId> CreateActorAndExecuteAsync(Type type, Event initialEvent = null, EventGroup group = null) =>
            this.CreateActorAndExecuteAsync(null, type, null, initialEvent, null, group);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, and with the
        /// specified optional <see cref="Event"/>. This event is given to the <see cref="Actor.InitializeAsync"/>
        /// method on the new actor. The method returns only when the actor is
        /// initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public virtual Task<ActorId> CreateActorAndExecuteAsync(Type type, string name, Event initialEvent = null, EventGroup group = null) =>
            this.CreateActorAndExecuteAsync(null, type, name, initialEvent, null, group);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/>, using the specified unbound
        /// actor id, and passes the specified optional <see cref="Event"/>. This event is given to
        /// the InitializeAsync method on the new actor. The method returns only when
        /// the actor is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        public virtual Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, Event initialEvent = null, EventGroup group = null) =>
            this.CreateActorAndExecuteAsync(id, type, null, initialEvent, null, group);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to an actor.
        /// </summary>
        public virtual void SendEvent(ActorId targetId, Event initialEvent, EventGroup group = default, SendOptions options = null) =>
            this.SendEvent(targetId, initialEvent, null, group, options);

        /// <summary>
        /// Sends an <see cref="Event"/> to an actor. Returns immediately if the target was already
        /// running. Otherwise blocks until the target handles the event and reaches quiescense.
        /// </summary>
        public virtual Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event initialEvent,
            EventGroup group = null, SendOptions options = null) =>
            this.SendEventAndExecuteAsync(targetId, initialEvent, null, group, options);

        /// <summary>
        /// Returns the current <see cref="EventGroup"/> of the actor with the specified id. Returns null
        /// if the id is not set, or if the <see cref="ActorId"/> is not associated with this runtime. During
        /// testing, the runtime asserts that the specified actor is currently executing.
        /// </summary>
        public virtual EventGroup GetCurrentEventGroup(ActorId currentActorId)
        {
            Actor actor = this.Context.GetActorWithId<Actor>(currentActorId);
            return actor?.CurrentEventGroup;
        }

        /// <summary>
        /// Creates a new <see cref="Actor"/> of the specified <see cref="Type"/>.
        /// </summary>
        internal virtual ActorId CreateActor(ActorId id, Type type, string name, Event initialEvent, Actor creator, EventGroup group)
        {
            Actor actor = this.CreateActor(id, type, name, creator, group);
            if (actor is StateMachine)
            {
                this.LogWriter.LogCreateStateMachine(actor.Id, creator?.Id.Name, creator?.Id.Type);
            }
            else
            {
                this.LogWriter.LogCreateActor(actor.Id, creator?.Id.Name, creator?.Id.Type);
            }

            this.RunActorEventHandler(actor, initialEvent, true);
            return actor.Id;
        }

        /// <summary>
        /// Creates a new <see cref="Actor"/> of the specified <see cref="Type"/>. The method
        /// returns only when the actor is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        internal virtual async Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, string name, Event initialEvent,
            Actor creator, EventGroup group)
        {
            Actor actor = this.CreateActor(id, type, name, creator, group);
            if (actor is StateMachine)
            {
                this.LogWriter.LogCreateStateMachine(actor.Id, creator?.Id.Name, creator?.Id.Type);
            }
            else
            {
                this.LogWriter.LogCreateActor(actor.Id, creator?.Id.Name, creator?.Id.Type);
            }

            await this.RunActorEventHandlerAsync(actor, initialEvent, true);
            return actor.Id;
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to an actor.
        /// </summary>
        internal virtual void SendEvent(ActorId targetId, Event e, Actor sender, EventGroup group, SendOptions options)
        {
            EnqueueStatus enqueueStatus = this.EnqueueEvent(targetId, e, sender, group, out Actor target);
            if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
            {
                this.RunActorEventHandler(target, null, false);
            }
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to an actor. Returns immediately if the target was
        /// already running. Otherwise blocks until the target handles the event and reaches quiescense.
        /// </summary>
        internal virtual async Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event e, Actor sender,
            EventGroup group, SendOptions options)
        {
            EnqueueStatus enqueueStatus = this.EnqueueEvent(targetId, e, sender, group, out Actor target);
            if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
            {
                await this.RunActorEventHandlerAsync(target, null, false);
                return true;
            }

            return enqueueStatus is EnqueueStatus.Dropped;
        }

        /// <summary>
        /// Creates a new <see cref="Actor"/> of the specified <see cref="Type"/>.
        /// </summary>
        internal virtual Actor CreateActor(ActorId id, Type type, string name, Actor creator, EventGroup group)
        {
            if (!type.IsSubclassOf(typeof(Actor)))
            {
                this.Assert(false, "Type '{0}' is not an actor.", type.FullName);
            }

            if (id is null)
            {
                id = this.CreateActorId(type, name);
            }
            else if (id.Runtime != null && id.Runtime != this)
            {
                this.Assert(false, "Unbound actor id '{0}' was created by another runtime.", id.Value);
            }
            else if (id.Type != type.FullName)
            {
                this.Assert(false, "Cannot bind actor id '{0}' of type '{1}' to an actor of type '{2}'.",
                    id.Value, id.Type, type.FullName);
            }
            else
            {
                id.Bind(this);
            }

            // If no group is provided then inherit the current group from the creator.
            if (group == null && creator != null)
            {
                group = creator.Manager.CurrentEventGroup;
            }

            Actor actor = ActorFactory.Create(type);
            ActorManager actorManager = new ActorManager(this.Context, actor, group);

            IEventQueue eventQueue = new EventQueue(actorManager);
            actor.Configure(actorManager, id, eventQueue);
            actor.SetupEventHandlers();

            if (!this.Context.ActorMap.TryAdd(id, actor))
            {
                string info = "This typically occurs if either the actor id was created by another runtime instance, " +
                    "or if a actor id from a previous runtime generation was deserialized, but the current runtime " +
                    "has not increased its generation value.";
                this.Assert(false, "An actor with id '{0}' was already created in generation '{1}'. {2}", id.Value, id.Generation, info);
            }

            return actor;
        }

        /// <summary>
        /// Enqueues an event to the actor with the specified id.
        /// </summary>
        private EnqueueStatus EnqueueEvent(ActorId targetId, Event e, Actor sender, EventGroup group, out Actor target)
        {
            if (e is null)
            {
                string message = sender != null ?
                    string.Format("{0} is sending a null event.", sender.Id.ToString()) :
                    "Cannot send a null event.";
                this.Assert(false, message);
            }

            if (targetId is null)
            {
                string message = (sender != null) ?
                    string.Format("{0} is sending event {1} to a null actor.", sender.Id.ToString(), e.ToString())
                    : string.Format("Cannot send event {0} to a null actor.", e.ToString());

                this.Assert(false, message);
            }

            target = this.Context.GetActorWithId<Actor>(targetId);

            // If no group is provided we default to passing along the group from the sender.
            if (group == null && sender != null)
            {
                group = sender.Manager.CurrentEventGroup;
            }

            Guid opId = group == null ? Guid.Empty : group.Id;
            if (target is null || target.IsHalted)
            {
                this.LogWriter.LogSendEvent(targetId, sender?.Id.Name, sender?.Id.Type,
                    (sender as StateMachine)?.CurrentStateName ?? string.Empty, e, opId, isTargetHalted: true);
                this.Context.HandleDroppedEvent(e, targetId);
                return EnqueueStatus.Dropped;
            }

            this.LogWriter.LogSendEvent(targetId, sender?.Id.Name, sender?.Id.Type,
                (sender as StateMachine)?.CurrentStateName ?? string.Empty, e, opId, isTargetHalted: false);

            EnqueueStatus enqueueStatus = target.Enqueue(e, group, null);
            if (enqueueStatus == EnqueueStatus.Dropped)
            {
                this.Context.HandleDroppedEvent(e, targetId);
            }

            return enqueueStatus;
        }

        /// <summary>
        /// Runs a new asynchronous actor event handler.
        /// This is a fire and forget invocation.
        /// </summary>
        private void RunActorEventHandler(Actor actor, Event initialEvent, bool isFresh)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (isFresh)
                    {
                        await actor.InitializeAsync(initialEvent);
                    }

                    await actor.RunEventHandlerAsync();
                }
                catch (Exception ex)
                {
                    this.Context.Scheduler.IsProgramExecuting = false;
                    this.RaiseOnFailureEvent(ex);
                }
                finally
                {
                    if (actor.IsHalted)
                    {
                        this.Context.ActorMap.TryRemove(actor.Id, out Actor _);
                    }
                }
            });
        }

        /// <summary>
        /// Runs a new asynchronous actor event handler.
        /// </summary>
        private async Task RunActorEventHandlerAsync(Actor actor, Event initialEvent, bool isFresh)
        {
            try
            {
                if (isFresh)
                {
                    await actor.InitializeAsync(initialEvent);
                }

                await actor.RunEventHandlerAsync();
            }
            catch (Exception ex)
            {
                this.Context.Scheduler.IsProgramExecuting = false;
                this.RaiseOnFailureEvent(ex);
                return;
            }
        }

        /// <summary>
        /// Creates a new timer that sends a <see cref="TimerElapsedEvent"/> to its owner actor.
        /// </summary>
        internal virtual IActorTimer CreateActorTimer(TimerInfo info, Actor owner) => new ActorTimer(info, owner);

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing.
        /// </summary>
        public bool RandomBoolean() => this.GetNondeterministicBooleanChoice(2, null, null);

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing. The value is used to generate a number
        /// in the range [0..maxValue), where 0 triggers true.
        /// </summary>
        public bool RandomBoolean(int maxValue) => this.GetNondeterministicBooleanChoice(maxValue, null, null);

        /// <summary>
        /// Returns a controlled nondeterministic boolean choice.
        /// </summary>
        internal virtual bool GetNondeterministicBooleanChoice(int maxValue, string callerName, string callerType)
        {
            bool result = false;
            if (this.Context.ValueGenerator.Next(maxValue) == 0)
            {
                result = true;
            }

            this.LogWriter.LogRandom(result, callerName, callerType);
            return result;
        }

        /// <summary>
        /// Returns a nondeterministic integer, that can be controlled during
        /// analysis or testing. The value is used to generate an integer in
        /// the range [0..maxValue).
        /// </summary>
        public int RandomInteger(int maxValue) => this.GetNondeterministicIntegerChoice(maxValue, null, null);

        /// <summary>
        /// Returns a controlled nondeterministic integer choice.
        /// </summary>
        internal virtual int GetNondeterministicIntegerChoice(int maxValue, string callerName, string callerType)
        {
            var result = this.Context.ValueGenerator.Next(maxValue);
            this.LogWriter.LogRandom(result, callerName, callerType);
            return result;
        }

        /// <summary>
        /// Checks if the specified event is currently ignored.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual bool IsEventIgnored(Event e) => this.Instance is StateMachine stateMachine ?
            stateMachine.IsEventIgnoredInCurrentState(e) : this.Instance.IsEventIgnored(e);

        /// <summary>
        /// Checks if the specified event is currently deferred.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual bool IsEventDeferred(Event e) => this.Instance is StateMachine stateMachine ?
            stateMachine.IsEventDeferredInCurrentState(e) : false;

        /// <summary>
        /// Checks if a default handler is currently available.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual bool IsDefaultHandlerAvailable() => this.Instance is StateMachine stateMachine ?
            stateMachine.IsDefaultHandlerInstalledInCurrentState() :
            this.Instance.IsDefaultHandlerAvailable;

        /// <summary>
        /// Notifies the actor that an event has been enqueued.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void OnEnqueueEvent(Event e, EventGroup group, EventInfo eventInfo) =>
            this.LogWriter.LogEnqueueEvent(this.Instance.Id, e);

        /// <summary>
        /// Notifies the actor that an event has been raised.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void OnRaiseEvent(Event e, EventGroup group, EventInfo eventInfo)
        {
            if (this.Instance is StateMachine stateMachine)
            {
                this.LogRaisedEvent(stateMachine, e);
            }
            else
            {
                this.LogWriter.LogRaiseEvent(this.Instance.Id, default, e);
            }
        }

        /// <summary>
        /// Notifies the actor that it is waiting to receive an event of one of the specified types.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void OnWaitEvent(IEnumerable<Type> eventTypes) => this.LogWaitEvent(this.Instance, eventTypes);

        /// <summary>
        /// Notifies the actor that an event it was waiting to receive has been enqueued.
        /// </summary>
        internal virtual void OnReceiveEvent(Event e, EventGroup group, EventInfo eventInfo)
        {
            if (group != null)
            {
                // Inherit the event group of the receive operation, if it is non-null.
                this.CurrentEventGroup = group;
            }

            this.LogReceivedEvent(this.Instance, e, eventInfo);
        }

        /// <summary>
        /// Notifies the actor that an event it was waiting to receive was already in the
        /// event queue when the actor invoked the receive statement.
        /// </summary>
        internal virtual void OnReceiveEventWithoutWaiting(Event e, EventGroup group, EventInfo eventInfo)
        {
            if (group != null)
            {
                // Inherit the event group id of the receive operation, if it is non-null.
                this.CurrentEventGroup = group;
            }

            this.LogReceivedEventWithoutWaiting(this.Instance, e, eventInfo);
        }

        /// <summary>
        /// Notifies the actor that an event has been dropped.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void OnDropEvent(Event e, EventGroup group, EventInfo eventInfo) =>
            this.Context.HandleDroppedEvent(e, this.Instance.Id);

        /// <summary>
        /// Logs that the invoked an action.
        /// </summary>
        internal virtual void LogInvokedAction(Actor actor, MethodInfo action, string handlingStateName, string currentStateName)
        {
            if (this.Configuration.IsVerbose || this.IsExecutionControlled)
            {
                this.LogWriter.LogExecuteAction(actor.Id, handlingStateName, currentStateName, action.Name);
            }
        }

        /// <summary>
        /// Logs that the dequeued an <see cref="Event"/>.
        /// </summary>
        internal virtual void LogDequeuedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose || this.IsExecutionControlled)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
                this.LogWriter.LogDequeueEvent(actor.Id, stateName, e);
            }
        }

        /// <summary>
        /// Logs that the dequeued the default <see cref="Event"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void LogDefaultEventDequeued(Actor actor)
        {
        }

        /// <summary>
        /// Notifies that the inbox of the specified actor is about to be checked
        /// to see if the default event handler should fire.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void LogDefaultEventHandlerCheck(Actor actor)
        {
        }

        /// <summary>
        /// Logs that the raised an <see cref="Event"/>.
        /// </summary>
        internal virtual void LogRaisedEvent(Actor actor, Event e)
        {
            if (this.Configuration.IsVerbose || this.IsExecutionControlled)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
                this.LogWriter.LogRaiseEvent(actor.Id, stateName, e);
            }
        }

        /// <summary>
        /// Logs that the is handling a raised <see cref="Event"/>.
        /// </summary>
        internal virtual void LogHandleRaisedEvent(Actor actor, Event e)
        {
            if (this.IsExecutionControlled)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
                this.LogWriter.LogHandleRaisedEvent(actor.Id, stateName, e);
            }
        }

        /// <summary>
        /// Logs that the called <see cref="Actor.ReceiveEventAsync(Type[])"/> or one of its overloaded methods.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void LogReceiveCalled(Actor actor)
        {
        }

        /// <summary>
        /// Logs that the enqueued an event that it was waiting to receive.
        /// </summary>
        protected virtual void LogReceivedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
                this.LogWriter.LogReceiveEvent(actor.Id, stateName, e, wasBlocked: true);
            }
        }

        /// <summary>
        /// Logs that the received an event without waiting because the event
        /// was already in the inbox when the actor invoked the receive statement.
        /// </summary>
        protected virtual void LogReceivedEventWithoutWaiting(Actor actor, Event e, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
                this.LogWriter.LogReceiveEvent(actor.Id, stateName, e, wasBlocked: false);
            }
        }

        /// <summary>
        /// Logs that the is waiting for the specified task to complete.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void LogWaitTask(Actor actor, Task task)
        {
        }

        /// <summary>
        /// Logs that the is waiting to receive an event of one of the specified types.
        /// </summary>
        protected virtual void LogWaitEvent(Actor actor, IEnumerable<Type> eventTypes)
        {
            if (this.Configuration.IsVerbose)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
                var eventWaitTypesArray = eventTypes.ToArray();
                if (eventWaitTypesArray.Length == 1)
                {
                    this.LogWriter.LogWaitEvent(actor.Id, stateName, eventWaitTypesArray[0]);
                }
                else
                {
                    this.LogWriter.LogWaitEvent(actor.Id, stateName, eventWaitTypesArray);
                }
            }
        }

        /// <summary>
        /// Logs that the state machine entered a state.
        /// </summary>
        internal void LogEnteredState(StateMachine stateMachine)
        {
            if (this.Configuration.IsVerbose || this.IsExecutionControlled)
            {
                this.LogWriter.LogStateTransition(stateMachine.Id, stateMachine.CurrentStateName, isEntry: true);
            }
        }

        /// <summary>
        /// Logs that the state machine exited a state.
        /// </summary>
        internal void LogExitedState(StateMachine stateMachine)
        {
            if (this.Configuration.IsVerbose || this.IsExecutionControlled)
            {
                this.LogWriter.LogStateTransition(stateMachine.Id, stateMachine.CurrentStateName, isEntry: false);
            }
        }

        /// <summary>
        /// Logs that the state machine invoked pop.
        /// </summary>
        internal void LogPopState(StateMachine stateMachine)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                this.SpecificationEngine.AssertExpectedCallerActor(stateMachine, "Pop");
                this.LogWriter.LogPopState(stateMachine.Id, string.Empty, stateMachine.CurrentStateName);
            }
        }

        /// <summary>
        /// Logs that the state machine invoked an action.
        /// </summary>
        internal void LogInvokedOnEntryAction(StateMachine stateMachine, MethodInfo action)
        {
            if (this.Configuration.IsVerbose || this.IsExecutionControlled)
            {
                string stateName = stateMachine.CurrentStateName;
                this.LogWriter.LogExecuteAction(stateMachine.Id, stateName, stateName, action.Name);
            }
        }

        /// <summary>
        /// Logs that the state machine invoked an action.
        /// </summary>
        internal void LogInvokedOnExitAction(StateMachine stateMachine, MethodInfo action)
        {
            if (this.Configuration.IsVerbose || this.IsExecutionControlled)
            {
                string stateName = stateMachine.CurrentStateName;
                this.LogWriter.LogExecuteAction(stateMachine.Id, stateName, stateName, action.Name);
            }
        }

        /// <summary>
        /// Returns the cached state of the actor.
        /// </summary>
        internal virtual int GetCachedState() => 0;

        /// <summary>
        /// Get the coverage graph information, if any.
        /// </summary>
        internal CoverageInfo GetCoverageInfo() => this.Context.GetCoverageInfo();

        /// <summary>
        /// Registers a new specification monitor of the specified <see cref="Type"/>.
        /// </summary>
        public void RegisterMonitor<T>()
            where T : Monitor =>
            this.TryCreateMonitor(typeof(T));

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        public void Monitor<T>(Event e)
            where T : Monitor
        {
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot monitor a null event.");
            this.InvokeMonitor(typeof(T), e, null, null, null);
        }

        /// <summary>
        /// Tries to create a new <see cref="Specifications.Monitor"/> of the specified <see cref="Type"/>.
        /// </summary>
        protected bool TryCreateMonitor(Type type) => this.SpecificationEngine.TryCreateMonitor(
            type, this.Context.CoverageInfo, this.LogWriter);

        /// <summary>
        /// Invokes the specified <see cref="Specifications.Monitor"/> with the specified <see cref="Event"/>.
        /// </summary>
        internal void InvokeMonitor(Type type, Event e, string senderName, string senderType, string senderStateName) =>
            this.SpecificationEngine.InvokeMonitor(type, e, senderName, senderType, senderStateName);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public void Assert(bool predicate) => this.SpecificationEngine.Assert(predicate);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public void Assert(bool predicate, string s, object arg0) => this.SpecificationEngine.Assert(predicate, s, arg0);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public void Assert(bool predicate, string s, object arg0, object arg1) => this.SpecificationEngine.Assert(predicate, s, arg0, arg1);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public void Assert(bool predicate, string s, object arg0, object arg1, object arg2) =>
            this.SpecificationEngine.Assert(predicate, s, arg0, arg1, arg2);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public void Assert(bool predicate, string s, params object[] args) => this.SpecificationEngine.Assert(predicate, s, args);

        /// <summary>
        /// Raises the <see cref="OnFailure"/> event with the specified <see cref="Exception"/>.
        /// </summary>
        internal void RaiseOnFailureEvent(Exception exception) => this.Context.Runtime.RaiseOnFailureEvent(exception);

        /// <inheritdoc/>
        [Obsolete("Please set the Logger property directory instead of calling this method.")]
        public TextWriter SetLogger(TextWriter logger)
        {
            var result = this.LogWriter.SetLogger(new TextWriterLogger(logger));
            if (result != null)
            {
                return result.TextWriter;
            }

            return null;
        }

        /// <inheritdoc/>
        public void RegisterLog(IActorRuntimeLog log) => this.LogWriter.RegisterLog(log);

        /// <inheritdoc/>
        public void RemoveLog(IActorRuntimeLog log) => this.LogWriter.RemoveLog(log);

        /// <summary>
        /// Terminates the actor runtime.
        /// </summary>
        public void Stop() => this.Context.Scheduler.ForceStop();

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Context.Dispose();
            }
        }

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
