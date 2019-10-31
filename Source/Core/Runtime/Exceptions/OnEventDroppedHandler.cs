// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Handles the <see cref="IActorRuntime.OnEventDropped"/> event.
    /// </summary>
    public delegate void OnEventDroppedHandler(Event e, ActorId target);
}
