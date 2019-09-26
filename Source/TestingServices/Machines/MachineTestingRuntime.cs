// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Coyote.Machines;
using Microsoft.Coyote.Machines.Timers;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.TestingServices.Timers;
using Microsoft.Coyote.Threading;
using Microsoft.Coyote.Threading.Tasks;

using EventInfo = Microsoft.Coyote.Runtime.EventInfo;
using Monitor = Microsoft.Coyote.Specifications.Monitor;

namespace Microsoft.Coyote.TestingServices.Runtime
{
    /// <summary>
    /// Runtime for testing a machine in isolation.
    /// </summary>
    internal sealed class MachineTestingRuntime : CoyoteRuntime
    {
        /// <summary>
        /// The machine being tested.
        /// </summary>
        internal readonly Machine Machine;

        /// <summary>
        /// The inbox of the machine being tested.
        /// </summary>
        internal readonly EventQueue MachineInbox;

        /// <summary>
        /// Task completion source that completes when the machine being tested reaches quiescence.
        /// </summary>
        private TaskCompletionSource<bool> QuiescenceCompletionSource;

        /// <summary>
        /// True if the machine is waiting to receive and event, else false.
        /// </summary>
        internal bool IsMachineWaitingToReceiveEvent { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineTestingRuntime"/> class.
        /// </summary>
        internal MachineTestingRuntime(Type machineType, Configuration configuration)
            : base(configuration)
        {
            if (!machineType.IsSubclassOf(typeof(Machine)))
            {
                this.Assert(false, "Type '{0}' is not a machine.", machineType.FullName);
            }

            var mid = new MachineId(machineType, null, this);

            this.Machine = MachineFactory.Create(machineType);
            IMachineStateManager stateManager = new MachineStateManager(this, this.Machine, Guid.Empty);
            this.MachineInbox = new EventQueue(stateManager);

            this.Machine.Initialize(this, mid, stateManager, this.MachineInbox);
            this.Machine.InitializeStateInformation();

            this.LogWriter.OnCreateMachine(this.Machine.Id, null);

            this.MachineMap.TryAdd(mid, this.Machine);

            this.IsMachineWaitingToReceiveEvent = false;
        }

        /// <summary>
        /// Starts executing the machine-under-test by transitioning it to its initial state
        /// and passing an optional initialization event.
        /// </summary>
        internal Task StartAsync(Event initialEvent)
        {
            this.RunMachineEventHandler(this.Machine, initialEvent, true);
            return this.QuiescenceCompletionSource.Task;
        }

        /// <summary>
        /// Creates a machine id that is uniquely tied to the specified unique name. The
        /// returned machine id can either be a fresh id (not yet bound to any machine),
        /// or it can be bound to a previously created machine. In the second case, this
        /// machine id can be directly used to communicate with the corresponding machine.
        /// </summary>
        public override MachineId CreateMachineIdFromName(Type type, string machineName) => new MachineId(type, machineName, this, true);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with
        /// the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        public override MachineId CreateMachine(Type type, Event e = null, Guid opGroupId = default) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        public override MachineId CreateMachine(Type type, string machineName, Event e = null, Guid opGroupId = default) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a new machine of the specified type, using the specified <see cref="MachineId"/>.
        /// This method optionally passes an <see cref="Event"/> to the new machine, which can only
        /// be used to access its payload, and cannot be handled.
        /// </summary>
        public override MachineId CreateMachine(MachineId mid, Type type, Event e = null, Guid opGroupId = default) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with the
        /// specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when
        /// the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public override Task<MachineId> CreateMachineAndExecuteAsync(Type type, Event e = null, Guid opGroupId = default) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and with
        /// the specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when the
        /// machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public override Task<MachineId> CreateMachineAndExecuteAsync(Type type, string machineName, Event e = null, Guid opGroupId = default) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This
        /// event can only be used to access its payload, and cannot be handled. The method
        /// returns only when the machine is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        public override Task<MachineId> CreateMachineAndExecuteAsync(MachineId mid, Type type, Event e = null, Guid opGroupId = default) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        public override void SendEvent(MachineId target, Event e, Guid opGroupId = default, SendOptions options = null) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Sends an <see cref="Event"/> to a machine. Returns immediately if the target machine was already
        /// running. Otherwise blocks until the machine handles the event and reaches quiescense.
        /// </summary>
        public override Task<bool> SendEventAndExecuteAsync(MachineId target, Event e, Guid opGroupId = default, SendOptions options = null) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Returns the operation group id of the specified machine. Returns <see cref="Guid.Empty"/>
        /// if the id is not set, or if the <see cref="MachineId"/> is not associated with this runtime.
        /// During testing, the runtime asserts that the specified machine is currently executing.
        /// </summary>
        public override Guid GetCurrentOperationGroupId(MachineId currentMachine) => Guid.Empty;

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>.
        /// </summary>
        internal override MachineId CreateMachine(MachineId mid, Type type, string machineName, Event e,
            Machine creator, Guid opGroupId)
        {
            mid = mid ?? new MachineId(type, null, this);
            this.LogWriter.OnCreateMachine(mid, creator?.Id);
            return mid;
        }

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>. The
        /// method returns only when the created machine reaches quiescence.
        /// </summary>
        internal override Task<MachineId> CreateMachineAndExecuteAsync(MachineId mid, Type type, string machineName, Event e,
            Machine creator, Guid opGroupId)
        {
            mid = mid ?? new MachineId(type, null, this);
            this.LogWriter.OnCreateMachine(mid, creator?.Id);
            return Task.FromResult(mid);
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        internal override void SendEvent(MachineId target, Event e, AsyncMachine sender, Guid opGroupId, SendOptions options)
        {
            this.Assert(sender is null || this.Machine.Id.Equals(sender.Id),
                string.Format("Only machine '{0}' can send an event during this test.", this.Machine.Id.ToString()));
            this.Assert(target != null, string.Format("Machine '{0}' is sending to a null machine.", this.Machine.Id.ToString()));
            this.Assert(e != null, string.Format("Machine '{0}' is sending a null event.", this.Machine.Id.ToString()));

            // The operation group id of this operation is set using the following precedence:
            // (1) To the specified send operation group id, if it is non-empty.
            // (2) To the operation group id of the sender machine, if it exists and is non-empty.
            // (3) To the empty operation group id.
            if (opGroupId == Guid.Empty && sender != null)
            {
                opGroupId = sender.OperationGroupId;
            }

            if (this.Machine.IsHalted)
            {
                this.LogWriter.OnSend(target, sender?.Id, (sender as Machine)?.CurrentStateName ?? string.Empty,
                    e.GetType().FullName, opGroupId, isTargetHalted: true);
                return;
            }

            this.LogWriter.OnSend(target, sender?.Id, (sender as Machine)?.CurrentStateName ?? string.Empty,
                e.GetType().FullName, opGroupId, isTargetHalted: false);

            if (!target.Equals(this.Machine.Id))
            {
                // Drop all events sent to a machine other than the machine-under-test.
                return;
            }

            EnqueueStatus enqueueStatus = this.Machine.Enqueue(e, opGroupId, null);
            if (enqueueStatus == EnqueueStatus.EventHandlerNotRunning)
            {
                this.RunMachineEventHandler(this.Machine, null, false);
            }
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine. Returns immediately if the target machine was
        /// already running. Otherwise blocks until the machine handles the event and reaches quiescense.
        /// </summary>
        internal override Task<bool> SendEventAndExecuteAsync(MachineId target, Event e, AsyncMachine sender,
            Guid opGroupId, SendOptions options)
        {
            this.SendEvent(target, e, sender, opGroupId, options);
            return this.QuiescenceCompletionSource.Task;
        }

        /// <summary>
        /// Runs a new asynchronous machine event handler.
        /// </summary>
        private Task RunMachineEventHandler(Machine machine, Event initialEvent, bool isFresh)
        {
            this.QuiescenceCompletionSource = new TaskCompletionSource<bool>();

            return Task.Run(async () =>
            {
                try
                {
                    if (isFresh)
                    {
                        await machine.GotoStartState(initialEvent);
                    }

                    await machine.RunEventHandlerAsync();
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
        /// Creates a new <see cref="ControlledTask"/> to execute the specified asynchronous work.
        /// </summary>
        internal override ControlledTask CreateControlledTask(Action action, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a new <see cref="ControlledTask"/> to execute the specified asynchronous work.
        /// </summary>
        internal override ControlledTask CreateControlledTask(Func<ControlledTask> function, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a new <see cref="ControlledTask{TResult}"/> to execute the specified asynchronous work.
        /// </summary>
        internal override ControlledTask<TResult> CreateControlledTask<TResult>(Func<TResult> function,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a new <see cref="ControlledTask{TResult}"/> to execute the specified asynchronous work.
        /// </summary>
        internal override ControlledTask<TResult> CreateControlledTask<TResult>(Func<ControlledTask<TResult>> function,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a new <see cref="ControlledTask"/> to execute the specified asynchronous delay.
        /// </summary>
        internal override ControlledTask CreateControlledTaskDelay(int millisecondsDelay, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a new <see cref="ControlledTask"/> to execute the specified asynchronous delay.
        /// </summary>
        internal override ControlledTask CreateControlledTaskDelay(TimeSpan delay, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> associated with a completion source.
        /// </summary>
        internal override ControlledTask CreateControlledTaskCompletionSource(Task task) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a <see cref="ControlledTask{TResult}"/> associated with a completion source.
        /// </summary>
        internal override ControlledTask<TResult> CreateControlledTaskCompletionSource<TResult>(Task<TResult> task) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        internal override ControlledTask WaitAllTasksAsync(params ControlledTask[] tasks) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        internal override ControlledTask WaitAllTasksAsync(params Task[] tasks) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        internal override ControlledTask WaitAllTasksAsync(IEnumerable<ControlledTask> tasks) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        internal override ControlledTask WaitAllTasksAsync(IEnumerable<Task> tasks) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        internal override ControlledTask<TResult[]> WaitAllTasksAsync<TResult>(params ControlledTask<TResult>[] tasks) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        internal override ControlledTask<TResult[]> WaitAllTasksAsync<TResult>(params Task<TResult>[] tasks) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        internal override ControlledTask<TResult[]> WaitAllTasksAsync<TResult>(IEnumerable<ControlledTask<TResult>> tasks) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        internal override ControlledTask<TResult[]> WaitAllTasksAsync<TResult>(IEnumerable<Task<TResult>> tasks) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        internal override ControlledTask<Task> WaitAnyTaskAsync(params ControlledTask[] tasks) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        internal override ControlledTask<Task> WaitAnyTaskAsync(params Task[] tasks) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        internal override ControlledTask<Task> WaitAnyTaskAsync(IEnumerable<ControlledTask> tasks) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        internal override ControlledTask<Task> WaitAnyTaskAsync(IEnumerable<Task> tasks) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        internal override ControlledTask<Task<TResult>> WaitAnyTaskAsync<TResult>(params ControlledTask<TResult>[] tasks) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        internal override ControlledTask<Task<TResult>> WaitAnyTaskAsync<TResult>(params Task<TResult>[] tasks) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        internal override ControlledTask<Task<TResult>> WaitAnyTaskAsync<TResult>(IEnumerable<ControlledTask<TResult>> tasks) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        internal override ControlledTask<Task<TResult>> WaitAnyTaskAsync<TResult>(IEnumerable<Task<TResult>> tasks) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete execution.
        /// </summary>
        internal override int WaitAnyTask(params ControlledTask[] tasks) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified number of milliseconds.
        /// </summary>
        internal override int WaitAnyTask(ControlledTask[] tasks, int millisecondsTimeout) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified number of milliseconds or until a cancellation
        /// token is cancelled.
        /// </summary>
        internal override int WaitAnyTask(ControlledTask[] tasks, int millisecondsTimeout, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete
        /// execution unless the wait is cancelled.
        /// </summary>
        internal override int WaitAnyTask(ControlledTask[] tasks, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified time interval.
        /// </summary>
        internal override int WaitAnyTask(ControlledTask[] tasks, TimeSpan timeout) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a controlled awaiter that switches into a target environment.
        /// </summary>
        internal override ControlledYieldAwaitable.ControlledYieldAwaiter CreateControlledYieldAwaiter() =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Ends the wait for the completion of the yield operation.
        /// </summary>
        internal override void OnGetYieldResult(YieldAwaitable.YieldAwaiter awaiter) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Sets the action to perform when the yield operation completes.
        /// </summary>
        internal override void OnYieldCompleted(Action continuation, YieldAwaitable.YieldAwaiter awaiter) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Schedules the continuation action that is invoked when the yield operation completes.
        /// </summary>
        internal override void OnUnsafeYieldCompleted(Action continuation, YieldAwaitable.YieldAwaiter awaiter) =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a mutual exclusion lock that is compatible with <see cref="ControlledTask"/> objects.
        /// </summary>
        internal override ControlledLock CreateControlledLock() =>
            throw new NotSupportedException("Invoking this method is not supported in machine unit testing mode.");

        /// <summary>
        /// Creates a new timer that sends a <see cref="TimerElapsedEvent"/> to its owner machine.
        /// </summary>
        internal override IMachineTimer CreateMachineTimer(TimerInfo info, Machine owner)
        {
            var mid = this.CreateMachineId(typeof(MockMachineTimer));
            this.CreateMachine(mid, typeof(MockMachineTimer), new TimerSetupEvent(info, owner, this.Configuration.TimeoutDelay));
            return this.GetMachineFromId<MockMachineTimer>(mid);
        }

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
        internal override void Monitor(Type type, AsyncMachine sender, Event e)
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
        internal override bool GetNondeterministicBooleanChoice(AsyncMachine machine, int maxValue)
        {
            Random random = new Random(DateTime.Now.Millisecond);

            bool result = false;
            if (random.Next(maxValue) == 0)
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
        internal override bool GetFairNondeterministicBooleanChoice(AsyncMachine machine, string uniqueId)
        {
            return this.GetNondeterministicBooleanChoice(machine, 2);
        }

        /// <summary>
        /// Returns a nondeterministic integer choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        internal override int GetNondeterministicIntegerChoice(AsyncMachine machine, int maxValue)
        {
            Random random = new Random(DateTime.Now.Millisecond);
            var result = random.Next(maxValue);

            this.LogWriter.OnRandom(machine?.Id, result);

            return result;
        }

        /// <summary>
        /// Notifies that a machine entered a state.
        /// </summary>
        internal override void NotifyEnteredState(Machine machine)
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
        internal override void NotifyExitedState(Machine machine)
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
        internal override void NotifyInvokedAction(Machine machine, MethodInfo action, Event receivedEvent)
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
        internal override void NotifyRaisedEvent(Machine machine, Event e, EventInfo eventInfo)
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
        internal override void NotifyDequeuedEvent(Machine machine, Event e, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose)
            {
                this.LogWriter.OnDequeue(machine.Id, machine.CurrentStateName, e.GetType().FullName);
            }
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive an event of one of the specified types.
        /// </summary>
        internal override void NotifyWaitEvent(Machine machine, IEnumerable<Type> eventTypes)
        {
            if (this.Configuration.IsVerbose)
            {
                var eventWaitTypesArray = eventTypes.ToArray();
                if (eventWaitTypesArray.Length == 1)
                {
                    this.LogWriter.OnWait(this.Machine.Id, this.Machine.CurrentStateName, eventWaitTypesArray[0]);
                }
                else
                {
                    this.LogWriter.OnWait(this.Machine.Id, this.Machine.CurrentStateName, eventWaitTypesArray);
                }
            }

            this.IsMachineWaitingToReceiveEvent = true;
            this.QuiescenceCompletionSource.SetResult(true);
        }

        /// <summary>
        /// Notifies that a machine received an event that it was waiting for.
        /// </summary>
        internal override void NotifyReceivedEvent(Machine machine, Event e, EventInfo eventInfo)
        {
            this.LogWriter.OnReceive(machine.Id, machine.CurrentStateName, e.GetType().FullName, wasBlocked: true);
            this.IsMachineWaitingToReceiveEvent = false;
            this.QuiescenceCompletionSource = new TaskCompletionSource<bool>();
        }

        /// <summary>
        /// Notifies that a machine received an event without waiting because the event
        /// was already in the inbox when the machine invoked the receive statement.
        /// </summary>
        internal override void NotifyReceivedEventWithoutWaiting(Machine machine, Event e, EventInfo eventInfo)
        {
            this.LogWriter.OnReceive(machine.Id, machine.CurrentStateName, e.GetType().FullName, wasBlocked: false);
        }

        /// <summary>
        /// Notifies that a machine has halted.
        /// </summary>
        internal override void NotifyHalted(Machine machine)
        {
            this.MachineMap.TryRemove(machine.Id, out AsyncMachine _);
        }

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.MachineMap.Clear();
            }

            base.Dispose(disposing);
        }
    }
}
