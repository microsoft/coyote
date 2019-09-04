// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;
using Microsoft.Coyote.SmartSockets;
using Microsoft.Coyote.TestingServices;

namespace CoyoteTester.Interfaces
{
    [DataContract]
    public class TestReportMessage : SocketMessage
    {
        [DataMember]
        public uint ProcessId { get; set; }

        [DataMember]
        public TestReport TestReport { get; set; }

        public TestReportMessage(string id, string name, uint processId, TestReport testReport)
            : base(id, name)
        {
            this.ProcessId = processId;
            this.TestReport = testReport;
        }
    }
}
