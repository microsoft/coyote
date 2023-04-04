// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Actors.UnitTesting
{
    /// <summary>
    /// Execution context for testing an actor in isolation.
    /// </summary>
    internal sealed class SiloedExecutionContext<T> : ActorExecutionContext
        where T : Actor
    {
        /// <summary>
        /// The actor being tested.
        /// </summary>
        internal readonly Actor Instance;

        /// <summary>
        /// The inbox of the actor being tested.
        /// </summary>
        internal readonly EventQueue Inbox;

        /// <summary>
        /// Completes when the actor being tested reaches quiescence.
        /// </summary>
        private TaskCompletionSource<bool> ActorCompletionSource;

        /// <summary>
        /// True if the actor is waiting to receive and event, else false.
        /// </summary>
        internal bool IsActorWaitingToReceiveEvent { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SiloedExecutionContext{T}"/> class.
        /// </summary>
        internal SiloedExecutionContext(Configuration configuration, ActorLogManager logManager, LogWriter logWriter)
            : base(configuration, logManager)
        {
            // First instantiate the runtime.
            CoyoteRuntime runtime = RuntimeProvider.CreateAndInstall(configuration, logWriter, logManager, this);
            this.WithRuntime(runtime);

            // Then initialize the actor instance to manage in isolation.
            var id = this.CreateActorId(typeof(T), null);
            this.Instance = ActorFactory.Create(typeof(T));
            ActorOperation op = this.GetOrCreateActorOperation(id, this.Instance);
            this.Inbox = new EventQueue(this.Instance);
            this.Instance.Configure(this, id, op, this.Inbox, null);
            this.Instance.SetupEventHandlers();

            string creator = typeof(ActorTestKit<>).ToString();
            if (this.Instance is StateMachine)
            {
                this.LogManager.LogCreateStateMachine(this.Instance.Id, creator, creator);
            }
            else
            {
                this.LogManager.LogCreateActor(this.Instance.Id, creator, creator);
            }

            this.IsActorWaitingToReceiveEvent = false;
        }

        /// <summary>
        /// Starts executing the actor-under-test by transitioning it to its initial state
        /// and passing an optional initialization event.
        /// </summary>
        internal Task StartAsync(Event initialEvent)
        {
            this.RunActorEventHandlerAsync(this.Instance, initialEvent, true);
            return this.ActorCompletionSource.Task;
        }

        /// <inheritdoc/>
        public override ActorId CreateActor(Type type, Event initialEvent = null, EventGroup eventGroup = null) =>
            this.CreateActor(null, type, null, initialEvent, null, eventGroup);

        /// <inheritdoc/>
        public override ActorId CreateActor(Type type, string name, Event initialEvent = null, EventGroup eventGroup = null) =>
            this.CreateActor(null, type, name, initialEvent, null, eventGroup);

        /// <inheritdoc/>
        public override ActorId CreateActor(ActorId id, Type type, Event initialEvent = null, EventGroup eventGroup = null) =>
            this.CreateActor(id, type, null, initialEvent, null, eventGroup);

        /// <inheritdoc/>
        public override Task<ActorId> CreateActorAndExecuteAsync(Type type, Event initialEvent = null, EventGroup eventGroup = null) =>
            this.CreateActorAndExecuteAsync(null, type, null, initialEvent, null, eventGroup);

        /// <inheritdoc/>
        public override Task<ActorId> CreateActorAndExecuteAsync(Type type, string name, Event initialEvent = null, EventGroup eventGroup = null) =>
            this.CreateActorAndExecuteAsync(null, type, name, initialEvent, null, eventGroup);

        /// <inheritdoc/>
        public override Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, Event initialEvent = null, EventGroup eventGroup = null) =>
            this.CreateActorAndExecuteAsync(id, type, null, initialEvent, null, eventGroup);

        /// <inheritdoc/>
        internal override ActorId CreateActor(ActorId id, Type type, string name, Event initialEvent, Actor creator, EventGroup eventGroup)
        {
            id ??= this.CreateActorId(type, null);
            if (typeof(StateMachine).IsAssignableFrom(type))
            {
                this.LogManager.LogCreateStateMachine(id, creator?.Id.Name, creator?.Id.Type);
            }
            else
            {
                this.LogManager.LogCreateActor(id, creator?.Id.Name, creator?.Id.Type);
            }

            return id;
        }

        /// <inheritdoc/>
        internal override Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, string name, Event initialEvent,
            Actor creator, EventGroup eventGroup)
        {
            id ??= this.CreateActorId(type, null);
            if (typeof(StateMachine).IsAssignableFrom(type))
            {
                this.LogManager.LogCreateStateMachine(id, creator?.Id.Name, creator?.Id.Type);
            }
            else
            {
                this.LogManager.LogCreateActor(id, creator?.Id.Name, creator?.Id.Type);
            }

            return Task.FromResult(id);
        }

        /// <inheritdoc/>
        public override void SendEvent(ActorId targetId, Event initialEvent, EventGroup eventGroup = default, SendOptions options = null) =>
            this.SendEvent(targetId, initialEvent, null, eventGroup, options);

        /// <inheritdoc/>
        public override Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event initialEvent, EventGroup eventGroup = null,
            SendOptions options = null) =>
            this.SendEventAndExecuteAsync(targetId, initialEvent, null, eventGroup, options);

        /// <inheritdoc/>
        internal override void SendEvent(ActorId targetId, Event e, Actor sender, EventGroup eventGroup, SendOptions options)
        {
            this.Assert(sender is null || sender.Id == this.Instance.Id,
                "Only {0} can send an event during this test.", this.Instance.Id.ToString());
            this.Assert(e != null, "{0} is sending a null event.", this.Instance.Id.ToString());
            this.Assert(targetId != null, "{0} is sending event {1} to a null actor.", this.Instance.Id.ToString(), e.ToString());

            // If no group is provided we default to passing along the group from the sender.
            if (eventGroup is null && sender != null)
            {
                eventGroup = sender.EventGroup;
            }

            Guid opId = eventGroup is null ? Guid.Empty : eventGroup.Id;
            if (this.Instance.IsHalted)
            {
                this.LogManager.LogSendEvent(targetId, sender?.Id.Name, sender?.Id.Type,
                    (sender as StateMachine)?.CurrentStateName ?? default, e, opId, isTargetHalted: true);
                return;
            }

            this.LogManager.LogSendEvent(targetId, sender?.Id.Name, sender?.Id.Type,
                (sender as StateMachine)?.CurrentStateName ?? default, e, opId, isTargetHalted: false);

            if (targetId != this.Instance.Id)
            {
                // Drop all events sent to an actor other than the actor-under-test.
                return;
            }

            EnqueueStatus enqueueStatus = this.Instance.Enqueue(e, eventGroup, null);
            if (enqueueStatus == EnqueueStatus.EventHandlerNotRunning)
            {
                this.RunActorEventHandlerAsync(this.Instance, null, false);
            }
        }

        /// <inheritdoc/>
        internal override Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event e, Actor sender, EventGroup eventGroup,
            SendOptions options)
        {
            this.SendEvent(targetId, e, sender, eventGroup, options);
            return this.ActorCompletionSource.Task;
        }

        /// <summary>
        /// Runs a new asynchronous actor event handler.
        /// </summary>
        private Task RunActorEventHandlerAsync(Actor actor, Event initialEvent, bool isFresh)
        {
            this.ActorCompletionSource = new TaskCompletionSource<bool>();
            return Task.Run(async () =>
            {
                try
                {
                    if (isFresh)
                    {
                        await actor.InitializeAsync(initialEvent);
                    }

                    await actor.RunEventHandlerAsync();
                    this.ActorCompletionSource.SetResult(true);
                }
                catch (Exception ex)
                {
                    this.Runtime.IsRunning = false;
                    this.RaiseOnFailureEvent(ex);
                    this.ActorCompletionSource.SetException(ex);
                }
            });
        }

        /// <inheritdoc/>
        internal override IActorTimer CreateActorTimer(TimerInfo info, Actor owner) => null;

        /// <inheritdoc/>
        internal override void LogReceivedEvent(Actor actor, Event e)
        {
            base.LogReceivedEvent(actor, e);
            this.IsActorWaitingToReceiveEvent = false;
            this.ActorCompletionSource = new TaskCompletionSource<bool>();
        }

        /// <inheritdoc/>
        internal override void LogWaitEvent(Actor actor, IEnumerable<Type> eventTypes)
        {
            base.LogWaitEvent(actor, eventTypes);
            this.IsActorWaitingToReceiveEvent = true;
            this.ActorCompletionSource.SetResult(true);
        }

        /// <inheritdoc/>
        public override EventGroup GetCurrentEventGroup(ActorId currentActorId) =>
            this.Instance.Id == currentActorId ? this.Instance.CurrentEventGroup : EventGroup.Null;
    }
}
