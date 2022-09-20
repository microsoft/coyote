// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Interface that allows an external module to track what
    /// is happening in the <see cref="ICoyoteRuntime"/>.
    /// </summary>
    /// <remarks>
    /// See <see href="/coyote/concepts/actors/logging">Logging</see> for more information.
    /// </remarks>
    public interface IRuntimeLog
    {
        /// <summary>
        /// Invoked when the specified monitor has been created.
        /// </summary>
        /// <param name="monitorType">The name of the type of the monitor that has been created.</param>
        void OnCreateMonitor(string monitorType);

        /// <summary>
        /// Invoked when the specified monitor executes an action.
        /// </summary>
        /// <param name="monitorType">Name of type of the monitor that is executing the action.</param>
        /// <param name="stateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        void OnMonitorExecuteAction(string monitorType, string stateName, string actionName);

        /// <summary>
        /// Invoked when the specified monitor is about to process an event.
        /// </summary>
        /// <param name="monitorType">Name of type of the monitor that will process the event.</param>
        /// <param name="stateName">The name of the state in which the event is being raised.</param>
        /// <param name="senderName">The name of the sender, if any.</param>
        /// <param name="senderType">The type of the sender, if any.</param>
        /// <param name="senderStateName">The name of the state the sender is in.</param>
        /// <param name="e">The event being processed.</param>
        void OnMonitorProcessEvent(string monitorType, string stateName, string senderName,
            string senderType, string senderStateName, Event e);

        /// <summary>
        /// Invoked when the specified monitor raised an event.
        /// </summary>
        /// <param name="monitorType">Name of type of the monitor raising the event.</param>
        /// <param name="stateName">The name of the state in which the event is being raised.</param>
        /// <param name="e">The event being raised.</param>
        void OnMonitorRaiseEvent(string monitorType, string stateName, Event e);

        /// <summary>
        /// Invoked when the specified monitor enters or exits a state.
        /// </summary>
        /// <param name="monitorType">The name of the type of the monitor entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited; if <paramref name="isInHotState"/>
        /// is not null, then the temperature is appended to the state name in brackets, e.g. "stateName[hot]".</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        /// <param name="isInHotState">If true, the monitor is in a hot state; if false, the monitor is in a cold state;
        /// else no liveness state is available.</param>
        void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState);

        /// <summary>
        /// Invoked when the specified monitor finds an error.
        /// </summary>
        /// <param name="monitorType">The name of the type of the monitor.</param>
        /// <param name="stateName">The name of the current state.</param>
        /// <param name="isInHotState">If true, the monitor is in a hot state; if false, the monitor is in a cold state;
        /// else no liveness state is available.</param>
        void OnMonitorError(string monitorType, string stateName, bool? isInHotState);

        /// <summary>
        /// Invoked when the specified controlled nondeterministic boolean result has been obtained.
        /// </summary>
        /// <param name="result">The nondeterministic boolean result.</param>
        /// <param name="callerName">The name of the caller, if any.</param>
        /// <param name="callerType">The type of the caller, if any.</param>
        void OnRandom(bool result, string callerName, string callerType);

        /// <summary>
        /// Invoked when the specified controlled nondeterministic integer result has been obtained.
        /// </summary>
        /// <param name="result">The nondeterministic integer result.</param>
        /// <param name="callerName">The name of the caller, if any.</param>
        /// <param name="callerType">The type of the caller, if any.</param>
        void OnRandom(int result, string callerName, string callerType);

        /// <summary>
        /// Invoked when the specified assertion failure has occurred.
        /// </summary>
        /// <param name="error">The text of the error.</param>
        void OnAssertionFailure(string error);

        /// <summary>
        /// Invoked when a log is complete (and is about to be closed).
        /// </summary>
        void OnCompleted();
    }
}
