// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Coyote.Timers
{
    /// <summary>
    /// Interface of a timer that can send timeout events to its owner machine.
    /// </summary>
    internal interface IMachineTimer : IDisposable
    {
        /// <summary>
        /// Stores information about this timer.
        /// </summary>
        TimerInfo Info { get; }
    }
}
