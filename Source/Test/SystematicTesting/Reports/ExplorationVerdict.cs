// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Summarizes the outcome of a systematic testing exploration, so that callers can decide
    /// whether the exploration was complete without parsing the human-readable report.
    /// </summary>
    public sealed class ExplorationVerdict
    {
        /// <summary>
        /// The status of the exploration.
        /// </summary>
        public ExplorationStatus Status { get; }

        /// <summary>
        /// The reasons why the exploration is considered incomplete. This list is empty when
        /// the status is <see cref="ExplorationStatus.Complete"/>. When the status is
        /// <see cref="ExplorationStatus.BugFound"/>, it lists limitations, such as uncontrolled
        /// invocations, that can affect reproducing the bug.
        /// </summary>
        public IReadOnlyList<IncompleteExplorationReason> Reasons { get; }

        /// <summary>
        /// Warnings about the exploration that do not affect its status.
        /// </summary>
        public IReadOnlyList<ExplorationWarning> Warnings { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExplorationVerdict"/> class.
        /// </summary>
        internal ExplorationVerdict(ExplorationStatus status, IEnumerable<IncompleteExplorationReason> reasons,
            IEnumerable<ExplorationWarning> warnings)
        {
            this.Status = status;
            this.Reasons = reasons.ToList();
            this.Warnings = warnings.ToList();
        }

        /// <summary>
        /// Returns a string that represents the verdict, including any reasons.
        /// </summary>
        public override string ToString() => this.Reasons.Count > 0 ?
            $"{this.Status} ({string.Join(", ", this.Reasons)})" :
            this.Status.ToString();
    }

    /// <summary>
    /// The status of a systematic testing exploration.
    /// </summary>
    public enum ExplorationStatus
    {
        /// <summary>
        /// The exploration found no bugs and detected no limitation to its coverage.
        /// </summary>
        Complete = 0,

        /// <summary>
        /// The exploration found no bugs, but detected limitations to its coverage that
        /// are described by <see cref="ExplorationVerdict.Reasons"/>.
        /// </summary>
        Incomplete,

        /// <summary>
        /// The exploration found at least one bug.
        /// </summary>
        BugFound,

        /// <summary>
        /// The exploration terminated with at least one internal error.
        /// </summary>
        InternalError
    }

    /// <summary>
    /// The reasons why a systematic testing exploration is considered incomplete.
    /// </summary>
    public enum IncompleteExplorationReason
    {
        /// <summary>
        /// At least one invocation was not intercepted and controlled during testing, so the
        /// explored interleavings are not exhaustive and bug traces might not be reproducible.
        /// </summary>
        UncontrolledInvocations = 0,

        /// <summary>
        /// At least one execution path was truncated because it reached the max-steps bound.
        /// </summary>
        TruncatedExecutionPaths,

        /// <summary>
        /// At least one fair execution path exceeded the unfair max-steps bound. This is only
        /// a reason when strict bound checking is enabled, otherwise it is reported as a warning.
        /// </summary>
        ExceededUnfairStepsBound,

        /// <summary>
        /// Fewer execution paths were explored than the number of requested testing iterations.
        /// </summary>
        InsufficientExecutionPaths,

        /// <summary>
        /// No scheduling decisions were taken during testing, so there were no interleavings to explore.
        /// </summary>
        NoSchedulingDecisions
    }

    /// <summary>
    /// Warnings about observations made during a systematic testing exploration that do
    /// not affect its status.
    /// </summary>
    public enum ExplorationWarning
    {
        /// <summary>
        /// At least one fair execution path exceeded the unfair max-steps bound before terminating.
        /// </summary>
        ExceededUnfairStepsBound = 0
    }
}
