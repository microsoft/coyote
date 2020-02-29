// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Machines;

namespace Microsoft.Coyote.TestingServices.Tests.LogMessages
{
    internal class CustomLogWriter : RuntimeLogWriter
    {
        public override void OnEnqueue(MachineId machineId, string eventName)
        {
        }

        public override void OnSend(MachineId targetMachineId, MachineId senderId, string senderStateName, string eventName,
            Guid opGroupId, bool isTargetHalted)
        {
        }

        protected override string FormatOnCreateMachineLogMessage(MachineId machineId, MachineId creator) => $"<CreateLog>.";

        protected override string FormatOnMachineStateLogMessage(MachineId machineId, string stateName, bool isEntry) => $"<StateLog>.";
    }
}
