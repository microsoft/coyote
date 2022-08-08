// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// This class implements <see cref="IActorRuntimeLog"/> and generates output in a a human readable text format.
    /// </summary>
    /// <remarks>
    /// See <see href="/coyote/concepts/actors/logging">Logging</see> for more information.
    /// </remarks>
    public class ActorRuntimeLogTextFormatter : RuntimeLogTextFormatter, IActorRuntimeLog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActorRuntimeLogTextFormatter"/> class.
        /// </summary>
        public ActorRuntimeLogTextFormatter()
            : base()
        {
        }

        /// <inheritdoc/>
        public virtual void OnCreateActor(ActorId id, string creatorName, string creatorType)
        {
            var source = creatorName ?? $"thread '{Thread.CurrentThread.ManagedThreadId}'";
            var text = $"<CreateLog> {id} was created by {source}.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public void OnCreateStateMachine(ActorId id, string creatorName, string creatorType)
        {
            var source = creatorName ?? $"thread '{Thread.CurrentThread.ManagedThreadId}'";
            var text = $"<CreateLog> {id} was created by {source}.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnCreateTimer(TimerInfo info)
        {
            string text;
            var source = info.OwnerId?.Name ?? $"thread '{Thread.CurrentThread.ManagedThreadId}'";
            if (info.Period.TotalMilliseconds >= 0)
            {
                text = $"<TimerLog> Timer '{info}' (due-time:{info.DueTime.TotalMilliseconds}ms; " +
                    $"period :{info.Period.TotalMilliseconds}ms) was created by {source}.";
            }
            else
            {
                text = $"<TimerLog> Timer '{info}' (due-time:{info.DueTime.TotalMilliseconds}ms) was created by {source}.";
            }

            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnDefaultEventHandler(ActorId id, string stateName)
        {
            string text;
            if (stateName is null)
            {
                text = $"<ActorLog> {id} is executing the default handler.";
            }
            else
            {
                text = $"<ActorLog> {id} is executing the default handler in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public void OnEventHandlerTerminated(ActorId id, string stateName, DequeueStatus dequeueStatus)
        {
            if (dequeueStatus != DequeueStatus.Unavailable)
            {
                string text;
                if (stateName is null)
                {
                    text = $"<ActorLog> The event handler of {id} terminated with '{dequeueStatus}' dequeue status.";
                }
                else
                {
                    text = $"<ActorLog> The event handler of {id} terminated in state '{stateName}' with '{dequeueStatus}' dequeue status.";
                }

                this.Logger.WriteLine(text);
            }
        }

        /// <inheritdoc/>
        public virtual void OnDequeueEvent(ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            string text;
            if (string.IsNullOrEmpty(stateName))
            {
                text = $"<DequeueLog> {id} dequeued event '{eventName}'.";
            }
            else
            {
                text = $"<DequeueLog> {id} dequeued event '{eventName}' in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnEnqueueEvent(ActorId id, Event e)
        {
            string eventName = e.GetType().FullName;
            string text = $"<EnqueueLog> {id} enqueued event '{eventName}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
        {
            string text;
            if (stateName is null)
            {
                text = $"<ExceptionLog> {id} running action '{actionName}' chose to handle exception '{ex.GetType().Name}'.";
            }
            else
            {
                text = $"<ExceptionLog> {id} running action '{actionName}' in state '{stateName}' chose to handle exception '{ex.GetType().Name}'.";
            }

            this.Logger.WriteLine(LogSeverity.Warning, text);
        }

        /// <inheritdoc/>
        public virtual void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
        {
            string text;
            if (stateName is null)
            {
                text = $"<ExceptionLog> {id} running action '{actionName}' threw exception '{ex.GetType().Name}'.";
            }
            else
            {
                text = $"<ExceptionLog> {id} running action '{actionName}' in state '{stateName}' threw exception '{ex.GetType().Name}'.";
            }

            this.Logger.WriteLine(LogSeverity.Warning, text);
        }

        /// <inheritdoc/>
        public virtual void OnExecuteAction(ActorId id, string handlingStateName, string currentStateName, string actionName)
        {
            string text;
            if (currentStateName is null)
            {
                text = $"<ActionLog> {id} invoked action '{actionName}'.";
            }
            else if (handlingStateName != currentStateName)
            {
                text = $"<ActionLog> {id} invoked action '{actionName}' in state '{currentStateName}' where action was declared by state '{handlingStateName}'.";
            }
            else
            {
                text = $"<ActionLog> {id} invoked action '{actionName}' in state '{currentStateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnGotoState(ActorId id, string currentStateName, string newStateName)
        {
            string text = $"<GotoLog> {id} is transitioning from state '{currentStateName}' to state '{newStateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnHalt(ActorId id, int inboxSize)
        {
            string text = $"<HaltLog> {id} halted with {inboxSize} events in its inbox.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnPopState(ActorId id, string currentStateName, string restoredStateName)
        {
            currentStateName = string.IsNullOrEmpty(currentStateName) ? "[not recorded]" : currentStateName;
            var reenteredStateName = restoredStateName ?? string.Empty;
            var text = $"<PopLog> {id} popped state '{currentStateName}' and reentered state '{reenteredStateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnPopStateUnhandledEvent(ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            var text = $"<PopLog> {id} popped state {stateName} due to unhandled event '{eventName}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnPushState(ActorId id, string currentStateName, string newStateName)
        {
            string text = $"<PushLog> {id} pushed from state '{currentStateName}' to state '{newStateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnRaiseEvent(ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            string text;
            if (stateName is null)
            {
                text = $"<RaiseLog> {id} raised event '{eventName}'.";
            }
            else
            {
                text = $"<RaiseLog> {id} raised event '{eventName}' in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnHandleRaisedEvent(ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            string text;
            if (stateName is null)
            {
                text = $"<RaiseLog> {id} is handling the raised event '{eventName}'.";
            }
            else
            {
                text = $"<RaiseLog> {id} is handling the raised event '{eventName}' in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnReceiveEvent(ActorId id, string stateName, Event e, bool wasBlocked)
        {
            string eventName = e.GetType().FullName;
            string text;
            var unblocked = wasBlocked ? " and unblocked" : string.Empty;
            if (stateName is null)
            {
                text = $"<ReceiveLog> {id} dequeued event '{eventName}'{unblocked}.";
            }
            else
            {
                text = $"<ReceiveLog> {id} dequeued event '{eventName}'{unblocked} in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnSendEvent(ActorId targetActorId, string senderName, string senderType, string senderStateName,
            Event e, Guid eventGroupId, bool isTargetHalted)
        {
            var eventGroupIdMsg = eventGroupId != Guid.Empty ? $" (event group '{eventGroupId}')" : string.Empty;
            var isHalted = isTargetHalted ? $" which has halted" : string.Empty;
            var sender = senderName != null ?
                senderStateName != null ? $"{senderName} in state '{senderStateName}'" : $"{senderName}" :
                $"Thread '{Thread.CurrentThread.ManagedThreadId}'";
            var eventName = e.GetType().FullName;
            var text = $"<SendLog> {sender} sent event '{eventName}' to {targetActorId}{isHalted}{eventGroupIdMsg}.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnStateTransition(ActorId id, string stateName, bool isEntry)
        {
            var direction = isEntry ? "enters" : "exits";
            var text = $"<StateLog> {id} {direction} state '{stateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnStopTimer(TimerInfo info)
        {
            var source = info.OwnerId?.Name ?? $"thread '{Thread.CurrentThread.ManagedThreadId}'";
            var text = $"<TimerLog> Timer '{info}' was stopped and disposed by {source}.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnWaitEvent(ActorId id, string stateName, Type eventType)
        {
            string text;
            if (stateName is null)
            {
                text = $"<ReceiveLog> {id} is waiting to dequeue an event of type '{eventType.FullName}'.";
            }
            else
            {
                text = $"<ReceiveLog> {id} is waiting to dequeue an event of type '{eventType.FullName}' in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
        {
            string text;
            string eventNames;
            if (eventTypes.Length is 0)
            {
                eventNames = "'<missing>'";
            }
            else if (eventTypes.Length is 1)
            {
                eventNames = "'" + eventTypes[0].FullName + "'";
            }
            else if (eventTypes.Length is 2)
            {
                eventNames = "'" + eventTypes[0].FullName + "' or '" + eventTypes[1].FullName + "'";
            }
            else if (eventTypes.Length is 3)
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
                text = $"<ReceiveLog> {id} is waiting to dequeue an event of type {eventNames}.";
            }
            else
            {
                text = $"<ReceiveLog> {id} is waiting to dequeue an event of type {eventNames} in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }
    }
}
