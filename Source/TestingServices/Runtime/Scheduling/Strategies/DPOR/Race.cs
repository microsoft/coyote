// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.Coyote.TestingServices.Scheduling.Strategies.DPOR
{
    /// <summary>
    /// Represents a race (two visible operation that are concurrent but dependent)
    /// that can be reversed to reach a different terminal state.
    /// </summary>
    internal class Race
    {
        /// <summary>
        /// The index of the first racing visible operation.
        /// </summary>
        public int A;

        /// <summary>
        /// The index of the second racing visible operation.
        /// </summary>
        public int B;

        /// <summary>
        /// Initializes a new instance of the <see cref="Race"/> class.
        /// </summary>
        /// <param name="a">The index of the first racing visible operation.</param>
        /// <param name="b">The index of the second racing visible operation.</param>
        public Race(int a, int b)
        {
            this.A = a;
            this.B = b;
        }
    }
}
