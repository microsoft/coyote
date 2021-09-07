// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
#if !DEBUG
using System.Diagnostics;
#endif
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors.Coverage;
using Microsoft.Coyote.Actors.Mocks;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Actors.Timers.Mocks;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using IODebug = Microsoft.Coyote.IO.Debug;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// The execution context of an actor program.
    /// </summary>
    internal class ActorExecutionContext : IActorRuntime
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
        internal readonly CoyoteRuntime Runtime;

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
        internal bool IsRunning => this.Runtime.IsRunning;

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
        /// Initializes a new instance of the <see cref="ActorExecutionContext"/> class.
        /// </summary>
        internal ActorExecutionContext(Configuration configuration, CoyoteRuntime runtime, SpecificationEngine specificationEngine,
            IRandomValueGenerator valueGenerator, LogWriter logWriter)
        {
            this.Configuration = configuration;
            this.Runtime = runtime;
            this.SpecificationEngine = specificationEngine;
            this.ActorMap = new ConcurrentDictionary<ActorId, Actor>();
            this.CoverageInfo = new CoverageInfo();
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
            if (eventGroup is null && creator != null)
            {
                eventGroup = creator.EventGroup;
            }

            Actor actor = ActorFactory.Create(type);
            IEventQueue eventQueue = new EventQueue(actor);
            actor.Configure(this, id, null, eventQueue, eventGroup);
            actor.SetupEventHandlers();

            if (!this.ActorMap.TryAdd(id, actor))
            {
                this.Assert(false, $"An actor with id '{id.Value}' was already created by another runtime instance.");
            }

            return actor;
        }

        /// <inheritdoc/>
        public virtual void SendEvent(ActorId targetId, Event initialEvent, EventGroup eventGroup = default, SendOptions options = null) =>
            this.SendEvent(targetId, initialEvent, null, eventGroup, options);

        /// <inheritdoc/>
        public virtual Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event initialEvent,
            EventGroup eventGroup = null, SendOptions options = null) =>
            this.SendEventAndExecuteAsync(targetId, initialEvent, null, eventGroup, options);

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
            if (eventGroup is null && sender != null)
            {
                eventGroup = sender.EventGroup;
            }

            Guid opId = eventGroup is null ? Guid.Empty : eventGroup.Id;
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
                    this.Runtime.IsRunning = false;
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
                this.Runtime.IsRunning = false;
                this.RaiseOnFailureEvent(ex);
                return;
            }
        }

        /// <summary>
        /// Creates a new timer that sends a <see cref="TimerElapsedEvent"/> to its owner actor.
        /// </summary>
        internal virtual IActorTimer CreateActorTimer(TimerInfo info, Actor owner) => new ActorTimer(info, owner);

        /// <inheritdoc/>
        public virtual EventGroup GetCurrentEventGroup(ActorId currentActorId)
        {
            Actor actor = this.GetActorWithId<Actor>(currentActorId);
            return actor?.CurrentEventGroup;
        }

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
            if (this.ValueGenerator.Next(maxValue) is 0)
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
        internal virtual void LogDequeuedEvent(Actor actor, Event e, EventInfo eventInfo, bool isFreshDequeue)
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
            if (this.Configuration.IsVerbose)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : default;
                this.LogWriter.LogHandleRaisedEvent(actor.Id, stateName, e);
            }
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
        /// Logs that the specified actor is waiting to receive an event of one of the specified types.
        /// </summary>
        internal virtual void LogWaitEvent(Actor actor, IEnumerable<Type> eventTypes)
        {
            if (this.Configuration.IsVerbose)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : default;
                var eventWaitTypesArray = eventTypes.ToArray();
                if (eventWaitTypesArray.Length is 1)
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
        /// Logs that the event handler of the specified actor terminated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void LogEventHandlerTerminated(Actor actor, DequeueStatus dequeueStatus)
        {
            if (this.Configuration.IsVerbose)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : default;
                this.LogWriter.LogEventHandlerTerminated(actor.Id, stateName, dequeueStatus);
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
        /// Returns the program counter of the specified actor.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int GetActorProgramCounter(ActorId actorId) => 0;

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
        public void Stop() => this.Runtime.ForceStop();

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

        /// <summary>
        /// The mocked execution context of an actor program.
        /// </summary>
        internal sealed class Mock : ActorExecutionContext
        {
            /// <summary>
            /// Set of all created actor ids.
            /// </summary>
            private readonly ConcurrentDictionary<ActorId, byte> ActorIds;

            /// <summary>
            /// Map that stores all unique names and their corresponding actor ids.
            /// </summary>
            private readonly ConcurrentDictionary<string, ActorId> NameValueToActorId;

            /// <summary>
            /// Map of program counters used for state-caching to distinguish
            /// scheduling from non-deterministic choices.
            /// </summary>
            private readonly ConcurrentDictionary<ActorId, int> ProgramCounterMap;

            /// <summary>
            /// If true, the actor execution is controlled, else false.
            /// </summary>
            internal override bool IsExecutionControlled => true;

            /// <summary>
            /// Initializes a new instance of the <see cref="Mock"/> class.
            /// </summary>
            internal Mock(Configuration configuration, CoyoteRuntime runtime, SpecificationEngine specificationEngine,
                IRandomValueGenerator valueGenerator, LogWriter logWriter)
                : base(configuration, runtime, specificationEngine, valueGenerator, logWriter)
            {
                this.ActorIds = new ConcurrentDictionary<ActorId, byte>();
                this.NameValueToActorId = new ConcurrentDictionary<string, ActorId>();
                this.ProgramCounterMap = new ConcurrentDictionary<ActorId, int>();
            }

            /// <inheritdoc/>
            public override ActorId CreateActorIdFromName(Type type, string name)
            {
                // It is important that all actor ids use the monotonically incrementing
                // value as the id during testing, and not the unique name.
                var id = this.NameValueToActorId.GetOrAdd(name, key => this.CreateActorId(type, key));
                this.ActorIds.TryAdd(id, 0);
                return id;
            }

            /// <inheritdoc/>
            public override ActorId CreateActor(Type type, Event initialEvent = null, EventGroup eventGroup = null) =>
                this.CreateActor(null, type, null, initialEvent, eventGroup);

            /// <inheritdoc/>
            public override ActorId CreateActor(Type type, string name, Event initialEvent = null, EventGroup eventGroup = null) =>
                this.CreateActor(null, type, name, initialEvent, eventGroup);

            /// <inheritdoc/>
            public override ActorId CreateActor(ActorId id, Type type, Event initialEvent = null, EventGroup eventGroup = null)
            {
                this.Assert(id != null, "Cannot create an actor using a null actor id.");
                return this.CreateActor(id, type, null, initialEvent, eventGroup);
            }

            /// <inheritdoc/>
            public override Task<ActorId> CreateActorAndExecuteAsync(Type type, Event initialEvent = null, EventGroup eventGroup = null) =>
                this.CreateActorAndExecuteAsync(null, type, null, initialEvent, eventGroup);

            /// <inheritdoc/>
            public override Task<ActorId> CreateActorAndExecuteAsync(Type type, string name, Event initialEvent = null, EventGroup eventGroup = null) =>
                this.CreateActorAndExecuteAsync(null, type, name, initialEvent, eventGroup);

            /// <inheritdoc/>
            public override Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, Event initialEvent = null, EventGroup eventGroup = null)
            {
                this.Assert(id != null, "Cannot create an actor using a null actor id.");
                return this.CreateActorAndExecuteAsync(id, type, null, initialEvent, eventGroup);
            }

            /// <summary>
            /// Creates a new actor of the specified <see cref="Type"/> and name, using the specified
            /// unbound actor id, and passes the specified optional <see cref="Event"/>. This event
            /// can only be used to access its payload, and cannot be handled.
            /// </summary>
            internal ActorId CreateActor(ActorId id, Type type, string name, Event initialEvent = null, EventGroup eventGroup = null)
            {
                var creatorOp = this.Runtime.GetExecutingOperation<ActorOperation>();
                return this.CreateActor(id, type, name, initialEvent, creatorOp?.Actor, eventGroup);
            }

            /// <summary>
            /// Creates a new <see cref="Actor"/> of the specified <see cref="Type"/>.
            /// </summary>
            internal override ActorId CreateActor(ActorId id, Type type, string name, Event initialEvent, Actor creator, EventGroup eventGroup)
            {
                this.AssertExpectedCallerActor(creator, "CreateActor");
                Actor actor = this.CreateActor(id, type, name, creator, eventGroup);
                this.RunActorEventHandler(actor, initialEvent, true, null);
                return actor.Id;
            }

            /// <summary>
            /// Creates a new actor of the specified <see cref="Type"/> and name, using the specified
            /// unbound actor id, and passes the specified optional <see cref="Event"/>. This event
            /// can only be used to access its payload, and cannot be handled. The method returns only
            /// when the actor is initialized and the <see cref="Event"/> (if any) is handled.
            /// </summary>
            internal Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, string name, Event initialEvent = null,
                EventGroup eventGroup = null)
            {
                var creatorOp = this.Runtime.GetExecutingOperation<ActorOperation>();
                return this.CreateActorAndExecuteAsync(id, type, name, initialEvent, creatorOp?.Actor, eventGroup);
            }

            /// <summary>
            /// Creates a new <see cref="Actor"/> of the specified <see cref="Type"/>. The method
            /// returns only when the actor is initialized and the <see cref="Event"/> (if any)
            /// is handled.
            /// </summary>
            internal override async Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, string name, Event initialEvent,
                Actor creator, EventGroup eventGroup)
            {
                this.AssertExpectedCallerActor(creator, "CreateActorAndExecuteAsync");
                this.Assert(creator != null, "Only an actor can call 'CreateActorAndExecuteAsync': avoid calling " +
                    "it directly from the test method; instead call it through a test driver actor.");

                Actor actor = this.CreateActor(id, type, name, creator, eventGroup);
                this.RunActorEventHandler(actor, initialEvent, true, creator);

                // Wait until the actor reaches quiescence.
                await creator.ReceiveEventAsync(typeof(QuiescentEvent), rev => (rev as QuiescentEvent).ActorId == actor.Id);
                return await Task.FromResult(actor.Id);
            }

            /// <summary>
            /// Creates a new <see cref="Actor"/> of the specified <see cref="Type"/>.
            /// </summary>
            internal override Actor CreateActor(ActorId id, Type type, string name, Actor creator, EventGroup eventGroup)
            {
                this.Assert(type.IsSubclassOf(typeof(Actor)), "Type '{0}' is not an actor.", type.FullName);

                // Using ulong.MaxValue because a Create operation cannot specify
                // the id of its target, because the id does not exist yet.
                this.Runtime.ScheduleNextOperation(AsyncOperationType.Create);
                this.ResetProgramCounter(creator);

                if (id is null)
                {
                    id = this.CreateActorId(type, name);
                    this.ActorIds.TryAdd(id, 0);
                }
                else
                {
                    this.Assert(id.Runtime is null || id.Runtime == this, "Unbound actor id '{0}' was created by another runtime.", id.Value);
                    this.Assert(id.Type == type.FullName, "Cannot bind actor id '{0}' of type '{1}' to an actor of type '{2}'.",
                        id.Value, id.Type, type.FullName);
                    id.Bind(this);
                }

                // If a group was not provided, inherit the current event group from the creator (if any).
                if (eventGroup is null && creator != null)
                {
                    eventGroup = creator.EventGroup;
                }

                Actor actor = ActorFactory.Create(type);
                ActorOperation op = new ActorOperation(id.Value, id.Name, actor);
                IEventQueue eventQueue = new MockEventQueue(actor);
                actor.Configure(this, id, op, eventQueue, eventGroup);
                actor.SetupEventHandlers();

                if (this.Configuration.ReportActivityCoverage)
                {
                    actor.ReportActivityCoverage(this.CoverageInfo);
                }

                bool result = this.Runtime.RegisterOperation(op);
                this.Assert(result, "Actor id '{0}' is used by an existing or previously halted actor.", id.Value);
                if (actor is StateMachine)
                {
                    this.LogWriter.LogCreateStateMachine(id, creator?.Id.Name, creator?.Id.Type);
                }
                else
                {
                    this.LogWriter.LogCreateActor(id, creator?.Id.Name, creator?.Id.Type);
                }

                return actor;
            }

            /// <inheritdoc/>
            public override void SendEvent(ActorId targetId, Event initialEvent, EventGroup eventGroup = default, SendOptions options = null)
            {
                var senderOp = this.Runtime.GetExecutingOperation<ActorOperation>();
                this.SendEvent(targetId, initialEvent, senderOp?.Actor, eventGroup, options);
            }

            /// <inheritdoc/>
            public override Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event initialEvent,
                EventGroup eventGroup = null, SendOptions options = null)
            {
                var senderOp = this.Runtime.GetExecutingOperation<ActorOperation>();
                return this.SendEventAndExecuteAsync(targetId, initialEvent, senderOp?.Actor, eventGroup, options);
            }

            /// <summary>
            /// Sends an asynchronous <see cref="Event"/> to an actor.
            /// </summary>
            internal override void SendEvent(ActorId targetId, Event e, Actor sender, EventGroup eventGroup, SendOptions options)
            {
                if (e is null)
                {
                    string message = sender != null ?
                        string.Format("{0} is sending a null event.", sender.Id.ToString()) :
                        "Cannot send a null event.";
                    this.Assert(false, message);
                }

                if (sender != null)
                {
                    this.Assert(targetId != null, "{0} is sending event {1} to a null actor.", sender.Id, e);
                }
                else
                {
                    this.Assert(targetId != null, "Cannot send event {1} to a null actor.", e);
                }

                this.AssertExpectedCallerActor(sender, "SendEvent");

                EnqueueStatus enqueueStatus = this.EnqueueEvent(targetId, e, sender, eventGroup, options, out Actor target);
                if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
                {
                    this.RunActorEventHandler(target, null, false, null);
                }
            }

            /// <summary>
            /// Sends an asynchronous <see cref="Event"/> to an actor. Returns immediately if the target was
            /// already running. Otherwise blocks until the target handles the event and reaches quiescense.
            /// </summary>
            internal override async Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event e, Actor sender,
                EventGroup eventGroup, SendOptions options)
            {
                this.Assert(sender is StateMachine, "Only an actor can call 'SendEventAndExecuteAsync': avoid " +
                    "calling it directly from the test method; instead call it through a test driver actor.");
                this.Assert(e != null, "{0} is sending a null event.", sender.Id);
                this.Assert(targetId != null, "{0} is sending event {1} to a null actor.", sender.Id, e);
                this.AssertExpectedCallerActor(sender, "SendEventAndExecuteAsync");
                EnqueueStatus enqueueStatus = this.EnqueueEvent(targetId, e, sender, eventGroup, options, out Actor target);
                if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
                {
                    this.RunActorEventHandler(target, null, false, sender as StateMachine);
                    // Wait until the actor reaches quiescence.
                    await (sender as StateMachine).ReceiveEventAsync(typeof(QuiescentEvent), rev => (rev as QuiescentEvent).ActorId == targetId);
                    return true;
                }

                // EnqueueStatus.EventHandlerNotRunning is not returned by EnqueueEvent
                // (even when the actor was previously inactive) when the event e requires
                // no action by the actor (i.e., it implicitly handles the event).
                return enqueueStatus is EnqueueStatus.Dropped || enqueueStatus is EnqueueStatus.NextEventUnavailable;
            }

            /// <summary>
            /// Enqueues an event to the actor with the specified id.
            /// </summary>
            private EnqueueStatus EnqueueEvent(ActorId targetId, Event e, Actor sender, EventGroup eventGroup,
                SendOptions options, out Actor target)
            {
                target = this.Runtime.GetOperationWithId<ActorOperation>(targetId.Value)?.Actor;
                this.Assert(target != null,
                    "Cannot send event '{0}' to actor id '{1}' that is not bound to an actor instance.",
                    e.GetType().FullName, targetId.Value);

                this.Runtime.ScheduleNextOperation(AsyncOperationType.Send);
                this.ResetProgramCounter(sender as StateMachine);

                // If no group is provided we default to passing along the group from the sender.
                if (eventGroup is null && sender != null)
                {
                    eventGroup = sender.EventGroup;
                }

                if (target.IsHalted)
                {
                    Guid groupId = eventGroup is null ? Guid.Empty : eventGroup.Id;
                    this.LogWriter.LogSendEvent(targetId, sender?.Id.Name, sender?.Id.Type,
                        (sender as StateMachine)?.CurrentStateName ?? default, e, groupId, isTargetHalted: true);
                    this.Assert(options is null || !options.MustHandle,
                        "A must-handle event '{0}' was sent to {1} which has halted.", e.GetType().FullName, targetId);
                    this.HandleDroppedEvent(e, targetId);
                    return EnqueueStatus.Dropped;
                }

                EnqueueStatus enqueueStatus = this.EnqueueEvent(target, e, sender, eventGroup, options);
                if (enqueueStatus == EnqueueStatus.Dropped)
                {
                    this.HandleDroppedEvent(e, targetId);
                }

                return enqueueStatus;
            }

            /// <summary>
            /// Enqueues an event to the actor with the specified id.
            /// </summary>
            private EnqueueStatus EnqueueEvent(Actor actor, Event e, Actor sender, EventGroup eventGroup, SendOptions options)
            {
                EventOriginInfo originInfo;

                string stateName = null;
                if (sender is StateMachine senderStateMachine)
                {
                    originInfo = new EventOriginInfo(sender.Id, senderStateMachine.GetType().FullName,
                        NameResolver.GetStateNameForLogging(senderStateMachine.CurrentState));
                    stateName = senderStateMachine.CurrentStateName;
                }
                else if (sender is Actor senderActor)
                {
                    originInfo = new EventOriginInfo(sender.Id, senderActor.GetType().FullName, string.Empty);
                }
                else
                {
                    // Message comes from the environment.
                    originInfo = new EventOriginInfo(null, "Env", "Env");
                }

                EventInfo eventInfo = new EventInfo(e, originInfo)
                {
                    MustHandle = options?.MustHandle ?? false,
                    Assert = options?.Assert ?? -1
                };

                Guid opId = eventGroup is null ? Guid.Empty : eventGroup.Id;
                this.LogWriter.LogSendEvent(actor.Id, sender?.Id.Name, sender?.Id.Type, stateName,
                    e, opId, isTargetHalted: false);
                return actor.Enqueue(e, eventGroup, eventInfo);
            }

            /// <summary>
            /// Runs a new asynchronous event handler for the specified actor.
            /// This is a fire and forget invocation.
            /// </summary>
            /// <param name="actor">The actor that executes this event handler.</param>
            /// <param name="initialEvent">Optional event for initializing the actor.</param>
            /// <param name="isFresh">If true, then this is a new actor.</param>
            /// <param name="syncCaller">Caller actor that is blocked for quiscence.</param>
            private void RunActorEventHandler(Actor actor, Event initialEvent, bool isFresh, Actor syncCaller)
            {
                var op = actor.Operation;
                Task task = new Task(async () =>
                {
                    try
                    {
                        // Update the current controlled thread with this runtime instance,
                        // allowing future retrieval in the same controlled thread.
                        CoyoteRuntime.SetCurrentRuntime(this.Runtime);

                        this.Runtime.StartOperation(op);

                        if (isFresh)
                        {
                            await actor.InitializeAsync(initialEvent);
                        }

                        await actor.RunEventHandlerAsync();
                        if (syncCaller != null)
                        {
                            this.EnqueueEvent(syncCaller, new QuiescentEvent(actor.Id), actor, actor.CurrentEventGroup, null);
                        }

                        if (!actor.IsHalted)
                        {
                            this.ResetProgramCounter(actor);
                        }

                        this.Runtime.CompleteOperation(op);

                        // The actor is inactive or halted, schedule the next enabled operation.
                        this.Runtime.ScheduleNextOperation(AsyncOperationType.Stop);
                    }
                    catch (Exception ex)
                    {
                        this.ProcessUnhandledExceptionInOperation(op, ex);
                    }
                });

                task.Start();
                this.Runtime.WaitOperationStart(op);
            }

            /// <summary>
            /// Creates a new timer that sends a <see cref="TimerElapsedEvent"/> to its owner actor.
            /// </summary>
            internal override IActorTimer CreateActorTimer(TimerInfo info, Actor owner)
            {
                var id = this.CreateActorId(typeof(MockStateMachineTimer));
                this.CreateActor(id, typeof(MockStateMachineTimer), new TimerSetupEvent(info, owner, this.Configuration.TimeoutDelay));
                return this.Runtime.GetOperationWithId<ActorOperation>(id.Value).Actor as MockStateMachineTimer;
            }

            /// <inheritdoc/>
            public override EventGroup GetCurrentEventGroup(ActorId currentActorId)
            {
                var callerOp = this.Runtime.GetExecutingOperation<ActorOperation>();
                this.Assert(callerOp != null && currentActorId == callerOp.Actor.Id,
                    "Trying to access the event group id of {0}, which is not the currently executing actor.",
                    currentActorId);
                return callerOp.Actor.CurrentEventGroup;
            }

            /// <summary>
            /// Returns a controlled nondeterministic boolean choice.
            /// </summary>
            internal override bool GetNondeterministicBooleanChoice(int maxValue, string callerName, string callerType)
            {
                var caller = this.Runtime.GetExecutingOperation<ActorOperation>()?.Actor;
                if (caller is Actor callerActor)
                {
                    this.IncrementActorProgramCounter(callerActor.Id);
                }

                var choice = this.Runtime.GetNextNondeterministicBooleanChoice(maxValue);
                this.LogWriter.LogRandom(choice, callerName ?? caller?.Id.Name, callerType ?? caller?.Id.Type);
                return choice;
            }

            /// <summary>
            /// Returns a controlled nondeterministic integer choice.
            /// </summary>
            internal override int GetNondeterministicIntegerChoice(int maxValue, string callerName, string callerType)
            {
                var caller = this.Runtime.GetExecutingOperation<ActorOperation>()?.Actor;
                if (caller is Actor callerActor)
                {
                    this.IncrementActorProgramCounter(callerActor.Id);
                }

                var choice = this.Runtime.GetNextNondeterministicIntegerChoice(maxValue);
                this.LogWriter.LogRandom(choice, callerName ?? caller?.Id.Name, callerType ?? caller?.Id.Type);
                return choice;
            }

            /// <inheritdoc/>
            internal override void LogInvokedAction(Actor actor, MethodInfo action, string handlingStateName, string currentStateName) =>
                this.LogWriter.LogExecuteAction(actor.Id, handlingStateName, currentStateName, action.Name);

            /// <inheritdoc/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal override void LogEnqueuedEvent(Actor actor, Event e, EventGroup eventGroup, EventInfo eventInfo) =>
                this.LogWriter.LogEnqueueEvent(actor.Id, e);

            /// <inheritdoc/>
            internal override void LogDequeuedEvent(Actor actor, Event e, EventInfo eventInfo, bool isFreshDequeue)
            {
                if (!isFreshDequeue)
                {
                    // Skip the scheduling point, as this is the first dequeue of the event handler,
                    // to avoid unecessery context switches.
                    this.Runtime.ScheduleNextOperation(AsyncOperationType.Receive);
                    this.ResetProgramCounter(actor);
                }

                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
                this.LogWriter.LogDequeueEvent(actor.Id, stateName, e);
            }

            /// <inheritdoc/>
            internal override void LogDefaultEventDequeued(Actor actor)
            {
                this.Runtime.ScheduleNextOperation(AsyncOperationType.Receive);
                this.ResetProgramCounter(actor);
            }

            /// <inheritdoc/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal override void LogDefaultEventHandlerCheck(Actor actor) =>
                this.Runtime.ScheduleNextOperation(AsyncOperationType.Default);

            /// <inheritdoc/>
            internal override void LogRaisedEvent(Actor actor, Event e, EventGroup eventGroup, EventInfo eventInfo)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
                this.LogWriter.LogRaiseEvent(actor.Id, stateName, e);
            }

            /// <inheritdoc/>
            internal override void LogHandleRaisedEvent(Actor actor, Event e)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
                this.LogWriter.LogHandleRaisedEvent(actor.Id, stateName, e);
            }

            /// <inheritdoc/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal override void LogReceiveCalled(Actor actor) => this.AssertExpectedCallerActor(actor, "ReceiveEventAsync");

            /// <inheritdoc/>
            internal override void LogReceivedEvent(Actor actor, Event e, EventInfo eventInfo)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
                this.LogWriter.LogReceiveEvent(actor.Id, stateName, e, wasBlocked: true);
            }

            /// <inheritdoc/>
            internal override void LogReceivedEventWithoutWaiting(Actor actor, Event e, EventInfo eventInfo)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
                this.LogWriter.LogReceiveEvent(actor.Id, stateName, e, wasBlocked: false);
                this.Runtime.ScheduleNextOperation(AsyncOperationType.Receive);
                this.ResetProgramCounter(actor);
            }

            /// <inheritdoc/>
            internal override void LogWaitEvent(Actor actor, IEnumerable<Type> eventTypes)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
                var eventWaitTypesArray = eventTypes.ToArray();
                if (eventWaitTypesArray.Length is 1)
                {
                    this.LogWriter.LogWaitEvent(actor.Id, stateName, eventWaitTypesArray[0]);
                }
                else
                {
                    this.LogWriter.LogWaitEvent(actor.Id, stateName, eventWaitTypesArray);
                }

                this.Runtime.ScheduleNextOperation(AsyncOperationType.Join);
                this.ResetProgramCounter(actor);
            }

            /// <inheritdoc/>
            internal override void LogEventHandlerTerminated(Actor actor, DequeueStatus dequeueStatus)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
                this.LogWriter.LogEventHandlerTerminated(actor.Id, stateName, dequeueStatus);
            }

            /// <inheritdoc/>
            internal override void LogEnteredState(StateMachine stateMachine)
            {
                string stateName = stateMachine.CurrentStateName;
                this.LogWriter.LogStateTransition(stateMachine.Id, stateName, isEntry: true);
            }

            /// <inheritdoc/>
            internal override void LogExitedState(StateMachine stateMachine)
            {
                this.LogWriter.LogStateTransition(stateMachine.Id, stateMachine.CurrentStateName, isEntry: false);
            }

            /// <inheritdoc/>
            internal override void LogPopState(StateMachine stateMachine)
            {
                this.AssertExpectedCallerActor(stateMachine, "Pop");
                this.LogWriter.LogPopState(stateMachine.Id, default, stateMachine.CurrentStateName);
            }

            /// <inheritdoc/>
            internal override void LogInvokedOnEntryAction(StateMachine stateMachine, MethodInfo action)
            {
                string stateName = stateMachine.CurrentStateName;
                this.LogWriter.LogExecuteAction(stateMachine.Id, stateName, stateName, action.Name);
            }

            /// <inheritdoc/>
            internal override void LogInvokedOnExitAction(StateMachine stateMachine, MethodInfo action)
            {
                string stateName = stateMachine.CurrentStateName;
                this.LogWriter.LogExecuteAction(stateMachine.Id, stateName, stateName, action.Name);
            }

            /// <inheritdoc/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal override int GetActorProgramCounter(ActorId actorId) =>
                this.ProgramCounterMap.GetOrAdd(actorId, 0);

            /// <summary>
            /// Increments the program counter of the specified actor.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void IncrementActorProgramCounter(ActorId actorId) =>
                this.ProgramCounterMap.AddOrUpdate(actorId, 1, (id, value) => value + 1);

            /// <summary>
            /// Resets the program counter of the specified actor.
            /// </summary>
            private void ResetProgramCounter(Actor actor)
            {
                if (actor != null)
                {
                    this.ProgramCounterMap.AddOrUpdate(actor.Id, 0, (id, value) => 0);
                }
            }

            /// <inheritdoc/>
