// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;
using Microsoft.Coyote.SmartSockets;

namespace CoyoteTester.Interfaces
{
    [DataContract]
    public class TestServerMessage : SocketMessage
    {
        [DataMember]
        public bool Stop { get; set; }

        public TestServerMessage(string id, string name)
            : base(id, name)
        {
        }
    }
}
