// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.IO;

namespace Microsoft.Coyote.Core.Tests.IO
{
    internal class CustomActorRuntimeLogWriter : ActorRuntimeLogWriter
    {
        public override void OnEnqueueEvent(ActorId actorId, string eventName)
        {
        }

        public override void OnSendEvent(ActorId targetActorId, ActorId senderId, string senderStateName, string eventName,
            Guid opGroupId, bool isTargetHalted)
        {
        }

        protected override string FormatOnCreateActorLogMessage(ActorId actorId, ActorId creator) => $"<CreateLog>.";

        protected override string FormatOnStateTransitionLogMessage(ActorId actorId, string stateName, bool isEntry) => $"<StateLog>.";
    }
}
