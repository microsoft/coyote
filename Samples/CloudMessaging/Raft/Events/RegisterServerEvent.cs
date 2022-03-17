// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// Notifies a server that it has joined the Raft
    /// service and can start executing.
    /// </summary>
    [DataContract]
    public class RegisterServerEvent : Event
    {
        /// <summary>
        /// The server id that is being registered.
        /// </summary>
        public ActorId ServerId;
    }
}
