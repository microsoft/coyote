// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Coyote.Timers;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Runtime for executing machines in production.
    /// </summary>
    internal sealed class ProductionRuntime : MachineRuntime
    {
        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        private readonly List<Monitor> Monitors;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductionRuntime"/> class.
        /// </summary>
        internal ProductionRuntime()
            : this(Configuration.Create())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductionRuntime"/> class.
        /// </summary>
        internal ProductionRuntime(Configuration configuration)
            : base(configuration)
        {
            this.Monitors = new List<Monitor>();
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
            this.CreateMachine(null, type, null, e, null, opGroupId);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and
        /// with the specified optional <see cref="Event"/>. This event can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        public override MachineId CreateMachine(Type type, string machineName, Event e = null, Guid opGroupId = default) =>
            this.CreateMachine(null, type, machineName, e, null, opGroupId);

        /// <summary>
        /// Creates a new machine of the specified type, using the specified <see cref="MachineId"/>.
        /// This method optionally passes an <see cref="Event"/> to the new machine, which can only
        /// be used to access its payload, and cannot be handled.
        /// </summary>
        public override MachineId CreateMachine(MachineId mid, Type type, Event e = null, Guid opGroupId = default) =>
            this.CreateMachine(mid, type, null, e, null, opGroupId);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with the
        /// specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when
        /// the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public override Task<MachineId> CreateMachineAndExecuteAsync(Type type, Event e = null, Guid opGroupId = default) =>
            this.CreateMachineAndExecuteAsync(null, type, null, e, null, opGroupId);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and with
        /// the specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when the
        /// machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public override Task<MachineId> CreateMachineAndExecuteAsync(Type type, string machineName, Event e = null, Guid opGroupId = default) =>
            this.CreateMachineAndExecuteAsync(null, type, machineName, e, null, opGroupId);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This
        /// event can only be used to access its payload, and cannot be handled. The method
        /// returns only when the machine is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        public override Task<MachineId> CreateMachineAndExecuteAsync(MachineId mid, Type type, Event e = null, Guid opGroupId = default) =>
            this.CreateMachineAndExecuteAsync(mid, type, null, e, null, opGroupId);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and with the
        /// specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when
        /// the machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public override Task<MachineId> CreateMachineAndExecute(Type type, Event e = null, Guid opGroupId = default) =>
            this.CreateMachineAndExecuteAsync(null, type, null, e, null, opGroupId);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, and with
        /// the specified optional <see cref="Event"/>. This event can only be used to
        /// access its payload, and cannot be handled. The method returns only when the
        /// machine is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public override Task<MachineId> CreateMachineAndExecute(Type type, string machineName, Event e = null, Guid opGroupId = default) =>
            this.CreateMachineAndExecuteAsync(null, type, machineName, e, null, opGroupId);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/>, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This
        /// event can only be used to access its payload, and cannot be handled. The method
        /// returns only when the machine is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        public override Task<MachineId> CreateMachineAndExecute(MachineId mid, Type type, Event e = null, Guid opGroupId = default) =>
            this.CreateMachineAndExecuteAsync(mid, type, null, e, null, opGroupId);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        public override void SendEvent(MachineId target, Event e, Guid opGroupId = default, SendOptions options = null) =>
            this.SendEvent(target, e, null, opGroupId, options);

        /// <summary>
        /// Sends an <see cref="Event"/> to a machine. Returns immediately if the target machine was already
        /// running. Otherwise blocks until the machine handles the event and reaches quiescense.
        /// </summary>
        public override Task<bool> SendEventAndExecuteAsync(MachineId target, Event e, Guid opGroupId = default, SendOptions options = null) =>
            this.SendEventAndExecuteAsync(target, e, null, opGroupId, options);

        /// <summary>
        /// Sends an <see cref="Event"/> to a machine. Returns immediately if the target machine was already
        /// running. Otherwise blocks until the machine handles the event and reaches quiescense.
        /// </summary>
        public override Task<bool> SendEventAndExecute(MachineId target, Event e, Guid opGroupId = default, SendOptions options = null) =>
            this.SendEventAndExecuteAsync(target, e, opGroupId, options);

        /// <summary>
        /// Returns the operation group id of the specified machine. Returns <see cref="Guid.Empty"/>
        /// if the id is not set, or if the <see cref="MachineId"/> is not associated with this runtime.
        /// During testing, the runtime asserts that the specified machine is currently executing.
        /// </summary>
        public override Guid GetCurrentOperationGroupId(MachineId currentMachine)
        {
            Machine machine = this.GetMachineFromId<Machine>(currentMachine);
            return machine is null ? Guid.Empty : machine.OperationGroupId;
        }

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>.
        /// </summary>
        internal override MachineId CreateMachine(MachineId mid, Type type, string machineName, Event e,
            Machine creator, Guid opGroupId)
        {
            Machine machine = this.CreateMachine(mid, type, machineName, creator, opGroupId);
            this.Logger.OnCreateMachine(machine.Id, creator?.Id);
            this.RunMachineEventHandler(machine, e, true);
            return machine.Id;
        }

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>. The
        /// method returns only when the created machine reaches quiescence.
        /// </summary>
        internal override async Task<MachineId> CreateMachineAndExecuteAsync(MachineId mid, Type type, string machineName, Event e,
            Machine creator, Guid opGroupId)
        {
            Machine machine = this.CreateMachine(mid, type, machineName, creator, opGroupId);
            this.Logger.OnCreateMachine(machine.Id, creator?.Id);
            await this.RunMachineEventHandlerAsync(machine, e, true);
            return machine.Id;
        }

        /// <summary>
        /// Creates a new <see cref="Machine"/> of the specified <see cref="Type"/>.
        /// </summary>
        private Machine CreateMachine(MachineId mid, Type type, string machineName, Machine creator, Guid opGroupId)
        {
            if (!type.IsSubclassOf(typeof(Machine)))
            {
                this.Assert(false, "Type '{0}' is not a machine.", type.FullName);
            }

            if (mid is null)
            {
                mid = new MachineId(type, machineName, this);
            }
            else if (mid.Runtime != null && mid.Runtime != this)
            {
                this.Assert(false, "Unbound machine id '{0}' was created by another runtime.", mid.Value);
            }
            else if (mid.Type != type.FullName)
            {
                this.Assert(false, "Cannot bind machine id '{0}' of type '{1}' to a machine of type '{2}'.",
                    mid.Value, mid.Type, type.FullName);
            }
            else
            {
                mid.Bind(this);
            }

            // The operation group id of the machine is set using the following precedence:
            // (1) To the specified machine creation operation group id, if it is non-empty.
            // (2) To the operation group id of the creator machine, if it exists.
            // (3) To the empty operation group id.
            if (opGroupId == Guid.Empty && creator != null)
            {
                opGroupId = creator.OperationGroupId;
            }

            Machine machine = MachineFactory.Create(type);
            IMachineStateManager stateManager = new MachineStateManager(this, machine, opGroupId);
            IEventQueue eventQueue = new EventQueue(stateManager);

            machine.Initialize(this, mid, stateManager, eventQueue);
            machine.InitializeStateInformation();

            if (!this.MachineMap.TryAdd(mid, machine))
            {
                string info = "This typically occurs if either the machine id was created by another runtime instance, " +
                    "or if a machine id from a previous runtime generation was deserialized, but the current runtime " +
                    "has not increased its generation value.";
                this.Assert(false, "Machine with id '{0}' was already created in generation '{1}'. {2}", mid.Value, mid.Generation, info);
            }

            return machine;
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        internal override void SendEvent(MachineId target, Event e, AsyncMachine sender, Guid opGroupId, SendOptions options)
        {
            EnqueueStatus enqueueStatus = this.EnqueueEvent(target, e, sender, opGroupId, out Machine targetMachine);
            if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
            {
                this.RunMachineEventHandler(targetMachine, null, false);
            }
        }

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine. Returns immediately if the target machine was
        /// already running. Otherwise blocks until the machine handles the event and reaches quiescense.
        /// </summary>
        internal override async Task<bool> SendEventAndExecuteAsync(MachineId target, Event e, AsyncMachine sender,
            Guid opGroupId, SendOptions options)
        {
            EnqueueStatus enqueueStatus = this.EnqueueEvent(target, e, sender, opGroupId, out Machine targetMachine);
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
        private EnqueueStatus EnqueueEvent(MachineId target, Event e, AsyncMachine sender, Guid opGroupId, out Machine targetMachine)
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

            targetMachine = this.GetMachineFromId<Machine>(target);
            if (targetMachine is null)
            {
                this.Logger.OnSend(target, sender?.Id, (sender as Machine)?.CurrentStateName ?? string.Empty,
                    e.GetType().FullName, opGroupId, isTargetHalted: true);
                this.TryHandleDroppedEvent(e, target);
                return EnqueueStatus.Dropped;
            }

            this.Logger.OnSend(target, sender?.Id, (sender as Machine)?.CurrentStateName ?? string.Empty,
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
        private void RunMachineEventHandler(Machine machine, Event initialEvent, bool isFresh)
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
                        this.MachineMap.TryRemove(machine.Id, out AsyncMachine _);
                    }
                }
            });
        }

        /// <summary>
        /// Runs a new asynchronous machine event handler.
        /// </summary>
        private async Task RunMachineEventHandlerAsync(Machine machine, Event initialEvent, bool isFresh)
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
        /// Creates a new timer that sends a <see cref="TimerElapsedEvent"/> to its owner machine.
        /// </summary>
        internal override IMachineTimer CreateMachineTimer(TimerInfo info, Machine owner) => new MachineTimer(info, owner);

        /// <summary>
        /// Tries to create a new <see cref="Coyote.Monitor"/> of the specified <see cref="Type"/>.
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

            MachineId mid = new MachineId(type, null, this);
            Monitor monitor = (Monitor)Activator.CreateInstance(type);

            monitor.Initialize(this, mid);
            monitor.InitializeStateInformation();

            lock (this.Monitors)
            {
                this.Monitors.Add(monitor);
            }

            this.Logger.OnCreateMonitor(type.FullName, monitor.Id);

            monitor.GotoStartState();
        }

        /// <summary>
        /// Invokes the specified <see cref="Coyote.Monitor"/> with the specified <see cref="Event"/>.
        /// </summary>
        internal override void Monitor(Type type, AsyncMachine sender, Event e)
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
        internal override bool GetNondeterministicBooleanChoice(AsyncMachine machine, int maxValue)
        {
            Random random = new Random(DateTime.Now.Millisecond);

            bool result = false;
            if (random.Next(maxValue) == 0)
            {
                result = true;
            }

            this.Logger.OnRandom(machine?.Id, result);

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

            this.Logger.OnRandom(machine?.Id, result);

            return result;
        }

        /// <summary>
        /// Notifies that a machine entered a state.
        /// </summary>
        internal override void NotifyEnteredState(Machine machine)
        {
            if (this.Configuration.IsVerbose)
            {
                this.Logger.OnMachineState(machine.Id, machine.CurrentStateName, isEntry: true);
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
                this.Logger.OnMonitorState(monitor.GetType().FullName, monitor.Id, monitorState, true, monitor.GetHotState());
            }
        }

        /// <summary>
        /// Notifies that a machine exited a state.
        /// </summary>
        internal override void NotifyExitedState(Machine machine)
        {
            if (this.Configuration.IsVerbose)
            {
                this.Logger.OnMachineState(machine.Id, machine.CurrentStateName, isEntry: false);
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
                this.Logger.OnMonitorState(monitor.GetType().FullName, monitor.Id, monitorState, false, monitor.GetHotState());
            }
        }

        /// <summary>
        /// Notifies that a machine invoked an action.
        /// </summary>
        internal override void NotifyInvokedAction(Machine machine, MethodInfo action, Event receivedEvent)
        {
            if (this.Configuration.IsVerbose)
            {
                this.Logger.OnMachineAction(machine.Id, machine.CurrentStateName, action.Name);
            }
        }

        /// <summary>
        /// Notifies that a machine invoked an action.
        /// </summary>
        internal override void NotifyInvokedOnEntryAction(Machine machine, MethodInfo action, Event receivedEvent)
        {
            if (this.Configuration.IsVerbose)
            {
                this.Logger.OnMachineAction(machine.Id, machine.CurrentStateName, action.Name);
            }
        }

        /// <summary>
        /// Notifies that a machine invoked an action.
        /// </summary>
        internal override void NotifyInvokedOnExitAction(Machine machine, MethodInfo action, Event receivedEvent)
        {
            if (this.Configuration.IsVerbose)
            {
                this.Logger.OnMachineAction(machine.Id, machine.CurrentStateName, action.Name);
            }
        }

        /// <summary>
        /// Notifies that a monitor invoked an action.
        /// </summary>
        internal override void NotifyInvokedAction(Monitor monitor, MethodInfo action, Event receivedEvent)
        {
            if (this.Configuration.IsVerbose)
            {
                this.Logger.OnMonitorAction(monitor.GetType().FullName, monitor.Id, action.Name, monitor.CurrentStateName);
            }
        }

        /// <summary>
        /// Notifies that a machine raised an <see cref="Event"/>.
        /// </summary>
        internal override void NotifyRaisedEvent(Machine machine, Event e, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose)
            {
                this.Logger.OnMachineEvent(machine.Id, machine.CurrentStateName, e.GetType().FullName);
            }
        }

        /// <summary>
        /// Notifies that a monitor raised an <see cref="Event"/>.
        /// </summary>
        internal override void NotifyRaisedEvent(Monitor monitor, Event e, EventInfo eventInfo)
        {
            if (this.Configuration.IsVerbose)
            {
                this.Logger.OnMonitorEvent(monitor.GetType().FullName, monitor.Id, monitor.CurrentStateName,
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
                this.Logger.OnDequeue(machine.Id, machine.CurrentStateName, e.GetType().FullName);
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
                    this.Logger.OnWait(machine.Id, machine.CurrentStateName, eventWaitTypesArray[0]);
                }
                else
                {
                    this.Logger.OnWait(machine.Id, machine.CurrentStateName, eventWaitTypesArray);
                }
            }
        }

        /// <summary>
        /// Notifies that a machine enqueued an event that it was waiting to receive.
        /// </summary>
        internal override void NotifyReceivedEvent(Machine machine, Event e, EventInfo eventInfo)
        {
            this.Logger.OnReceive(machine.Id, machine.CurrentStateName, e.GetType().FullName, wasBlocked: true);
        }

        /// <summary>
        /// Notifies that a machine received an event without waiting because the event
        /// was already in the inbox when the machine invoked the receive statement.
        /// </summary>
        internal override void NotifyReceivedEventWithoutWaiting(Machine machine, Event e, EventInfo eventInfo)
        {
            this.Logger.OnReceive(machine.Id, machine.CurrentStateName, e.GetType().FullName, wasBlocked: false);
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
