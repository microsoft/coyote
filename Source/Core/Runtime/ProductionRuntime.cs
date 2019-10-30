// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Machines;
using Microsoft.Coyote.Machines.Timers;
using Microsoft.Coyote.Threading;
using Microsoft.Coyote.Threading.Tasks;

using Monitor = Microsoft.Coyote.Specifications.Monitor;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Runtime for executing machines in production.
    /// </summary>
    internal sealed class ProductionRuntime : CoyoteRuntime
    {
        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        private readonly List<Monitor> Monitors;

        /// <summary>
        /// Responsible for generating random values.
        /// </summary>
        private readonly Random ValueGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductionRuntime"/> class.
        /// </summary>
        internal ProductionRuntime(Configuration configuration)
            : base(configuration)
        {
            this.Monitors = new List<Monitor>();
            this.ValueGenerator = new Random(DateTime.Now.Millisecond);
        }

        /// <summary>
        /// Creates a actor id that is uniquely tied to the specified unique name. The
        /// returned actor id can either be a fresh id (not yet bound to any machine),
        /// or it can be bound to a previously created machine. In the second case, this
        /// actor id can be directly used to communicate with the corresponding machine.
        /// </summary>
        public override ActorId CreateActorIdFromName(Type type, string machineName) => new ActorId(type, machineName, this, true);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with
        /// the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        public override ActorId CreateMachine(Type type, Event e = null, Guid opGroupId = default) =>
            this.CreateMachine(null, type, null, e, null, opGroupId);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        public override ActorId CreateMachine(Type type, string machineName, Event e = null, Guid opGroupId = default) =>
            this.CreateMachine(null, type, machineName, e, null, opGroupId);

        /// <summary>
        /// Creates a new machine of the specified type, using the specified <see cref="ActorId"/>.
        /// This method optionally passes an <see cref="Event"/> to the new machine, which can only
        /// be used to access its payload, and cannot be handled.
        /// </summary>
        public override ActorId CreateMachine(ActorId id, Type type, Event e = null, Guid opGroupId = default) =>
            this.CreateMachine(id, type, null, e, null, opGroupId);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with the
        /// specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when
        /// the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public override Task<ActorId> CreateMachineAndExecuteAsync(Type type, Event e = null, Guid opGroupId = default) =>
            this.CreateMachineAndExecuteAsync(null, type, null, e, null, opGroupId);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and with
        /// the specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when the
        /// machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public override Task<ActorId> CreateMachineAndExecuteAsync(Type type, string machineName, Event e = null, Guid opGroupId = default) =>
            this.CreateMachineAndExecuteAsync(null, type, machineName, e, null, opGroupId);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>, using the specified
        /// unbound actor id, and passes the specified optional <see cref="Event"/>. This
        /// event can only be used to access its payload, and cannot be handled. The method
        /// returns only when the machine is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        public override Task<ActorId> CreateMachineAndExecuteAsync(ActorId id, Type type, Event e = null, Guid opGroupId = default) =>
            this.CreateMachineAndExecuteAsync(id, type, null, e, null, opGroupId);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        public override void SendEvent(ActorId target, Event e, Guid opGroupId = default, SendOptions options = null) =>
            this.SendEvent(target, e, null, opGroupId, options);

        /// <summary>
        /// Sends an <see cref="Event"/> to a machine. Returns immediately if the target machine was already
        /// running. Otherwise blocks until the machine handles the event and reaches quiescense.
        /// </summary>
        public override Task<bool> SendEventAndExecuteAsync(ActorId target, Event e, Guid opGroupId = default, SendOptions options = null) =>
            this.SendEventAndExecuteAsync(target, e, null, opGroupId, options);

        /// <summary>
        /// Returns the operation group id of the specified machine. Returns <see cref="Guid.Empty"/>
        /// if the id is not set, or if the <see cref="ActorId"/> is not associated with this runtime.
        /// During testing, the runtime asserts that the specified machine is currently executing.
        /// </summary>
        public override Guid GetCurrentOperationGroupId(ActorId currentMachine)
        {
            StateMachine machine = this.GetMachineFromId<StateMachine>(currentMachine);
            return machine is null ? Guid.Empty : machine.OperationGroupId;
        }

        /// <summary>
        /// Creates a new <see cref="StateMachine"/> of the specified <see cref="Type"/>.
        /// </summary>
        internal override ActorId CreateMachine(ActorId id, Type type, string machineName, Event e,
            StateMachine creator, Guid opGroupId)
        {
            StateMachine machine = this.CreateMachine(id, type, machineName, creator, opGroupId);
            this.LogWriter.OnCreateMachine(machine.Id, creator?.Id);
            this.RunMachineEventHandler(machine, e, true);
            return machine.Id;
        }

        /// <summary>
        /// Creates a new <see cref="StateMachine"/> of the specified <see cref="Type"/>. The
        /// method returns only when the created machine reaches quiescence.
        /// </summary>
        internal override async Task<ActorId> CreateMachineAndExecuteAsync(ActorId id, Type type, string machineName, Event e,
            StateMachine creator, Guid opGroupId)
        {
            StateMachine machine = this.CreateMachine(id, type, machineName, creator, opGroupId);
            this.LogWriter.OnCreateMachine(machine.Id, creator?.Id);
            await this.RunMachineEventHandlerAsync(machine, e, true);
            return machine.Id;
        }

        /// <summary>
        /// Creates a new <see cref="StateMachine"/> of the specified <see cref="Type"/>.
        /// </summary>
        private StateMachine CreateMachine(ActorId id, Type type, string machineName, StateMachine creator, Guid opGroupId)
        {
            if (!type.IsSubclassOf(typeof(StateMachine)))
            {
                this.Assert(false, "Type '{0}' is not a machine.", type.FullName);
            }

            if (id is null)
            {
                id = new ActorId(type, machineName, this);
            }
            else if (id.Runtime != null && id.Runtime != this)
            {
                this.Assert(false, "Unbound actor id '{0}' was created by another runtime.", id.Value);
            }
            else if (id.Type != type.FullName)
            {
                this.Assert(false, "Cannot bind actor id '{0}' of type '{1}' to a machine of type '{2}'.",
                    id.Value, id.Type, type.FullName);
            }
            else
            {
                id.Bind(this);
            }

            // The operation group id of the machine is set using the following precedence:
            // (1) To the specified machine creation operation group id, if it is non-empty.
            // (2) To the operation group id of the creator machine, if it exists.
            // (3) To the empty operation group id.
            if (opGroupId == Guid.Empty && creator != null)
            {
                opGroupId = creator.OperationGroupId;
            }

            StateMachine machine = StateMachineFactory.Create(type);
            IMachineStateManager stateManager = new MachineStateManager(this, machine, opGroupId);
            IEventQueue eventQueue = new EventQueue(stateManager);

            machine.Initialize(this, id, stateManager, eventQueue);
            machine.InitializeStateInformation();

            if (!this.MachineMap.TryAdd(id, machine))
            {
                string info = "This typically occurs if either the actor id was created by another runtime instance, " +
                    "or if a actor id from a previous runtime generation was deserialized, but the current runtime " +
                    "has not increased its generation value.";
                this.Assert(false, "Machine with id '{0}' was already created in generation '{1}'. {2}", id.Value, id.Generation, info);
            }

            return machine;
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        internal override void SendEvent(ActorId target, Event e, Actor sender, Guid opGroupId, SendOptions options)
        {
            EnqueueStatus enqueueStatus = this.EnqueueEvent(target, e, sender, opGroupId, out StateMachine targetMachine);
            if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
            {
                this.RunMachineEventHandler(targetMachine, null, false);
            }
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine. Returns immediately if the target machine was
        /// already running. Otherwise blocks until the machine handles the event and reaches quiescense.
        /// </summary>
        internal override async Task<bool> SendEventAndExecuteAsync(ActorId target, Event e, Actor sender,
            Guid opGroupId, SendOptions options)
        {
            EnqueueStatus enqueueStatus = this.EnqueueEvent(target, e, sender, opGroupId, out StateMachine targetMachine);
            if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
            {
                await this.RunMachineEventHandlerAsync(targetMachine, null, false);
                return true;
            }

            return enqueueStatus is EnqueueStatus.Dropped;
        }

        /// <summary>
        /// Enqueues an event to the machine with the specified id.
        /// </summary>
        private EnqueueStatus EnqueueEvent(ActorId target, Event e, Actor sender, Guid opGroupId, out StateMachine targetMachine)
        {
            if (target is null)
            {
                string message = sender != null ?
                    string.Format("Machine '{0}' is sending to a null machine.", sender.Id.ToString()) :
                    "Cannot send to a null machine.";
                this.Assert(false, message);
            }

            if (e is null)
            {
                string message = sender != null ?
                    string.Format("Machine '{0}' is sending a null event.", sender.Id.ToString()) :
                    "Cannot send a null event.";
                this.Assert(false, message);
            }

            // The operation group id of this operation is set using the following precedence:
            // (1) To the specified send operation group id, if it is non-empty.
            // (2) To the operation group id of the sender machine, if it exists and is non-empty.
            // (3) To the empty operation group id.
            if (opGroupId == Guid.Empty && sender != null)
            {
                opGroupId = sender.OperationGroupId;
            }

            targetMachine = this.GetMachineFromId<StateMachine>(target);
            if (targetMachine is null)
            {
                this.LogWriter.OnSend(target, sender?.Id, (sender as StateMachine)?.CurrentStateName ?? string.Empty,
                    e.GetType().FullName, opGroupId, isTargetHalted: true);
                this.TryHandleDroppedEvent(e, target);
                return EnqueueStatus.Dropped;
            }

            this.LogWriter.OnSend(target, sender?.Id, (sender as StateMachine)?.CurrentStateName ?? string.Empty,
                e.GetType().FullName, opGroupId, isTargetHalted: false);

            EnqueueStatus enqueueStatus = targetMachine.Enqueue(e, opGroupId, null);
            if (enqueueStatus == EnqueueStatus.Dropped)
            {
                this.TryHandleDroppedEvent(e, target);
            }

            return enqueueStatus;
        }

        /// <summary>
        /// Runs a new asynchronous machine event handler.
        /// This is a fire and forget invocation.
        /// </summary>
        private void RunMachineEventHandler(StateMachine machine, Event initialEvent, bool isFresh)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (isFresh)
                    {
                        await machine.GotoStartState(initialEvent);
                    }

                    await machine.RunEventHandlerAsync();
                }
                catch (Exception ex)
                {
                    this.IsRunning = false;
                    this.RaiseOnFailureEvent(ex);
                }
                finally
                {
                    if (machine.IsHalted)
                    {
                        this.MachineMap.TryRemove(machine.Id, out Actor _);
                    }
                }
            });
        }

        /// <summary>
        /// Runs a new asynchronous machine event handler.
        /// </summary>
        private async Task RunMachineEventHandlerAsync(StateMachine machine, Event initialEvent, bool isFresh)
        {
            try
            {
                if (isFresh)
                {
                    await machine.GotoStartState(initialEvent);
                }

                await machine.RunEventHandlerAsync();
            }
            catch (Exception ex)
            {
                this.IsRunning = false;
                this.RaiseOnFailureEvent(ex);
                return;
            }
        }

        /// <summary>
        /// Creates a new <see cref="ControlledTask"/> to execute the specified asynchronous work.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override ControlledTask CreateControlledTask(Action action, CancellationToken cancellationToken) =>
            new ControlledTask(Task.Run(action, cancellationToken));

        /// <summary>
        /// Creates a new <see cref="ControlledTask"/> to execute the specified asynchronous work.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override ControlledTask CreateControlledTask(Func<ControlledTask> function, CancellationToken cancellationToken)
        {
            return new ControlledTask(Task.Run(async () =>
            {
                await function();
            }, cancellationToken));
        }

        /// <summary>
        /// Creates a new <see cref="ControlledTask{TResult}"/> to execute the specified asynchronous work.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override ControlledTask<TResult> CreateControlledTask<TResult>(Func<TResult> function,
            CancellationToken cancellationToken) =>
            new ControlledTask<TResult>(Task.Run(function, cancellationToken));

        /// <summary>
        /// Creates a new <see cref="ControlledTask{TResult}"/> to execute the specified asynchronous work.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override ControlledTask<TResult> CreateControlledTask<TResult>(Func<ControlledTask<TResult>> function,
            CancellationToken cancellationToken)
        {
            return new ControlledTask<TResult>(Task.Run(async () =>
            {
                return await function();
            }, cancellationToken));
        }

        /// <summary>
        /// Creates a new <see cref="ControlledTask"/> to execute the specified asynchronous delay.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override ControlledTask CreateControlledTaskDelay(int millisecondsDelay, CancellationToken cancellationToken) =>
            new ControlledTask(Task.Delay(millisecondsDelay, cancellationToken));

        /// <summary>
        /// Creates a new <see cref="ControlledTask"/> to execute the specified asynchronous delay.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override ControlledTask CreateControlledTaskDelay(TimeSpan delay, CancellationToken cancellationToken) =>
            new ControlledTask(Task.Delay(delay, cancellationToken));

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> associated with a completion source.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override ControlledTask CreateControlledTaskCompletionSource(Task task) => new ControlledTask(task);

        /// <summary>
        /// Creates a <see cref="ControlledTask{TResult}"/> associated with a completion source.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override ControlledTask<TResult> CreateControlledTaskCompletionSource<TResult>(Task<TResult> task) =>
            new ControlledTask<TResult>(task);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override ControlledTask WaitAllTasksAsync(IEnumerable<ControlledTask> tasks) =>
            new ControlledTask(Task.WhenAll(tasks.Select(t => t.AwaiterTask)));

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override ControlledTask<TResult[]> WaitAllTasksAsync<TResult>(IEnumerable<ControlledTask<TResult>> tasks) =>
            new ControlledTask<TResult[]>(Task.WhenAll(tasks.Select(t => t.AwaiterTask)));

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override async ControlledTask<ControlledTask> WaitAnyTaskAsync(IEnumerable<ControlledTask> tasks)
        {
            Task result = await Task.WhenAny(tasks.Select(t => t.AwaiterTask));
            return tasks.First(task => task.Id == result.Id);
        }

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override async ControlledTask<ControlledTask<TResult>> WaitAnyTaskAsync<TResult>(IEnumerable<ControlledTask<TResult>> tasks)
        {
            Task<TResult> result = await Task.WhenAny(tasks.Select(t => t.AwaiterTask));
            return tasks.First(task => task.Id == result.Id);
        }

        /// <summary>
        /// Waits for all of the provided <see cref="ControlledTask"/> objects to complete execution.
        /// </summary>
        internal override void WaitAllTasks(params ControlledTask[] tasks) =>
            Task.WaitAll(tasks.Select(t => t.AwaiterTask).ToArray());

        /// <summary>
        /// Waits for all of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified number of milliseconds.
        /// </summary>
        internal override bool WaitAllTasks(ControlledTask[] tasks, int millisecondsTimeout) =>
            Task.WaitAll(tasks.Select(t => t.AwaiterTask).ToArray(), millisecondsTimeout);

        /// <summary>
        /// Waits for all of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified number of milliseconds or until a cancellation
        /// token is cancelled.
        /// </summary>
        internal override bool WaitAllTasks(ControlledTask[] tasks, int millisecondsTimeout, CancellationToken cancellationToken) =>
            Task.WaitAll(tasks.Select(t => t.AwaiterTask).ToArray(), millisecondsTimeout, cancellationToken);

        /// <summary>
        /// Waits for all of the provided <see cref="ControlledTask"/> objects to complete
        /// execution unless the wait is cancelled.
        /// </summary>
        internal override void WaitAllTasks(ControlledTask[] tasks, CancellationToken cancellationToken) =>
            Task.WaitAll(tasks.Select(t => t.AwaiterTask).ToArray(), cancellationToken);

        /// <summary>
        /// Waits for all of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified time interval.
        /// </summary>
        internal override bool WaitAllTasks(ControlledTask[] tasks, TimeSpan timeout) =>
            Task.WaitAll(tasks.Select(t => t.AwaiterTask).ToArray(), timeout);

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete execution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int WaitAnyTask(params ControlledTask[] tasks) =>
            Task.WaitAny(tasks.Select(t => t.AwaiterTask).ToArray());

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified number of milliseconds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int WaitAnyTask(ControlledTask[] tasks, int millisecondsTimeout) =>
            Task.WaitAny(tasks.Select(t => t.AwaiterTask).ToArray(), millisecondsTimeout);

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified number of milliseconds or until a cancellation
        /// token is cancelled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int WaitAnyTask(ControlledTask[] tasks, int millisecondsTimeout, CancellationToken cancellationToken) =>
            Task.WaitAny(tasks.Select(t => t.AwaiterTask).ToArray(), millisecondsTimeout, cancellationToken);

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete
        /// execution unless the wait is cancelled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int WaitAnyTask(ControlledTask[] tasks, CancellationToken cancellationToken) =>
            Task.WaitAny(tasks.Select(t => t.AwaiterTask).ToArray(), cancellationToken);

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified time interval.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int WaitAnyTask(ControlledTask[] tasks, TimeSpan timeout) =>
            Task.WaitAny(tasks.Select(t => t.AwaiterTask).ToArray(), timeout);

        /// <summary>
        /// Creates a controlled awaiter that switches into a target environment.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override ControlledYieldAwaitable.ControlledYieldAwaiter CreateControlledYieldAwaiter() =>
            new ControlledYieldAwaitable.ControlledYieldAwaiter(this, default);

        /// <summary>
        /// Ends the wait for the completion of the yield operation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override void OnGetYieldResult(YieldAwaitable.YieldAwaiter awaiter) => awaiter.GetResult();

        /// <summary>
        /// Sets the action to perform when the yield operation completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override void OnYieldCompleted(Action continuation, YieldAwaitable.YieldAwaiter awaiter) =>
            awaiter.OnCompleted(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the yield operation completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override void OnUnsafeYieldCompleted(Action continuation, YieldAwaitable.YieldAwaiter awaiter) =>
            awaiter.UnsafeOnCompleted(continuation);

        /// <summary>
        /// Creates a mutual exclusion lock that is compatible with <see cref="ControlledTask"/> objects.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override ControlledLock CreateControlledLock()
        {
            var id = (ulong)Interlocked.Increment(ref this.LockIdCounter) - 1;
            return new ControlledLock(id);
        }

        /// <summary>
        /// Creates a new timer that sends a <see cref="TimerElapsedEvent"/> to its owner machine.
        /// </summary>
        internal override IMachineTimer CreateMachineTimer(TimerInfo info, StateMachine owner) => new MachineTimer(info, owner);

        /// <summary>
        /// Tries to create a new <see cref="Coyote.Specifications.Monitor"/> of the specified <see cref="Type"/>.
        /// </summary>
        internal override void TryCreateMonitor(Type type)
        {
            // Check if monitors are enabled in production.
            if (!this.Configuration.EnableMonitorsInProduction)
            {
                return;
            }

            lock (this.Monitors)
            {
                if (this.Monitors.Any(m => m.GetType() == type))
                {
                    // Idempotence: only one monitor per type can exist.
                    return;
                }
            }

            this.Assert(type.IsSubclassOf(typeof(Monitor)), "Type '{0}' is not a subclass of Monitor.", type.FullName);

            ActorId id = new ActorId(type, null, this);
            Monitor monitor = (Monitor)Activator.CreateInstance(type);

            monitor.Initialize(this, id);
            monitor.InitializeStateInformation();

            lock (this.Monitors)
            {
                this.Monitors.Add(monitor);
            }

            this.LogWriter.OnCreateMonitor(type.FullName, monitor.Id);

            monitor.GotoStartState();
        }

        /// <summary>
        /// Invokes the specified <see cref="Coyote.Specifications.Monitor"/> with the specified <see cref="Event"/>.
        /// </summary>
        internal override void Monitor(Type type, Actor sender, Event e)
        {
            // Check if monitors are enabled in production.
            if (!this.Configuration.EnableMonitorsInProduction)
            {
                return;
            }

            Monitor monitor = null;

            lock (this.Monitors)
            {
                foreach (var m in this.Monitors)
                {
                    if (m.GetType() == type)
                    {
                        monitor = m;
                        break;
                    }
                }
            }

            if (monitor != null)
            {
                lock (monitor)
                {
                    monitor.MonitorEvent(e);
                }
            }
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        internal override bool GetNondeterministicBooleanChoice(Actor machine, int maxValue)
        {
            bool result = false;
            if (this.ValueGenerator.Next(maxValue) == 0)
            {
                result = true;
            }

            this.LogWriter.OnRandom(machine?.Id, result);
            return result;
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        internal override bool GetFairNondeterministicBooleanChoice(Actor machine, string uniqueId) =>
            this.GetNondeterministicBooleanChoice(machine, 2);

        /// <summary>
        /// Returns a nondeterministic integer choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        internal override int GetNondeterministicIntegerChoice(Actor machine, int maxValue)
        {
            var result = this.ValueGenerator.Next(maxValue);
            this.LogWriter.OnRandom(machine?.Id, result);
            return result;
        }

        /// <summary>
        /// Notifies that a machine entered a state.
        /// </summary>
        internal override void NotifyEnteredState(StateMachine machine)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.OnMachineState(machine.Id, machine.CurrentStateName, isEntry: true);
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
                this.LogWriter.OnMonitorState(monitor.GetType().FullName, monitor.Id, monitorState, true, monitor.GetHotState());
            }
        }

        /// <summary>
        /// Notifies that a machine exited a state.
        /// </summary>
        internal override void NotifyExitedState(StateMachine machine)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.OnMachineState(machine.Id, machine.CurrentStateName, isEntry: false);
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
                this.LogWriter.OnMonitorState(monitor.GetType().FullName, monitor.Id, monitorState, false, monitor.GetHotState());
            }
        }

        /// <summary>
        /// Notifies that a machine invoked an action.
        /// </summary>
        internal override void NotifyInvokedAction(StateMachine machine, MethodInfo action, Event receivedEvent)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.OnMachineAction(machine.Id, machine.CurrentStateName, action.Name);
            }
        }

        /// <summary>
        /// Notifies that a machine invoked an action.
        /// </summary>
        internal override void NotifyInvokedOnEntryAction(StateMachine machine, MethodInfo action, Event receivedEvent)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.OnMachineAction(machine.Id, machine.CurrentStateName, action.Name);
            }
        }

        /// <summary>
        /// Notifies that a machine invoked an action.
        /// </summary>
        internal override void NotifyInvokedOnExitAction(StateMachine machine, MethodInfo action, Event receivedEvent)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.OnMachineAction(machine.Id, machine.CurrentStateName, action.Name);
            }
        }

        /// <summary>
        /// Notifies that a monitor invoked an action.
        /// </summary>
        internal override void NotifyInvokedAction(Monitor monitor, MethodInfo action, Event receivedEvent)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.OnMonitorAction(monitor.GetType().FullName, monitor.Id, action.Name, monitor.CurrentStateName);
            }
        }

        /// <summary>
        /// Notifies that a machine raised an <see cref="Event"/>.
        /// </summary>
        internal override void NotifyRaisedEvent(StateMachine machine, Event e, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.OnMachineEvent(machine.Id, machine.CurrentStateName, e.GetType().FullName);
            }
        }

        /// <summary>
        /// Notifies that a monitor raised an <see cref="Event"/>.
        /// </summary>
        internal override void NotifyRaisedEvent(Monitor monitor, Event e, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.OnMonitorEvent(monitor.GetType().FullName, monitor.Id, monitor.CurrentStateName,
                    e.GetType().FullName, isProcessing: false);
            }
        }

        /// <summary>
        /// Notifies that a machine dequeued an <see cref="Event"/>.
        /// </summary>
        internal override void NotifyDequeuedEvent(StateMachine machine, Event e, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.OnDequeue(machine.Id, machine.CurrentStateName, e.GetType().FullName);
            }
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive an event of one of the specified types.
        /// </summary>
        internal override void NotifyWaitEvent(StateMachine machine, IEnumerable<Type> eventTypes)
        {
            if (this.Configuration.IsVerbose)
            {
                var eventWaitTypesArray = eventTypes.ToArray();
                if (eventWaitTypesArray.Length == 1)
                {
                    this.LogWriter.OnWait(machine.Id, machine.CurrentStateName, eventWaitTypesArray[0]);
                }
                else
                {
                    this.LogWriter.OnWait(machine.Id, machine.CurrentStateName, eventWaitTypesArray);
                }
            }
        }

        /// <summary>
        /// Notifies that a machine enqueued an event that it was waiting to receive.
        /// </summary>
        internal override void NotifyReceivedEvent(StateMachine machine, Event e, EventInfo eventInfo)
        {
            this.LogWriter.OnReceive(machine.Id, machine.CurrentStateName, e.GetType().FullName, wasBlocked: true);
        }

        /// <summary>
        /// Notifies that a machine received an event without waiting because the event
        /// was already in the inbox when the machine invoked the receive statement.
        /// </summary>
        internal override void NotifyReceivedEventWithoutWaiting(StateMachine machine, Event e, EventInfo eventInfo)
        {
            this.LogWriter.OnReceive(machine.Id, machine.CurrentStateName, e.GetType().FullName, wasBlocked: false);
        }

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Monitors.Clear();
                this.MachineMap.Clear();
            }

            base.Dispose(disposing);
        }
    }
}
