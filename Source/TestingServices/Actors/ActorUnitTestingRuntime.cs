// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Runtime;
using EventInfo = Microsoft.Coyote.Runtime.EventInfo;
using Monitor = Microsoft.Coyote.Specifications.Monitor;

namespace Microsoft.Coyote.TestingServices.Runtime
{
    /// <summary>
    /// Runtime for testing an actor in isolation.
    /// </summary>
    internal sealed class ActorUnitTestingRuntime : CoyoteRuntime
    {
        /// <summary>
        /// The actor being tested.
        /// </summary>
        internal readonly Actor Instance;

        /// <summary>
        /// The inbox of the actor being tested.
        /// </summary>
        internal readonly EventQueue ActorInbox;

        /// <summary>
        /// Task completion source that completes when the actor being tested reaches quiescence.
        /// </summary>
        private TaskCompletionSource<bool> QuiescenceCompletionSource;

        /// <summary>
        /// True if the actor is waiting to receive and event, else false.
        /// </summary>
        internal bool IsActorWaitingToReceiveEvent { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorUnitTestingRuntime"/> class.
        /// </summary>
        internal ActorUnitTestingRuntime(Type actorType, Configuration configuration)
            : base(configuration)
        {
            if (!actorType.IsSubclassOf(typeof(Actor)))
            {
                this.Assert(false, "Type '{0}' is not an actor.", actorType.FullName);
            }

            var id = new ActorId(actorType, null, this);
            this.Instance = ActorFactory.Create(actorType);
            IActorManager actorManager;
            if (this.Instance is StateMachine stateMachine)
            {
                actorManager = new StateMachineManager(this, stateMachine, Guid.Empty);
            }
            else
            {
                actorManager = new ActorManager(this, this.Instance, Guid.Empty);
            }

            this.ActorInbox = new EventQueue(actorManager);
            this.Instance.Configure(this, id, actorManager, this.ActorInbox);
            this.Instance.SetupEventHandlers();

            this.LogWriter.LogCreateActor(this.Instance.Id, null);

            this.IsActorWaitingToReceiveEvent = false;
        }

        /// <summary>
        /// Starts executing the actor-under-test by transitioning it to its initial state
        /// and passing an optional initialization event.
        /// </summary>
        internal Task StartAsync(Event initialEvent)
        {
            this.RunActorEventHandlerAsync(this.Instance, initialEvent, true);
            return this.QuiescenceCompletionSource.Task;
        }

        /// <summary>
        /// Creates a actor id that is uniquely tied to the specified unique name. The
        /// returned actor id can either be a fresh id (not yet bound to any actor), or
        /// it can be bound to a previously created actor. In the second case, this actor
        /// id can be directly used to communicate with the corresponding actor.
        /// </summary>
        public override ActorId CreateActorIdFromName(Type type, string name) => new ActorId(type, name, this, true);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and with the specified
        /// optional <see cref="Event"/>. This event can only be used to access its payload,
        /// and cannot be handled.
        /// </summary>
        public override ActorId CreateActor(Type type, Event initialEvent = null, Guid opGroupId = default) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, and with the
        /// specified optional <see cref="Event"/>. This event can only be used to access
        /// its payload, and cannot be handled.
        /// </summary>
        public override ActorId CreateActor(Type type, string name, Event initialEvent = null, Guid opGroupId = default) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <summary>
        /// Creates a new actor of the specified type, using the specified <see cref="ActorId"/>.
        /// This method optionally passes an <see cref="Event"/> to the new actor, which can only
        /// be used to access its payload, and cannot be handled.
        /// </summary>
        public override ActorId CreateActor(ActorId id, Type type, Event initialEvent = null, Guid opGroupId = default) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and with the specified
        /// optional <see cref="Event"/>. This event can only be used to access its payload,
        /// and cannot be handled. The method returns only when the actor is initialized and
        /// the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public override Task<ActorId> CreateActorAndExecuteAsync(Type type, Event initialEvent = null,
            Guid opGroupId = default) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, and with the
        /// specified optional <see cref="Event"/>. This event can only be used to access
        /// its payload, and cannot be handled. The method returns only when the actor is
        /// initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public override Task<ActorId> CreateActorAndExecuteAsync(Type type, string name, Event initialEvent = null,
            Guid opGroupId = default) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/>, using the specified unbound
        /// actor id, and passes the specified optional <see cref="Event"/>. This event can only
        /// be used to access its payload, and cannot be handled. The method returns only when
        /// the actor is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        public override Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, Event e = null, Guid opGroupId = default) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to an actor.
        /// </summary>
        public override void SendEvent(ActorId targetId, Event e, Guid opGroupId = default, SendOptions options = null) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <summary>
        /// Sends an <see cref="Event"/> to an actor. Returns immediately if the target was already
        /// running. Otherwise blocks until the target handles the event and reaches quiescense.
        /// </summary>
        public override Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event e, Guid opGroupId = default, SendOptions options = null) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <summary>
        /// Returns the operation group id of the actor with the specified id. Returns <see cref="Guid.Empty"/>
        /// if the id is not set, or if the <see cref="ActorId"/> is not associated with this runtime. During
        /// testing, the runtime asserts that the specified actor is currently executing.
        /// </summary>
        public override Guid GetCurrentOperationGroupId(ActorId currentActorId) => Guid.Empty;

        /// <summary>
        /// Creates a new <see cref="Actor"/> of the specified <see cref="Type"/>.
        /// </summary>
        internal override ActorId CreateActor(ActorId id, Type type, string name, Event initialEvent,
            Actor creator, Guid opGroupId)
        {
            id = id ?? new ActorId(type, null, this);
            this.LogWriter.LogCreateActor(id, creator?.Id);
            return id;
        }

        /// <summary>
        /// Creates a new <see cref="Actor"/> of the specified <see cref="Type"/>. The method
        /// returns only when the actor is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        internal override Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, string name,
            Event initialEvent, Actor creator, Guid opGroupId)
        {
            id = id ?? new ActorId(type, null, this);
            this.LogWriter.LogCreateActor(id, creator?.Id);
            return Task.FromResult(id);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to an actor.
        /// </summary>
        internal override void SendEvent(ActorId targetId, Event e, Actor sender, Guid opGroupId, SendOptions options)
        {
            this.Assert(sender is null || this.Instance.Id.Equals(sender.Id),
                string.Format("Only {0} can send an event during this test.", this.Instance.Id.ToString()));
            this.Assert(targetId != null, string.Format("{0} is sending to a null actor.", this.Instance.Id.ToString()));
            this.Assert(e != null, string.Format("{0} is sending a null event.", this.Instance.Id.ToString()));

            // The operation group id of this operation is set using the following precedence:
            // (1) To the specified send operation group id, if it is non-empty.
            // (2) To the operation group id of the sender actor, if it exists and is non-empty.
            // (3) To the empty operation group id.
            if (opGroupId == Guid.Empty && sender != null)
            {
                opGroupId = sender.OperationGroupId;
            }

            if (this.Instance.IsHalted)
            {
                this.LogWriter.LogSendEvent(targetId, sender?.Id, (sender as StateMachine)?.CurrentStateName ?? string.Empty,
                    e, opGroupId, isTargetHalted: true);
                return;
            }

            this.LogWriter.LogSendEvent(targetId, sender?.Id, (sender as StateMachine)?.CurrentStateName ?? string.Empty,
                e, opGroupId, isTargetHalted: false);

            if (!targetId.Equals(this.Instance.Id))
            {
                // Drop all events sent to an actor other than the actor-under-test.
                return;
            }

            EnqueueStatus enqueueStatus = this.Instance.Enqueue(e, opGroupId, null);
            if (enqueueStatus == EnqueueStatus.EventHandlerNotRunning)
            {
                this.RunActorEventHandlerAsync(this.Instance, null, false);
            }
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to an actor. Returns immediately if the target was
        /// already running. Otherwise blocks until the target handles the event and reaches quiescense.
        /// </summary>
        internal override Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event e, Actor sender,
            Guid opGroupId, SendOptions options)
        {
            this.SendEvent(targetId, e, sender, opGroupId, options);
            return this.QuiescenceCompletionSource.Task;
        }

        /// <summary>
        /// Runs a new asynchronous actor event handler.
        /// </summary>
        private Task RunActorEventHandlerAsync(Actor actor, Event initialEvent, bool isFresh)
        {
            this.QuiescenceCompletionSource = new TaskCompletionSource<bool>();

            return Task.Run(async () =>
            {
                try
                {
                    if (isFresh)
                    {
                        await actor.InitializeAsync(initialEvent);
                    }

                    await actor.RunEventHandlerAsync();
                    this.QuiescenceCompletionSource.SetResult(true);
                }
                catch (Exception ex)
                {
                    this.IsRunning = false;
                    this.RaiseOnFailureEvent(ex);
                    this.QuiescenceCompletionSource.SetException(ex);
                }
            });
        }

        /// <summary>
        /// Creates a new timer that sends a <see cref="TimerElapsedEvent"/> to its owner actor.
        /// </summary>
        internal override IActorTimer CreateActorTimer(TimerInfo info, Actor owner) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <summary>
        /// Tries to create a new <see cref="Coyote.Specifications.Monitor"/> of the specified <see cref="Type"/>.
        /// </summary>
        internal override void TryCreateMonitor(Type type)
        {
            // No-op in this runtime mode.
        }

        /// <summary>
        /// Invokes the specified <see cref="Coyote.Specifications.Monitor"/> with the specified <see cref="Event"/>.
        /// </summary>
        internal override void Monitor(Type type, Actor sender, Event e)
        {
            // No-op in this runtime mode.
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public override void Assert(bool predicate)
        {
            if (!predicate)
            {
                throw new AssertionFailureException("Detected an assertion failure.");
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public override void Assert(bool predicate, string s, object arg0)
        {
            if (!predicate)
            {
                throw new AssertionFailureException(string.Format(CultureInfo.InvariantCulture, s, arg0.ToString()));
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public override void Assert(bool predicate, string s, object arg0, object arg1)
        {
            if (!predicate)
            {
                throw new AssertionFailureException(string.Format(CultureInfo.InvariantCulture, s, arg0.ToString(), arg1.ToString()));
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public override void Assert(bool predicate, string s, object arg0, object arg1, object arg2)
        {
            if (!predicate)
            {
                throw new AssertionFailureException(string.Format(CultureInfo.InvariantCulture, s, arg0.ToString(), arg1.ToString(), arg2.ToString()));
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public override void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                throw new AssertionFailureException(string.Format(CultureInfo.InvariantCulture, s, args));
            }
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        internal override bool GetNondeterministicBooleanChoice(Actor caller, int maxValue)
        {
            Random random = new Random(DateTime.Now.Millisecond);

            bool result = false;
            if (random.Next(maxValue) == 0)
            {
                result = true;
            }

            this.LogWriter.LogRandom(caller?.Id, result);

            return result;
        }

        /// <summary>
        /// Returns a nondeterministic integer choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        internal override int GetNondeterministicIntegerChoice(Actor caller, int maxValue)
        {
            Random random = new Random(DateTime.Now.Millisecond);
            var result = random.Next(maxValue);

            this.LogWriter.LogRandom(caller?.Id, result);

            return result;
        }

        /// <summary>
        /// Notifies that an actor invoked an action.
        /// </summary>
        internal override void NotifyInvokedAction(Actor actor, MethodInfo action, Event receivedEvent)
        {
            if (this.Configuration.IsVerbose)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
                this.LogWriter.LogExecuteAction(actor.Id, stateName, action.Name);
            }
        }

        /// <summary>
        /// Notifies that an actor dequeued an <see cref="Event"/>.
        /// </summary>
        internal override void NotifyDequeuedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
                this.LogWriter.LogDequeueEvent(actor.Id, stateName, e);
            }
        }

        /// <summary>
        /// Notifies that an actor raised an <see cref="Event"/>.
        /// </summary>
        internal override void NotifyRaisedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
                this.LogWriter.LogRaiseEvent(actor.Id, stateName, e);
            }
        }

        /// <summary>
        /// Notifies that an actor is waiting to receive an event of one of the specified types.
        /// </summary>
        internal override void NotifyWaitEvent(Actor actor, IEnumerable<Type> eventTypes)
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

            this.IsActorWaitingToReceiveEvent = true;
            this.QuiescenceCompletionSource.SetResult(true);
        }

        /// <summary>
        /// Notifies that an actor enqueued an event that it was waiting to receive.
        /// </summary>
        internal override void NotifyReceivedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
                this.LogWriter.LogReceiveEvent(actor.Id, stateName, e, wasBlocked: true);
            }

            this.IsActorWaitingToReceiveEvent = false;
            this.QuiescenceCompletionSource = new TaskCompletionSource<bool>();
        }

        /// <summary>
        /// Notifies that an actor received an event without waiting because the event
        /// was already in the inbox when the actor invoked the receive statement.
        /// </summary>
        internal override void NotifyReceivedEventWithoutWaiting(Actor actor, Event e, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose)
            {
                string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
                this.LogWriter.LogReceiveEvent(actor.Id, stateName, e, wasBlocked: false);
            }
        }

        /// <summary>
        /// Notifies that a state machine entered a state.
        /// </summary>
        internal override void NotifyEnteredState(StateMachine stateMachine)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.LogStateTransition(stateMachine.Id, stateMachine.CurrentStateName, isEntry: true);
            }
        }

        /// <summary>
        /// Notifies that a state machine exited a state.
        /// </summary>
        internal override void NotifyExitedState(StateMachine stateMachine)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.LogStateTransition(stateMachine.Id, stateMachine.CurrentStateName, isEntry: false);
            }
        }

        /// <summary>
        /// Notifies that a monitor entered a state.
        /// </summary>
        internal override void NotifyEnteredState(Monitor monitor)
        {
            if (this.Configuration.IsVerbose)
            {
                string monitorState = monitor.CurrentStateNameWithTemperature;
                this.LogWriter.LogMonitorStateTransition(monitor.GetType().FullName, monitor.Id, monitorState,
                    true, monitor.GetHotState());
            }
        }

        /// <summary>
        /// Notifies that a monitor exited a state.
        /// </summary>
        internal override void NotifyExitedState(Monitor monitor)
        {
            if (this.Configuration.IsVerbose)
            {
                string monitorState = monitor.CurrentStateNameWithTemperature;
                this.LogWriter.LogMonitorStateTransition(monitor.GetType().FullName, monitor.Id, monitorState,
                    false, monitor.GetHotState());
            }
        }

        /// <summary>
        /// Notifies that a monitor invoked an action.
        /// </summary>
        internal override void NotifyInvokedAction(Monitor monitor, MethodInfo action, Event receivedEvent)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.LogMonitorExecuteAction(monitor.GetType().FullName, monitor.Id, action.Name,
                    monitor.CurrentStateNameWithTemperature);
            }
        }

        /// <summary>
        /// Notifies that a monitor raised an <see cref="Event"/>.
        /// </summary>
        internal override void NotifyRaisedEvent(Monitor monitor, Event e, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose)
            {
                string monitorState = monitor.CurrentStateNameWithTemperature;
                this.LogWriter.LogMonitorRaiseEvent(monitor.GetType().FullName, monitor.Id,
                    monitorState, e);
            }
        }
    }
}
