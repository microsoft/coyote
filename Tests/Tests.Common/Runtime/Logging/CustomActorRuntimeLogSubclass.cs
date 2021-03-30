// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.BugFinding.Tests.Runtime
{
    public class CustomActorRuntimeLogSubclass : ActorRuntimeLogTextFormatter
    {
        public override void OnCreateActor(ActorId id, string creatorName, string creatorType)
        {
            this.Logger.WriteLine("<CreateLog>.");
        }

        public override void OnEnqueueEvent(ActorId id, Event e)
        {
        }

        public override void OnSendEvent(ActorId targetActorId, string senderName, string senderType,
            string senderStateName, Event e, Guid eventGroupId, bool isTargetHalted)
        {
        }

        public override void OnStateTransition(ActorId id, string stateName, bool isEntry)
        {
            this.Logger.WriteLine("<StateLog>.");
        }

        public override void OnMonitorRaiseEvent(string monitorType, string stateName, Event e)
        {
        }
    }
}
