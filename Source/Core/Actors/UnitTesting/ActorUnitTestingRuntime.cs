// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors.Timers;

namespace Microsoft.Coyote.Actors.UnitTesting
{
    /// <summary>
    /// Runtime for testing an actor in isolation.
    /// </summary>
    internal sealed class ActorUnitTestingRuntime : ActorRuntime
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
        /// True if the actor is waiting to receive and event, else false.
        /// </summary>
        internal bool IsActorWaitingToReceiveEvent { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorUnitTestingRuntime"/> class.
        /// </summary>
        internal ActorUnitTestingRuntime(Configuration configuration, Type actorType, IRandomValueGenerator valueGenerator)
            : base(configuration, valueGenerator)
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
                actorManager = new StateMachineManager(this, stateMachine, null);
            }
            else
            {
                actorManager = new ActorManager(this, this.Instance, null);
            }

            this.ActorInbox = new EventQueue(actorManager);
            this.Instance.Configure(this, id, actorManager, this.ActorInbox);
            this.Instance.SetupEventHandlers();
            if (this.Instance is StateMachine)
            {
                this.LogWriter.LogCreateStateMachine(this.Instance.Id, null, null);
            }
            else
            {
                this.LogWriter.LogCreateActor(this.Instance.Id, null, null);
            }

            this.IsActorWaitingToReceiveEvent = false;
        }

        /// <summary>
        /// Starts executing the actor-under-test by transitioning it to its initial state
        /// and passing an optional initialization event.
        /// </summary>
        internal Task StartAsync(Event initialEvent)
        {
            var op = new QuiescentOperation();
            this.Instance.CurrentOperation = op;
            this.RunActorEventHandlerAsync(this.Instance, initialEvent, true);
            return op.Completion.Task;
        }

        /// <inheritdoc/>
        public override ActorId CreateActorIdFromName(Type type, string name) => new ActorId(type, name, this, true);

        /// <inheritdoc/>
        public override ActorId CreateActor(Type type, Event initialEvent = null, Operation op = null) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <inheritdoc/>
        public override ActorId CreateActor(Type type, string name, Event initialEvent = null, Operation op = null) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <inheritdoc/>
        public override ActorId CreateActor(ActorId id, Type type, Event initialEvent = null, Operation op = null) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <inheritdoc/>
        public override void SendEvent(ActorId targetId, Event e, Operation op = null, SendOptions options = null) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <inheritdoc/>
        internal override ActorId CreateActor(ActorId id, Type type, string name, Event initialEvent,
            Actor creator, Operation op)
        {
            id = id ?? new ActorId(type, null, this);
            if (typeof(StateMachine).IsAssignableFrom(type))
            {
                this.LogWriter.LogCreateStateMachine(id, creator?.Id.Name, creator?.Id.Type);
            }
            else
            {
                this.LogWriter.LogCreateActor(id, creator?.Id.Name, creator?.Id.Type);
            }

            return id;
        }

        /// <inheritdoc/>
        internal override void SendEvent(ActorId targetId, Event e, Actor sender, Operation op, SendOptions options)
        {
            this.Assert(sender is null || this.Instance.Id.Equals(sender.Id),
                string.Format("Only {0} can send an event during this test.", this.Instance.Id.ToString()));
            this.Assert(e != null, string.Format("{0} is sending a null event.", this.Instance.Id.ToString()));
            this.Assert(targetId != null, string.Format("{0} is sending event {1} to a null actor.", this.Instance.Id.ToString(), e.ToString()));

            // by default we pass the operation along on each SendEvent.
            if (op == null && sender != null)
            {
                op = sender.CurrentOperation;
            }

            Guid opGroupId = (op == null) ? Guid.Empty : op.Id;
            if (this.Instance.IsHalted)
            {
                this.LogWriter.LogSendEvent(targetId, sender?.Id.Name, sender?.Id.Type,
                    (sender as StateMachine)?.CurrentStateName ?? string.Empty, e, opGroupId, isTargetHalted: true);
                return;
            }

            this.LogWriter.LogSendEvent(targetId, sender?.Id.Name, sender?.Id.Type,
                (sender as StateMachine)?.CurrentStateName ?? string.Empty, e, opGroupId, isTargetHalted: false);

            if (!targetId.Equals(this.Instance.Id))
            {
                // Drop all events sent to an actor other than the actor-under-test.
                return;
            }

            EnqueueStatus enqueueStatus = this.Instance.Enqueue(e, op, null);
            if (enqueueStatus == EnqueueStatus.EventHandlerNotRunning)
            {
                this.RunActorEventHandlerAsync(this.Instance, null, false);
            }
        }

        /// <summary>
        /// Runs a new asynchronous actor event handler.
        /// </summary>
        private Task RunActorEventHandlerAsync(Actor actor, Event initialEvent, bool isFresh)
        {
            return Task.Run(async () =>
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
                    this.IsRunning = false;
                    this.RaiseOnFailureEvent(ex);
                }
            });
        }

        /// <inheritdoc/>
        internal override IActorTimer CreateActorTimer(TimerInfo info, Actor owner) =>
            throw new NotSupportedException("Invoking this method is not supported in actor unit testing mode.");

        /// <inheritdoc/>
        internal override void NotifyReceivedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            base.NotifyReceivedEvent(actor, e, eventInfo);
            this.IsActorWaitingToReceiveEvent = false;
        }

        /// <inheritdoc/>
        internal override void NotifyWaitEvent(Actor actor, IEnumerable<Type> eventTypes)
        {
            base.NotifyWaitEvent(actor, eventTypes);
            this.IsActorWaitingToReceiveEvent = true;
        }
    }
}
