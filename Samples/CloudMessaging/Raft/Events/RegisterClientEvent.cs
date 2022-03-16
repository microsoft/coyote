// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    /// <summary>
    /// Notifies the id of the client.
    /// </summary>
    [DataContract]
    public class RegisterClientEvent : Event
    {
        /// <summary>
        /// The cloient id that is being registered.
        /// </summary>
        public ActorId ClientId;
    }
}
