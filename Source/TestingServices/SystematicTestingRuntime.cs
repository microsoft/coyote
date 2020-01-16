// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.TestingServices.Coverage;
using Microsoft.Coyote.TestingServices.Scheduling;
using Microsoft.Coyote.TestingServices.Scheduling.Strategies;
using Microsoft.Coyote.TestingServices.StateCaching;
using Microsoft.Coyote.TestingServices.Threading;
using Microsoft.Coyote.TestingServices.Threading.Tasks;
using Microsoft.Coyote.TestingServices.Timers;
using Microsoft.Coyote.TestingServices.Tracing.Schedule;
using Microsoft.Coyote.Threading;
using Microsoft.Coyote.Threading.Tasks;
using Microsoft.Coyote.Utilities;
using EventInfo = Microsoft.Coyote.Runtime.EventInfo;
using Monitor = Microsoft.Coyote.Specifications.Monitor;

namespace Microsoft.Coyote.TestingServices.Runtime
{
    /// <summary>
    /// Runtime for systematically testing controlled concurrent entities
    /// and sources of nondeterminism in a specified program.
    /// </summary>
    internal sealed class SystematicTestingRuntime : CoyoteRuntime
    {
        /// <summary>
        /// The asynchronous operation scheduler.
        /// </summary>
        internal OperationScheduler Scheduler;

        /// <summary>
        /// The intercepting task scheduler.
        /// </summary>
        private readonly InterceptingTaskScheduler TaskScheduler;

        /// <summary>
        /// Data structure containing information regarding testing coverage.
        /// </summary>
        internal CoverageInfo CoverageInfo;

        /// <summary>
        /// The program state cache.
        /// </summary>
        internal StateCache StateCache;

        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        private readonly List<Monitor> Monitors;

        /// <summary>
        /// Map that stores all unique names and their corresponding actor ids.
        /// </summary>
        internal readonly ConcurrentDictionary<string, ActorId> NameValueToActorId;

        /// <summary>
        /// The root task id.
        /// </summary>
        internal readonly int? RootTaskId;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystematicTestingRuntime"/> class.
        /// </summary>
        internal SystematicTestingRuntime(Configuration configuration, ISchedulingStrategy strategy)
            : base(configuration)
        {
            this.Monitors = new List<Monitor>();
            this.RootTaskId = Task.CurrentId;
            this.NameValueToActorId = new ConcurrentDictionary<string, ActorId>();

            this.StateCache = new StateCache(this);
            this.CoverageInfo = new CoverageInfo();

            var scheduleTrace = new ScheduleTrace();
            if (configuration.EnableLivenessChecking && configuration.EnableCycleDetection)
            {
                strategy = new CycleDetectionStrategy(configuration, this.StateCache, scheduleTrace, this.Monitors, strategy);
            }
            else if (configuration.EnableLivenessChecking)
            {
                strategy = new TemperatureCheckingStrategy(configuration, this.Monitors, strategy);
            }

            this.Scheduler = new OperationScheduler(this, strategy, scheduleTrace, this.Configuration);
            this.TaskScheduler = new InterceptingTaskScheduler(this.Scheduler.ControlledTaskMap);

            // Set a provider to the runtime in each asynchronous control flow.
            Provider = new AsyncLocalRuntimeProvider(this);
        }

        /// <summary>
        /// Creates a actor id that is uniquely tied to the specified unique name. The
        /// returned actor id can either be a fresh id (not yet bound to any actor), or
        /// it can be bound to a previously created actor. In the second case, this actor
        /// id can be directly used to communicate with the corresponding actor.
        /// </summary>
        public override ActorId CreateActorIdFromName(Type type, string name)
        {
            // It is important that all actor ids use the monotonically incrementing
            // value as the id during testing, and not the unique name.
            var id = new ActorId(type, name, this);
            return this.NameValueToActorId.GetOrAdd(name, id);
        }

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and with the specified
        /// optional <see cref="Event"/>. This event can only be used to access its payload,
        /// and cannot be handled.
        /// </summary>
        public override ActorId CreateActor(Type type, Event initialEvent = null, Guid opGroupId = default) =>
            this.CreateActor(null, type, null, initialEvent, opGroupId);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, and with the
        /// specified optional <see cref="Event"/>. This event can only be used to access
        /// its payload, and cannot be handled.
        /// </summary>
        public override ActorId CreateActor(Type type, string name, Event initialEvent = null, Guid opGroupId = default) =>
            this.CreateActor(null, type, name, initialEvent, opGroupId);

        /// <summary>
        /// Creates a new actor of the specified type, using the specified <see cref="ActorId"/>.
        /// This method optionally passes an <see cref="Event"/> to the new actor, which can only
        /// be used to access its payload, and cannot be handled.
        /// </summary>
        public override ActorId CreateActor(ActorId id, Type type, Event initialEvent = null, Guid opGroupId = default)
        {
            this.Assert(id != null, "Cannot create an actor using a null actor id.");
            return this.CreateActor(id, type, null, initialEvent, opGroupId);
        }

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and with the specified
        /// optional <see cref="Event"/>. This event can only be used to access its payload,
        /// and cannot be handled. The method returns only when the actor is initialized and
        /// the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public override Task<ActorId> CreateActorAndExecuteAsync(Type type, Event e = null, Guid opGroupId = default) =>
            this.CreateActorAndExecuteAsync(null, type, null, e, opGroupId);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, and with the
        /// specified optional <see cref="Event"/>. This event can only be used to access
        /// its payload, and cannot be handled. The method returns only when the actor is
        /// initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public override Task<ActorId> CreateActorAndExecuteAsync(Type type, string name, Event e = null, Guid opGroupId = default) =>
            this.CreateActorAndExecuteAsync(null, type, name, e, opGroupId);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/>, using the specified unbound
        /// actor id, and passes the specified optional <see cref="Event"/>. This event can only
        /// be used to access its payload, and cannot be handled. The method returns only when
        /// the actor is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        public override Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, Event e = null, Guid opGroupId = default)
        {
            this.Assert(id != null, "Cannot create an actor using a null actor id.");
            return this.CreateActorAndExecuteAsync(id, type, null, e, opGroupId);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to an actor.
        /// </summary>
        public override void SendEvent(ActorId targetId, Event e, Guid opGroupId = default, SendOptions options = null)
        {
            var senderOp = this.Scheduler.GetExecutingOperation<ActorOperation>();
            this.SendEvent(targetId, e, senderOp?.Actor, opGroupId, options);
        }

        /// <summary>
        /// Sends an <see cref="Event"/> to an actor. Returns immediately if the target was already
        /// running. Otherwise blocks until the target handles the event and reaches quiescense.
        /// </summary>
        public override Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event e, Guid opGroupId = default,
            SendOptions options = null)
        {
            var senderOp = this.Scheduler.GetExecutingOperation<ActorOperation>();
            return this.SendEventAndExecuteAsync(targetId, e, senderOp?.Actor, opGroupId, options);
        }

