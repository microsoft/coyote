// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.IO;

namespace Microsoft.Coyote.TestingServices.Tests.LogMessages
{
    internal class CustomActorRuntimeLogWriter : ActorRuntimeLogWriter
    {
        public override void OnEnqueue(ActorId actorId, string eventName)
        {
        }

        public override void OnSend(ActorId targetActorId, ActorId senderId, string senderStateName, string eventName,
            Guid opGroupId, bool isTargetHalted)
        {
        }

        protected override string FormatOnCreateMachineLogMessage(ActorId actorId, ActorId creator) => $"<CreateLog>.";

        protected override string FormatOnMachineStateLogMessage(ActorId actorId, string stateName, bool isEntry) => $"<StateLog>.";
    }
}
