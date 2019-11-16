// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Tests.Common.Runtime
{
    public class CustomActorRuntimeLogFormatter : ActorRuntimeLogFormatter
    {
        public override bool GetCreateActorLog(ActorId id, ActorId creator, out string text)
        {
            text = "<CreateLog>.";
            return true;
        }

        public override bool GetEnqueueEventLog(ActorId id, string eventName, out string text)
        {
            text = string.Empty;
            return false;
        }

        public override bool GetSendEventLog(ActorId targetActorId, ActorId senderId, string senderStateName,
            string eventName, Guid opGroupId, bool isTargetHalted, out string text)
        {
            text = string.Empty;
            return false;
        }

        public override bool GetStateTransitionLog(ActorId id, string stateName, bool isEntry, out string text)
        {
            text = "<StateLog>.";
            return true;
        }

        public override bool GetMonitorRaiseEventLog(string monitorTypeName, ActorId id, string stateName, string eventName, out string text)
        {
            text = string.Empty;
            return false;
        }
    }
}
