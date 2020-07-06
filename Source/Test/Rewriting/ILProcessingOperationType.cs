// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// The type of IL processing to perform.
    /// </summary>
    internal enum ILProcessingOperationType
    {
        /// <summary>
        /// Do not change the current instruction.
        /// </summary>
        None,

        /// <summary>
        /// Insert the specified instructions after the current instruction.
        /// </summary>
        InsertAfter,

        /// <summary>
        /// Insert the specified instructions before the current instruction.
        /// </summary>
        InsertBefore,

        /// <summary>
        /// Replace the current instruction with the specified instructions.
        /// </summary>
        Replace
    }
}
