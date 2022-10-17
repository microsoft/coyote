// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Logging;
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
        /// <inheritdoc/>
        public virtual void OnCreateActor(ActorId id, string creatorName, string creatorType)
        {
            if (creatorName is null)
            {
                this.Logger.WriteLine("<CreateLog> {0} was created by thread '{1}'.", id, Thread.CurrentThread.ManagedThreadId);
            }
            else
            {
                this.Logger.WriteLine("<CreateLog> {0} was created by {1}.", id, creatorName);
            }
        }

        /// <inheritdoc/>
        public virtual void OnCreateStateMachine(ActorId id, string creatorName, string creatorType)
        {
            if (creatorName is null)
            {
                this.Logger.WriteLine("<CreateLog> {0} was created by thread '{1}'.", id, Thread.CurrentThread.ManagedThreadId);
            }
            else
            {
                this.Logger.WriteLine("<CreateLog> {0} was created by {1}.", id, creatorName);
            }
        }

        /// <inheritdoc/>
        public virtual void OnCreateTimer(TimerInfo info)
        {
            var source = info.OwnerId?.Name ?? $"thread '{Thread.CurrentThread.ManagedThreadId}'";
            if (info.Period.TotalMilliseconds >= 0)
            {
                this.Logger.WriteLine("<TimerLog> Timer '{0}' (due-time:{1}ms; period:{2}ms) was created by {3}.",
                    info, info.DueTime.TotalMilliseconds, info.Period.TotalMilliseconds, source);
            }
            else
            {
                this.Logger.WriteLine("<TimerLog> Timer '{0}' (due-time:{1}ms) was created by {2}.",
                    info, info.DueTime.TotalMilliseconds, source);
            }
        }

        /// <inheritdoc/>
        public virtual void OnDefaultEventHandler(ActorId id, string stateName)
        {
            if (stateName is null)
            {
                this.Logger.WriteLine("<ActorLog> {0} is executing the default handler.", id);
            }
            else
            {
                this.Logger.WriteLine("<ActorLog> {0} is executing the default handler in state '{1}'.", id, stateName);
            }
        }

        /// <inheritdoc/>
        public virtual void OnEventHandlerTerminated(ActorId id, string stateName, DequeueStatus dequeueStatus)
        {
            if (dequeueStatus != DequeueStatus.Unavailable)
            {
                if (stateName is null)
                {
                    this.Logger.WriteLine("<ActorLog> The event handler of {0} terminated with '{1}' dequeue status.",
                        id, dequeueStatus);
                }
                else
                {
                    this.Logger.WriteLine("<ActorLog> The event handler of {0} terminated in state '{1}' with '{2}' dequeue status.",
                        id, stateName, dequeueStatus);
                }
            }
        }

        /// <inheritdoc/>
        public virtual void OnDequeueEvent(ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            if (string.IsNullOrEmpty(stateName))
            {
                this.Logger.WriteLine("<DequeueLog> {0} dequeued event '{1}'.", id, eventName);
            }
            else
            {
                this.Logger.WriteLine("<DequeueLog> {0} dequeued event '{1}' in state '{2}'.", id, eventName, stateName);
            }
        }

        /// <inheritdoc/>
        public virtual void OnEnqueueEvent(ActorId id, Event e) =>
            this.Logger.WriteLine("<EnqueueLog> {0} enqueued event '{1}'.", id, e.GetType().FullName);

        /// <inheritdoc/>
        public virtual void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
        {
            if (stateName is null)
            {
                this.Logger.WriteLine(LogSeverity.Warning, "<ExceptionLog> {0} running action '{1}' chose to handle exception '{2}'.",
                    id, actionName, ex.GetType().Name);
            }
            else
            {
                this.Logger.WriteLine(LogSeverity.Warning, "<ExceptionLog> {0} running action '{1}' in state '{2}' chose to handle exception '{3}'.",
                    id, actionName, stateName, ex.GetType().Name);
            }
        }

        /// <inheritdoc/>
        public virtual void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
        {
            if (stateName is null)
            {
                this.Logger.WriteLine(LogSeverity.Warning, "<ExceptionLog> {0} running action '{1}' threw exception '{2}'.",
                    id, actionName, ex.GetType().Name);
            }
            else
            {
                this.Logger.WriteLine(LogSeverity.Warning, "<ExceptionLog> {0} running action '{1}' in state '{2}' threw exception '{3}'.",
                    id, actionName, stateName, ex.GetType().Name);
            }
        }

        /// <inheritdoc/>
        public virtual void OnExecuteAction(ActorId id, string handlingStateName, string currentStateName, string actionName)
        {
            if (currentStateName is null)
            {
                this.Logger.WriteLine("<ActionLog> {0} invoked action '{1}'.", id, actionName);
            }
            else if (handlingStateName != currentStateName)
            {
                this.Logger.WriteLine("<ActionLog> {0} invoked action '{1}' in state '{2}' where action was declared by state '{3}'.",
                    id, actionName, currentStateName, handlingStateName);
            }
            else
            {
                this.Logger.WriteLine("<ActionLog> {0} invoked action '{1}' in state '{2}'.",
                    id, actionName, currentStateName);
            }
        }

        /// <inheritdoc/>
        public virtual void OnGotoState(ActorId id, string currentStateName, string newStateName) =>
            this.Logger.WriteLine("<GotoLog> {0} is transitioning from state '{1}' to state '{2}'.",
                id, currentStateName, newStateName);

        /// <inheritdoc/>
        public virtual void OnHalt(ActorId id, int inboxSize)
        {
            if (inboxSize is 1)
            {
                this.Logger.WriteLine("<HaltLog> {0} halted with '{1}' event in its inbox.", id, inboxSize);
            }
            else
            {
                this.Logger.WriteLine("<HaltLog> {0} halted with '{1}' events in its inbox.", id, inboxSize);
            }
        }

        /// <inheritdoc/>
        public virtual void OnPopState(ActorId id, string currentStateName, string restoredStateName)
        {
            currentStateName = string.IsNullOrEmpty(currentStateName) ? "[not recorded]" : currentStateName;
            this.Logger.WriteLine("<PopLog> {0} popped state '{1}' and reentered state '{2}'.",
                id, currentStateName, restoredStateName ?? string.Empty);
        }

        /// <inheritdoc/>
        public virtual void OnPopStateUnhandledEvent(ActorId id, string stateName, Event e) =>
            this.Logger.WriteLine("<PopLog> {0} popped state '{1}' due to unhandled event '{2}'.",
                id, stateName, e.GetType().FullName);

        /// <inheritdoc/>
        public virtual void OnPushState(ActorId id, string currentStateName, string newStateName) =>
            this.Logger.WriteLine("<PushLog> {0} pushed from state '{1}' to state '{2}'.",
                id, currentStateName, newStateName);

        /// <inheritdoc/>
        public virtual void OnRaiseEvent(ActorId id, string stateName, Event e)
        {
            if (stateName is null)
            {
                this.Logger.WriteLine("<RaiseLog> {0} raised event '{1}'.", id, e.GetType().FullName);
            }
            else
            {
                this.Logger.WriteLine("<RaiseLog> {0} raised event '{1}' in state '{2}'.",
                    id, e.GetType().FullName, stateName);
            }
        }

        /// <inheritdoc/>
        public virtual void OnHandleRaisedEvent(ActorId id, string stateName, Event e)
        {
            if (stateName is null)
            {
                this.Logger.WriteLine("<RaiseLog> {0} is handling the raised event '{1}'.", id, e.GetType().FullName);
            }
            else
            {
                this.Logger.WriteLine("<RaiseLog> {0} is handling the raised event '{1}' in state '{2}'.",
                    id, e.GetType().FullName, stateName);
            }
        }

        /// <inheritdoc/>
        public virtual void OnReceiveEvent(ActorId id, string stateName, Event e, bool wasBlocked)
        {
            var unblocked = wasBlocked ? " and unblocked" : string.Empty;
            if (stateName is null)
            {
                this.Logger.WriteLine("<ReceiveLog> {0} dequeued event '{1}'{2}.", id,
                    e.GetType().FullName, unblocked);
            }
            else
            {
                this.Logger.WriteLine("<ReceiveLog> {0} dequeued event '{1}'{2} in state '{3}'.",
                    id, e.GetType().FullName, unblocked, stateName);
            }
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
            this.Logger.WriteLine("<SendLog> {0} sent event '{1}' to {2}{3}{4}.",
                sender, eventName, targetActorId, isHalted, eventGroupIdMsg);
        }

        /// <inheritdoc/>
        public virtual void OnStateTransition(ActorId id, string stateName, bool isEntry) =>
            this.Logger.WriteLine("<StateLog> {0} {1} state '{2}'.", id, isEntry ? "enters" : "exits", stateName);

        /// <inheritdoc/>
        public virtual void OnStopTimer(TimerInfo info)
        {
            if (info.OwnerId is null)
            {
                this.Logger.WriteLine("<TimerLog> Timer '{0}' was stopped and disposed by thread '{1}'.",
                    info, Thread.CurrentThread.ManagedThreadId);
            }
            else
            {
                this.Logger.WriteLine("<TimerLog> Timer '{0}' was stopped and disposed by {1}.", info, info.OwnerId.Name);
            }
        }

        /// <inheritdoc/>
        public virtual void OnWaitEvent(ActorId id, string stateName, Type eventType)
        {
            if (stateName is null)
            {
                this.Logger.WriteLine("<ReceiveLog> {0} is waiting to dequeue an event of type '{1}'.",
                    id, eventType.FullName);
            }
            else
            {
                this.Logger.WriteLine("<ReceiveLog> {0} is waiting to dequeue an event of type '{1}' in state '{2}'.",
                    id, eventType.FullName, stateName);
            }
        }

        /// <inheritdoc/>
        public virtual void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
        {
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
                this.Logger.WriteLine("<ReceiveLog> {0} is waiting to dequeue an event of type {1}.",
                    id, eventNames);
            }
            else
            {
                this.Logger.WriteLine("<ReceiveLog> {0} is waiting to dequeue an event of type {1} in state '{2}'.",
                    id, eventNames, stateName);
            }
        }
    }
}
