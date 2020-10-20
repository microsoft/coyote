// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Actors.Timers.Mocks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting;

namespace Microsoft.Coyote.Actors.Mocks
{
    /// <summary>
    /// Implements an actor manager that is used during systematic testing.
    /// </summary>
    internal class MockActorManager : ActorManager
    {
        /// <summary>
        /// Program counter used for state-caching. Distinguishes
        /// scheduling from non-deterministic choices.
        /// </summary>
        internal int ProgramCounter;

        /// <summary>
        /// True if a transition statement was called in the current action, else false.
        /// </summary>
        internal bool IsTransitionStatementCalledInCurrentAction;

        /// <summary>
        /// True if the actor is executing an on exit action, else false.
        /// </summary>
        internal bool IsInsideOnExit;

        /// <summary>
        /// The asynchronous operation scheduler.
        /// </summary>
        internal OperationScheduler Scheduler => this.Context.Scheduler;

        /// <summary>
        /// If true, the actor execution is controlled, else false.
        /// </summary>
        protected override bool IsExecutionControlled => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockActorManager"/> class.
        /// </summary>
        internal MockActorManager(ActorExecutionContext context, Actor instance, EventGroup group)
            : base(context, instance, group)
        {
            this.ProgramCounter = 0;
            this.IsTransitionStatementCalledInCurrentAction = false;
            this.IsInsideOnExit = false;
        }

        /// <inheritdoc/>
        public override ActorId CreateActorIdFromName(Type type, string name)
        {
            // It is important that all actor ids use the monotonically incrementing
            // value as the id during testing, and not the unique name.
            var id = this.CreateActorId(type, name);
            return this.Context.NameValueToActorId.GetOrAdd(name, id);
        }

        /// <inheritdoc/>
        public override ActorId CreateActor(Type type, Event initialEvent = null, EventGroup group = null) =>
            this.CreateActor(null, type, null, initialEvent, group);

        /// <inheritdoc/>
        public override ActorId CreateActor(Type type, string name, Event initialEvent = null, EventGroup group = null) =>
            this.CreateActor(null, type, name, initialEvent, group);

        /// <inheritdoc/>
        public override ActorId CreateActor(ActorId id, Type type, Event initialEvent = null, EventGroup group = null)
        {
            this.Assert(id != null, "Cannot create an actor using a null actor id.");
            return this.CreateActor(id, type, null, initialEvent, group);
        }

        /// <inheritdoc/>
        public override Task<ActorId> CreateActorAndExecuteAsync(Type type, Event initialEvent = null, EventGroup group = null) =>
            this.CreateActorAndExecuteAsync(null, type, null, initialEvent, group);

        /// <inheritdoc/>
        public override Task<ActorId> CreateActorAndExecuteAsync(Type type, string name, Event initialEvent = null, EventGroup group = null) =>
            this.CreateActorAndExecuteAsync(null, type, name, initialEvent, group);

        /// <inheritdoc/>
        public override Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, Event initialEvent = null, EventGroup group = null)
        {
            this.Assert(id != null, "Cannot create an actor using a null actor id.");
            return this.CreateActorAndExecuteAsync(id, type, null, initialEvent, group);
        }

        /// <inheritdoc/>
        public override void SendEvent(ActorId targetId, Event initialEvent, EventGroup group = default, SendOptions options = null)
        {
            var senderOp = this.Scheduler.GetExecutingOperation<ActorOperation>();
            this.SendEvent(targetId, initialEvent, senderOp?.Actor, group, options);
        }

