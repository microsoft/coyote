// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Mono.Cecil.Cil;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// An IL processing operation to perform.
    /// </summary>
    internal struct ILProcessingOperation
    {
        /// <summary>
        /// A cached no-op IL processing operation.
        /// </summary>
        internal static ILProcessingOperation None { get; } = new ILProcessingOperation(ILProcessingOperationType.None);

        /// <summary>
        /// A cached array containing a single instruction to optimize for memory in the common scenario.
        /// </summary>
        /// <remarks>
        /// This is not thread safe, but we are not running rewriting in parallel.
        /// </remarks>
        private static Instruction[] SingleInstruction { get; } = new Instruction[1];

        /// <summary>
        /// The type of IL processing to perform.
        /// </summary>
        internal readonly ILProcessingOperationType Type;

        /// <summary>
        /// The new instructions to add.
        /// </summary>
        internal readonly Instruction[] Instructions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ILProcessingOperation"/> struct.
        /// </summary>
        private ILProcessingOperation(ILProcessingOperationType type)
        {
            this.Type = type;
            this.Instructions = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ILProcessingOperation"/> struct.
        /// </summary>
        internal ILProcessingOperation(ILProcessingOperationType type, Instruction instruction)
        {
            this.Type = type;
            SingleInstruction[0] = instruction;
            this.Instructions = SingleInstruction;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ILProcessingOperation"/> struct.
        /// </summary>
        internal ILProcessingOperation(ILProcessingOperationType type, params Instruction[] instructions)
        {
            this.Type = type;
            this.Instructions = instructions;
        }
    }
}
