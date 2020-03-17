// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Handles the <see cref="IActorRuntime.OnFailure"/> event.
    /// </summary>
    public delegate void OnFailureHandler(Exception ex);
}