        /// <inheritdoc/>
        public override Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event initialEvent,
            EventGroup group = null, SendOptions options = null)
        {
            var senderOp = this.Scheduler.GetExecutingOperation<ActorOperation>();
            return this.SendEventAndExecuteAsync(targetId, initialEvent, senderOp?.Actor, group, options);
        }

        /// <inheritdoc/>
        public override EventGroup GetCurrentEventGroup(ActorId currentActorId)
        {
            var callerOp = this.Scheduler.GetExecutingOperation<ActorOperation>();
            this.Assert(callerOp != null && currentActorId == callerOp.Actor.Id,
                "Trying to access the event group id of {0}, which is not the currently executing actor.",
                currentActorId);
            return callerOp.Actor.CurrentEventGroup;
        }

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, using the specified
        /// unbound actor id, and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled.
        /// </summary>
        internal ActorId CreateActor(ActorId id, Type type, string name, Event initialEvent = null, EventGroup group = null)
        {
            var creatorOp = this.Scheduler.GetExecutingOperation<ActorOperation>();
            return this.CreateActor(id, type, name, initialEvent, creatorOp?.Actor, group);
        }

        /// <summary>
        /// Creates a new <see cref="Actor"/> of the specified <see cref="Type"/>.
        /// </summary>
        internal override ActorId CreateActor(ActorId id, Type type, string name, Event initialEvent, Actor creator, EventGroup group)
        {
            this.SpecificationEngine.AssertExpectedCallerActor(creator, "CreateActor");
            Actor actor = this.CreateActor(id, type, name, creator, group);
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
            EventGroup group = null)
        {
            var creatorOp = this.Scheduler.GetExecutingOperation<ActorOperation>();
            return this.CreateActorAndExecuteAsync(id, type, name, initialEvent, creatorOp?.Actor, group);
        }

        /// <summary>
        /// Creates a new <see cref="Actor"/> of the specified <see cref="Type"/>. The method
        /// returns only when the actor is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        internal override async Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, string name, Event initialEvent,
            Actor creator, EventGroup group)
        {
            this.SpecificationEngine.AssertExpectedCallerActor(creator, "CreateActorAndExecuteAsync");
            this.Assert(creator != null, "Only an actor can call 'CreateActorAndExecuteAsync': avoid calling " +
                "it directly from the test method; instead call it through a test driver actor.");

            Actor actor = this.CreateActor(id, type, name, creator, group);
            this.RunActorEventHandler(actor, initialEvent, true, creator);

            // Wait until the actor reaches quiescence.
            await creator.ReceiveEventAsync(typeof(QuiescentEvent), rev => (rev as QuiescentEvent).ActorId == actor.Id);
            return await Task.FromResult(actor.Id);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to an actor.
        /// </summary>
        internal override void SendEvent(ActorId targetId, Event e, Actor sender, EventGroup group, SendOptions options)
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

            this.SpecificationEngine.AssertExpectedCallerActor(sender, "SendEvent");

            EnqueueStatus enqueueStatus = this.EnqueueEvent(targetId, e, sender, group, options, out Actor target);
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
            EventGroup group, SendOptions options)
        {
            this.Assert(sender is StateMachine, "Only an actor can call 'SendEventAndExecuteAsync': avoid " +
                "calling it directly from the test method; instead call it through a test driver actor.");
            this.Assert(e != null, "{0} is sending a null event.", sender.Id);
            this.Assert(targetId != null, "{0} is sending event {1} to a null actor.", sender.Id, e);
            this.SpecificationEngine.AssertExpectedCallerActor(sender, "SendEventAndExecuteAsync");
            EnqueueStatus enqueueStatus = this.EnqueueEvent(targetId, e, sender, group, options, out Actor target);
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
        /// Creates a new <see cref="Actor"/> of the specified <see cref="Type"/>.
        /// </summary>
        internal override Actor CreateActor(ActorId id, Type type, string name, Actor creator, EventGroup group)
        {
            this.Assert(type.IsSubclassOf(typeof(Actor)), "Type '{0}' is not an actor.", type.FullName);

            // Using ulong.MaxValue because a Create operation cannot specify
            // the id of its target, because the id does not exist yet.
            this.Scheduler.ScheduleNextOperation();
            ResetProgramCounter(creator);

            if (id is null)
            {
                id = this.CreateActorId(type, name);
            }
            else
            {
                this.Assert(id.Runtime is null || id.Runtime == this, "Unbound actor id '{0}' was created by another runtime.", id.Value);
                this.Assert(id.Type == type.FullName, "Cannot bind actor id '{0}' of type '{1}' to an actor of type '{2}'.",
                    id.Value, id.Type, type.FullName);
                id.Bind(this);
            }

            // If a group was not provided, inherit the current event group from the creator (if any).
            if (group == null && creator != null)
            {
                group = creator.Manager.CurrentEventGroup;
            }

            Actor actor = ActorFactory.Create(type);
            ActorManager actorManager = new MockActorManager(this.Context, actor, group);

            IEventQueue eventQueue = new MockEventQueue(actorManager, actor);
            actor.Configure(actorManager, id, eventQueue);
            actor.SetupEventHandlers();

            if (this.Configuration.ReportActivityCoverage)
            {
                actor.ReportActivityCoverage(this.Context.CoverageInfo);
            }

            bool result = this.Scheduler.RegisterOperation(new ActorOperation(actor, this.Scheduler));
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

        /// <summary>
        /// Enqueues an event to the actor with the specified id.
        /// </summary>
        private EnqueueStatus EnqueueEvent(ActorId targetId, Event e, Actor sender, EventGroup group, SendOptions options, out Actor target)
        {
            target = this.Scheduler.GetOperationWithId<ActorOperation>(targetId.Value)?.Actor;
            this.Assert(target != null,
                "Cannot send event '{0}' to actor id '{1}' that is not bound to an actor instance.",
                e.GetType().FullName, targetId.Value);

            this.Scheduler.ScheduleNextOperation();
            ResetProgramCounter(sender as StateMachine);

            // If no group is provided we default to passing along the group from the sender.
            if (group == null && sender != null)
            {
                group = sender.Manager.CurrentEventGroup;
            }

            if (target.IsHalted)
            {
                Guid groupId = group == null ? Guid.Empty : group.Id;
                this.LogWriter.LogSendEvent(targetId, sender?.Id.Name, sender?.Id.Type,
                    (sender as StateMachine)?.CurrentStateName ?? string.Empty, e, groupId, isTargetHalted: true);
                this.Assert(options is null || !options.MustHandle,
                    "A must-handle event '{0}' was sent to {1} which has halted.", e.GetType().FullName, targetId);
                this.TryHandleDroppedEvent(e, targetId);
                return EnqueueStatus.Dropped;
            }

            EnqueueStatus enqueueStatus = this.EnqueueEvent(target, e, sender, group, options);
            if (enqueueStatus == EnqueueStatus.Dropped)
            {
                this.TryHandleDroppedEvent(e, targetId);
            }

            return enqueueStatus;
        }

        /// <summary>
        /// Enqueues an event to the actor with the specified id.
        /// </summary>
        private EnqueueStatus EnqueueEvent(Actor actor, Event e, Actor sender, EventGroup group, SendOptions options)
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

            Guid opId = group == null ? Guid.Empty : group.Id;
            this.LogWriter.LogSendEvent(actor.Id, sender?.Id.Name, sender?.Id.Type, stateName,
                e, opId, isTargetHalted: false);
            return actor.Enqueue(e, group, eventInfo);
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
            var op = this.Scheduler.GetOperationWithId<ActorOperation>(actor.Id.Value);
            Task task = new Task(async () =>
            {
                try
                {
                    // Update the current asynchronous control flow with this runtime instance,
                    // allowing future retrieval in the same asynchronous call stack.
                    CoyoteRuntime.AssignAsyncControlFlowRuntime(this.Context.Runtime);

                    this.Scheduler.StartOperation(op);

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
                        ResetProgramCounter(actor);
                    }

                    IO.Debug.WriteLine("<ScheduleDebug> Completed operation {0} on task '{1}'.", actor.Id, Task.CurrentId);
                    op.OnCompleted();

                    // The actor is inactive or halted, schedule the next enabled operation.
                    this.Scheduler.ScheduleNextOperation();
                }
                catch (Exception ex)
                {
                    this.ProcessUnhandledExceptionInOperation(op, ex);
                }
            });

            task.Start();
            this.Scheduler.WaitOperationStart(op);
        }

        /// <summary>
        /// Creates a new timer that sends a <see cref="TimerElapsedEvent"/> to its owner actor.
        /// </summary>
        internal override IActorTimer CreateActorTimer(TimerInfo info, Actor owner)
        {
            var id = this.CreateActorId(typeof(MockStateMachineTimer));
            this.CreateActor(id, typeof(MockStateMachineTimer), new TimerSetupEvent(info, owner, this.Configuration.TimeoutDelay));
            return this.Scheduler.GetOperationWithId<ActorOperation>(id.Value).Actor as MockStateMachineTimer;
        }

        /// <summary>
        /// Returns a controlled nondeterministic boolean choice.
        /// </summary>
        internal override bool GetNondeterministicBooleanChoice(int maxValue, string callerName, string callerType)
        {
            var caller = this.Scheduler.GetExecutingOperation<ActorOperation>()?.Actor;
            if (caller is Actor callerActor)
            {
                (callerActor.Manager as MockActorManager).ProgramCounter++;
            }

            var choice = this.Scheduler.GetNextNondeterministicBooleanChoice(maxValue);
            this.LogWriter.LogRandom(choice, callerName ?? caller?.Id.Name, callerType ?? caller?.Id.Type);
            return choice;
        }

        /// <summary>
        /// Returns a controlled nondeterministic integer choice.
        /// </summary>
        internal override int GetNondeterministicIntegerChoice(int maxValue, string callerName, string callerType)
        {
            var caller = this.Scheduler.GetExecutingOperation<ActorOperation>()?.Actor;
            if (caller is Actor callerActor)
            {
                (callerActor.Manager as MockActorManager).ProgramCounter++;
            }

            var choice = this.Scheduler.GetNextNondeterministicIntegerChoice(maxValue);
            this.LogWriter.LogRandom(choice, callerName ?? caller?.Id.Name, callerType ?? caller?.Id.Type);
            return choice;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override void OnDropEvent(Event e, EventGroup group, EventInfo eventInfo)
        {
            this.Assert(!eventInfo.MustHandle, "{0} halted before dequeueing must-handle event '{1}'.",
                this.Instance.Id, e.GetType().FullName);
            this.TryHandleDroppedEvent(e, this.Instance.Id);
        }

        /// <summary>
        /// Logs that the dequeued an <see cref="Event"/>.
        /// </summary>
        internal override void LogDequeuedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            var op = this.Scheduler.GetOperationWithId<ActorOperation>(actor.Id.Value);

            // Skip `ReceiveEventAsync` if the last operation exited the previous event handler,
            // to avoid scheduling duplicate `ReceiveEventAsync` operations.
            if (op.SkipNextReceiveSchedulingPoint)
            {
                op.SkipNextReceiveSchedulingPoint = false;
            }
            else
            {
                this.Scheduler.ScheduleNextOperation();
                ResetProgramCounter(actor);
            }

            base.LogDequeuedEvent(actor, e, eventInfo);
        }

        /// <summary>
        /// Logs that the dequeued the default <see cref="Event"/>.
        /// </summary>
        internal override void LogDefaultEventDequeued(Actor actor)
        {
            this.Scheduler.ScheduleNextOperation();
            ResetProgramCounter(actor);
        }

        /// <summary>
        /// Notifies that the inbox of the specified actor is about to be checked
        /// to see if the default event handler should fire.
        /// </summary>
        internal override void LogDefaultEventHandlerCheck(Actor actor)
        {
            this.Scheduler.ScheduleNextOperation();
        }

        /// <summary>
        /// Logs that the called <see cref="Actor.ReceiveEventAsync(Type[])"/> or one of its overloaded methods.
        /// </summary>
        internal override void LogReceiveCalled(Actor actor)
        {
            this.SpecificationEngine.AssertExpectedCallerActor(actor, "ReceiveEventAsync");
        }

        /// <summary>
        /// Logs that the enqueued an event that it was waiting to receive.
        /// </summary>
        protected override void LogReceivedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
            this.LogWriter.LogReceiveEvent(actor.Id, stateName, e, wasBlocked: true);
            var op = this.Scheduler.GetOperationWithId<ActorOperation>(actor.Id.Value);
            op.OnReceivedEvent();
        }

        /// <summary>
        /// Logs that the received an event without waiting because the event
        /// was already in the inbox when the actor invoked the receive statement.
        /// </summary>
        protected override void LogReceivedEventWithoutWaiting(Actor actor, Event e, EventInfo eventInfo)
        {
            string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
            this.LogWriter.LogReceiveEvent(actor.Id, stateName, e, wasBlocked: false);
            this.Scheduler.ScheduleNextOperation();
            ResetProgramCounter(actor);
        }

        /// <summary>
        /// Logs that the is waiting for the specified task to complete.
        /// </summary>
        internal override void LogWaitTask(Actor actor, Task task)
        {
            this.Assert(task != null, "{0} is waiting for a null task to complete.", actor.Id);

            bool finished = task.IsCompleted || task.IsCanceled || task.IsFaulted;
            if (!finished)
            {
                this.Assert(finished,
                    "Controlled task '{0}' is trying to wait for an uncontrolled task or awaiter to complete. Please " +
                    "make sure to avoid using concurrency APIs (e.g. 'Task.Run', 'Task.Delay' or 'Task.Yield' from " +
                    "the 'System.Threading.Tasks' namespace) inside actor handlers. If you are using external libraries " +
                    "that are executing concurrently, you will need to mock them during testing.",
                    Task.CurrentId);
            }
        }

        /// <summary>
        /// Logs that the is waiting to receive an event of one of the specified types.
        /// </summary>
        protected override void LogWaitEvent(Actor actor, IEnumerable<Type> eventTypes)
        {
            string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
            var op = this.Scheduler.GetOperationWithId<ActorOperation>(actor.Id.Value);
            op.OnWaitEvent(eventTypes);

            var eventWaitTypesArray = eventTypes.ToArray();
            if (eventWaitTypesArray.Length == 1)
            {
                this.LogWriter.LogWaitEvent(actor.Id, stateName, eventWaitTypesArray[0]);
            }
            else
            {
                this.LogWriter.LogWaitEvent(actor.Id, stateName, eventWaitTypesArray);
            }

            this.Scheduler.ScheduleNextOperation();
            ResetProgramCounter(actor);
        }

        /// <inheritdoc/>
        internal override int GetCachedState()
        {
            unchecked
            {
                var hash = 19;
                hash = (hash * 31) + this.IsEventHandlerRunning.GetHashCode();
                hash = (hash * 31) + this.ProgramCounter;
                return hash;
            }
        }

        /// <summary>
        /// Resets the program counter of the specified actor.
        /// </summary>
        protected static void ResetProgramCounter(Actor actor)
        {
            if (actor != null)
            {
                (actor.Manager as MockActorManager).ProgramCounter = 0;
            }
        }

        /// <summary>
        /// Processes an unhandled exception in the specified asynchronous operation.
        /// </summary>
        private void ProcessUnhandledExceptionInOperation(AsyncOperation op, Exception ex)
        {
            // TODO: testing only.
            string message = null;
            Exception exception = UnwrapException(ex);
            if (exception is ExecutionCanceledException || exception is TaskSchedulerException)
            {
                IO.Debug.WriteLine("<Exception> {0} was thrown from operation '{1}'.",
                    exception.GetType().Name, op.Name);
            }
            else if (exception is ObjectDisposedException)
            {
                IO.Debug.WriteLine("<Exception> {0} was thrown from operation '{1}' with reason '{2}'.",
                    exception.GetType().Name, op.Name, ex.Message);
            }
            else if (op is ActorOperation actorOp)
            {
                message = string.Format(CultureInfo.InvariantCulture,
                    $"Unhandled exception. {exception.GetType()} was thrown in actor {actorOp.Name}, " +
                    $"'{exception.Source}':\n" +
                    $"   {exception.Message}\n" +
                    $"The stack trace is:\n{exception.StackTrace}");
            }
            else
            {
                message = string.Format(CultureInfo.InvariantCulture, $"Unhandled exception. {exception}");
            }

            if (message != null)
            {
                // Report the unhandled exception.
                this.Scheduler.NotifyUnhandledException(exception, message);
            }
        }

        /// <summary>
        /// Unwraps the specified exception.
        /// </summary>
        private static Exception UnwrapException(Exception ex)
        {
            // TODO: testing only.
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
    }
}
