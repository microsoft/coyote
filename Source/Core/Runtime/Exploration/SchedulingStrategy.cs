// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace Microsoft.Coyote.Runtime.Exploration
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
        /// Probabilistic random-walk scheduling.
        /// </summary>
        [EnumMember(Value = "ProbabilisticRandom")]
        ProbabilisticRandom,

        /// <summary>
        /// Depth-first search scheduling.
        /// </summary>
        [EnumMember(Value = "DFS")]
        DFS
    }
}
