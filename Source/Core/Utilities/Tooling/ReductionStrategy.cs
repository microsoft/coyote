// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.Coyote.Utilities
{
    /// <summary>
    /// Type of reduction strategy.
    /// </summary>
    public enum ReductionStrategy
    {
        /// <summary>
        /// No reduction.
        /// </summary>
        None = 0,

        /// <summary>
        /// Reduction strategy that omits scheduling points.
        /// </summary>
        OmitSchedulingPoints,

        /// <summary>
        /// Reduction strategy that forces scheduling points.
        /// </summary>
        ForceSchedule
    }
}
