﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;
using Microsoft.Coyote.SmartSockets;

namespace CoyoteTester.Interfaces
{
    [DataContract]
    public class BugFoundMessage : SocketMessage
    {
        [DataMember]
        public uint ProcessId { get; set; }

        public BugFoundMessage(string id, string name, uint processId)
            : base(id, name)
        {
            this.ProcessId = processId;
        }
    }
}
