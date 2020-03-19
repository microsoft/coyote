// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.SystematicTesting.Tests.Runtime
{
    public class CustomActorRuntimeLogSubclass : ActorRuntimeLogTextFormatter
    {
        public override void OnCreateActor(ActorId id, string creatorType, string creatorName)
        {
            this.Logger.WriteLine("<CreateLog>.");
        }

        public override void OnEnqueueEvent(ActorId id, Event e)
        {
        }

        public override void OnSendEvent(ActorId targetActorId, string senderType, string senderName, string senderStateName,
            Event e, Guid opGroupId, bool isTargetHalted)
        {
        }

        public override void OnStateTransition(ActorId id, string stateName, bool isEntry)
        {
            this.Logger.WriteLine("<StateLog>.");
        }

        public override void OnMonitorRaiseEvent(string monitorTypeName, string stateName, Event e)
        {
        }
    }
}
