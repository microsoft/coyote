// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Microsoft.Coyote.Utilities
{
    /// <summary>
    /// Coyote runtime scheduling strategy.
    /// </summary>
    [DataContract]
    public enum SchedulingStrategy
    {
        /// <summary>
        /// Interactive scheduling.
        /// </summary>
        [EnumMember(Value = "Interactive")]
        Interactive = 0,

        /// <summary>
        /// Replay scheduling.
        /// </summary>
        [EnumMember(Value = "Replay")]
        Replay,

        /// <summary>
        /// Portfolio scheduling.
        /// </summary>
        [EnumMember(Value = "Portfolio")]
        Portfolio,

        /// <summary>
        /// Random scheduling.
        /// </summary>
        [EnumMember(Value = "Random")]
        Random,

        /// <summary>
        /// Probabilistic random-walk scheduling.
        /// </summary>
        [EnumMember(Value = "ProbabilisticRandom")]
        ProbabilisticRandom,

        /// <summary>
        /// Prioritized scheduling.
        /// </summary>
        [EnumMember(Value = "PCT")]
        PCT,

        /// <summary>
        /// Prioritized scheduling with Random tail.
        /// </summary>
        [EnumMember(Value = "FairPCT")]
        FairPCT,

        /// <summary>
        /// Depth-first search scheduling.
        /// </summary>
        [EnumMember(Value = "DFS")]
        DFS,

        /// <summary>
        /// Depth-first search scheduling with
        /// iterative deepening.
        /// </summary>
        [EnumMember(Value = "IDDFS")]
        IDDFS,

        /// <summary>
        /// Delay-bounding scheduling.
        /// </summary>
        [EnumMember(Value = "DelayBounding")]
        DelayBounding,

        /// <summary>
        /// Random delay-bounding scheduling.
        /// </summary>
        [EnumMember(Value = "RandomDelayBounding")]
        RandomDelayBounding
    }
}
