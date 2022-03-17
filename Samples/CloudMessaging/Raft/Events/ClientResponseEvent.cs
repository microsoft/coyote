// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// Used to issue a client response.
    /// </summary>
    [DataContract]
    public class ClientResponseEvent : Event
    {
        [DataMember]
        public readonly string Command;

        [DataMember]
        public readonly string Server;

        public ClientResponseEvent(string command, string server)
        {
            this.Command = command;
            this.Server = server;
        }
    }
}
