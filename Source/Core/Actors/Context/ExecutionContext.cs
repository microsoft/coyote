// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
#if !DEBUG
using System.Diagnostics;
#endif
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
using Microsoft.Coyote.SystematicTesting;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// The execution context of an actor program.
    /// </summary>
    internal class ExecutionContext : IActorRuntime
    {
        /// <summary>
        /// Object used to synchronize access to the <see cref="OnFailure"/> event.
        /// </summary>
        private static readonly object OnFailureSyncObject = new object();

        /// <summary>
        /// The configuration used by the runtime.
        /// </summary>
        internal readonly Configuration Configuration;

        /// <summary>
        /// The runtime associated with this context.
        /// </summary>
        protected readonly CoyoteRuntime Runtime;

        /// <summary>
        /// The asynchronous operation scheduler, if available.
        /// </summary>
        internal readonly OperationScheduler Scheduler;

        /// <summary>
        /// Responsible for checking specifications.
        /// </summary>
        private readonly SpecificationEngine SpecificationEngine;

        /// <summary>
        /// Map from unique actor ids to actors.
        /// </summary>
        protected readonly ConcurrentDictionary<ActorId, Actor> ActorMap;

        /// <summary>
        /// Data structure containing information regarding testing coverage.
        /// </summary>
        internal readonly CoverageInfo CoverageInfo;

        /// <summary>
        /// Responsible for generating random values.
        /// </summary>
        private readonly IRandomValueGenerator ValueGenerator;

        /// <summary>
        /// Responsible for writing to all registered <see cref="IActorRuntimeLog"/> objects.
        /// </summary>
        internal readonly LogWriter LogWriter;

        /// <inheritdoc/>
        public ILogger Logger
        {
            get => this.LogWriter.Logger;

            set
            {
                using var v = this.LogWriter.SetLogger(value);
            }
        }

        /// <summary>
        /// True if the actor program is running, else false.
        /// </summary>
        internal bool IsRunning => this.Scheduler.IsProgramExecuting;

        /// <summary>
        /// If true, the actor execution is controlled, else false.
        /// </summary>
        internal virtual bool IsExecutionControlled => false;

        /// <inheritdoc/>
        public event OnEventDroppedHandler OnEventDropped;

        /// <inheritdoc/>
        public event OnFailureHandler OnFailure
        {
            add
            {
                lock (OnFailureSyncObject)
                {
                    this.Runtime.OnFailure += value;
                }
            }

            remove
            {
                lock (OnFailureSyncObject)
                {
                    this.Runtime.OnFailure -= value;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionContext"/> class.
        /// </summary>
        internal ExecutionContext(Configuration configuration, CoyoteRuntime runtime, OperationScheduler scheduler,
            SpecificationEngine specificationEngine, CoverageInfo coverageInfo, IRandomValueGenerator valueGenerator,
            LogWriter logWriter)
        {
            this.Configuration = configuration;
            this.Runtime = runtime;
            this.Scheduler = scheduler;
            this.SpecificationEngine = specificationEngine;
            this.ActorMap = new ConcurrentDictionary<ActorId, Actor>();
            this.CoverageInfo = coverageInfo;
            this.ValueGenerator = valueGenerator;
            this.LogWriter = logWriter;
        }

        /// <inheritdoc/>
        public ActorId CreateActorId(Type type, string name = null) => new ActorId(type, this.GetNextOperationId(), name, this);

        /// <inheritdoc/>
        public virtual ActorId CreateActorIdFromName(Type type, string name) => new ActorId(type, 0, name, this, true);

        /// <inheritdoc/>
        public virtual ActorId CreateActor(Type type, Event initialEvent = null, EventGroup eventGroup = null) =>
            this.CreateActor(null, type, null, initialEvent, null, eventGroup);

        /// <inheritdoc/>
        public virtual ActorId CreateActor(Type type, string name, Event initialEvent = null, EventGroup eventGroup = null) =>
            this.CreateActor(null, type, name, initialEvent, null, eventGroup);

        /// <inheritdoc/>
        public virtual ActorId CreateActor(ActorId id, Type type, Event initialEvent = null, EventGroup eventGroup = null) =>
            this.CreateActor(id, type, null, initialEvent, null, eventGroup);

        /// <inheritdoc/>
        public virtual Task<ActorId> CreateActorAndExecuteAsync(Type type, Event initialEvent = null, EventGroup eventGroup = null) =>
            this.CreateActorAndExecuteAsync(null, type, null, initialEvent, null, eventGroup);

        /// <inheritdoc/>
        public virtual Task<ActorId> CreateActorAndExecuteAsync(Type type, string name, Event initialEvent = null, EventGroup eventGroup = null) =>
            this.CreateActorAndExecuteAsync(null, type, name, initialEvent, null, eventGroup);

        /// <inheritdoc/>
        public virtual Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, Event initialEvent = null, EventGroup eventGroup = null) =>
            this.CreateActorAndExecuteAsync(id, type, null, initialEvent, null, eventGroup);

        /// <inheritdoc/>
        public virtual void SendEvent(ActorId targetId, Event initialEvent, EventGroup eventGroup = default, SendOptions options = null) =>
            this.SendEvent(targetId, initialEvent, null, eventGroup, options);

        /// <inheritdoc/>
        public virtual Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event initialEvent,
            EventGroup eventGroup = null, SendOptions options = null) =>
            this.SendEventAndExecuteAsync(targetId, initialEvent, null, eventGroup, options);

        /// <inheritdoc/>
        public virtual EventGroup GetCurrentEventGroup(ActorId currentActorId)
        {
            Actor actor = this.GetActorWithId<Actor>(currentActorId);
            return actor?.CurrentEventGroup;
        }

        /// <summary>
        /// Creates a new <see cref="Actor"/> of the specified <see cref="Type"/>.
        /// </summary>
        internal virtual ActorId CreateActor(ActorId id, Type type, string name, Event initialEvent, Actor creator, EventGroup eventGroup)
        {
            Actor actor = this.CreateActor(id, type, name, creator, eventGroup);
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
            Actor creator, EventGroup eventGroup)
        {
            Actor actor = this.CreateActor(id, type, name, creator, eventGroup);
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
        internal virtual void SendEvent(ActorId targetId, Event e, Actor sender, EventGroup eventGroup, SendOptions options)
        {
            EnqueueStatus enqueueStatus = this.EnqueueEvent(targetId, e, sender, eventGroup, out Actor target);
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
            EventGroup eventGroup, SendOptions options)
        {
            EnqueueStatus enqueueStatus = this.EnqueueEvent(targetId, e, sender, eventGroup, out Actor target);
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
        internal virtual Actor CreateActor(ActorId id, Type type, string name, Actor creator, EventGroup eventGroup)
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

            // If no event group is provided then inherit the current group from the creator.
            if (eventGroup == null && creator != null)
            {
                eventGroup = creator.EventGroup;
            }

            Actor actor = ActorFactory.Create(type);
            IEventQueue eventQueue = new EventQueue(actor);
            actor.Configure(this, id, eventQueue, eventGroup);
            actor.SetupEventHandlers();

            if (!this.ActorMap.TryAdd(id, actor))
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
        private EnqueueStatus EnqueueEvent(ActorId targetId, Event e, Actor sender, EventGroup eventGroup, out Actor target)
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

            target = this.GetActorWithId<Actor>(targetId);

            // If no group is provided we default to passing along the group from the sender.
            if (eventGroup == null && sender != null)
            {
                eventGroup = sender.EventGroup;
            }

            Guid opId = eventGroup == null ? Guid.Empty : eventGroup.Id;
            if (target is null || target.IsHalted)
            {
                this.LogWriter.LogSendEvent(targetId, sender?.Id.Name, sender?.Id.Type,
                    (sender as StateMachine)?.CurrentStateName ?? default, e, opId, isTargetHalted: true);
                this.HandleDroppedEvent(e, targetId);
                return EnqueueStatus.Dropped;
            }

            this.LogWriter.LogSendEvent(targetId, sender?.Id.Name, sender?.Id.Type,
                (sender as StateMachine)?.CurrentStateName ?? default, e, opId, isTargetHalted: false);

            EnqueueStatus enqueueStatus = target.Enqueue(e, eventGroup, null);
            if (enqueueStatus == EnqueueStatus.Dropped)
            {
                this.HandleDroppedEvent(e, targetId);
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
                    this.Scheduler.IsProgramExecuting = false;
                    this.RaiseOnFailureEvent(ex);
                }
                finally
                {
                    if (actor.IsHalted)
                    {
                        this.ActorMap.TryRemove(actor.Id, out Actor _);
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
                this.Scheduler.IsProgramExecuting = false;
                this.RaiseOnFailureEvent(ex);
                return;
            }
        }

        /// <summary>
        /// Creates a new timer that sends a <see cref="TimerElapsedEvent"/> to its owner actor.
        /// </summary>
        internal virtual IActorTimer CreateActorTimer(TimerInfo info, Actor owner) => new ActorTimer(info, owner);

        /// <summary>
        /// Gets the actor of type <typeparamref name="TActor"/> with the specified id,
        /// or null if no such actor exists.
        /// </summary>
        internal TActor GetActorWithId<TActor>(ActorId id)
            where TActor : Actor =>
            id != null && this.ActorMap.TryGetValue(id, out Actor value) &&
            value is TActor actor ? actor : null;

        /// <summary>
        /// Returns the next available unique operation id.
        /// </summary>
        /// <returns>Value representing the next available unique operation id.</returns>
        internal ulong GetNextOperationId() => this.Runtime.GetNextOperationId();

        /// <inheritdoc/>
        public bool RandomBoolean() => this.GetNondeterministicBooleanChoice(2, null, null);

        /// <inheritdoc/>
        public bool RandomBoolean(int maxValue) => this.GetNondeterministicBooleanChoice(maxValue, null, null);

        /// <summary>
        /// Returns a controlled nondeterministic boolean choice.
        /// </summary>
        internal virtual bool GetNondeterministicBooleanChoice(int maxValue, string callerName, string callerType)
        {
            bool result = false;
            if (this.ValueGenerator.Next(maxValue) == 0)
            {
                result = true;
            }

            this.LogWriter.LogRandom(result, callerName, callerType);
            return result;
        }

        /// <inheritdoc/>
        public int RandomInteger(int maxValue) => this.GetNondeterministicIntegerChoice(maxValue, null, null);

        /// <summary>
        /// Returns a controlled nondeterministic integer choice.
        /// </summary>
        internal virtual int GetNondeterministicIntegerChoice(int maxValue, string callerName, string callerType)
        {
            var result = this.ValueGenerator.Next(maxValue);
            this.LogWriter.LogRandom(result, callerName, callerType);
            return result;
        }

        /// <summary>
        /// Logs that the specified actor invoked an action.
        /// </summary>
        internal virtual void LogInvokedAction(Actor actor, MethodInfo action, string handlingStateName, string currentStateName)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.LogExecuteAction(actor.Id, handlingStateName, currentStateName, action.Name);
            }
        }

        /// <summary>
        /// Logs that the specified actor enqueued an <see cref="Event"/>.
        /// </summary>
        internal virtual void LogEnqueuedEvent(Actor actor, Event e, EventGroup eventGroup, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.LogEnqueueEvent(actor.Id, e);
            }
        }

        /// <summary>
        /// Logs that the specified actor dequeued an <see cref="Event"/>.
        /// </summary>
        internal virtual void LogDequeuedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : default;
                this.LogWriter.LogDequeueEvent(actor.Id, stateName, e);
            }
        }

        /// <summary>
        /// Logs that the specified actor dequeued the default <see cref="Event"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void LogDefaultEventDequeued(Actor actor)
        {
        }

        /// <summary>
        /// Notifies that the inbox of the specified actor is about to be
        /// checked to see if the default event handler should fire.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void LogDefaultEventHandlerCheck(Actor actor)
        {
        }

        /// <summary>
        /// Logs that the specified actor raised an <see cref="Event"/>.
        /// </summary>
        internal virtual void LogRaisedEvent(Actor actor, Event e, EventGroup eventGroup, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : default;
                this.LogWriter.LogRaiseEvent(actor.Id, stateName, e);
            }
        }

        /// <summary>
        /// Logs that the specified actor is handling a raised <see cref="Event"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void LogHandleRaisedEvent(Actor actor, Event e)
        {
        }

        /// <summary>
        /// Logs that the specified actor called <see cref="Actor.ReceiveEventAsync(Type[])"/>
        /// or one of its overloaded methods.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void LogReceiveCalled(Actor actor)
        {
        }

        /// <summary>
        /// Logs that the specified actor enqueued an event that it was waiting to receive.
        /// </summary>
        internal virtual void LogReceivedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : default;
                this.LogWriter.LogReceiveEvent(actor.Id, stateName, e, wasBlocked: true);
            }
        }

        /// <summary>
        /// Logs that the specified actor received an event without waiting because the event
        /// was already in the inbox when the actor invoked the receive statement.
        /// </summary>
        internal virtual void LogReceivedEventWithoutWaiting(Actor actor, Event e, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : default;
                this.LogWriter.LogReceiveEvent(actor.Id, stateName, e, wasBlocked: false);
            }
        }

        /// <summary>
        /// Logs that the specified actor is waiting for the specified task to complete.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void LogWaitTask(Actor actor, Task task)
        {
        }

        /// <summary>
        /// Logs that the specified actor is waiting to receive an event of one of the specified types.
        /// </summary>
        internal virtual void LogWaitEvent(Actor actor, IEnumerable<Type> eventTypes)
        {
            if (this.Configuration.IsVerbose)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : default;
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
        /// Logs that the specified state machine entered a state.
        /// </summary>
        internal virtual void LogEnteredState(StateMachine stateMachine)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.LogStateTransition(stateMachine.Id, stateMachine.CurrentStateName, isEntry: true);
            }
        }

        /// <summary>
        /// Logs that the specified state machine exited a state.
        /// </summary>
        internal virtual void LogExitedState(StateMachine stateMachine)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.LogStateTransition(stateMachine.Id, stateMachine.CurrentStateName, isEntry: false);
            }
        }

        /// <summary>
        /// Logs that the specified state machine invoked pop.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void LogPopState(StateMachine stateMachine)
        {
        }

        /// <summary>
        /// Logs that the specified state machine invoked an action.
        /// </summary>
        internal virtual void LogInvokedOnEntryAction(StateMachine stateMachine, MethodInfo action)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.LogExecuteAction(stateMachine.Id, stateMachine.CurrentStateName,
                    stateMachine.CurrentStateName, action.Name);
            }
        }

        /// <summary>
        /// Logs that the specified state machine invoked an action.
        /// </summary>
        internal virtual void LogInvokedOnExitAction(StateMachine stateMachine, MethodInfo action)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.LogExecuteAction(stateMachine.Id, stateMachine.CurrentStateName,
                    stateMachine.CurrentStateName, action.Name);
            }
        }

        /// <summary>
        /// Builds the coverage graph information, if any. This information is only available
        /// when <see cref="Configuration.ReportActivityCoverage"/> is enabled.
        /// </summary>
        internal CoverageInfo BuildCoverageInfo()
        {
            var result = this.CoverageInfo;
            if (result != null)
            {
                var builder = this.LogWriter.GetLogsOfType<ActorRuntimeLogGraphBuilder>().FirstOrDefault();
                if (builder != null)
                {
                    result.CoverageGraph = builder.SnapshotGraph(this.Configuration.IsDgmlBugGraph);
                }

                var eventCoverage = this.LogWriter.GetLogsOfType<ActorRuntimeLogEventCoverage>().FirstOrDefault();
                if (eventCoverage != null)
                {
                    result.EventInfo = eventCoverage.EventCoverage;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the current hashed state of the actors.
        /// </summary>
        /// <remarks>
        /// The hash is updated in each execution step.
        /// </remarks>
        internal virtual int GetHashedActorState() => 0;

        /// <summary>
        /// Returns the program counter of the specified actor.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int GetActorProgramCounter(ActorId actorId) => 0;

        /// <summary>
        /// Increments the program counter of the specified actor.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void IncrementActorProgramCounter(ActorId actorId)
        {
        }

        /// <inheritdoc/>
        public void RegisterMonitor<T>()
            where T : Monitor =>
            this.TryCreateMonitor(typeof(T));

        /// <inheritdoc/>
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
            type, this.CoverageInfo, this.LogWriter);

        /// <summary>
        /// Invokes the specified <see cref="Specifications.Monitor"/> with the specified <see cref="Event"/>.
        /// </summary>
        internal void InvokeMonitor(Type type, Event e, string senderName, string senderType, string senderStateName) =>
            this.SpecificationEngine.InvokeMonitor(type, e, senderName, senderType, senderStateName);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assert(bool predicate) => this.SpecificationEngine.Assert(predicate);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assert(bool predicate, string s, object arg0) => this.SpecificationEngine.Assert(predicate, s, arg0);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assert(bool predicate, string s, object arg0, object arg1) => this.SpecificationEngine.Assert(predicate, s, arg0, arg1);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assert(bool predicate, string s, object arg0, object arg1, object arg2) =>
            this.SpecificationEngine.Assert(predicate, s, arg0, arg1, arg2);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Assert(bool predicate, string s, params object[] args) => this.SpecificationEngine.Assert(predicate, s, args);

        /// <summary>
        /// Asserts that the actor calling an actor method is also the actor that is currently executing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void AssertExpectedCallerActor(Actor caller, string calledAPI)
        {
        }

        /// <summary>
        /// Raises the <see cref="OnFailure"/> event with the specified <see cref="Exception"/>.
        /// </summary>
        internal void RaiseOnFailureEvent(Exception exception) => this.Runtime.RaiseOnFailureEvent(exception);

        /// <summary>
        /// Handle the specified dropped <see cref="Event"/>.
        /// </summary>
        internal void HandleDroppedEvent(Event e, ActorId id) => this.OnEventDropped?.Invoke(e, id);

        /// <summary>
        /// Throws an <see cref="AssertionFailureException"/> exception containing the specified exception.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void WrapAndThrowException(Exception exception, string s, params object[] args) =>
            this.SpecificationEngine.WrapAndThrowException(exception, s, args);

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

        /// <inheritdoc/>
        public void Stop() => this.Scheduler.ForceStop();

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.ActorMap.Clear();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