#if !DEBUG
            [DebuggerHidden]
#endif
            internal override void AssertExpectedCallerActor(Actor caller, string calledAPI)
            {
                if (caller is null)
                {
                    return;
                }

                var op = this.Runtime.GetExecutingOperation<ActorOperation>();
                if (op is null)
                {
                    return;
                }

                this.Assert(op.Actor.Equals(caller), "{0} invoked {1} on behalf of {2}.",
                    op.Actor.Id, calledAPI, caller.Id);
            }

            /// <summary>
            /// Processes an unhandled exception in the specified asynchronous operation.
            /// </summary>
            private void ProcessUnhandledExceptionInOperation(AsyncOperation op, Exception ex)
            {
                string message = null;
                Exception exception = UnwrapException(ex);
                if (exception is ExecutionCanceledException || exception is TaskSchedulerException)
                {
                    IODebug.WriteLine("<Exception> {0} was thrown from operation '{1}'.",
                        exception.GetType().Name, op.Name);
                }
                else if (exception is ObjectDisposedException)
                {
                    IODebug.WriteLine("<Exception> {0} was thrown from operation '{1}' with reason '{2}'.",
                        exception.GetType().Name, op.Name, ex.Message);
                }
                else if (op is ActorOperation actorOp)
                {
                    message = string.Format(CultureInfo.InvariantCulture,
                        $"Unhandled exception '{exception.GetType()}' was thrown in actor '{actorOp.Name}', " +
                        $"'{exception.Source}':\n" +
                        $"   {exception.Message}\n" +
                        $"The stack trace is:\n{exception.StackTrace}");
                }
                else
                {
                    message = CoyoteRuntime.FormatUnhandledException(exception);
                }

                if (message != null)
                {
                    // Report the unhandled exception.
                    this.Runtime.NotifyUnhandledException(exception, message, cancelExecution: false);
                }
            }

            /// <summary>
            /// Unwraps the specified exception.
            /// </summary>
            private static Exception UnwrapException(Exception ex)
            {
                Exception exception = ex;
                while (exception is TargetInvocationException)
                {
                    exception = exception.InnerException;
                }

                if (exception is AggregateException)
                {
                    exception = exception.InnerException;
                }

                return exception;
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.NameValueToActorId.Clear();
                    this.ProgramCounterMap.Clear();
                    foreach (var id in this.ActorIds)
                    {
                        // Unbind the runtime to avoid memory leaks if the user holds the id.
                        id.Key.Bind(null);
                    }

                    this.ActorIds.Clear();
                }

                base.Dispose(disposing);
            }
        }
    }
}
