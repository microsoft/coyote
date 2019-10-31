// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Runtime.Exploration;

namespace Microsoft.Coyote.IO
{
    /// <summary>
    /// Default implementation of a log writer that logs runtime
    /// messages using the installed <see cref="ILogger"/>.
    /// </summary>
    public class ActorRuntimeLogWriter : IActorRuntimeLog, IDisposable
    {
        /// <summary>
        /// Used to log messages. To set a custom logger, use the runtime
        /// method <see cref="IActorRuntime.SetLogger(ILogger)"/>.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorRuntimeLogWriter"/> class
        /// with the default logger.
        /// </summary>
        public ActorRuntimeLogWriter()
        {
            this.Logger = new NulLogger();
        }

        /// <summary>
        /// Allows you to chain log writers.
        /// </summary>
        public IActorRuntimeLog Next { get; set; }

        /// <summary>
        /// Called when an event is about to be enqueued to a state machine.
        /// </summary>
        /// <param name="id">Id of the state machine that the event is being enqueued to.</param>
        /// <param name="eventName">Name of the event.</param>
        public virtual void OnEnqueue(ActorId id, string eventName)
        {
            this.Next?.OnEnqueue(id, eventName);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnEnqueueLogMessage(id, eventName));
            }
        }

        /// <summary>
        /// Called when an event is dequeued by a state machine.
        /// </summary>
        /// <param name="id">Id of the state machine that the event is being dequeued by.</param>
        /// <param name="currStateName">The name of the current state of the state machine, if any.</param>
        /// <param name="eventName">Name of the event.</param>
        public virtual void OnDequeue(ActorId id, string currStateName, string eventName)
        {
            this.Next?.OnDequeue(id, currStateName, eventName);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnDequeueLogMessage(id, currStateName, eventName));
            }
        }

        /// <summary>
        /// Called when the default event handler for a state is about to be executed.
        /// </summary>
        /// <param name="id">Id of the state machine that the state will execute in.</param>
        /// <param name="currStateName">Name of the current state of the state machine.</param>
        public virtual void OnDefault(ActorId id, string currStateName)
        {
            this.Next?.OnDefault(id, currStateName);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnDefaultLogMessage(id, currStateName));
            }
        }

        /// <summary>
        /// Called when a state machine transitions states via a 'goto'.
        /// </summary>
        /// <param name="id">Id of the state machine.</param>
        /// <param name="currStateName">The name of the current state of the state machine, if any.</param>
        /// <param name="newStateName">The target state of goto.</param>
        public virtual void OnGoto(ActorId id, string currStateName, string newStateName)
        {
            this.Next?.OnGoto(id, currStateName, newStateName);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnGotoLogMessage(id, currStateName, newStateName));
            }
        }

        /// <summary>
        /// Called when a state machine is being pushed to a state.
        /// </summary>
        /// <param name="id">Id of the state machine being pushed to the state.</param>
        /// <param name="currStateName">The name of the current state of the state machine, if any.</param>
        /// <param name="newStateName">The state the state machine is pushed to.</param>
        public virtual void OnPush(ActorId id, string currStateName, string newStateName)
        {
            this.Next?.OnPush(id, currStateName, newStateName);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnPushLogMessage(id, currStateName, newStateName));
            }
        }

        /// <summary>
        /// Called when a state machine has been popped from a state.
        /// </summary>
        /// <param name="id">Id of the state machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the state machine, if any.</param>
        /// <param name="restoredStateName">The name of the state being re-entered, if any</param>
        public virtual void OnPop(ActorId id, string currStateName, string restoredStateName)
        {
            this.Next?.OnPop(id, currStateName, restoredStateName);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnPopLogMessage(id, currStateName, restoredStateName));
            }
        }

        /// <summary>
        /// When an event cannot be handled in the current state, its exit handler is executed and then the state is
        /// popped and any previous "current state" is reentered. This handler is called when that pop has been done.
        /// </summary>
        /// <param name="id">Id of the state machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the state machine, if any.</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        public virtual void OnPopUnhandledEvent(ActorId id, string currStateName, string eventName)
        {
            this.Next?.OnPopUnhandledEvent(id, currStateName, eventName);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnPopUnhandledEventLogMessage(id, currStateName, eventName));
            }
        }

        /// <summary>
        /// Called when an event is received by a state machine.
        /// </summary>
        /// <param name="id">Id of the state machine that received the event.</param>
        /// <param name="currStateName">The name of the current state of the state machine, if any.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="wasBlocked">The state machine was waiting for one or more specific events,
        /// and <paramref name="eventName"/> was one of them</param>
        public virtual void OnReceive(ActorId id, string currStateName, string eventName, bool wasBlocked)
        {
            this.Next?.OnReceive(id, currStateName, eventName, wasBlocked);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnReceiveLogMessage(id, currStateName, eventName, wasBlocked));
            }
        }

        /// <summary>
        /// Called when a state machine waits to receive an event of a specified type.
        /// </summary>
        /// <param name="id">Id of the state machine that is entering the wait state.</param>
        /// <param name="currStateName">The name of the current state of the state machine, if any.</param>
        /// <param name="eventType">The type of the event being waited for.</param>
        public virtual void OnWait(ActorId id, string currStateName, Type eventType)
        {
            this.Next?.OnWait(id, currStateName, eventType);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnWaitLogMessage(id, currStateName, eventType));
            }
        }

        /// <summary>
        /// Called when a state machine waits to receive an event of one of the specified types.
        /// </summary>
        /// <param name="id">Id of the state machine that is entering the wait state.</param>
        /// <param name="currStateName">The name of the current state of the state machine, if any.</param>
        /// <param name="eventTypes">The types of the events being waited for, if any.</param>
        public virtual void OnWait(ActorId id, string currStateName, params Type[] eventTypes)
        {
            this.Next?.OnWait(id, currStateName, eventTypes);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnWaitLogMessage(id, currStateName, eventTypes));
            }
        }

        /// <summary>
        /// Called when an event is sent to a target state machine.
        /// </summary>
        /// <param name="targetActorId">Id of the target state machine.</param>
        /// <param name="senderId">The id of the state machine that sent the event, if any.</param>
        /// <param name="senderStateName">The name of the current state of the sender state machine, if any.</param>
        /// <param name="eventName">The event being sent.</param>
        /// <param name="opGroupId">Id used to identify the send operation.</param>
        /// <param name="isTargetHalted">Is the target state machine halted.</param>
        public virtual void OnSend(ActorId targetActorId, ActorId senderId, string senderStateName, string eventName,
            Guid opGroupId, bool isTargetHalted)
        {
            this.Next?.OnSend(targetActorId, senderId, senderStateName, eventName, opGroupId, isTargetHalted);

            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnSendLogMessage(targetActorId, senderId, senderStateName, eventName, opGroupId, isTargetHalted));
            }
        }

        /// <summary>
        /// Called when a state machine has been created.
        /// </summary>
        /// <param name="id">The id of the state machine that has been created.</param>
        /// <param name="creator">Id of the creator state machine, null otherwise.</param>
        public virtual void OnCreateStateMachine(ActorId id, ActorId creator)
        {
            this.Next?.OnCreateStateMachine(id, creator);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnCreateMachineLogMessage(id, creator));
            }
        }

        /// <summary>
        /// Called when a monitor has been created.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor that has been created.</param>
        /// <param name="id">The id of the monitor that has been created.</param>
        public virtual void OnCreateMonitor(string monitorTypeName, ActorId id)
        {
            this.Next?.OnCreateMonitor(monitorTypeName, id);

            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnCreateMonitorLogMessage(monitorTypeName, id));
            }
        }

        /// <summary>
        /// Called when a state machine timer has been created.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        public virtual void OnCreateTimer(TimerInfo info)
        {
            this.Next?.OnCreateTimer(info);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnCreateTimerLogMessage(info));
            }
        }

        /// <summary>
        /// Called when a state machine timer has been stopped.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        public virtual void OnStopTimer(TimerInfo info)
        {
            this.Next?.OnStopTimer(info);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnStopTimerLogMessage(info));
            }
        }

        /// <summary>
        /// Called when a state machine has been halted.
        /// </summary>
        /// <param name="id">The id of the state machine that has been halted.</param>
        /// <param name="inboxSize">Approximate size of the state machine inbox.</param>
        public virtual void OnHalt(ActorId id, int inboxSize)
        {
            this.Next?.OnHalt(id, inboxSize);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnHaltLogMessage(id, inboxSize));
            }
        }

        /// <summary>
        /// Called when a random result has been obtained.
        /// </summary>
        /// <param name="id">The id of the source state machine, if any; otherwise, the runtime itself was the source.</param>
        /// <param name="result">The random result (may be bool or int).</param>
        public virtual void OnRandom(ActorId id, object result)
        {
            this.Next?.OnRandom(id, result);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnRandomLogMessage(id, result));
            }
        }

        /// <summary>
        /// Called when a state machine enters or exits a state.
        /// </summary>
        /// <param name="id">The id of the state machine entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited.</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        public virtual void OnMachineState(ActorId id, string stateName, bool isEntry)
        {
            this.Next?.OnMachineState(id, stateName, isEntry);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnMachineStateLogMessage(id, stateName, isEntry));
            }
        }

        /// <summary>
        /// Called when a state machine raises an event.
        /// </summary>
        /// <param name="id">The id of the state machine raising the event.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="eventName">The name of the event being raised.</param>
        public virtual void OnMachineEvent(ActorId id, string currStateName, string eventName)
        {
            this.Next?.OnMachineEvent(id, currStateName, eventName);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnMachineEventLogMessage(id, currStateName, eventName));
            }
        }

        /// <summary>
        /// Called when a state machine handled a raised event.
        /// </summary>
        /// <param name="id">The id of the state machine handling the event.</param>
        /// <param name="currStateName">The name of the state in which the event is being handled.</param>
        /// <param name="eventName">The name of the event being handled.</param>
        public void OnHandleRaisedEvent(ActorId id, string currStateName, string eventName)
        {
            this.Next?.OnHandleRaisedEvent(id, currStateName, eventName);
        }

        /// <summary>
        /// Called when a state machine executes an action.
        /// </summary>
        /// <param name="id">The id of the state machine executing the action.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public virtual void OnMachineAction(ActorId id, string currStateName, string actionName)
        {
            this.Next?.OnMachineAction(id, currStateName, actionName);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnMachineActionLogMessage(id, currStateName, actionName));
            }
        }

        /// <summary>
        /// Called when a state machine throws an exception
        /// </summary>
        /// <param name="id">The id of the state machine that threw the exception.</param>
        /// <param name="currStateName">The name of the current state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        public virtual void OnMachineExceptionThrown(ActorId id, string currStateName, string actionName, Exception ex)
        {
            this.Next?.OnMachineExceptionThrown(id, currStateName, actionName, ex);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnMachineExceptionThrownLogMessage(id, currStateName, actionName, ex));
            }
        }

        /// <summary>
        /// Called when a state machine OnException method is used to handle a thrown exception
        /// </summary>
        /// <param name="id">The id of the state machine that threw the exception.</param>
        /// <param name="currStateName">The name of the current state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        public virtual void OnMachineExceptionHandled(ActorId id, string currStateName, string actionName, Exception ex)
        {
            this.Next?.OnMachineExceptionHandled(id, currStateName, actionName, ex);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnMachineExceptionHandledLogMessage(id, currStateName, actionName, ex));
            }
        }

        /// <summary>
        /// Called when a monitor enters or exits a state.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor entering or exiting the state</param>
        /// <param name="id">The id of the monitor entering or exiting the state</param>
        /// <param name="stateName">The name of the state being entered or exited; if <paramref name="isInHotState"/>
        /// is not null, then the temperature is appended to the statename in brackets, e.g. "stateName[hot]".</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        /// <param name="isInHotState">If true, the monitor is in a hot state; if false, the monitor is in a cold state;
        /// else no liveness state is available.</param>
        public virtual void OnMonitorState(string monitorTypeName, ActorId id, string stateName,
            bool isEntry, bool? isInHotState)
        {
            this.Next?.OnMonitorState(monitorTypeName, id, stateName, isEntry, isInHotState);

            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnMonitorStateLogMessage(monitorTypeName, id, stateName, isEntry, isInHotState));
            }
        }

        /// <summary>
        /// Called when a monitor is about to process or has raised an event.
        /// </summary>
        /// <param name="senderId">The sender of the event.</param>
        /// <param name="monitorTypeName">Name of type of the monitor that will process or has raised the event.</param>
        /// <param name="id">The id of the monitor that will process or has raised the event</param>
        /// <param name="currStateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="isProcessing">If true, the monitor is processing the event; otherwise it has raised it.</param>
        public virtual void OnMonitorEvent(ActorId senderId, string monitorTypeName, ActorId id, string currStateName,
            string eventName, bool isProcessing)
        {
            this.Next?.OnMonitorEvent(senderId, monitorTypeName, id, currStateName, eventName, isProcessing);

            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnMonitorEventLogMessage(monitorTypeName, id, currStateName, eventName, isProcessing));
            }
        }

        /// <summary>
        /// Called when a monitor executes an action.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that is executing the action.</param>
        /// <param name="id">The id of the monitor that is executing the action</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public virtual void OnMonitorAction(string monitorTypeName, ActorId id, string currStateName, string actionName)
        {
            this.Next?.OnMonitorAction(monitorTypeName, id, currStateName, actionName);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnMonitorActionLogMessage(monitorTypeName, id, currStateName, actionName));
            }
        }

        /// <summary>
        /// Called for general error reporting via pre-constructed text.
        /// </summary>
        /// <param name="text">The text of the error report.</param>
        public virtual void OnError(string text)
        {
            this.Next?.OnError(text);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnErrorLogMessage(text));
            }
        }

        /// <summary>
        /// Called for errors detected by a specific scheduling strategy.
        /// </summary>
        /// <param name="strategy">The scheduling strategy that was used.</param>
        /// <param name="strategyDescription">More information about the scheduling strategy.</param>
        public virtual void OnStrategyError(SchedulingStrategy strategy, string strategyDescription)
        {
            this.Next?.OnStrategyError(strategy, strategyDescription);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnStrategyErrorLogMessage(strategy, strategyDescription));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnEnqueue"/> log message and its parameters.
        /// </summary>
        /// <param name="id">Id of the state machine that the event is being enqueued to.</param>
        /// <param name="eventName">Name of the event.</param>
        protected virtual string FormatOnEnqueueLogMessage(ActorId id, string eventName) =>
            $"<EnqueueLog> Machine '{id}' enqueued event '{eventName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnDequeue"/> log message and its parameters.
        /// </summary>
        /// <param name="id">Id of the state machine that the event is being dequeued by.</param>
        /// <param name="currStateName">The name of the current state of the state machine, if any.</param>
        /// <param name="eventName">Name of the event.</param>
        protected virtual string FormatOnDequeueLogMessage(ActorId id, string currStateName, string eventName) =>
            $"<DequeueLog> Machine '{id}' in state '{currStateName}' dequeued event '{eventName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnDefault"/> log message and its parameters.
        /// </summary>
        /// <param name="id">Id of the state machine that the state will execute in.</param>
        /// <param name="currStateName">Name of the current state of the state machine.</param>
        protected virtual string FormatOnDefaultLogMessage(ActorId id, string currStateName) =>
            $"<DefaultLog> Machine '{id}' in state '{currStateName}' is executing the default handler.";

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnGoto"/> log message and its parameters.
        /// </summary>
        /// <param name="id">Id of the state machine.</param>
        /// <param name="currStateName">The name of the current state of the state machine, if any.</param>
        /// <param name="newStateName">The target state of goto.</param>
        protected virtual string FormatOnGotoLogMessage(ActorId id, string currStateName, string newStateName) =>
            $"<GotoLog> Machine '{id}' is transitioning from state '{currStateName}' to state '{newStateName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnPush"/> log message and its parameters.
        /// </summary>
        /// <param name="id">Id of the state machine being pushed to the state.</param>
        /// <param name="currStateName">The name of the current state of the state machine, if any.</param>
        /// <param name="newStateName">The state the state machine is pushed to.</param>
        protected virtual string FormatOnPushLogMessage(ActorId id, string currStateName, string newStateName) =>
            $"<PushLog> Machine '{id}' pushed from state '{currStateName}' to state '{newStateName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnPop"/> log message and its parameters.
        /// </summary>
        /// <param name="id">Id of the state machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the state machine, if any.</param>
        /// <param name="restoredStateName">The name of the state being re-entered, if any</param>
        protected virtual string FormatOnPopLogMessage(ActorId id, string currStateName, string restoredStateName)
        {
            currStateName = string.IsNullOrEmpty(currStateName) ? "[not recorded]" : currStateName;
            var reenteredStateName = restoredStateName ?? string.Empty;
            return $"<PopLog> Machine '{id}' popped state '{currStateName}' and reentered state '{reenteredStateName}'.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnPopUnhandledEvent"/> log message and its parameters.
        /// </summary>
        /// <param name="id">Id of the state machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the state machine, if any.</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        protected virtual string FormatOnPopUnhandledEventLogMessage(ActorId id, string currStateName, string eventName)
        {
            var reenteredStateName = string.IsNullOrEmpty(currStateName)
                ? string.Empty
                : $" and reentered state '{currStateName}";
            return $"<PopLog> Machine '{id}' popped with unhandled event '{eventName}'{reenteredStateName}.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnReceive"/> log message and its parameters.
        /// </summary>
        /// <param name="id">Id of the state machine that received the event.</param>
        /// <param name="currStateName">The name of the current state of the state machine, if any.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="wasBlocked">The state machine was waiting for one or more specific events,
        /// and <paramref name="eventName"/> was one of them</param>
        protected virtual string FormatOnReceiveLogMessage(ActorId id, string currStateName, string eventName, bool wasBlocked)
        {
            var unblocked = wasBlocked ? " and unblocked" : string.Empty;
            return $"<ReceiveLog> Machine '{id}' in state '{currStateName}' dequeued event '{eventName}'{unblocked}.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnWait(ActorId, string, Type)"/> log message and its parameters.
        /// </summary>
        /// <param name="id">Id of the state machine that is entering the wait state.</param>
        /// <param name="currStateName">The name of the current state of the state machine, if any.</param>
        /// <param name="eventType">The type of the event being waited for.</param>
        protected virtual string FormatOnWaitLogMessage(ActorId id, string currStateName, Type eventType) =>
            $"<ReceiveLog> Machine '{id}' in state '{currStateName}' is waiting to dequeue an event of type '{eventType.FullName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnWait(ActorId, string, Type[])"/> log message and its parameters.
        /// </summary>
        /// <param name="id">Id of the state machine that is entering the wait state.</param>
        /// <param name="currStateName">The name of the current state of the state machine, if any.</param>
        /// <param name="eventTypes">The types of the events being waited for, if any.</param>
        protected virtual string FormatOnWaitLogMessage(ActorId id, string currStateName, params Type[] eventTypes)
        {
            string eventNames;
            if (eventTypes.Length == 0)
            {
                eventNames = "'<missing>'";
            }
            else if (eventTypes.Length == 1)
            {
                eventNames = "'" + eventTypes[0].FullName + "'";
            }
            else if (eventTypes.Length == 2)
            {
                eventNames = "'" + eventTypes[0].FullName + "' or '" + eventTypes[1].FullName + "'";
            }
            else if (eventTypes.Length == 3)
            {
                eventNames = "'" + eventTypes[0].FullName + "', '" + eventTypes[1].FullName + "' or '" + eventTypes[2].FullName + "'";
            }
            else
            {
                string[] eventNameArray = new string[eventTypes.Length - 1];
                for (int i = 0; i < eventTypes.Length - 2; i++)
                {
                    eventNameArray[i] = eventTypes[i].FullName;
                }

                eventNames = "'" + string.Join("', '", eventNameArray) + "' or '" + eventTypes[eventTypes.Length - 1].FullName + "'";
            }

            return $"<ReceiveLog> Machine '{id}' in state '{currStateName}' is waiting to dequeue an event of type {eventNames}.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnSend"/> log message and its parameters.
        /// </summary>
        /// <param name="targetActorId">Id of the target state machine.</param>
        /// <param name="senderId">The id of the state machine that sent the event, if any.</param>
        /// <param name="senderStateName">The name of the current state of the sender state machine, if any.</param>
        /// <param name="eventName">The event being sent.</param>
        /// <param name="opGroupId">Id used to identify the send operation.</param>
        /// <param name="isTargetHalted">Is the target state machine halted.</param>
        protected virtual string FormatOnSendLogMessage(ActorId targetActorId, ActorId senderId, string senderStateName,
            string eventName, Guid opGroupId, bool isTargetHalted)
        {
            var opGroupIdMsg = opGroupId != Guid.Empty ? $" (operation group '{opGroupId}')" : string.Empty;
            var target = isTargetHalted ? $"halted machine '{targetActorId}'" : $"machine '{targetActorId}'";
            var sender = senderId != null ? $"Machine '{senderId}' in state '{senderStateName}'" : $"The runtime";
            return $"<SendLog> {sender} sent event '{eventName}' to {target}{opGroupIdMsg}.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnCreateStateMachine"/> log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the state machine that has been created.</param>
        /// <param name="creator">Id of the creator state machine, null otherwise.</param>
        protected virtual string FormatOnCreateMachineLogMessage(ActorId id, ActorId creator)
        {
            var source = creator is null ? "the runtime" : $"machine '{creator.Name}'";
            return $"<CreateLog> Machine '{id}' was created by {source}.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnCreateMonitor"/> log message and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor that has been created.</param>
        /// <param name="id">The id of the monitor that has been created.</param>
        protected virtual string FormatOnCreateMonitorLogMessage(string monitorTypeName, ActorId id) =>
            $"<CreateLog> Monitor '{monitorTypeName}' with id '{id}' was created.";

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnCreateTimer"/> log message and its parameters.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        protected virtual string FormatOnCreateTimerLogMessage(TimerInfo info)
        {
            var source = info.OwnerId is null ? "the runtime" : $"machine '{info.OwnerId.Name}'";
            if (info.Period.TotalMilliseconds >= 0)
            {
                return $"<TimerLog> Timer '{info}' (due-time:{info.DueTime.TotalMilliseconds}ms; " +
                    $"period :{info.Period.TotalMilliseconds}ms) was created by {source}.";
            }
            else
            {
                return $"<TimerLog> Timer '{info}' (due-time:{info.DueTime.TotalMilliseconds}ms) was created by {source}.";
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnStopTimer"/> log message and its parameters.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        protected virtual string FormatOnStopTimerLogMessage(TimerInfo info)
        {
            var source = info.OwnerId is null ? "the runtime" : $"machine '{info.OwnerId.Name}'";
            return $"<TimerLog> Timer '{info}' was stopped and disposed by {source}.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnHalt"/> log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the state machine that has been halted.</param>
        /// <param name="inboxSize">Approximate size of the state machine inbox.</param>
        protected virtual string FormatOnHaltLogMessage(ActorId id, int inboxSize) =>
            $"<HaltLog> Machine '{id}' halted with '{inboxSize}' events in its inbox.";

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnRandom"/> log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the source state machine, if any; otherwise, the runtime itself was the source.</param>
        /// <param name="result">The random result (may be bool or int).</param>
        protected virtual string FormatOnRandomLogMessage(ActorId id, object result)
        {
            var source = id != null ? $"Machine '{id}'" : "Runtime";
            return $"<RandomLog> {source} nondeterministically chose '{result}'.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnMachineState"/> log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the state machine entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited.</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        protected virtual string FormatOnMachineStateLogMessage(ActorId id, string stateName, bool isEntry)
        {
            var direction = isEntry ? "enters" : "exits";
            return $"<StateLog> Machine '{id}' {direction} state '{stateName}'.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnMachineEvent"/> log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the state machine raising the event.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="eventName">The name of the event being raised.</param>
        protected virtual string FormatOnMachineEventLogMessage(ActorId id, string currStateName, string eventName) =>
            $"<RaiseLog> Machine '{id}' in state '{currStateName}' raised event '{eventName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnMachineAction"/> log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the state machine executing the action.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        protected virtual string FormatOnMachineActionLogMessage(ActorId id, string currStateName, string actionName) =>
            $"<ActionLog> Machine '{id}' in state '{currStateName}' invoked action '{actionName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnMachineExceptionThrown"/> log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the state machine that threw the exception.</param>
        /// <param name="currStateName">The name of the current state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        protected virtual string FormatOnMachineExceptionThrownLogMessage(ActorId id, string currStateName, string actionName, Exception ex) =>
            $"<ExceptionLog> Machine '{id}' in state '{currStateName}' running action '{actionName}' threw an exception '{ex.GetType().Name}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnMachineExceptionHandled"/> log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the state machine that threw the exception.</param>
        /// <param name="currStateName">The name of the current state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        protected virtual string FormatOnMachineExceptionHandledLogMessage(ActorId id, string currStateName, string actionName, Exception ex) =>
            $"<ExceptionLog> Machine '{id}' in state '{currStateName}' running action '{actionName}' chose to handle the exception '{ex.GetType().Name}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnMonitorState"/> log message and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor entering or exiting the state</param>
        /// <param name="id">The id of the monitor entering or exiting the state</param>
        /// <param name="stateName">The name of the state being entered or exited; if <paramref name="isInHotState"/>
        /// is not null, then the temperature is appended to the statename in brackets, e.g. "stateName[hot]".</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        /// <param name="isInHotState">If true, the monitor is in a hot state; if false, the monitor is in a cold state;
        /// else no liveness state is available.</param>
        protected virtual string FormatOnMonitorStateLogMessage(string monitorTypeName, ActorId id, string stateName, bool isEntry, bool? isInHotState)
        {
            var liveness = isInHotState.HasValue ? (isInHotState.Value ? "'hot' " : "'cold' ") : string.Empty;
            var direction = isEntry ? "enters" : "exits";
            return $"<MonitorLog> Monitor '{monitorTypeName}' with id '{id}' {direction} {liveness}state '{stateName}'.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnMonitorEvent"/> log message and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that will process or has raised the event.</param>
        /// <param name="id">The id of the monitor that will process or has raised the event</param>
        /// <param name="currStateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="isProcessing">If true, the monitor is processing the event; otherwise it has raised it.</param>
        protected virtual string FormatOnMonitorEventLogMessage(string monitorTypeName, ActorId id, string currStateName, string eventName, bool isProcessing)
        {
            var activity = isProcessing ? "is processing" : "raised";
            return $"<MonitorLog> Monitor '{monitorTypeName}' with id '{id}' in state '{currStateName}' {activity} event '{eventName}'.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnMonitorAction"/> log message and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that is executing the action.</param>
        /// <param name="id">The id of the monitor that is executing the action</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        protected virtual string FormatOnMonitorActionLogMessage(string monitorTypeName, ActorId id, string currStateName, string actionName) =>
            $"<MonitorLog> Monitor '{monitorTypeName}' with id '{id}' in state '{currStateName}' executed action '{actionName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnError"/> log message and its parameters.
        /// </summary>
        /// <param name="text">The text of the error report.</param>
        protected virtual string FormatOnErrorLogMessage(string text) => text;

        /// <summary>
        /// Returns a string formatted for the <see cref="ActorRuntimeLogWriter.OnStrategyError"/> log message and its parameters.
        /// </summary>
        /// <param name="strategy">The scheduling strategy that was used.</param>
        /// <param name="strategyDescription">More information about the scheduling strategy.</param>
        protected virtual string FormatOnStrategyErrorLogMessage(SchedulingStrategy strategy, string strategyDescription)
        {
            var desc = string.IsNullOrEmpty(strategyDescription) ? $" Description: {strategyDescription}" : string.Empty;
            return $"<StrategyLog> Found bug using '{strategy}' strategy.{desc}";
        }

        /// <summary>
        /// Disposes the log writer.
        /// </summary>
        public virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Disposes the log writer.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
