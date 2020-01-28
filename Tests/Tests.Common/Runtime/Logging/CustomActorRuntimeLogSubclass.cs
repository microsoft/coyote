// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime.Logging;

namespace Microsoft.Coyote.TestingServices.Tests.Runtime
{
    public class CustomActorRuntimeLogSubclass : ActorRuntimeLogTextFormatter
    {
        public override void OnCreateActor(ActorId id, ActorId creator)
        {
            this.Logger.WriteLine("<CreateLog>.");
        }

        public override void OnEnqueueEvent(ActorId id, string eventName)
        {
        }

        public override void OnSendEvent(ActorId targetActorId, ActorId senderId, string senderStateName,
            string eventName, Guid opGroupId, bool isTargetHalted)
        {
        }

        public override void OnStateTransition(ActorId id, string stateName, bool isEntry)
        {
            this.Logger.WriteLine("<StateLog>.");
        }

        public override void OnMonitorRaiseEvent(string monitorTypeName, ActorId id, string stateName, string eventName)
        {
        }
    }
}
