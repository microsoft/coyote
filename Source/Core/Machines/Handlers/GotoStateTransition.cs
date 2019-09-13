// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Coyote.Machines
{
    /// <summary>
    /// Defines a goto state transition.
    /// </summary>
    internal sealed class GotoStateTransition
    {
        /// <summary>
        /// The target state.
        /// </summary>
        public Type TargetState;

        /// <summary>
        /// An optional lambda function that executes after the
        /// on-exit handler of the exiting state.
        /// </summary>
        public string Lambda;

        /// <summary>
        /// Initializes a new instance of the <see cref="GotoStateTransition"/> class.
        /// </summary>
        /// <param name="targetState">The target state.</param>
        /// <param name="lambda">Lambda function that executes after the on-exit handler of the exiting state.</param>
        public GotoStateTransition(Type targetState, string lambda)
        {
            this.TargetState = targetState;
            this.Lambda = lambda;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GotoStateTransition"/> class.
        /// </summary>
        /// <param name="targetState">The target state.</param>
        public GotoStateTransition(Type targetState)
        {
            this.TargetState = targetState;
            this.Lambda = null;
        }
    }
}