        /// <summary>
        /// Returns the operation group id of the actor with the specified id. Returns <see cref="Guid.Empty"/>
        /// if the id is not set, or if the <see cref="ActorId"/> is not associated with this runtime. During
        /// testing, the runtime asserts that the specified actor is currently executing.
        /// </summary>
        public override Guid GetCurrentOperationGroupId(ActorId currentActorId)
        {
            var callerOp = this.Scheduler.GetExecutingOperation<ActorOperation>();
            this.Assert(callerOp != null && currentActorId == callerOp.Actor.Id,
                "Trying to access the operation group id of '{0}', which is not the currently executing actor.",
                currentActorId);
            return callerOp.Actor.OperationGroupId;
        }

        /// <summary>
        /// Runs the specified test method.
        /// </summary>
        internal void RunTest(Delegate testMethod, string testName)
        {
            this.Assert(testMethod != null, "Unable to run a null test method.");
            this.Assert(Task.CurrentId != null, "The test must execute inside a controlled task.");

            testName = string.IsNullOrEmpty(testName) ? string.Empty : $" '{testName}'";
            this.Logger.WriteLine($"<TestLog> Running test{testName}.");

            this.DispatchWork(new TestEntryPointExecutor(this, testMethod), null);
        }

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, using the specified
        /// unbound actor id, and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled.
        /// </summary>
        internal ActorId CreateActor(ActorId id, Type type, string name, Event initialEvent = null,
            Guid opGroupId = default)
        {
            var creatorOp = this.Scheduler.GetExecutingOperation<ActorOperation>();
            return this.CreateActor(id, type, name, initialEvent, creatorOp?.Actor, opGroupId);
        }

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/>.
        /// </summary>
        internal override ActorId CreateActor(ActorId id, Type type, string name, Event initialEvent, Actor creator,
            Guid opGroupId)
        {
            this.AssertExpectedCallerActor(creator, "CreateActor");

            Actor actor = this.CreateActor(id, type, name, creator, opGroupId);
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
            Guid opGroupId = default)
        {
            var creatorOp = this.Scheduler.GetExecutingOperation<ActorOperation>();
            return this.CreateActorAndExecuteAsync(id, type, name, initialEvent, creatorOp?.Actor, opGroupId);
        }

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/>. The method returns only
        /// when the actor is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        internal override async Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, string name,
            Event initialEvent, Actor creator, Guid opGroupId)
        {
            this.AssertExpectedCallerActor(creator, "CreateActorAndExecuteAsync");
            this.Assert(creator != null, "Only an actor can call 'CreateActorAndExecuteAsync': avoid calling " +
                "it directly from the 'Test' method; instead call it through a 'harness' actor.");

            Actor actor = this.CreateActor(id, type, name, creator, opGroupId);
            this.RunActorEventHandler(actor, initialEvent, true, creator);

            // Wait until the actor reaches quiescence.
            await creator.ReceiveEventAsync(typeof(QuiescentEvent), rev => (rev as QuiescentEvent).ActorId == actor.Id);
            return await Task.FromResult(actor.Id);
        }

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/>.
        /// </summary>
        private Actor CreateActor(ActorId id, Type type, string name, Actor creator, Guid opGroupId)
        {
            this.Assert(type.IsSubclassOf(typeof(Actor)), "Type '{0}' is not an actor.", type.FullName);

            // Using ulong.MaxValue because a 'Create' operation cannot specify
            // the id of its target, because the id does not exist yet.
            this.Scheduler.ScheduleNextEnabledOperation();
            ResetProgramCounter(creator);

            if (id is null)
            {
                id = new ActorId(type, name, this);
            }
            else
            {
                this.Assert(id.Runtime is null || id.Runtime == this, "Unbound actor id '{0}' was created by another runtime.", id.Value);
                this.Assert(id.Type == type.FullName, "Cannot bind actor id '{0}' of type '{1}' to an actor of type '{2}'.",
                    id.Value, id.Type, type.FullName);
                id.Bind(this);
            }

            // The operation group id of the actor is set using the following precedence:
            // (1) To the specified actor creation operation group id, if it is non-empty.
            // (2) To the operation group id of the creator actor, if it exists and is non-empty.
            // (3) To the empty operation group id.
            if (opGroupId == Guid.Empty && creator != null)
            {
                opGroupId = creator.OperationGroupId;
            }

            Actor actor = ActorFactory.Create(type);
            IActorManager actorManager;
            if (actor is StateMachine stateMachine)
            {
                actorManager = new MockStateMachineManager(this, stateMachine, opGroupId);
            }
            else
            {
                actorManager = new MockActorManager(this, actor, opGroupId);
            }

            IEventQueue eventQueue = new MockEventQueue(actorManager, actor);
            actor.Configure(this, id, actorManager, eventQueue);
            actor.SetupEventHandlers();

            if (this.Configuration.ReportActivityCoverage)
            {
                this.ReportActivityCoverageOfActor(actor);
            }

            bool result = this.Scheduler.RegisterOperation(new ActorOperation(actor, this.Scheduler));
            this.Assert(result, "Actor id '{0}' is used by an existing or previously halted actor.", id.Value);
            this.LogWriter.LogCreateActor(id, creator?.Id);

            return actor;
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to an actor.
        /// </summary>
        internal override void SendEvent(ActorId targetId, Event e, Actor sender, Guid opGroupId, SendOptions options)
        {
            if (sender != null)
            {
                this.Assert(targetId != null, "'{0}' is sending to a null actor.", sender.Id);
                this.Assert(e != null, "'{0}' is sending a null event.", sender.Id);
            }
            else
            {
                this.Assert(targetId != null, "Cannot send to a null actor.");
                this.Assert(e != null, "Cannot send a null event.");
            }

            this.AssertExpectedCallerActor(sender, "SendEvent");

            EnqueueStatus enqueueStatus = this.EnqueueEvent(targetId, e, sender, opGroupId, options, out Actor target);
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
            Guid opGroupId, SendOptions options)
        {
            this.Assert(sender is StateMachine, "Only an actor can call 'SendEventAndExecuteAsync': avoid " +
                "calling it directly from the 'Test' method; instead call it through a 'harness' actor.");
            this.Assert(targetId != null, "'{0}' is sending to a null actor.", sender.Id);
            this.Assert(e != null, "'{0}' is sending a null event.", sender.Id);
            this.AssertExpectedCallerActor(sender, "SendEventAndExecuteAsync");

            EnqueueStatus enqueueStatus = this.EnqueueEvent(targetId, e, sender, opGroupId, options, out Actor target);
            if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
            {
                this.RunActorEventHandler(target, null, false, sender as StateMachine);

                // Wait until the actor reaches quiescence.
                await (sender as StateMachine).ReceiveEventAsync(typeof(QuiescentEvent), rev => (rev as QuiescentEvent).ActorId == targetId);
                return true;
            }

            // 'EnqueueStatus.EventHandlerNotRunning' is not returned by 'EnqueueEvent'
            // (even when the actor was previously inactive) when the event 'e' requires
            // no action by the actor (i.e., it implicitly handles the event).
            return enqueueStatus is EnqueueStatus.Dropped || enqueueStatus is EnqueueStatus.NextEventUnavailable;
        }

        /// <summary>
        /// Enqueues an event to the actor with the specified id.
        /// </summary>
        private EnqueueStatus EnqueueEvent(ActorId targetId, Event e, Actor sender, Guid opGroupId,
            SendOptions options, out Actor target)
        {
            target = this.Scheduler.GetOperationWithId<ActorOperation>(targetId.Value)?.Actor;
            this.Assert(target != null,
                "Cannot send event '{0}' to actor id '{1}' that is not bound to an actor instance.",
                e.GetType().FullName, targetId.Value);

            this.Scheduler.ScheduleNextEnabledOperation();
            ResetProgramCounter(sender as StateMachine);

            // The operation group id of this operation is set using the following precedence:
            // (1) To the specified send operation group id, if it is non-empty.
            // (2) To the operation group id of the sender actor, if it exists and is non-empty.
            // (3) To the empty operation group id.
            if (opGroupId == Guid.Empty && sender != null)
            {
                opGroupId = sender.OperationGroupId;
            }

            if (target.IsHalted)
            {
                this.LogWriter.LogSendEvent(targetId, sender?.Id, (sender as StateMachine)?.CurrentStateName ?? string.Empty,
                    e.GetType().FullName, opGroupId, isTargetHalted: true);
                this.Assert(options is null || !options.MustHandle,
                    "A must-handle event '{0}' was sent to '{1}' which has halted.", e.GetType().FullName, targetId);
                this.TryHandleDroppedEvent(e, targetId);
                return EnqueueStatus.Dropped;
            }

            EnqueueStatus enqueueStatus = this.EnqueueEvent(target, e, sender, opGroupId, options);
            if (enqueueStatus == EnqueueStatus.Dropped)
            {
                this.TryHandleDroppedEvent(e, targetId);
            }

            return enqueueStatus;
        }

        /// <summary>
        /// Enqueues an event to the actor with the specified id.
        /// </summary>
        private EnqueueStatus EnqueueEvent(Actor actor, Event e, Actor sender, Guid opGroupId, SendOptions options)
        {
            EventOriginInfo originInfo;

            var stateName = string.Empty;
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
                Assert = options?.Assert ?? -1,
                SendStep = this.Scheduler.ScheduledSteps
            };

            this.LogWriter.LogSendEvent(actor.Id, sender?.Id, stateName, e.GetType().FullName, opGroupId, isTargetHalted: false);
            return actor.Enqueue(e, opGroupId, eventInfo);
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
            op.OnEnabled();

            Task task = new Task(async () =>
            {
                try
                {
                    // Set the runtime in the async control flow runtime provider, allowing
                    // future retrieval in the same asynchronous call stack.
                    Provider.SetCurrentRuntime(this);

                    OperationScheduler.StartOperation(op);

                    if (isFresh)
                    {
                        await actor.InitializeAsync(initialEvent);
                    }

                    await actor.RunEventHandlerAsync();
                    if (syncCaller != null)
                    {
                        this.EnqueueEvent(syncCaller, new QuiescentEvent(actor.Id), actor, actor.OperationGroupId, null);
                    }

                    if (!actor.IsHalted)
                    {
                        ResetProgramCounter(actor);
                    }

                    IO.Debug.WriteLine($"<ScheduleDebug> Completed event handler of '{actor.Id}' on task '{Task.CurrentId}'.");
                    op.OnCompleted();

                    // The actor is inactive or halted, schedule the next enabled operation.
                    this.Scheduler.ScheduleNextEnabledOperation();
                }
                catch (Exception ex)
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
                        IO.Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from '{actor.Id}'.");
                    }
                    else if (innerException is TaskSchedulerException)
                    {
                        IO.Debug.WriteLine($"<Exception> TaskSchedulerException was thrown from '{actor.Id}'.");
                    }
                    else if (innerException is ObjectDisposedException)
                    {
                        IO.Debug.WriteLine($"<Exception> ObjectDisposedException was thrown from '{actor.Id}' with reason '{ex.Message}'.");
                    }
                    else
                    {
                        // Reports the unhandled exception.
                        string message = string.Format(CultureInfo.InvariantCulture,
                            $"Exception '{ex.GetType()}' was thrown in '{actor.Id}', " +
                            $"'{ex.Source}':\n" +
                            $"   {ex.Message}\n" +
                            $"The stack trace is:\n{ex.StackTrace}");
                        this.Scheduler.NotifyAssertionFailure(message, killTasks: true, cancelExecution: false);
                    }
                }
            });

            this.Scheduler.ScheduleOperation(op, task);
            task.Start(this.TaskScheduler);
            this.Scheduler.WaitOperationStart(op);
        }

        /// <summary>
        /// Creates a new <see cref="ControlledTask"/> to execute the specified asynchronous work.
        /// </summary>
        [DebuggerStepThrough]
        internal override ControlledTask CreateControlledTask(Action action, CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            this.Assert(action != null, "The task cannot execute a null action.");
            var executor = new ActionExecutor(this, action);
            this.DispatchWork(executor, null);
            return new MachineTask(this, executor.AwaiterTask);
        }

        /// <summary>
        /// Creates a new <see cref="ControlledTask"/> to execute the specified asynchronous work.
        /// </summary>
        [DebuggerStepThrough]
        internal override ControlledTask CreateControlledTask(Func<ControlledTask> function, CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            this.Assert(function != null, "The task cannot execute a null function.");
            var executor = new FuncExecutor(this, function);
            this.DispatchWork(executor, null);
            return new MachineTask(this, executor.AwaiterTask);
        }

        /// <summary>
        /// Creates a new <see cref="ControlledTask{TResult}"/> to execute the specified asynchronous work.
        /// </summary>
        [DebuggerStepThrough]
        internal override ControlledTask<TResult> CreateControlledTask<TResult>(Func<TResult> function,
            CancellationToken cancellationToken)
        {
            this.Assert(function != null, "The task cannot execute a null function.");
            var executor = new FuncExecutor<TResult>(this, function);
            this.DispatchWork(executor, null);
            return new MachineTask<TResult>(this, executor.AwaiterTask);
        }

        /// <summary>
        /// Creates a new <see cref="ControlledTask{TResult}"/> to execute the specified asynchronous work.
        /// </summary>
        [DebuggerStepThrough]
        internal override ControlledTask<TResult> CreateControlledTask<TResult>(Func<ControlledTask<TResult>> function,
            CancellationToken cancellationToken)
        {
            this.Assert(function != null, "The task cannot execute a null function.");
            var executor = new FuncTaskExecutor<TResult>(this, function);
            this.DispatchWork(executor, null);
            return new MachineTask<TResult>(this, executor.AwaiterTask);
        }

        /// <summary>
        /// Creates a new <see cref="ControlledTask"/> to execute the specified asynchronous delay.
        /// </summary>
        [DebuggerStepThrough]
        internal override ControlledTask CreateControlledTaskDelay(int millisecondsDelay, CancellationToken cancellationToken) =>
            this.CreateControlledTaskDelay(TimeSpan.FromMilliseconds(millisecondsDelay), cancellationToken);

        /// <summary>
        /// Creates a new <see cref="ControlledTask"/> to execute the specified asynchronous delay.
        /// </summary>
        [DebuggerStepThrough]
        internal override ControlledTask CreateControlledTaskDelay(TimeSpan delay, CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            if (delay.TotalMilliseconds == 0)
            {
                // If the delay is 0, then complete synchronously.
                return ControlledTask.CompletedTask;
            }

            var executor = new DelayExecutor(this);
            this.DispatchWork(executor, null);
            return new MachineTask(this, executor.AwaiterTask);
        }

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> associated with the completion of an async
        /// method generated by <see cref="AsyncControlledTaskMethodBuilder"/>.
        /// </summary>
        [DebuggerStepThrough]
        internal override ControlledTask CreateAsyncControlledTaskMethodBuilderTask(Task task)
        {
            if (!this.Scheduler.IsRunning)
            {
                return ControlledTask.FromException(new ExecutionCanceledException());
            }

            this.Scheduler.CheckNoExternalConcurrencyUsed();
            return new MachineTask(this, task);
        }

        /// <summary>
        /// Creates a <see cref="ControlledTask{TResult}"/> associated with the completion of an async
        /// method generated by <see cref="AsyncControlledTaskMethodBuilder"/>.
        /// </summary>
        [DebuggerStepThrough]
        internal override ControlledTask<TResult> CreateAsyncControlledTaskMethodBuilderTask<TResult>(Task<TResult> task)
        {
            if (!this.Scheduler.IsRunning)
            {
                return ControlledTask.FromException<TResult>(new ExecutionCanceledException());
            }

            this.Scheduler.CheckNoExternalConcurrencyUsed();
            return new MachineTask<TResult>(this, task);
        }

        /// <summary>
        /// Schedules the specified <see cref="ControlledTaskExecutor"/> to be executed asynchronously.
        /// This is a fire and forget invocation.
        /// </summary>
        [DebuggerStepThrough]
        internal void DispatchWork(ControlledTaskExecutor executor, Task parentTask)
        {
            var op = new TaskOperation(executor, this.Scheduler);
            this.Scheduler.RegisterOperation(op);
            op.OnEnabled();

            Task task = new Task(async () =>
            {
                try
                {
                    // Set the runtime in the async control flow runtime provider, allowing
                    // future retrieval in the same asynchronous call stack.
                    Provider.SetCurrentRuntime(this);

                    OperationScheduler.StartOperation(op);
                    if (parentTask != null)
                    {
                        op.OnWaitTask(parentTask);
                    }

                    try
                    {
                        await executor.ExecuteAsync();
                    }
                    catch (Exception ex)
                    {
                        // Reports the unhandled exception.
                        string message = string.Format(CultureInfo.InvariantCulture,
                            $"Exception '{ex.GetType()}' was thrown in task {Task.CurrentId}, " +
                            $"'{ex.Source}':\n" +
                            $"   {ex.Message}\n" +
                            $"The stack trace is:\n{ex.StackTrace}");
                        IO.Debug.WriteLine($"<Exception> {message}");
                        executor.TryCompleteWithException(ex);
                    }

                    IO.Debug.WriteLine($"<ScheduleDebug> Completed '{executor.Id}' on task '{Task.CurrentId}'.");
                    op.OnCompleted();

                    // Task has completed, schedule the next enabled operation.
                    this.Scheduler.ScheduleNextEnabledOperation();
                }
                catch (Exception ex)
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
                        IO.Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from task '{Task.CurrentId}'.");
                    }
                    else if (innerException is TaskSchedulerException)
                    {
                        IO.Debug.WriteLine($"<Exception> TaskSchedulerException was thrown from task '{Task.CurrentId}'.");
                    }
                    else
                    {
                        // Reports the unhandled exception.
                        string message = string.Format(CultureInfo.InvariantCulture,
                            $"Exception '{ex.GetType()}' was thrown in task {Task.CurrentId}, " +
                            $"'{ex.Source}':\n" +
                            $"   {ex.Message}\n" +
                            $"The stack trace is:\n{ex.StackTrace}");
                        this.Scheduler.NotifyAssertionFailure(message, killTasks: true, cancelExecution: false);
                    }
                }
            });

            IO.Debug.WriteLine($"<CreateLog> '{executor.Id}' was created to execute task '{task.Id}'.");
            this.Scheduler.ScheduleOperation(op, task);
            task.Start();
            this.Scheduler.WaitOperationStart(op);
            this.Scheduler.ScheduleNextEnabledOperation();
        }

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        [DebuggerStepThrough]
        internal override ControlledTask WaitAllTasksAsync(IEnumerable<ControlledTask> tasks)
        {
            this.Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            this.Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.Assert(callerOp != null,
                "Uncontrolled task with id '{0}' invoked a when-all operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: true);

            List<Exception> exceptions = null;
            foreach (var task in tasks)
            {
                if (task.IsFaulted)
                {
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

                    exceptions.Add(task.Exception);
                }
            }

            if (exceptions != null)
            {
                return ControlledTask.FromException(new AggregateException(exceptions));
            }
            else
            {
                return ControlledTask.CompletedTask;
            }
        }

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        [DebuggerStepThrough]
        internal override ControlledTask<TResult[]> WaitAllTasksAsync<TResult>(IEnumerable<ControlledTask<TResult>> tasks)
        {
            this.Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            this.Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.Assert(callerOp != null,
                "Uncontrolled task with id '{0}' invoked a when-all operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: true);

            int idx = 0;
            TResult[] result = new TResult[tasks.Count()];
            foreach (var task in tasks)
            {
                result[idx] = task.Result;
                idx++;
            }

            return ControlledTask.FromResult(result);
        }

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        [DebuggerStepThrough]
        internal override ControlledTask<ControlledTask> WaitAnyTaskAsync(IEnumerable<ControlledTask> tasks)
        {
            this.Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            this.Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.Assert(callerOp != null,
                "Uncontrolled task with id '{0}' invoked a when-any operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: false);

            ControlledTask result = null;
            foreach (var task in tasks)
            {
                if (task.IsCompleted)
                {
                    result = task;
                    break;
                }
            }

            return ControlledTask.FromResult(result);
        }

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        [DebuggerStepThrough]
        internal override ControlledTask<ControlledTask<TResult>> WaitAnyTaskAsync<TResult>(IEnumerable<ControlledTask<TResult>> tasks)
        {
            this.Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            this.Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.Assert(callerOp != null,
                "Uncontrolled task with id '{0}' invoked a when-any operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: false);

            ControlledTask<TResult> result = null;
            foreach (var task in tasks)
            {
                if (task.IsCompleted)
                {
                    result = task;
                    break;
                }
            }

            return ControlledTask.FromResult(result);
        }

        /// <summary>
        /// Waits for all of the provided <see cref="ControlledTask"/> objects to complete execution.
        /// </summary>
        internal override void WaitAllTasks(params ControlledTask[] tasks) =>
            this.WaitAllTasks(tasks, Timeout.Infinite, default);

        /// <summary>
        /// Waits for all of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified number of milliseconds.
        /// </summary>
        internal override bool WaitAllTasks(ControlledTask[] tasks, int millisecondsTimeout) =>
            this.WaitAllTasks(tasks, millisecondsTimeout, default);

        /// <summary>
        /// Waits for all of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified number of milliseconds or until a cancellation
        /// token is cancelled.
        /// </summary>
        internal override bool WaitAllTasks(ControlledTask[] tasks, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            this.Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            this.Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.Assert(callerOp != null,
                "Uncontrolled task with id '{0}' invoked a wait-all operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: true);

            // TODO: support timeouts during testing, this would become false if there is a timeout.
            return true;
        }

        /// <summary>
        /// Waits for all of the provided <see cref="ControlledTask"/> objects to complete
        /// execution unless the wait is cancelled.
        /// </summary>
        internal override void WaitAllTasks(ControlledTask[] tasks, CancellationToken cancellationToken) =>
            this.WaitAllTasks(tasks, Timeout.Infinite, cancellationToken);

        /// <summary>
        /// Waits for all of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified time interval.
        /// </summary>
        internal override bool WaitAllTasks(ControlledTask[] tasks, TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return this.WaitAllTasks(tasks, (int)totalMilliseconds, default);
        }

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete execution.
        /// </summary>
        [DebuggerStepThrough]
        internal override int WaitAnyTask(params ControlledTask[] tasks) =>
            this.WaitAnyTask(tasks, Timeout.Infinite, default);

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified number of milliseconds.
        /// </summary>
        [DebuggerStepThrough]
        internal override int WaitAnyTask(ControlledTask[] tasks, int millisecondsTimeout) =>
            this.WaitAnyTask(tasks, millisecondsTimeout, default);

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified number of milliseconds or until a cancellation
        /// token is cancelled.
        /// </summary>
        [DebuggerStepThrough]
        internal override int WaitAnyTask(ControlledTask[] tasks, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            this.Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            this.Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.Assert(callerOp != null,
                "Uncontrolled task with id '{0}' invoked a wait-any operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: false);

            int result = -1;
            for (int i = 0; i < tasks.Length; i++)
            {
                if (tasks[i].IsCompleted)
                {
                    result = i;
                    break;
                }
            }

            // TODO: support timeouts during testing, this would become false if there is a timeout.
            return result;
        }

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete
        /// execution unless the wait is cancelled.
        /// </summary>
        [DebuggerStepThrough]
        internal override int WaitAnyTask(ControlledTask[] tasks, CancellationToken cancellationToken) =>
            this.WaitAnyTask(tasks, Timeout.Infinite, cancellationToken);

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified time interval.
        /// </summary>
        [DebuggerStepThrough]
        internal override int WaitAnyTask(ControlledTask[] tasks, TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return this.WaitAnyTask(tasks, (int)totalMilliseconds, default);
        }

        /// <summary>
        /// Creates a controlled awaiter that switches into a target environment.
        /// </summary>
        [DebuggerStepThrough]
        internal override ControlledYieldAwaitable.ControlledYieldAwaiter CreateControlledYieldAwaiter()
        {
            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            callerOp.OnGetControlledAwaiter();
            return new ControlledYieldAwaitable.ControlledYieldAwaiter(this, default);
        }

        /// <summary>
        /// Ends the wait for the completion of the yield operation.
        /// </summary>
        [DebuggerStepThrough]
        internal override void OnGetYieldResult(YieldAwaitable.YieldAwaiter awaiter)
        {
            this.Scheduler.ScheduleNextEnabledOperation();
            awaiter.GetResult();
        }

        /// <summary>
        /// Sets the action to perform when the yield operation completes.
        /// </summary>
        [DebuggerHidden]
        internal override void OnYieldCompleted(Action continuation, YieldAwaitable.YieldAwaiter awaiter) =>
            this.DispatchYield(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the yield operation completes.
        /// </summary>
        [DebuggerHidden]
        internal override void OnUnsafeYieldCompleted(Action continuation, YieldAwaitable.YieldAwaiter awaiter) =>
            this.DispatchYield(continuation);

        /// <summary>
        /// Dispatches the work.
        /// </summary>
        [DebuggerHidden]
        private void DispatchYield(Action continuation)
        {
            try
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                this.Assert(callerOp != null,
                    "Uncontrolled task with id '{0}' invoked a yield operation.",
                    Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");
                IO.Debug.WriteLine("<ControlledTask> '{0}' is executing a yield operation.", callerOp.Id);
                this.DispatchWork(new ActionExecutor(this, continuation), null);
            }
            catch (ExecutionCanceledException)
            {
                IO.Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from task '{Task.CurrentId}'.");
            }
        }

        /// <summary>
        /// Creates a mutual exclusion lock that is compatible with <see cref="ControlledTask"/> objects.
        /// </summary>
        internal override ControlledLock CreateControlledLock()
        {
            var id = (ulong)Interlocked.Increment(ref this.LockIdCounter) - 1;
            return new MachineLock(this, id);
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
        /// Tries to create a new monitor of the given type.
        /// </summary>
        internal override void TryCreateMonitor(Type type)
        {
            if (this.Monitors.Any(m => m.GetType() == type))
            {
                // Idempotence: only one monitor per type can exist.
                return;
            }

            this.Assert(type.IsSubclassOf(typeof(Monitor)), "Type '{0}' is not a subclass of Monitor.", type.FullName);

            ActorId id = new ActorId(type, null, this);

            Monitor monitor = Activator.CreateInstance(type) as Monitor;
            monitor.Initialize(this, id);
            monitor.InitializeStateInformation();

            this.LogWriter.LogCreateMonitor(type.FullName, monitor.Id);

            if (this.Configuration.ReportActivityCoverage)
            {
                this.ReportActivityCoverageOfMonitor(monitor);
            }

            this.Monitors.Add(monitor);

            monitor.GotoStartState();
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        internal override void Monitor(Type type, Actor sender, Event e)
        {
            this.AssertExpectedCallerActor(sender, "Monitor");
            foreach (var m in this.Monitors)
            {
                if (m.GetType() == type)
                {
                    m.MonitorEvent(sender, e);
                    break;
                }
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        [DebuggerHidden]
        public override void Assert(bool predicate)
        {
            if (!predicate)
            {
                this.Scheduler.NotifyAssertionFailure("Detected an assertion failure.");
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        [DebuggerHidden]
        public override void Assert(bool predicate, string s, object arg0)
        {
            if (!predicate)
            {
                this.Scheduler.NotifyAssertionFailure(string.Format(CultureInfo.InvariantCulture, s, arg0.ToString()));
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        [DebuggerHidden]
        public override void Assert(bool predicate, string s, object arg0, object arg1)
        {
            if (!predicate)
            {
                this.Scheduler.NotifyAssertionFailure(string.Format(CultureInfo.InvariantCulture, s, arg0.ToString(), arg1.ToString()));
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        [DebuggerHidden]
        public override void Assert(bool predicate, string s, object arg0, object arg1, object arg2)
        {
            if (!predicate)
            {
                this.Scheduler.NotifyAssertionFailure(string.Format(CultureInfo.InvariantCulture, s, arg0.ToString(), arg1.ToString(), arg2.ToString()));
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        [DebuggerHidden]
        public override void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                this.Scheduler.NotifyAssertionFailure(string.Format(CultureInfo.InvariantCulture, s, args));
            }
        }

        /// <summary>
        /// Asserts that the actor calling an actor method is also
        /// the actor that is currently executing.
        /// </summary>
        [DebuggerHidden]
        private void AssertExpectedCallerActor(Actor caller, string calledAPI)
        {
            if (caller is null)
            {
                return;
            }

            var op = this.Scheduler.GetExecutingOperation<ActorOperation>();
            if (op is null)
            {
                return;
            }

            this.Assert(op.Actor.Equals(caller), "'{0}' invoked {1} on behalf of '{2}'.",
                op.Actor.Id, calledAPI, caller.Id);
        }

        /// <summary>
        /// Checks that no monitor is in a hot state upon program termination.
        /// If the program is still running, then this method returns without
        /// performing a check.
        /// </summary>
        [DebuggerHidden]
        internal void CheckNoMonitorInHotStateAtTermination()
        {
            if (!this.Scheduler.HasFullyExploredSchedule)
            {
                return;
            }

            foreach (var monitor in this.Monitors)
            {
                if (monitor.IsInHotState(out string stateName))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "Monitor '{0}' detected liveness bug in hot state '{1}' at the end of program execution.",
                        monitor.GetType().FullName, stateName);
                    this.Scheduler.NotifyAssertionFailure(message, killTasks: false, cancelExecution: false);
                }
            }
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        internal override bool GetNondeterministicBooleanChoice(Actor caller, int maxValue)
        {
            caller = caller ?? this.Scheduler.GetExecutingOperation<ActorOperation>()?.Actor;
            this.AssertExpectedCallerActor(caller, "Random");

            if (caller is StateMachine callerStateMachine)
            {
                (callerStateMachine.Manager as MockStateMachineManager).ProgramCounter++;
            }
            else if (caller is Actor callerActor)
            {
                (callerActor.Manager as MockActorManager).ProgramCounter++;
            }

            var choice = this.Scheduler.GetNextNondeterministicBooleanChoice(maxValue);
            this.LogWriter.LogRandom(caller?.Id, choice);
            return choice;
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        internal override bool GetFairNondeterministicBooleanChoice(Actor caller, string uniqueId)
        {
            caller = caller ?? this.Scheduler.GetExecutingOperation<ActorOperation>()?.Actor;
            this.AssertExpectedCallerActor(caller, "FairRandom");

            if (caller is StateMachine callerStateMachine)
            {
                (callerStateMachine.Manager as MockStateMachineManager).ProgramCounter++;
            }
            else if (caller is Actor callerActor)
            {
                (callerActor.Manager as MockActorManager).ProgramCounter++;
            }

            var choice = this.Scheduler.GetNextNondeterministicBooleanChoice(2, uniqueId);
            this.LogWriter.LogRandom(caller?.Id, choice);
            return choice;
        }

        /// <summary>
        /// Returns a nondeterministic integer, that can be
        /// controlled during analysis or testing.
        /// </summary>
        internal override int GetNondeterministicIntegerChoice(Actor caller, int maxValue)
        {
            caller = caller ?? this.Scheduler.GetExecutingOperation<ActorOperation>()?.Actor;
            this.AssertExpectedCallerActor(caller, "RandomInteger");

            var choice = this.Scheduler.GetNextNondeterministicIntegerChoice(maxValue);
            this.LogWriter.LogRandom(caller?.Id, choice);
            return choice;
        }

        /// <summary>
        /// Injects a context switch point that can be systematically explored during testing.
        /// </summary>
        internal override void ExploreContextSwitch()
        {
            var callerOp = this.Scheduler.GetExecutingOperation<AsyncOperation>();
            if (callerOp != null)
            {
                this.Scheduler.ScheduleNextEnabledOperation();
            }
        }

        /// <summary>
        /// Notifies that an actor invoked an action.
        /// </summary>
        internal override void NotifyInvokedAction(Actor actor, MethodInfo action, Event receivedEvent)
        {
            string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
            this.LogWriter.LogExecuteAction(actor.Id, stateName, action.Name);
        }

        /// <summary>
        /// Notifies that an actor dequeued an <see cref="Event"/>.
        /// </summary>
        internal override void NotifyDequeuedEvent(Actor actor, Event e, EventInfo eventInfo)
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
                this.Scheduler.ScheduleNextEnabledOperation();
                ResetProgramCounter(actor);
            }

            string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
            this.LogWriter.LogDequeueEvent(actor.Id, stateName, eventInfo.EventName);
        }

        /// <summary>
        /// Notifies that an actor dequeued the default <see cref="Event"/>.
        /// </summary>
        internal override void NotifyDefaultEventDequeued(Actor actor)
        {
            this.Scheduler.ScheduleNextEnabledOperation();
            ResetProgramCounter(actor);
        }

        /// <summary>
        /// Notifies that the inbox of the specified actor is about to be
        /// checked to see if the default event handler should fire.
        /// </summary>
        internal override void NotifyDefaultEventHandlerCheck(Actor actor)
        {
            this.Scheduler.ScheduleNextEnabledOperation();
        }

        /// <summary>
        /// Notifies that an actor raised an <see cref="Event"/>.
        /// </summary>
        internal override void NotifyRaisedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
            this.LogWriter.LogRaiseEvent(actor.Id, stateName, eventInfo.EventName);
        }

        /// <summary>
        /// Notifies that an actor is handling a raised event.
        /// </summary>
        internal override void NotifyHandleRaisedEvent(Actor actor, Event e)
        {
            string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
            this.LogWriter.LogHandleRaisedEvent(actor.Id, stateName, e.GetType().FullName);
        }

        /// <summary>
        /// Notifies that an actor called <see cref="Actor.ReceiveEventAsync(Type[])"/>
        /// or one of its overloaded methods.
        /// </summary>
        internal override void NotifyReceiveCalled(Actor actor)
        {
            this.AssertExpectedCallerActor(actor, "ReceiveEventAsync");
        }

        /// <summary>
        /// Notifies that an actor enqueued an event that it was waiting to receive.
        /// </summary>
        internal override void NotifyReceivedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
            this.LogWriter.LogReceiveEvent(actor.Id, stateName, e.GetType().FullName, wasBlocked: true);
            var op = this.Scheduler.GetOperationWithId<ActorOperation>(actor.Id.Value);
            op.OnReceivedEvent();
        }

        /// <summary>
        /// Notifies that an actor received an event without waiting because the event
        /// was already in the inbox when the actor invoked the receive statement.
        /// </summary>
        internal override void NotifyReceivedEventWithoutWaiting(Actor actor, Event e, EventInfo eventInfo)
        {
            string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
            this.LogWriter.LogReceiveEvent(actor.Id, stateName, e.GetType().FullName, wasBlocked: false);
            this.Scheduler.ScheduleNextEnabledOperation();
            ResetProgramCounter(actor);
        }

        /// <summary>
        /// Notifies that an actor is waiting for the specified task to complete.
        /// </summary>
        internal override void NotifyWaitTask(Actor actor, Task task)
        {
            this.Assert(task != null, "'{0}' is waiting for a null task to complete.", actor.Id);
            this.Assert(task.IsCompleted || task.IsCanceled || task.IsFaulted,
                "'{0}' is trying to wait for an uncontrolled task or awaiter to complete. Please make sure to avoid using " +
                "concurrency APIs such as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside actor handlers. If you are using " +
                "external libraries that are executing concurrently, you will need to mock them during testing.",
                Task.CurrentId);
        }

        /// <summary>
        /// Notifies that an actor is waiting to receive an event of one of the specified types.
        /// </summary>
        internal override void NotifyWaitEvent(Actor actor, IEnumerable<Type> eventTypes)
        {
            string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : string.Empty;
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

            this.Scheduler.ScheduleNextEnabledOperation();
            ResetProgramCounter(actor);
        }

        /// <summary>
        /// Notifies that a state machine entered a state.
        /// </summary>
        internal override void NotifyEnteredState(StateMachine stateMachine)
        {
            string stateName = stateMachine.CurrentStateName;
            this.LogWriter.LogStateTransition(stateMachine.Id, stateName, isEntry: true);
        }

        /// <summary>
        /// Notifies that a state machine exited a state.
        /// </summary>
        internal override void NotifyExitedState(StateMachine stateMachine)
        {
            this.LogWriter.LogStateTransition(stateMachine.Id, stateMachine.CurrentStateName, isEntry: false);
        }

        /// <summary>
        /// Notifies that a state machine invoked pop.
        /// </summary>
        internal override void NotifyPopState(StateMachine stateMachine)
        {
            this.AssertExpectedCallerActor(stateMachine, "Pop");
            this.LogWriter.LogPopState(stateMachine.Id, string.Empty, stateMachine.CurrentStateName);
        }

        /// <summary>
        /// Notifies that a state machine invoked an action.
        /// </summary>
        internal override void NotifyInvokedOnEntryAction(StateMachine stateMachine, MethodInfo action, Event receivedEvent)
        {
            string stateName = stateMachine.CurrentStateName;
            this.LogWriter.LogExecuteAction(stateMachine.Id, stateName, action.Name);
        }

        /// <summary>
        /// Notifies that a state machine invoked an action.
        /// </summary>
        internal override void NotifyInvokedOnExitAction(StateMachine stateMachine, MethodInfo action, Event receivedEvent)
        {
            string stateName = stateMachine.CurrentStateName;
            this.LogWriter.LogExecuteAction(stateMachine.Id, stateName, action.Name);
        }

        /// <summary>
        /// Notifies that a monitor entered a state.
        /// </summary>
        internal override void NotifyEnteredState(Monitor monitor)
        {
            string monitorState = monitor.CurrentStateNameWithTemperature;
            this.LogWriter.LogMonitorStateTransition(monitor.GetType().FullName, monitor.Id, monitorState, true, monitor.GetHotState());
        }

        /// <summary>
        /// Notifies that a monitor exited a state.
        /// </summary>
        internal override void NotifyExitedState(Monitor monitor)
        {
            this.LogWriter.LogMonitorStateTransition(monitor.GetType().FullName, monitor.Id,
                monitor.CurrentStateNameWithTemperature, false, monitor.GetHotState());
        }

        /// <summary>
        /// Notifies that a monitor invoked an action.
        /// </summary>
        internal override void NotifyInvokedAction(Monitor monitor, MethodInfo action, Event receivedEvent)
        {
            string monitorState = monitor.CurrentStateNameWithTemperature;
            this.LogWriter.LogMonitorExecuteAction(monitor.GetType().FullName, monitor.Id, monitorState, action.Name);
        }

        /// <summary>
        /// Notifies that a monitor raised an <see cref="Event"/>.
        /// </summary>
        internal override void NotifyRaisedEvent(Monitor monitor, Event e, EventInfo eventInfo)
        {
            string monitorState = monitor.CurrentStateNameWithTemperature;
            this.LogWriter.LogMonitorRaiseEvent(monitor.GetType().FullName, monitor.Id,
                monitorState, eventInfo.EventName);
        }

        /// <summary>
        /// Notifies that the currently executing task operation started executing the
        /// specified asynchronous controlled task state machine.
        /// </summary>
        internal override void NotifyStartedAsyncControlledTaskStateMachine(Type stateMachineType)
        {
            try
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                callerOp.SetRootAsyncControlledTaskStateMachine(stateMachineType);
            }
            catch (RuntimeException ex)
            {
                this.Assert(false, ex.Message);
            }
        }

        /// <summary>
        /// Notifies that the currently executing task operation is scheduling the next action
        /// when the specified awaiter completes.
        /// </summary>
        [DebuggerHidden]
        internal override void NotifyInvokedAwaitOnCompleted(Type awaiterType, Type stateMachineType)
        {
            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.Assert(callerOp.IsAwaiterControlled, "Controlled task '{0}' is trying to wait for an uncontrolled " +
                "task or awaiter to complete. Please make sure to use Coyote APIs to express concurrency " +
                "(e.g. ControlledTask instead of Task).",
                Task.CurrentId);
            this.Assert(awaiterType.Namespace == typeof(ControlledTask).Namespace,
                "Controlled task '{0}' is trying to wait for an uncontrolled task or awaiter to complete. " +
                "Please make sure to use Coyote APIs to express concurrency (e.g. ControlledTask instead of Task).",
                Task.CurrentId);
            callerOp.SetExecutingAsyncControlledTaskStateMachineType(stateMachineType);
        }

        /// <summary>
        /// Notifies that a <see cref="ControlledTaskExecutor"/> is waiting for the specified task to complete.
        /// </summary>
        internal void NotifyWaitTask(ControlledTaskExecutor actor, Task task)
        {
            this.Assert(task != null, "Controlled task '{0}' is waiting for a null task to complete.", Task.CurrentId);
            if (!task.IsCompleted)
            {
                var op = this.Scheduler.GetOperationWithId<TaskOperation>(actor.Id.Value);
                op.OnWaitTask(task);
            }
        }

        /// <summary>
        /// Get the coverage graph information (if any). This information is only available
        /// when <see cref="Configuration.ReportActivityCoverage"/> is enabled.
        /// </summary>
        /// <returns>A new CoverageInfo object.</returns>
        public CoverageInfo GetCoverageInfo()
        {
            var result = this.CoverageInfo;
            if (result != null)
            {
                var builder = this.LogWriter.GetLogsOfType<ActorRuntimeLogGraphBuilder>().FirstOrDefault();
                if (builder != null)
                {
                    result.CoverageGraph = builder.Graph;
                }
            }

            return result;
        }

        /// <summary>
        /// Reports actors that are to be covered in coverage report.
        /// </summary>
        private void ReportActivityCoverageOfActor(Actor actor)
        {
            var name = actor.GetType().FullName;
            if (this.CoverageInfo.IsMachineDeclared(name))
            {
                return;
            }

            if (actor is StateMachine stateMachine)
            {
                // Fetch states.
                var states = stateMachine.GetAllStates();
                foreach (var state in states)
                {
                    this.CoverageInfo.DeclareMachineState(name, state);
                }

                // Fetch registered events.
                var pairs = stateMachine.GetAllStateEventPairs();
                foreach (var tup in pairs)
                {
                    this.CoverageInfo.DeclareStateEvent(name, tup.Item1, tup.Item2);
                }
            }
        }

        /// <summary>
        /// Reports coverage for the specified monitor.
        /// </summary>
        private void ReportActivityCoverageOfMonitor(Monitor monitor)
        {
            var monitorName = monitor.GetType().FullName;
            if (this.CoverageInfo.IsMachineDeclared(monitorName))
            {
                return;
            }

            // Fetch states.
            var states = monitor.GetAllStates();

            foreach (var state in states)
            {
                this.CoverageInfo.DeclareMachineState(monitorName, state);
            }

            // Fetch registered events.
            var pairs = monitor.GetAllStateEventPairs();

            foreach (var tup in pairs)
            {
                this.CoverageInfo.DeclareStateEvent(monitorName, tup.Item1, tup.Item2);
            }
        }

        /// <summary>
        /// Resets the program counter of the specified actor.
        /// </summary>
        private static void ResetProgramCounter(Actor actor)
        {
            if (actor is StateMachine stateMachine)
            {
                (stateMachine.Manager as MockStateMachineManager).ProgramCounter = 0;
            }
            else if (actor != null)
            {
                (actor.Manager as MockActorManager).ProgramCounter = 0;
            }
        }

        /// <summary>
        /// Returns the fingerprint of the current program state.
        /// </summary>
        [DebuggerStepThrough]
        internal Fingerprint GetProgramState()
        {
            Fingerprint fingerprint = null;

            unchecked
            {
                int hash = 19;

                foreach (var operation in this.Scheduler.GetRegisteredOperations().OrderBy(op => op.Id))
                {
                    if (operation is ActorOperation actorOperation && !actorOperation.Actor.IsHalted)
                    {
                        hash = (hash * 31) + actorOperation.Actor.GetCachedState();
                    }
                }

                foreach (var monitor in this.Monitors)
                {
                    hash = (hash * 31) + monitor.GetCachedState();
                }

                fingerprint = new Fingerprint(hash);
            }

            return fingerprint;
        }

        /// <summary>
        /// Throws an <see cref="AssertionFailureException"/> exception
        /// containing the specified exception.
        /// </summary>
        [DebuggerStepThrough]
        internal override void WrapAndThrowException(Exception exception, string s, params object[] args)
        {
            string message = string.Format(CultureInfo.InvariantCulture, s, args);
            this.Scheduler.NotifyAssertionFailure(message);
        }

        /// <summary>
        /// Waits until all actors have finished execution.
        /// </summary>
        [DebuggerStepThrough]
        internal async Task WaitAsync()
        {
            await this.Scheduler.WaitAsync();
            this.IsRunning = false;
        }

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        [DebuggerStepThrough]
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Monitors.Clear();
            }

            base.Dispose(disposing);
        }
    }
}
