// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Runtime.Exploration;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Default implementation of an <see cref="IActorRuntimeLog"/> formatter.
    /// </summary>
    public class ActorRuntimeLogFormatter : IActorRuntimeLogFormatter
    {
        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnCreateActor"/> log callback.
        /// </summary>
        /// <param name="id">The id of the actor that has been created.</param>
        /// <param name="creator">The id of the creator, null otherwise.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetCreateActorLog(ActorId id, ActorId creator, out string text)
        {
            var source = creator is null ? "the runtime" : $"'{creator.Name}'";
            text = $"<CreateLog> '{id}' was created by {source}.";
            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnExecuteAction"/> log callback.
        /// </summary>
        /// <param name="id">The id of the actor executing the action.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetExecuteActionLog(ActorId id, string stateName, string actionName, out string text)
        {
            if (stateName is null)
            {
                text = $"<ActionLog> '{id}' invoked action '{actionName}'.";
            }
            else
            {
                text = $"<ActionLog> '{id}' invoked action '{actionName}' in state '{stateName}'.";
            }

            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnSendEvent"/> log callback.
        /// </summary>
        /// <param name="targetActorId">The id of the target actor.</param>
        /// <param name="senderId">The id of the actor that sent the event, if any.</param>
        /// <param name="senderStateName">The state name, if the sender actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">The event being sent.</param>
        /// <param name="opGroupId">The id used to identify the send operation.</param>
        /// <param name="isTargetHalted">Is the target actor halted.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetSendEventLog(ActorId targetActorId, ActorId senderId, string senderStateName,
            string eventName, Guid opGroupId, bool isTargetHalted, out string text)
        {
            var opGroupIdMsg = opGroupId != Guid.Empty ? $" (operation group '{opGroupId}')" : string.Empty;
            var isHalted = isTargetHalted ? $" which has halted" : string.Empty;
            var sender = senderId != null ? $"'{senderId}' in state '{senderStateName}'" : $"The runtime";
            text = $"<SendLog> {sender} sent event '{eventName}' to '{targetActorId}'{isHalted}{opGroupIdMsg}.";
            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnRaiseEvent"/> log callback.
        /// </summary>
        /// <param name="id">The id of the actor raising the event.</param>
        /// <param name="stateName">The name of the current state.</param>
        /// <param name="eventName">The name of the event being raised.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetRaiseEventLog(ActorId id, string stateName, string eventName, out string text)
        {
            if (stateName is null)
            {
                text = $"<RaiseLog> '{id}' raised event '{eventName}'.";
            }
            else
            {
                text = $"<RaiseLog> '{id}' raised event '{eventName}' in state '{stateName}'.";
            }

            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnEnqueueEvent"/> log callback.
        /// </summary>
        /// <param name="id">The id of the actor that the event is being enqueued to.</param>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetEnqueueEventLog(ActorId id, string eventName, out string text)
        {
            text = $"<EnqueueLog> '{id}' enqueued event '{eventName}'.";
            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnDequeueEvent"/> log callback.
        /// </summary>
        /// <param name="id">The id of the actor that the event is being dequeued by.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetDequeueEventLog(ActorId id, string stateName, string eventName, out string text)
        {
            if (stateName is null)
            {
                text = $"<DequeueLog> '{id}' dequeued event '{eventName}'.";
            }
            else
            {
                text = $"<DequeueLog> '{id}' dequeued event '{eventName}' in state '{stateName}'.";
            }

            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnReceiveEvent"/> log callback.
        /// </summary>
        /// <param name="id">The id of the actor that received the event.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="wasBlocked">The actor was waiting for one or more specific events,
        /// and <paramref name="eventName"/> was one of them</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetReceiveEventLog(ActorId id, string stateName, string eventName, bool wasBlocked, out string text)
        {
            var unblocked = wasBlocked ? " and unblocked" : string.Empty;
            if (stateName is null)
            {
                text = $"<ReceiveLog> '{id}' dequeued event '{eventName}'{unblocked}.";
            }
            else
            {
                text = $"<ReceiveLog> '{id}' dequeued event '{eventName}'{unblocked} in state '{stateName}'.";
            }

            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnWaitEvent(ActorId, string, Type)"/> log callback.
        /// </summary>
        /// <param name="id">The id of the actor that is entering the wait state.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventType">The type of the event being waited for.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetWaitEventLog(ActorId id, string stateName, Type eventType, out string text)
        {
            if (stateName is null)
            {
                text = $"<ReceiveLog> '{id}' is waiting to dequeue an event of type '{eventType.FullName}'.";
            }
            else
            {
                text = $"<ReceiveLog> '{id}' is waiting to dequeue an event of type '{eventType.FullName}' in state '{stateName}'.";
            }

            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnWaitEvent(ActorId, string, Type[])"/> log callback.
        /// </summary>
        /// <param name="id">The id of the actor that is entering the wait state.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventTypes">The types of the events being waited for, if any.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetWaitEventLog(ActorId id, string stateName, Type[] eventTypes, out string text)
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

            if (stateName is null)
            {
                text = $"<ReceiveLog> '{id}' is waiting to dequeue an event of type {eventNames}.";
            }
            else
            {
                text = $"<ReceiveLog> '{id}' is waiting to dequeue an event of type {eventNames} in state '{stateName}'.";
            }

            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnRandom"/> log callback.
        /// </summary>
        /// <param name="id">The id of the source actor, if any; otherwise, the runtime itself was the source.</param>
        /// <param name="result">The random result (may be bool or int).</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetRandomLog(ActorId id, object result, out string text)
        {
            var source = id != null ? $"'{id}'" : "Runtime";
            text = $"<RandomLog> {source} nondeterministically chose '{result}'.";
            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnStateTransition"/> log callback.
        /// </summary>
        /// <param name="id">The id of the state machine entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited.</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetStateTransitionLog(ActorId id, string stateName, bool isEntry, out string text)
        {
            var direction = isEntry ? "enters" : "exits";
            text = $"<StateLog> '{id}' {direction} state '{stateName}'.";
            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnGotoState"/> log callback.
        /// </summary>
        /// <param name="id">The id of the actor.</param>
        /// <param name="currStateName">The name of the current state.</param>
        /// <param name="newStateName">The target state of the transition.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetGotoStateLog(ActorId id, string currStateName, string newStateName, out string text)
        {
            text = $"<GotoLog> '{id}' is transitioning from state '{currStateName}' to state '{newStateName}'.";
            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnPushState"/> log callback.
        /// </summary>
        /// <param name="id">The id of the actor being pushed to the state.</param>
        /// <param name="currStateName">The name of the current state.</param>
        /// <param name="newStateName">The target state of the transition.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetPushStateLog(ActorId id, string currStateName, string newStateName, out string text)
        {
            text = $"<PushLog> '{id}' pushed from state '{currStateName}' to state '{newStateName}'.";
            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnPopState"/> log callback.
        /// </summary>
        /// <param name="id">The id of the actor that the pop executed in.</param>
        /// <param name="currStateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="restoredStateName">The name of the state being re-entered, if any</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetPopStateLog(ActorId id, string currStateName, string restoredStateName, out string text)
        {
            currStateName = string.IsNullOrEmpty(currStateName) ? "[not recorded]" : currStateName;
            var reenteredStateName = restoredStateName ?? string.Empty;
            text = $"<PopLog> '{id}' popped state '{currStateName}' and reentered state '{reenteredStateName}'.";
            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnHalt"/> log callback.
        /// </summary>
        /// <param name="id">The id of the actor that has been halted.</param>
        /// <param name="inboxSize">Approximate size of the inbox.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetHaltLog(ActorId id, int inboxSize, out string text)
        {
            text = $"<HaltLog> '{id}' halted with '{inboxSize}' events in its inbox.";
            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnDefaultEventHandler"/> log callback.
        /// </summary>
        /// <param name="id">The id of the actor that the state will execute in.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetDefaultEventHandlerLog(ActorId id, string stateName, out string text)
        {
            if (stateName is null)
            {
                text = $"<DefaultLog> '{id}' is executing the default handler.";
            }
            else
            {
                text = $"<DefaultLog> '{id}' is executing the default handler in state '{stateName}'.";
            }

            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnPopUnhandledEvent"/> log callback.
        /// </summary>
        /// <param name="id">The id of the actor that the pop executed in.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetPopUnhandledEventLog(ActorId id, string stateName, string eventName, out string text)
        {
            var reenteredStateName = string.IsNullOrEmpty(stateName)
                ? string.Empty
                : $" and reentered state '{stateName}";
            text = $"<PopLog> '{id}' popped with unhandled event '{eventName}'{reenteredStateName}.";
            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnExceptionThrown"/> log callback.
        /// </summary>
        /// <param name="id">The id of the actor that threw the exception.</param>
        /// <param name="stateName">The name of the current state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetExceptionThrownLog(ActorId id, string stateName, string actionName, Exception ex, out string text)
        {
            if (stateName is null)
            {
                text = $"<ExceptionLog> '{id}' running action '{actionName}' threw exception '{ex.GetType().Name}'.";
            }
            else
            {
                text = $"<ExceptionLog> '{id}' running action '{actionName}' in state '{stateName}' threw exception '{ex.GetType().Name}'.";
            }

            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnExceptionHandled"/> log callback.
        /// </summary>
        /// <param name="id">The id of the actor that threw the exception.</param>
        /// <param name="stateName">The name of the current state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetExceptionHandledLog(ActorId id, string stateName, string actionName, Exception ex, out string text)
        {
            if (stateName is null)
            {
                text = $"<ExceptionLog> '{id}' running action '{actionName}' chose to handle exception '{ex.GetType().Name}'.";
            }
            else
            {
                text = $"<ExceptionLog> '{id}' running action '{actionName}' in state '{stateName}' chose to handle exception '{ex.GetType().Name}'.";
            }

            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnCreateTimer"/> log callback.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetCreateTimerLog(TimerInfo info, out string text)
        {
            var source = info.OwnerId is null ? "the runtime" : $"'{info.OwnerId.Name}'";
            if (info.Period.TotalMilliseconds >= 0)
            {
                text = $"<TimerLog> Timer '{info}' (due-time:{info.DueTime.TotalMilliseconds}ms; " +
                    $"period :{info.Period.TotalMilliseconds}ms) was created by {source}.";
            }
            else
            {
                text = $"<TimerLog> Timer '{info}' (due-time:{info.DueTime.TotalMilliseconds}ms) was created by {source}.";
            }

            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnStopTimer"/> log callback.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetStopTimerLog(TimerInfo info, out string text)
        {
            var source = info.OwnerId is null ? "the runtime" : $"'{info.OwnerId.Name}'";
            text = $"<TimerLog> Timer '{info}' was stopped and disposed by {source}.";
            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnCreateMonitor"/> log callback.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor that has been created.</param>
        /// <param name="id">The id of the monitor that has been created.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetCreateMonitorLog(string monitorTypeName, ActorId id, out string text)
        {
            text = $"<CreateLog> Monitor '{monitorTypeName}' with id '{id}' was created.";
            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnMonitorExecuteAction"/> log callback.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that is executing the action.</param>
        /// <param name="id">The id of the monitor that is executing the action</param>
        /// <param name="stateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetMonitorExecuteActionLog(string monitorTypeName, ActorId id,
            string stateName, string actionName, out string text)
        {
            text = $"<MonitorLog> Monitor '{monitorTypeName}' with id '{id}' executed action '{actionName}' in state '{stateName}'.";
            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnMonitorProcessEvent"/> log callback.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that will process the event.</param>
        /// <param name="id">The id of the monitor that will process the event.</param>
        /// <param name="stateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetMonitorProcessEventLog(string monitorTypeName, ActorId id,
            string stateName, string eventName, out string text)
        {
            text = $"<MonitorLog> Monitor '{monitorTypeName}' with id '{id}' is processing event '{eventName}' in state '{stateName}'.";
            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnMonitorRaiseEvent"/> log callback.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor raising the event.</param>
        /// <param name="id">The id of the monitor raising the event.</param>
        /// <param name="stateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetMonitorRaiseEventLog(string monitorTypeName, ActorId id,
            string stateName, string eventName, out string text)
        {
            text = $"<MonitorLog> Monitor '{monitorTypeName}' with id '{id}' raised event '{eventName}' in state '{stateName}'.";
            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnMonitorStateTransition"/> log callback.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor entering or exiting the state</param>
        /// <param name="id">The id of the monitor entering or exiting the state</param>
        /// <param name="stateName">The name of the state being entered or exited; if <paramref name="isInHotState"/>
        /// is not null, then the temperature is appended to the statename in brackets, e.g. "stateName[hot]".</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        /// <param name="isInHotState">If true, the monitor is in a hot state; if false, the monitor is in a cold state;
        /// else no liveness state is available.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetMonitorStateTransitionLog(string monitorTypeName, ActorId id, string stateName,
            bool isEntry, bool? isInHotState, out string text)
        {
            var liveness = isInHotState.HasValue ? (isInHotState.Value ? "'hot' " : "'cold' ") : string.Empty;
            var direction = isEntry ? "enters" : "exits";
            text = $"<MonitorLog> Monitor '{monitorTypeName}' with id '{id}' {direction} {liveness}state '{stateName}'.";
            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnAssertionFailure"/> log callback.
        /// </summary>
        /// <param name="error">The text of the error.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetErrorLog(string error, out string text)
        {
            text = error;
            return true;
        }

        /// <summary>
        /// Returns the text for the <see cref="IActorRuntimeLog.OnStrategyDescription"/> log callback.
        /// </summary>
        /// <param name="strategy">The scheduling strategy that was used.</param>
        /// <param name="strategyDescription">More information about the scheduling strategy.</param>
        /// <param name="text">The text to log.</param>
        /// <returns>True to log the text, or false to ignore it.</returns>
        public virtual bool GetStrategyErrorLog(SchedulingStrategy strategy, string strategyDescription, out string text)
        {
            var desc = string.IsNullOrEmpty(strategyDescription) ? $" Description: {strategyDescription}" : string.Empty;
            text = $"<StrategyLog> Found bug using '{strategy}' strategy.{desc}";
            return true;
        }
    }
}
