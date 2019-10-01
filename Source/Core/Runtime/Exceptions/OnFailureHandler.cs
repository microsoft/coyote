// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Handles the <see cref="IMachineRuntime.OnFailure"/> event.
    /// </summary>
    public delegate void OnFailureHandler(Exception ex);
}
