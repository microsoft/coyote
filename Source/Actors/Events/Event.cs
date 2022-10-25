// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Abstract class representing an event that can be send to
    /// an <see cref="Actor"/> or <see cref="StateMachine"/>.
    /// </summary>
    [DataContract]
    public abstract class Event
    {
    }
}
