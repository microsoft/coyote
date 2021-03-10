// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Testing.Systematic
{
    /// <summary>
    /// The schedule step type.
    /// </summary>
    internal enum ScheduleStepType
    {
        SchedulingChoice = 0,
        NondeterministicChoice,
        FairNondeterministicChoice
    }
}
