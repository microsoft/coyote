// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Machines;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Handles the <see cref="IMachineRuntime.OnEventDropped"/> event.
    /// </summary>
    public delegate void OnEventDroppedHandler(Event e, ActorId target);
}
