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

using Microsoft.Coyote.Machines;
using Microsoft.Coyote.Machines.Timers;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.TestingServices.Coverage;
using Microsoft.Coyote.TestingServices.Scheduling;
using Microsoft.Coyote.TestingServices.Scheduling.Strategies;
using Microsoft.Coyote.TestingServices.StateCaching;
using Microsoft.Coyote.TestingServices.Threading;
using Microsoft.Coyote.TestingServices.Threading.Tasks;
using Microsoft.Coyote.TestingServices.Timers;
using Microsoft.Coyote.TestingServices.Tracing.Error;
using Microsoft.Coyote.TestingServices.Tracing.Schedule;
using Microsoft.Coyote.Threading;
using Microsoft.Coyote.Threading.Tasks;
using Microsoft.Coyote.Utilities;

using EventInfo = Microsoft.Coyote.Runtime.EventInfo;
using Monitor = Microsoft.Coyote.Specifications.Monitor;

namespace Microsoft.Coyote.TestingServices.Runtime
{
    /// <summary>
    /// Runtime for systematically testing machines by controlling the scheduler.
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
        /// The bug trace.
        /// </summary>
        internal BugTrace BugTrace;

        /// <summary>
        /// Data structure containing information
        /// regarding testing coverage.
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
        /// Map from unique ids to operations.
        /// </summary>
        private readonly ConcurrentDictionary<ulong, MachineOperation> MachineOperations;

        /// <summary>
        /// Map that stores all unique names and their corresponding machine ids.
        /// </summary>
        internal readonly ConcurrentDictionary<string, MachineId> NameValueToMachineId;

        /// <summary>
        /// Set of all machine Ids created by this runtime.
        /// </summary>
        internal HashSet<MachineId> CreatedMachineIds;

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
            this.MachineOperations = new ConcurrentDictionary<ulong, MachineOperation>();
            this.RootTaskId = Task.CurrentId;
            this.CreatedMachineIds = new HashSet<MachineId>();
            this.NameValueToMachineId = new ConcurrentDictionary<string, MachineId>();

