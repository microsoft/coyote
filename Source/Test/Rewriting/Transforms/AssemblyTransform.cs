// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// An abstract interface for transforming code using a visitor pattern.
    /// This is used by the <see cref="AssemblyRewriter"/> to manage multiple different
    /// transforms in a single pass over an assembly.
    /// </summary>
    internal abstract class AssemblyTransform
    {
        /// <summary>
        /// Notify visitor we are starting a new Module.
        /// </summary>
        internal abstract void VisitModule(ModuleDefinition module);

        /// <summary>
        /// Notify visitor we are visiting a new TypeDefinition.
        /// </summary>
        internal abstract void VisitType(TypeDefinition typeDef);

        /// <summary>
        /// Notify visitor we are visiting a field inside the TypeDefinition just given to VisitType.
        /// </summary>
        internal abstract void VisitField(FieldDefinition field);

        /// <summary>
        /// Notify visitor we are visiting a method inside the TypeDefinition just given to VisitType.
        /// </summary>
        internal abstract void VisitMethod(MethodDefinition method);

        /// <summary>
        /// Notify visitor we are visiting a variable declaration inside the MethodDefinition just given to VisitMethod.
        /// </summary>
        internal abstract void VisitVariable(VariableDefinition variable);

        /// <summary>
        /// Visit an IL instruction inside the MethodDefinition body, and get back a possibly transformed instruction.
        /// </summary>
        /// <param name="instruction">The instruction to visit.</param>
        /// <returns>Return the last modified instruction or the same one if it was not changed.</returns>
        internal abstract Instruction VisitInstruction(Instruction instruction);

        /// <summary>
        /// Visit an <see cref="ExceptionHandler"/> inside the MethodDefinition.  In the case of nested try/catch blocks
        /// the inner block is visited first before the outer block.
        /// </summary>
        internal abstract void VisitExceptionHandler(ExceptionHandler handler);
    }
}
