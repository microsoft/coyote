// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;
using Microsoft.Coyote.SmartSockets;

namespace CoyoteTester.Interfaces
{
    [DataContract]
    public class TestProgressMessage : SocketMessage
    {
        [DataMember]
        public uint ProcessId { get; set; }

        [DataMember]
        public double Progress { get; set; }

        public TestProgressMessage(string id, string name, uint processId, double progress)
            : base(id, name)
        {
            this.ProcessId = processId;
            this.Progress = progress;
        }
    }
}
