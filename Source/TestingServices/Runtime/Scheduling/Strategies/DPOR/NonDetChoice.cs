// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.Coyote.TestingServices.Scheduling.Strategies.DPOR
{
    /// <summary>
    /// Stores the outcome of a nondetereminstic (nondet) choice.
    /// </summary>
    internal struct NonDetChoice
    {
        /// <summary>
        /// Is this nondet choice a boolean choice?
        /// If so, <see cref="Choice"/> is 0 or 1.
        /// Otherwise, it can be any int value.
        /// </summary>
        public bool IsBoolChoice;

        /// <summary>
        /// The nondet choice; 0 or 1 if this is a bool choice;
        /// otherwise, any int.
        /// </summary>
        public int Choice;
    }
}
