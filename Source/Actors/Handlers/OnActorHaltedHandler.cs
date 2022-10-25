// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Handles the <see cref="IActorRuntime.OnActorHalted"/> event.
    /// </summary>
    public delegate void OnActorHaltedHandler(ActorId id);
}
