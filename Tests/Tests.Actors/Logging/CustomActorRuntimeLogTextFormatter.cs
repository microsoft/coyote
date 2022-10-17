// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Actors.Tests.Logging
{
    public class CustomActorRuntimeLogTextFormatter : ActorRuntimeLogTextFormatter
    {
        /// <inheritdoc/>
        public override void OnCreateActor(ActorId id, string creatorName, string creatorType) =>
            this.Logger.WriteLine("<CreateActorLog>");

        /// <inheritdoc/>
        public override void OnCreateStateMachine(ActorId id, string creatorName, string creatorType) =>
            this.Logger.WriteLine("<CreateStateMachineLog>");

        /// <inheritdoc/>
        public override void OnEnqueueEvent(ActorId id, Event e) =>
            this.Logger.WriteLine("<EnqueueLog>");

        /// <inheritdoc/>
        public override void OnDequeueEvent(ActorId id, string stateName, Event e) =>
            this.Logger.WriteLine("<DequeueLog>");

        /// <inheritdoc/>
        public override void OnSendEvent(ActorId targetActorId, string senderName, string senderType,
            string senderStateName, Event e, Guid eventGroupId, bool isTargetHalted) =>
            this.Logger.WriteLine("<SendLog>");

        /// <inheritdoc/>
        public override void OnGotoState(ActorId id, string currentStateName, string newStateName) =>
            this.Logger.WriteLine("<GotoLog>");

        /// <inheritdoc/>
        public override void OnStateTransition(ActorId id, string stateName, bool isEntry) =>
            this.Logger.WriteLine("<StateLog>");

        /// <inheritdoc/>
        public override void OnExecuteAction(ActorId id, string handlingStateName, string currentStateName, string actionName) =>
            this.Logger.WriteLine("<ActionLog>");

        /// <inheritdoc/>
        public override void OnCreateMonitor(string monitorType) =>
            this.Logger.WriteLine("<CreateMonitorLog>");

        /// <inheritdoc/>
        public override void OnMonitorExecuteAction(string monitorType, string stateName, string actionName) =>
            this.Logger.WriteLine("<MonitorActionLog>");

        /// <inheritdoc/>
        public override void OnMonitorProcessEvent(string monitorType, string stateName, string senderName,
            string senderType, string senderStateName, Event e) =>
            this.Logger.WriteLine("<MonitorProcessLog>");

        /// <inheritdoc/>
        public override void OnMonitorRaiseEvent(string monitorType, string stateName, Event e) =>
            this.Logger.WriteLine("<MonitorRaiseLog>");

        /// <inheritdoc/>
        public override void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState) =>
            this.Logger.WriteLine("<MonitorStateLog>");
    }
}