            this.BugTrace = new BugTrace();
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
        /// Creates a machine id that is uniquely tied to the specified unique name. The
        /// returned machine id can either be a fresh id (not yet bound to any machine),
        /// or it can be bound to a previously created machine. In the second case, this
        /// machine id can be directly used to communicate with the corresponding machine.
        /// </summary>
        public override MachineId CreateMachineIdFromName(Type type, string machineName)
        {
            // It is important that all machine ids use the monotonically incrementing
            // value as the id during testing, and not the unique name.
            var mid = new MachineId(type, machineName, this);
            return this.NameValueToMachineId.GetOrAdd(machineName, mid);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with
        /// the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        public override MachineId CreateMachine(Type type, Event e = null, Guid opGroupId = default) =>
            this.CreateMachine(null, type, null, e, opGroupId);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        public override MachineId CreateMachine(Type type, string machineName, Event e = null, Guid opGroupId = default) =>
            this.CreateMachine(null, type, machineName, e, opGroupId);

        /// <summary>
        /// Creates a new machine of the specified type, using the specified <see cref="MachineId"/>.
        /// This method optionally passes an <see cref="Event"/> to the new machine, which can only
        /// be used to access its payload, and cannot be handled.
        /// </summary>
        public override MachineId CreateMachine(MachineId mid, Type type, Event e = null, Guid opGroupId = default)
        {
            this.Assert(mid != null, "Cannot create a machine using a null machine id.");
            return this.CreateMachine(mid, type, null, e, opGroupId);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with the
        /// specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when
        /// the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public override Task<MachineId> CreateMachineAndExecuteAsync(Type type, Event e = null, Guid opGroupId = default) =>
            this.CreateMachineAndExecuteAsync(null, type, null, e, opGroupId);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and with
        /// the specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when the
        /// machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public override Task<MachineId> CreateMachineAndExecuteAsync(Type type, string machineName, Event e = null, Guid opGroupId = default) =>
            this.CreateMachineAndExecuteAsync(null, type, machineName, e, opGroupId);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This
        /// event can only be used to access its payload, and cannot be handled. The method
        /// returns only when the machine is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        public override Task<MachineId> CreateMachineAndExecuteAsync(MachineId mid, Type type, Event e = null, Guid opGroupId = default)
        {
            this.Assert(mid != null, "Cannot create a machine using a null machine id.");
            return this.CreateMachineAndExecuteAsync(mid, type, null, e, opGroupId);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        public override void SendEvent(MachineId target, Event e, Guid opGroupId = default, SendOptions options = null)
        {
            this.SendEvent(target, e, this.GetExecutingMachine<StateMachine>(), opGroupId, options);
        }

        /// <summary>
        /// Sends an <see cref="Event"/> to a machine. Returns immediately if the target machine was already
        /// running. Otherwise blocks until the machine handles the event and reaches quiescense.
        /// </summary>
        public override Task<bool> SendEventAndExecuteAsync(MachineId target, Event e, Guid opGroupId = default, SendOptions options = null) =>
            this.SendEventAndExecuteAsync(target, e, this.GetExecutingMachine<StateMachine>(), opGroupId, options);

        /// <summary>
        /// Returns the operation group id of the specified machine. Returns <see cref="Guid.Empty"/>
        /// if the id is not set, or if the <see cref="MachineId"/> is not associated with this runtime.
        /// During testing, the runtime asserts that the specified machine is currently executing.
        /// </summary>
        public override Guid GetCurrentOperationGroupId(MachineId currentMachine)
        {
            this.Assert(currentMachine == this.GetCurrentMachineId(),
                "Trying to access the operation group id of '{0}', which is not the currently executing machine.",
                currentMachine);

            StateMachine machine = this.GetMachineFromId<StateMachine>(currentMachine);
            return machine is null ? Guid.Empty : machine.OperationGroupId;
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

            var machine = new TestExecutionMachine(this, testMethod);
            this.DispatchWork(machine, null);
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled.
        /// </summary>
        internal MachineId CreateMachine(MachineId mid, Type type, string machineName, Event e = null, Guid opGroupId = default)
        {
            StateMachine creator = this.GetExecutingMachine<StateMachine>();
            return this.CreateMachine(mid, type, machineName, e, creator, opGroupId);
        }

        /// <summary>
        /// Creates a new <see cref="StateMachine"/> of the specified <see cref="Type"/>.
        /// </summary>
        internal override MachineId CreateMachine(MachineId mid, Type type, string machineName, Event e,
            StateMachine creator, Guid opGroupId)
        {
            this.AssertCorrectCallerMachine(creator, "CreateMachine");
            if (creator != null)
            {
                this.AssertNoPendingTransitionStatement(creator, "create a machine");
            }

            StateMachine machine = this.CreateMachine(mid, type, machineName, creator, opGroupId);

            this.BugTrace.AddCreateMachineStep(creator, machine.Id, e is null ? null : new EventInfo(e));
            this.RunMachineEventHandler(machine, e, true, null);

            return machine.Id;
        }

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled. The method returns only
        /// when the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        internal Task<MachineId> CreateMachineAndExecuteAsync(MachineId mid, Type type, string machineName, Event e = null,
            Guid opGroupId = default)
        {
            StateMachine creator = this.GetExecutingMachine<StateMachine>();
            return this.CreateMachineAndExecuteAsync(mid, type, machineName, e, creator, opGroupId);
        }

        /// <summary>
        /// Creates a new <see cref="StateMachine"/> of the specified <see cref="Type"/>. The
        /// method returns only when the machine is initialized and the <see cref="Event"/>
        /// (if any) is handled.
        /// </summary>
        internal override async Task<MachineId> CreateMachineAndExecuteAsync(MachineId mid, Type type, string machineName, Event e,
            StateMachine creator, Guid opGroupId)
        {
            this.AssertCorrectCallerMachine(creator, "CreateMachineAndExecute");
            this.Assert(creator != null,
                "Only a machine can call 'CreateMachineAndExecute': avoid calling it directly from the 'Test' method; instead call it through a 'harness' machine.");
            this.AssertNoPendingTransitionStatement(creator, "create a machine");

            StateMachine machine = this.CreateMachine(mid, type, machineName, creator, opGroupId);

            this.BugTrace.AddCreateMachineStep(creator, machine.Id, e is null ? null : new EventInfo(e));
            this.RunMachineEventHandler(machine, e, true, creator);

            // Wait until the machine reaches quiescence.
            await creator.Receive(typeof(QuiescentEvent), rev => (rev as QuiescentEvent).MachineId == machine.Id);

            return await Task.FromResult(machine.Id);
        }

        /// <summary>
        /// Creates a new <see cref="StateMachine"/> of the specified <see cref="Type"/>.
        /// </summary>
        private StateMachine CreateMachine(MachineId mid, Type type, string machineName, StateMachine creator, Guid opGroupId)
        {
            this.Assert(type.IsSubclassOf(typeof(StateMachine)), "Type '{0}' is not a machine.", type.FullName);

            // Using ulong.MaxValue because a 'Create' operation cannot specify
            // the id of its target, because the id does not exist yet.
            this.Scheduler.ScheduleNextEnabledOperation();
            ResetProgramCounter(creator);

            if (mid is null)
            {
                mid = new MachineId(type, machineName, this);
            }
            else
            {
                this.Assert(mid.Runtime is null || mid.Runtime == this, "Unbound machine id '{0}' was created by another runtime.", mid.Value);
                this.Assert(mid.Type == type.FullName, "Cannot bind machine id '{0}' of type '{1}' to a machine of type '{2}'.",
                    mid.Value, mid.Type, type.FullName);
                mid.Bind(this);
            }

            // The operation group id of the machine is set using the following precedence:
            // (1) To the specified machine creation operation group id, if it is non-empty.
            // (2) To the operation group id of the creator machine, if it exists and is non-empty.
            // (3) To the empty operation group id.
            if (opGroupId == Guid.Empty && creator != null)
            {
                opGroupId = creator.OperationGroupId;
            }

            StateMachine machine = StateMachineFactory.Create(type);
            IMachineStateManager stateManager = new SerializedMachineStateManager(this, machine, opGroupId);
            IEventQueue eventQueue = new SerializedMachineEventQueue(stateManager, machine);
            machine.Initialize(this, mid, stateManager, eventQueue);
            machine.InitializeStateInformation();

            if (this.Configuration.ReportActivityCoverage)
            {
                this.ReportActivityCoverageOfMachine(machine);
            }

            bool result = this.MachineMap.TryAdd(mid, machine);
            this.Assert(result, "Machine id '{0}' is used by an existing machine.", mid.Value);

            this.Assert(!this.CreatedMachineIds.Contains(mid),
                "Machine id '{0}' of a previously halted machine cannot be reused to create a new machine of type '{1}'",
                mid.Value, type.FullName);
            this.CreatedMachineIds.Add(mid);
            this.MachineOperations.GetOrAdd(mid.Value, new MachineOperation(machine, this.Scheduler));

            this.LogWriter.OnCreateMachine(mid, creator?.Id);

            return machine;
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        internal override void SendEvent(MachineId target, Event e, Actor sender, Guid opGroupId, SendOptions options)
        {
            if (sender != null)
            {
                this.Assert(target != null, "Machine '{0}' is sending to a null machine.", sender.Id);
                this.Assert(e != null, "Machine '{0}' is sending a null event.", sender.Id);
            }
            else
            {
                this.Assert(target != null, "Cannot send to a null machine.");
                this.Assert(e != null, "Cannot send a null event.");
            }

            this.AssertCorrectCallerMachine(sender as StateMachine, "SendEvent");

            EnqueueStatus enqueueStatus = this.EnqueueEvent(target, e, sender, opGroupId, options, out StateMachine targetMachine);
            if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
            {
                this.RunMachineEventHandler(targetMachine, null, false, null);
            }
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine. Returns immediately if the target machine was
        /// already running. Otherwise blocks until the machine handles the event and reaches quiescense.
        /// </summary>
        internal override async Task<bool> SendEventAndExecuteAsync(MachineId target, Event e, Actor sender,
            Guid opGroupId, SendOptions options)
        {
            this.Assert(sender is StateMachine,
                "Only a machine can call 'SendEventAndExecute': avoid calling it directly from the 'Test' method; instead call it through a 'harness' machine.");
            this.Assert(target != null, "Machine '{0}' is sending to a null machine.", sender.Id);
            this.Assert(e != null, "Machine '{0}' is sending a null event.", sender.Id);
            this.AssertCorrectCallerMachine(sender as StateMachine, "SendEventAndExecute");

            EnqueueStatus enqueueStatus = this.EnqueueEvent(target, e, sender, opGroupId, options, out StateMachine targetMachine);
            if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
            {
                this.RunMachineEventHandler(targetMachine, null, false, sender as StateMachine);

                // Wait until the machine reaches quiescence.
                await (sender as StateMachine).Receive(typeof(QuiescentEvent), rev => (rev as QuiescentEvent).MachineId == target);
                return true;
            }

            // 'EnqueueStatus.EventHandlerNotRunning' is not returned by 'EnqueueEvent' (even when
            // the machine was previously inactive) when the event 'e' requires no action by the
            // machine (i.e., it implicitly handles the event).
            return enqueueStatus is EnqueueStatus.Dropped || enqueueStatus is EnqueueStatus.NextEventUnavailable;
        }

        /// <summary>
        /// Enqueues an event to the machine with the specified id.
        /// </summary>
        private EnqueueStatus EnqueueEvent(MachineId target, Event e, Actor sender, Guid opGroupId,
            SendOptions options, out StateMachine targetMachine)
        {
            this.Assert(this.CreatedMachineIds.Contains(target),
                "Cannot send event '{0}' to machine id '{1}' that was never previously bound to a machine of type '{2}'",
                e.GetType().FullName, target.Value, target.Type);

            this.Scheduler.ScheduleNextEnabledOperation();
            ResetProgramCounter(sender as StateMachine);

            // The operation group id of this operation is set using the following precedence:
            // (1) To the specified send operation group id, if it is non-empty.
            // (2) To the operation group id of the sender machine, if it exists and is non-empty.
            // (3) To the empty operation group id.
            if (opGroupId == Guid.Empty && sender != null)
            {
                opGroupId = sender.OperationGroupId;
            }

            targetMachine = this.GetMachineFromId<StateMachine>(target);
            if (targetMachine is null || targetMachine.IsHalted)
            {
                this.LogWriter.OnSend(target, sender?.Id, (sender as StateMachine)?.CurrentStateName ?? string.Empty,
                    e.GetType().FullName, opGroupId, isTargetHalted: true);
                this.Assert(options is null || !options.MustHandle,
                    "A must-handle event '{0}' was sent to the halted machine '{1}'.", e.GetType().FullName, target);
                this.TryHandleDroppedEvent(e, target);
                return EnqueueStatus.Dropped;
            }

            if (sender is StateMachine)
            {
                this.AssertNoPendingTransitionStatement(sender as StateMachine, "send an event");
            }

            EnqueueStatus enqueueStatus = this.EnqueueEvent(targetMachine, e, sender, opGroupId, options);
            if (enqueueStatus == EnqueueStatus.Dropped)
            {
                this.TryHandleDroppedEvent(e, target);
            }

            return enqueueStatus;
        }

        /// <summary>
        /// Enqueues an event to the machine with the specified id.
        /// </summary>
        private EnqueueStatus EnqueueEvent(StateMachine machine, Event e, Actor sender, Guid opGroupId, SendOptions options)
        {
            EventOriginInfo originInfo;
            if (sender is StateMachine senderMachine)
            {
                originInfo = new EventOriginInfo(sender.Id, senderMachine.GetType().FullName,
                    NameResolver.GetStateNameForLogging(senderMachine.CurrentState));
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
                Assume = options?.Assume ?? -1,
                SendStep = this.Scheduler.ScheduledSteps
            };

            this.LogWriter.OnSend(machine.Id, sender?.Id, (sender as StateMachine)?.CurrentStateName ?? string.Empty,
                e.GetType().FullName, opGroupId, isTargetHalted: false);

            if (sender != null)
            {
                var stateName = sender is StateMachine ? (sender as StateMachine).CurrentStateName : string.Empty;
                this.BugTrace.AddSendEventStep(sender.Id, stateName, eventInfo, machine.Id);
            }

            return machine.Enqueue(e, opGroupId, eventInfo);
        }

        /// <summary>
        /// Runs a new asynchronous event handler for the specified machine.
        /// This is a fire and forget invocation.
        /// </summary>
        /// <param name="machine">Machine that executes this event handler.</param>
        /// <param name="initialEvent">Event for initializing the machine.</param>
        /// <param name="isFresh">If true, then this is a new machine.</param>
        /// <param name="syncCaller">Caller machine that is blocked for quiscence.</param>
        private void RunMachineEventHandler(StateMachine machine, Event initialEvent, bool isFresh, StateMachine syncCaller)
        {
            MachineOperation op = this.GetAsynchronousOperation(machine.Id.Value);

            Task task = new Task(async () =>
            {
                try
                {
                    // Set the runtime in the async control flow runtime provider, allowing
                    // future retrieval in the same asynchronous call stack.
                    Provider.SetCurrentRuntime(this);

                    OperationScheduler.NotifyOperationStarted(op);

                    if (isFresh)
                    {
                        await machine.GotoStartState(initialEvent);
                    }

                    await machine.RunEventHandlerAsync();
                    if (syncCaller != null)
                    {
                        this.EnqueueEvent(syncCaller, new QuiescentEvent(machine.Id), machine, machine.OperationGroupId, null);
                    }

                    if (machine.IsHalted)
                    {
                        this.MachineMap.TryRemove(machine.Id, out Actor _);
                    }
                    else
                    {
                        ResetProgramCounter(machine);
                    }

                    IO.Debug.WriteLine($"<ScheduleDebug> Completed event handler of '{machine.Id}' on task '{Task.CurrentId}'.");
                    op.OnCompleted();

                    // Machine is inactive or halted, schedule the next enabled operation.
                    this.Scheduler.ScheduleNextEnabledOperation();
                }
                catch (Exception ex)
                {
                    this.MachineMap.TryRemove(machine.Id, out Actor _);

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
                        IO.Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from machine '{machine.Id}'.");
                    }
                    else if (innerException is TaskSchedulerException)
                    {
                        IO.Debug.WriteLine($"<Exception> TaskSchedulerException was thrown from machine '{machine.Id}'.");
                    }
                    else if (innerException is ObjectDisposedException)
                    {
                        IO.Debug.WriteLine($"<Exception> ObjectDisposedException was thrown from machine '{machine.Id}' with reason '{ex.Message}'.");
                    }
                    else
                    {
                        // Reports the unhandled exception.
                        string message = string.Format(CultureInfo.InvariantCulture,
                            $"Exception '{ex.GetType()}' was thrown in machine '{machine.Id}', " +
                            $"'{ex.Source}':\n" +
                            $"   {ex.Message}\n" +
                            $"The stack trace is:\n{ex.StackTrace}");
                        this.Scheduler.NotifyAssertionFailure(message, killTasks: true, cancelExecution: false);
                    }
                }
            });

            op.OnCreated();
            this.Scheduler.NotifyOperationCreated(op, task);

            task.Start(this.TaskScheduler);
            this.Scheduler.WaitForOperationToStart(op);
        }

        /// <summary>
        /// Creates a new <see cref="ControlledTask"/> to execute the specified asynchronous work.
        /// </summary>
        [DebuggerStepThrough]
        internal override ControlledTask CreateControlledTask(Action action, CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            this.Assert(action != null, "The task cannot execute a null action.");
            var machine = new ActionMachine(this, action);
            this.DispatchWork(machine, null);
            return new MachineTask(this, machine.AwaiterTask, MachineTaskType.ExplicitTask);
        }

        /// <summary>
        /// Creates a new <see cref="ControlledTask"/> to execute the specified asynchronous work.
        /// </summary>
        [DebuggerStepThrough]
        internal override ControlledTask CreateControlledTask(Func<ControlledTask> function, CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            this.Assert(function != null, "The task cannot execute a null function.");
            var machine = new FuncMachine(this, function);
            this.DispatchWork(machine, null);
            return new MachineTask(this, machine.AwaiterTask, MachineTaskType.ExplicitTask);
        }

        /// <summary>
        /// Creates a new <see cref="ControlledTask{TResult}"/> to execute the specified asynchronous work.
        /// </summary>
        [DebuggerStepThrough]
        internal override ControlledTask<TResult> CreateControlledTask<TResult>(Func<TResult> function,
            CancellationToken cancellationToken)
        {
            this.Assert(function != null, "The task cannot execute a null function.");
            var machine = new FuncMachine<TResult>(this, function);
            this.DispatchWork(machine, null);
            return new MachineTask<TResult>(this, machine.AwaiterTask, MachineTaskType.ExplicitTask);
        }

        /// <summary>
        /// Creates a new <see cref="ControlledTask{TResult}"/> to execute the specified asynchronous work.
        /// </summary>
        [DebuggerStepThrough]
        internal override ControlledTask<TResult> CreateControlledTask<TResult>(Func<ControlledTask<TResult>> function,
            CancellationToken cancellationToken)
        {
            this.Assert(function != null, "The task cannot execute a null function.");
            var machine = new FuncTaskMachine<TResult>(this, function);
            this.DispatchWork(machine, null);
            return new MachineTask<TResult>(this, machine.AwaiterTask, MachineTaskType.ExplicitTask);
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

            var machine = new DelayMachine(this);
            this.DispatchWork(machine, null);
            return new MachineTask(this, machine.AwaiterTask, MachineTaskType.ExplicitTask);
        }

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> associated with a completion source.
        /// </summary>
        [DebuggerStepThrough]
        internal override ControlledTask CreateControlledTaskCompletionSource(Task task)
        {
            if (!this.Scheduler.IsRunning)
            {
                return ControlledTask.FromException(new ExecutionCanceledException());
            }

            this.Scheduler.CheckNoExternalConcurrencyUsed();
            return new MachineTask(this, task, MachineTaskType.CompletionSourceTask);
        }

        /// <summary>
        /// Creates a <see cref="ControlledTask{TResult}"/> associated with a completion source.
        /// </summary>
        [DebuggerStepThrough]
        internal override ControlledTask<TResult> CreateControlledTaskCompletionSource<TResult>(Task<TResult> task)
        {
            if (!this.Scheduler.IsRunning)
            {
                return ControlledTask.FromException<TResult>(new ExecutionCanceledException());
            }

            this.Scheduler.CheckNoExternalConcurrencyUsed();
            return new MachineTask<TResult>(this, task, MachineTaskType.CompletionSourceTask);
        }

        /// <summary>
        /// Schedules the specified <see cref="ControlledTaskMachine"/> to be executed asynchronously.
        /// This is a fire and forget invocation.
        /// </summary>
        [DebuggerStepThrough]
        internal void DispatchWork(ControlledTaskMachine machine, Task parentTask)
        {
            MachineOperation op = new MachineOperation(machine, this.Scheduler);

            this.MachineOperations.GetOrAdd(machine.Id.Value, op);
            this.MachineMap.TryAdd(machine.Id, machine);
            this.CreatedMachineIds.Add(machine.Id);

            Task task = new Task(async () =>
            {
                try
                {
                    // Set the runtime in the async control flow runtime provider, allowing
                    // future retrieval in the same asynchronous call stack.
                    Provider.SetCurrentRuntime(this);

                    OperationScheduler.NotifyOperationStarted(op);
                    if (parentTask != null)
                    {
                        op.OnWaitTask(parentTask);
                    }

                    try
                    {
                        await machine.ExecuteAsync();
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
                        machine.TryCompleteWithException(ex);
                    }

                    // TODO: properly cleanup controlled tasks.
                    this.MachineMap.TryRemove(machine.Id, out Actor _);

                    IO.Debug.WriteLine($"<ScheduleDebug> Completed '{machine.Id}' on task '{Task.CurrentId}'.");
                    op.OnCompleted();

                    // Task has completed, schedule the next enabled operation.
                    this.Scheduler.ScheduleNextEnabledOperation();
                }
                catch (Exception ex)
                {
                    // TODO: properly cleanup controlled tasks.
                    this.MachineMap.TryRemove(machine.Id, out Actor _);

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

            IO.Debug.WriteLine($"<CreateLog> Machine '{machine.Id}' was created to execute task '{task.Id}'.");

            op.OnCreated();
            this.Scheduler.NotifyOperationCreated(op, task);

            task.Start();
            this.Scheduler.WaitForOperationToStart(op);
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

            Actor caller = this.GetExecutingMachine<Actor>();
            this.Assert(caller != null,
                "Task with id '{0}' that is not controlled by the Coyote runtime invoked a when-all operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");

            MachineOperation callerOp = this.GetAsynchronousOperation(caller.Id.Value);
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

            Actor caller = this.GetExecutingMachine<Actor>();
            this.Assert(caller != null,
                "Task with id '{0}' that is not controlled by the Coyote runtime invoked a when-all operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");

            MachineOperation callerOp = this.GetAsynchronousOperation(caller.Id.Value);
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

            Actor caller = this.GetExecutingMachine<Actor>();
            this.Assert(caller != null,
                "Task with id '{0}' that is not controlled by the Coyote runtime invoked a when-any operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");

            MachineOperation callerOp = this.GetAsynchronousOperation(caller.Id.Value);
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

            Actor caller = this.GetExecutingMachine<Actor>();
            this.Assert(caller != null,
                "Task with id '{0}' that is not controlled by the Coyote runtime invoked a when-any operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");

            MachineOperation callerOp = this.GetAsynchronousOperation(caller.Id.Value);
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

            Actor caller = this.GetExecutingMachine<Actor>();
            this.Assert(caller != null,
                "Task with id '{0}' that is not controlled by the Coyote runtime invoked a wait-all operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");

            MachineOperation callerOp = this.GetAsynchronousOperation(caller.Id.Value);
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

            Actor caller = this.GetExecutingMachine<Actor>();
            this.Assert(caller != null,
                "Task with id '{0}' that is not controlled by the Coyote runtime invoked a wait-any operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");

            MachineOperation callerOp = this.GetAsynchronousOperation(caller.Id.Value);
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
            Actor caller = this.GetExecutingMachine<Actor>();
            MachineOperation callerOp = this.GetAsynchronousOperation(caller.Id.Value);
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
                Actor caller = this.GetExecutingMachine<Actor>();
                this.Assert(caller != null,
                    "Task with id '{0}' that is not controlled by the Coyote runtime invoked a yield operation.",
                    Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");

                if (caller is StateMachine machine)
                {
                    this.Assert((machine.StateManager as SerializedMachineStateManager).IsInsideControlledTaskHandler,
                        "Machine '{0}' is executing a yield operation inside a handler that does not return a 'ControlledTask'.", caller.Id);
                }

                IO.Debug.WriteLine("<ControlledTask> Machine '{0}' is executing a yield operation.", caller.Id);
                this.DispatchWork(new ActionMachine(this, continuation), null);
                IO.Debug.WriteLine("<ControlledTask> Machine '{0}' is executing a yield operation.", caller.Id);
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
        /// Creates a new timer that sends a <see cref="TimerElapsedEvent"/> to its owner machine.
        /// </summary>
        internal override IMachineTimer CreateMachineTimer(TimerInfo info, StateMachine owner)
        {
            var mid = this.CreateMachineId(typeof(MockMachineTimer));
            this.CreateMachine(mid, typeof(MockMachineTimer), new TimerSetupEvent(info, owner, this.Configuration.TimeoutDelay));
            return this.GetMachineFromId<MockMachineTimer>(mid);
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

            MachineId mid = new MachineId(type, null, this);

            Monitor monitor = Activator.CreateInstance(type) as Monitor;
            monitor.Initialize(this, mid);
            monitor.InitializeStateInformation();

            this.LogWriter.OnCreateMonitor(type.FullName, monitor.Id);

            this.ReportActivityCoverageOfMonitor(monitor);
            this.BugTrace.AddCreateMonitorStep(mid);

            this.Monitors.Add(monitor);

            monitor.GotoStartState();
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        internal override void Monitor(Type type, Actor sender, Event e)
        {
            this.AssertCorrectCallerMachine(sender as StateMachine, "Monitor");
            foreach (var m in this.Monitors)
            {
                if (m.GetType() == type)
                {
                    if (this.Configuration.ReportActivityCoverage)
                    {
                        this.ReportActivityCoverageOfMonitorEvent(sender, m, e);
                        this.ReportActivityCoverageOfMonitorTransition(m, e);
                    }

                    m.MonitorEvent(e);
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
        /// Asserts that a transition statement (raise, goto or pop) has not
        /// already been called. Records that RGP has been called.
        /// </summary>
        [DebuggerHidden]
        internal void AssertTransitionStatement(StateMachine machine)
        {
            var stateManager = machine.StateManager as SerializedMachineStateManager;
            this.Assert(!stateManager.IsInsideOnExit,
                "Machine '{0}' has called raise, goto, push or pop inside an OnExit method.",
                machine.Id.Name);
            this.Assert(!stateManager.IsTransitionStatementCalledInCurrentAction,
                "Machine '{0}' has called multiple raise, goto, push or pop in the same action.",
                machine.Id.Name);
            stateManager.IsTransitionStatementCalledInCurrentAction = true;
        }

        /// <summary>
        /// Asserts that a transition statement (raise, goto or pop) has not already been called.
        /// </summary>
        [DebuggerHidden]
        private void AssertNoPendingTransitionStatement(StateMachine machine, string action)
        {
            if (!this.Configuration.EnableNoApiCallAfterTransitionStmtAssertion)
            {
                // The check is disabled.
                return;
            }

            var stateManager = machine.StateManager as SerializedMachineStateManager;
            this.Assert(!stateManager.IsTransitionStatementCalledInCurrentAction,
                "Machine '{0}' cannot {1} after calling raise, goto, push or pop in the same action.",
                machine.Id.Name, action);
        }

        /// <summary>
        /// Asserts that the machine calling a machine method is also
        /// the machine that is currently executing.
        /// </summary>
        [DebuggerHidden]
        private void AssertCorrectCallerMachine(StateMachine callerMachine, string calledAPI)
        {
            if (callerMachine is null)
            {
                return;
            }

            var executingMachine = this.GetExecutingMachine<StateMachine>();
            if (executingMachine is null)
            {
                return;
            }

            this.Assert(executingMachine.Equals(callerMachine), "Machine '{0}' invoked {1} on behalf of machine '{2}'.",
                executingMachine.Id, calledAPI, callerMachine.Id);
        }

        /// <summary>
        /// Asserts that the currently executing controlled task is awaiting a controlled awaiter.
        /// </summary>
        [DebuggerHidden]
        internal override void AssertAwaitingControlledAwaiter<TAwaiter>(ref TAwaiter awaiter)
        {
            this.AssertAwaitingControlledAwaiter(awaiter.GetType());
        }

        /// <summary>
        /// Asserts that the currently executing controlled task is awaiting a controlled awaiter.
        /// </summary>
        [DebuggerHidden]
        internal override void AssertAwaitingUnsafeControlledAwaiter<TAwaiter>(ref TAwaiter awaiter)
        {
            this.AssertAwaitingControlledAwaiter(awaiter.GetType());
        }

        [DebuggerHidden]
        private void AssertAwaitingControlledAwaiter(Type awaiterType)
        {
            Actor caller = this.GetExecutingMachine<Actor>();
            MachineOperation callerOp = this.GetAsynchronousOperation(caller.Id.Value);
            this.Assert(callerOp.IsAwaiterControlled, "Controlled task '{0}' is trying to wait for an uncontrolled task or awaiter to complete. " +
                "Please make sure to use Coyote APIs to express concurrency (e.g. ControlledTask instead of Task).", Task.CurrentId);
            this.Assert(awaiterType.Namespace == typeof(ControlledTask).Namespace,
                "Controlled task '{0}' is trying to wait for an uncontrolled task or awaiter to complete. " +
                "Please make sure to use Coyote APIs to express concurrency (e.g. ControlledTask instead of Task).", Task.CurrentId);
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
            caller = caller ?? this.GetExecutingMachine<StateMachine>();
            this.AssertCorrectCallerMachine(caller as StateMachine, "Random");
            if (caller is StateMachine machine)
            {
                this.AssertNoPendingTransitionStatement(caller as StateMachine, "invoke 'Random'");
                (machine.StateManager as SerializedMachineStateManager).ProgramCounter++;
            }

            var choice = this.Scheduler.GetNextNondeterministicBooleanChoice(maxValue);
            this.LogWriter.OnRandom(caller?.Id, choice);

            var stateName = caller is StateMachine ? (caller as StateMachine).CurrentStateName : string.Empty;
            this.BugTrace.AddRandomChoiceStep(caller?.Id, stateName, choice);

            return choice;
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        internal override bool GetFairNondeterministicBooleanChoice(Actor caller, string uniqueId)
        {
            caller = caller ?? this.GetExecutingMachine<StateMachine>();
            this.AssertCorrectCallerMachine(caller as StateMachine, "FairRandom");
            if (caller is StateMachine machine)
            {
                this.AssertNoPendingTransitionStatement(caller as StateMachine, "invoke 'FairRandom'");
                (machine.StateManager as SerializedMachineStateManager).ProgramCounter++;
            }

            var choice = this.Scheduler.GetNextNondeterministicBooleanChoice(2, uniqueId);
            this.LogWriter.OnRandom(caller?.Id, choice);

            var stateName = caller is StateMachine ? (caller as StateMachine).CurrentStateName : string.Empty;
            this.BugTrace.AddRandomChoiceStep(caller?.Id, stateName, choice);

            return choice;
        }

        /// <summary>
        /// Returns a nondeterministic integer, that can be
        /// controlled during analysis or testing.
        /// </summary>
        internal override int GetNondeterministicIntegerChoice(Actor caller, int maxValue)
        {
            caller = caller ?? this.GetExecutingMachine<StateMachine>();
            this.AssertCorrectCallerMachine(caller as StateMachine, "RandomInteger");
            if (caller is StateMachine)
            {
                this.AssertNoPendingTransitionStatement(caller as StateMachine, "invoke 'RandomInteger'");
            }

            var choice = this.Scheduler.GetNextNondeterministicIntegerChoice(maxValue);
            this.LogWriter.OnRandom(caller?.Id, choice);

            var stateName = caller is StateMachine ? (caller as StateMachine).CurrentStateName : string.Empty;
            this.BugTrace.AddRandomChoiceStep(caller?.Id, stateName, choice);

            return choice;
        }

        /// <summary>
        /// Injects a context switch point that can be systematically explored during testing.
        /// </summary>
        internal override void ExploreContextSwitch()
        {
            Actor caller = this.GetExecutingMachine<Actor>();
            if (caller != null)
            {
                this.Scheduler.ScheduleNextEnabledOperation();
            }
        }

        /// <summary>
        /// Notifies that a machine entered a state.
        /// </summary>
        internal override void NotifyEnteredState(StateMachine machine)
        {
            string machineState = machine.CurrentStateName;
            this.BugTrace.AddGotoStateStep(machine.Id, machineState);

            this.LogWriter.OnMachineState(machine.Id, machineState, isEntry: true);
        }

        /// <summary>
        /// Notifies that a monitor entered a state.
        /// </summary>
        internal override void NotifyEnteredState(Monitor monitor)
        {
            string monitorState = monitor.CurrentStateNameWithTemperature;
            this.BugTrace.AddGotoStateStep(monitor.Id, monitorState);

            this.LogWriter.OnMonitorState(monitor.GetType().FullName, monitor.Id, monitorState, true, monitor.GetHotState());
        }

        /// <summary>
        /// Notifies that a machine exited a state.
        /// </summary>
        internal override void NotifyExitedState(StateMachine machine)
        {
            this.LogWriter.OnMachineState(machine.Id, machine.CurrentStateName, isEntry: false);
        }

        /// <summary>
        /// Notifies that a monitor exited a state.
        /// </summary>
        internal override void NotifyExitedState(Monitor monitor)
        {
            string monitorState = monitor.CurrentStateNameWithTemperature;
            this.LogWriter.OnMonitorState(monitor.GetType().FullName, monitor.Id, monitorState, false, monitor.GetHotState());
        }

        /// <summary>
        /// Notifies that a machine invoked an action.
        /// </summary>
        internal override void NotifyInvokedAction(StateMachine machine, MethodInfo action, Event receivedEvent)
        {
            (machine.StateManager as SerializedMachineStateManager).IsTransitionStatementCalledInCurrentAction = false;
            if (action.ReturnType == typeof(ControlledTask))
            {
                (machine.StateManager as SerializedMachineStateManager).IsInsideControlledTaskHandler = true;
            }

            string machineState = machine.CurrentStateName;
            this.BugTrace.AddInvokeActionStep(machine.Id, machineState, action);
            this.LogWriter.OnMachineAction(machine.Id, machineState, action.Name);
        }

        /// <summary>
        /// Notifies that a machine completed an action.
        /// </summary>
        internal override void NotifyCompletedAction(StateMachine machine, MethodInfo action, Event receivedEvent)
        {
            (machine.StateManager as SerializedMachineStateManager).IsTransitionStatementCalledInCurrentAction = false;
            (machine.StateManager as SerializedMachineStateManager).IsInsideControlledTaskHandler = false;
        }

        /// <summary>
        /// Notifies that a machine invoked an action.
        /// </summary>
        internal override void NotifyInvokedOnEntryAction(StateMachine machine, MethodInfo action, Event receivedEvent)
        {
            (machine.StateManager as SerializedMachineStateManager).IsTransitionStatementCalledInCurrentAction = false;
            if (action.ReturnType == typeof(ControlledTask))
            {
                (machine.StateManager as SerializedMachineStateManager).IsInsideControlledTaskHandler = true;
            }

            string machineState = machine.CurrentStateName;
            this.BugTrace.AddInvokeActionStep(machine.Id, machineState, action);
            this.LogWriter.OnMachineAction(machine.Id, machineState, action.Name);
        }

        /// <summary>
        /// Notifies that a machine completed invoking an action.
        /// </summary>
        internal override void NotifyCompletedOnEntryAction(StateMachine machine, MethodInfo action, Event receivedEvent)
        {
            (machine.StateManager as SerializedMachineStateManager).IsTransitionStatementCalledInCurrentAction = false;
            (machine.StateManager as SerializedMachineStateManager).IsInsideControlledTaskHandler = false;
        }

        /// <summary>
        /// Notifies that a machine invoked an action.
        /// </summary>
        internal override void NotifyInvokedOnExitAction(StateMachine machine, MethodInfo action, Event receivedEvent)
        {
            (machine.StateManager as SerializedMachineStateManager).IsInsideOnExit = true;
            (machine.StateManager as SerializedMachineStateManager).IsTransitionStatementCalledInCurrentAction = false;
            if (action.ReturnType == typeof(ControlledTask))
            {
                (machine.StateManager as SerializedMachineStateManager).IsInsideControlledTaskHandler = true;
            }

            string machineState = machine.CurrentStateName;
            this.BugTrace.AddInvokeActionStep(machine.Id, machineState, action);
            this.LogWriter.OnMachineAction(machine.Id, machineState, action.Name);
        }

        /// <summary>
        /// Notifies that a machine completed invoking an action.
        /// </summary>
        internal override void NotifyCompletedOnExitAction(StateMachine machine, MethodInfo action, Event receivedEvent)
        {
            (machine.StateManager as SerializedMachineStateManager).IsInsideOnExit = false;
            (machine.StateManager as SerializedMachineStateManager).IsTransitionStatementCalledInCurrentAction = false;
            (machine.StateManager as SerializedMachineStateManager).IsInsideControlledTaskHandler = false;
        }

        /// <summary>
        /// Notifies that a monitor invoked an action.
        /// </summary>
        internal override void NotifyInvokedAction(Monitor monitor, MethodInfo action, Event receivedEvent)
        {
            string monitorState = monitor.CurrentStateName;
            this.BugTrace.AddInvokeActionStep(monitor.Id, monitorState, action);
            this.LogWriter.OnMonitorAction(monitor.GetType().FullName, monitor.Id, action.Name, monitorState);
        }

        /// <summary>
        /// Notifies that a machine raised an <see cref="Event"/>.
        /// </summary>
        internal override void NotifyRaisedEvent(StateMachine machine, Event e, EventInfo eventInfo)
        {
            this.AssertTransitionStatement(machine);
            string machineState = machine.CurrentStateName;
            this.BugTrace.AddRaiseEventStep(machine.Id, machineState, eventInfo);
            this.LogWriter.OnMachineEvent(machine.Id, machineState, eventInfo.EventName);
        }

        /// <summary>
        /// Notifies that a monitor raised an <see cref="Event"/>.
        /// </summary>
        internal override void NotifyRaisedEvent(Monitor monitor, Event e, EventInfo eventInfo)
        {
            string monitorState = monitor.CurrentStateName;
            this.BugTrace.AddRaiseEventStep(monitor.Id, monitorState, eventInfo);
            this.LogWriter.OnMonitorEvent(monitor.GetType().FullName, monitor.Id, monitor.CurrentStateName,
                eventInfo.EventName, isProcessing: false);
        }

        /// <summary>
        /// Notifies that a machine dequeued an <see cref="Event"/>.
        /// </summary>
        internal override void NotifyDequeuedEvent(StateMachine machine, Event e, EventInfo eventInfo)
        {
            MachineOperation op = this.GetAsynchronousOperation(machine.Id.Value);

            // Skip `Receive` if the last operation exited the previous event handler,
            // to avoid scheduling duplicate `Receive` operations.
            if (op.SkipNextReceiveSchedulingPoint)
            {
                op.SkipNextReceiveSchedulingPoint = false;
            }
            else
            {
                this.Scheduler.ScheduleNextEnabledOperation();
                ResetProgramCounter(machine);
            }

            this.LogWriter.OnDequeue(machine.Id, machine.CurrentStateName, eventInfo.EventName);
            this.BugTrace.AddDequeueEventStep(machine.Id, machine.CurrentStateName, eventInfo);

            if (this.Configuration.ReportActivityCoverage)
            {
                this.ReportActivityCoverageOfReceivedEvent(machine, eventInfo);
                this.ReportActivityCoverageOfStateTransition(machine, e);
            }
        }

        /// <summary>
        /// Notifies that a machine invoked pop.
        /// </summary>
        internal override void NotifyPop(StateMachine machine)
        {
            this.AssertCorrectCallerMachine(machine, "Pop");
            this.AssertTransitionStatement(machine);

            this.LogWriter.OnPop(machine.Id, string.Empty, machine.CurrentStateName);

            if (this.Configuration.ReportActivityCoverage)
            {
                this.ReportActivityCoverageOfPopTransition(machine, machine.CurrentState, machine.GetStateTypeAtStackIndex(1));
            }
        }

        /// <summary>
        /// Notifies that a machine called Receive.
        /// </summary>
        internal override void NotifyReceiveCalled(StateMachine machine)
        {
            this.AssertCorrectCallerMachine(machine, "Receive");
            this.AssertNoPendingTransitionStatement(machine, "invoke 'Receive'");
        }

        /// <summary>
        /// Notifies that a machine is handling a raised event.
        /// </summary>
        internal override void NotifyHandleRaisedEvent(StateMachine machine, Event e)
        {
            if (this.Configuration.ReportActivityCoverage)
            {
                this.ReportActivityCoverageOfStateTransition(machine, e);
            }
        }

        /// <summary>
        /// Notifies that a machine is waiting for the specified task to complete.
        /// </summary>
        internal override void NotifyWaitTask(StateMachine machine, Task task)
        {
            this.Assert(task != null, "Machine '{0}' is waiting for a null task to complete.", machine.Id);
            this.Assert(task.IsCompleted || task.IsCanceled || task.IsFaulted,
                "Machine '{0}' is trying to wait for an uncontrolled task or awaiter to complete. Please make sure to avoid " +
                "using concurrency APIs such as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside machine handlers. If you are " +
                "using external libraries that are executing concurrently, you will need to mock them during testing.",
                Task.CurrentId);
        }

        /// <summary>
        /// Notifies that a <see cref="ControlledTaskMachine"/> is waiting for the specified task to complete.
        /// </summary>
        internal override void NotifyWaitTask(ControlledTaskMachine machine, Task task)
        {
            this.Assert(task != null, "Controlled task '{0}' is waiting for a null task to complete.", Task.CurrentId);
            MachineOperation callerOp = this.GetAsynchronousOperation(machine.Id.Value);
            if (!task.IsCompleted)
            {
                callerOp.OnWaitTask(task);
            }
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive an event of one of the specified types.
        /// </summary>
        internal override void NotifyWaitEvent(StateMachine machine, IEnumerable<Type> eventTypes)
        {
            MachineOperation op = this.GetAsynchronousOperation(machine.Id.Value);
            op.OnWaitEvent(eventTypes);

            string eventNames;
            var eventWaitTypesArray = eventTypes.ToArray();
            if (eventWaitTypesArray.Length == 1)
            {
                this.LogWriter.OnWait(machine.Id, machine.CurrentStateName, eventWaitTypesArray[0]);
                eventNames = eventWaitTypesArray[0].FullName;
            }
            else
            {
                this.LogWriter.OnWait(machine.Id, machine.CurrentStateName, eventWaitTypesArray);
                if (eventWaitTypesArray.Length > 0)
                {
                    string[] eventNameArray = new string[eventWaitTypesArray.Length - 1];
                    for (int i = 0; i < eventWaitTypesArray.Length - 2; i++)
                    {
                        eventNameArray[i] = eventWaitTypesArray[i].FullName;
                    }

                    eventNames = string.Join(", ", eventNameArray) + " or " + eventWaitTypesArray[eventWaitTypesArray.Length - 1].FullName;
                }
                else
                {
                    eventNames = string.Empty;
                }
            }

            this.BugTrace.AddWaitToReceiveStep(machine.Id, machine.CurrentStateName, eventNames);
            this.Scheduler.ScheduleNextEnabledOperation();
            ResetProgramCounter(machine);
        }

        /// <summary>
        /// Notifies that a machine enqueued an event that it was waiting to receive.
        /// </summary>
        internal override void NotifyReceivedEvent(StateMachine machine, Event e, EventInfo eventInfo)
        {
            this.LogWriter.OnReceive(machine.Id, machine.CurrentStateName, e.GetType().FullName, wasBlocked: true);
            this.BugTrace.AddReceivedEventStep(machine.Id, machine.CurrentStateName, eventInfo);

            MachineOperation op = this.GetAsynchronousOperation(machine.Id.Value);
            op.OnReceivedEvent();

            if (this.Configuration.ReportActivityCoverage)
            {
                this.ReportActivityCoverageOfReceivedEvent(machine, eventInfo);
            }
        }

        /// <summary>
        /// Notifies that a machine received an event without waiting because the event
        /// was already in the inbox when the machine invoked the receive statement.
        /// </summary>
        internal override void NotifyReceivedEventWithoutWaiting(StateMachine machine, Event e, EventInfo eventInfo)
        {
            this.LogWriter.OnReceive(machine.Id, machine.CurrentStateName, e.GetType().FullName, wasBlocked: false);
            this.Scheduler.ScheduleNextEnabledOperation();
            ResetProgramCounter(machine);
        }

        /// <summary>
        /// Notifies that a machine has halted.
        /// </summary>
        internal override void NotifyHalted(StateMachine machine)
        {
            this.BugTrace.AddHaltStep(machine.Id, null);
        }

        /// <summary>
        /// Notifies that the inbox of the specified machine is about to be
        /// checked to see if the default event handler should fire.
        /// </summary>
        internal override void NotifyDefaultEventHandlerCheck(StateMachine machine)
        {
            this.Scheduler.ScheduleNextEnabledOperation();
        }

        /// <summary>
        /// Notifies that the default handler of the specified machine has been fired.
        /// </summary>
        internal override void NotifyDefaultHandlerFired(StateMachine machine)
        {
            this.Scheduler.ScheduleNextEnabledOperation();
            ResetProgramCounter(machine);
        }

        /// <summary>
        /// Reports coverage for the specified received event.
        /// </summary>
        private void ReportActivityCoverageOfReceivedEvent(StateMachine machine, EventInfo eventInfo)
        {
            string originMachine = eventInfo.OriginInfo.SenderMachineName;
            string originState = eventInfo.OriginInfo.SenderStateName;
            string edgeLabel = eventInfo.EventName;
            string destMachine = machine.GetType().FullName;
            string destState = NameResolver.GetStateNameForLogging(machine.CurrentState);

            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }

        /// <summary>
        /// Reports coverage for the specified monitor event.
        /// </summary>
        private void ReportActivityCoverageOfMonitorEvent(Actor sender, Monitor monitor, Event e)
        {
            string originMachine = sender is null ? "Env" : sender.GetType().FullName;
            string originState = sender is null ? "Env" :
                (sender is StateMachine) ? NameResolver.GetStateNameForLogging((sender as StateMachine).CurrentState) : "Env";

            string edgeLabel = e.GetType().FullName;
            string destMachine = monitor.GetType().FullName;
            string destState = NameResolver.GetStateNameForLogging(monitor.CurrentState);

            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }

        /// <summary>
        /// Reports coverage for the specified machine.
        /// </summary>
        private void ReportActivityCoverageOfMachine(StateMachine machine)
        {
            var machineName = machine.GetType().FullName;
            if (this.CoverageInfo.IsMachineDeclared(machineName))
            {
                return;
            }

            // Fetch states.
            var states = machine.GetAllStates();
            foreach (var state in states)
            {
                this.CoverageInfo.DeclareMachineState(machineName, state);
            }

            // Fetch registered events.
            var pairs = machine.GetAllStateEventPairs();
            foreach (var tup in pairs)
            {
                this.CoverageInfo.DeclareStateEvent(machineName, tup.Item1, tup.Item2);
            }
        }

        /// <summary>
        /// Reports coverage for the specified monitor.
        /// </summary>
        private void ReportActivityCoverageOfMonitor(Monitor monitor)
        {
            var monitorName = monitor.GetType().FullName;

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
        /// Reports coverage for the specified state transition.
        /// </summary>
        private void ReportActivityCoverageOfStateTransition(StateMachine machine, Event e)
        {
            string originMachine = machine.GetType().FullName;
            string originState = NameResolver.GetStateNameForLogging(machine.CurrentState);
            string destMachine = machine.GetType().FullName;

            string edgeLabel;
            string destState;
            if (e is GotoStateEvent gotoStateEvent)
            {
                edgeLabel = "goto";
                destState = NameResolver.GetStateNameForLogging(gotoStateEvent.State);
            }
            else if (e is PushStateEvent pushStateEvent)
            {
                edgeLabel = "push";
                destState = NameResolver.GetStateNameForLogging(pushStateEvent.State);
            }
            else if (machine.GotoTransitions.ContainsKey(e.GetType()))
            {
                edgeLabel = e.GetType().FullName;
                destState = NameResolver.GetStateNameForLogging(
                    machine.GotoTransitions[e.GetType()].TargetState);
            }
            else if (machine.PushTransitions.ContainsKey(e.GetType()))
            {
                edgeLabel = e.GetType().FullName;
                destState = NameResolver.GetStateNameForLogging(
                    machine.PushTransitions[e.GetType()].TargetState);
            }
            else
            {
                return;
            }

            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }

        /// <summary>
        /// Reports coverage for a pop transition.
        /// </summary>
        private void ReportActivityCoverageOfPopTransition(StateMachine machine, Type fromState, Type toState)
        {
            string originMachine = machine.GetType().FullName;
            string originState = NameResolver.GetStateNameForLogging(fromState);
            string destMachine = machine.GetType().FullName;
            string edgeLabel = "pop";
            string destState = NameResolver.GetStateNameForLogging(toState);

            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }

        /// <summary>
        /// Reports coverage for the specified state transition.
        /// </summary>
        private void ReportActivityCoverageOfMonitorTransition(Monitor monitor, Event e)
        {
            string originMachine = monitor.GetType().FullName;
            string originState = NameResolver.GetStateNameForLogging(monitor.CurrentState);
            string destMachine = originMachine;

            string edgeLabel;
            string destState;
            if (e is GotoStateEvent)
            {
                edgeLabel = "goto";
                destState = NameResolver.GetStateNameForLogging((e as GotoStateEvent).State);
            }
            else if (monitor.GotoTransitions.ContainsKey(e.GetType()))
            {
                edgeLabel = e.GetType().FullName;
                destState = NameResolver.GetStateNameForLogging(
                    monitor.GotoTransitions[e.GetType()].TargetState);
            }
            else
            {
                return;
            }

            this.CoverageInfo.AddTransition(originMachine, originState, edgeLabel, destMachine, destState);
        }

        /// <summary>
        /// Resets the program counter of the specified machine.
        /// </summary>
        private static void ResetProgramCounter(StateMachine machine)
        {
            if (machine != null)
            {
                (machine.StateManager as SerializedMachineStateManager).ProgramCounter = 0;
            }
        }

        /// <summary>
        /// Gets the currently executing machine of type <typeparamref name="TMachine"/>,
        /// or null if no such machine is currently executing.
        /// </summary>
        [DebuggerStepThrough]
        internal TMachine GetExecutingMachine<TMachine>()
            where TMachine : Actor
        {
            if (Task.CurrentId.HasValue &&
                this.Scheduler.ControlledTaskMap.TryGetValue(Task.CurrentId.Value, out MachineOperation op) &&
                op?.Machine is TMachine machine)
            {
                return machine;
            }

            return null;
        }

        /// <summary>
        /// Gets the id of the currently executing machine.
        /// </summary>
        internal MachineId GetCurrentMachineId() => this.GetExecutingMachine<Actor>()?.Id;

        /// <summary>
        /// Gets the asynchronous operation associated with the specified id.
        /// </summary>
        [DebuggerStepThrough]
        internal MachineOperation GetAsynchronousOperation(ulong id)
        {
            if (!this.IsRunning)
            {
                throw new ExecutionCanceledException();
            }

            this.MachineOperations.TryGetValue(id, out MachineOperation op);
            return op;
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

                foreach (var machine in this.MachineMap.Values.OrderBy(mi => mi.Id.Value))
                {
                    if (machine is StateMachine m)
                    {
                        hash = (hash * 31) + m.GetCachedState();
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
        /// Waits until all machines have finished execution.
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
                this.MachineMap.Clear();
                this.MachineOperations.Clear();
            }

            base.Dispose(disposing);
        }
    }
}
