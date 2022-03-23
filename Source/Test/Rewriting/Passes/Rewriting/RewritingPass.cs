// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// An abstract implementation of a pass that rewrites IL.
    /// </summary>
    internal abstract class RewritingPass : Pass
    {
        /// <summary>
        /// True if the current method body has been modified, else false.
        /// </summary>
        internal bool IsMethodBodyModified { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RewritingPass"/> class.
        /// </summary>
        protected RewritingPass(IEnumerable<AssemblyInfo> visitedAssemblies, ILogger logger)
            : base(visitedAssemblies, logger)
        {
        }

        /// <summary>
        /// Finds the matching method in the specified declaring type, if any.
        /// </summary>
        protected static MethodDefinition FindMethod(string name, TypeDefinition declaringType,
            params TypeReference[] parameterTypes)
        {
            MethodDefinition match = null;
            foreach (var method in declaringType.Methods)
            {
                if (method.Name == name && method.Parameters.Count == parameterTypes.Length)
                {
                    bool isMatch = true;
                    for (int i = 0; isMatch && i < method.Parameters.Count; i++)
                    {
                        var left = parameterTypes[i];
                        var right = method.Parameters[i].ParameterType;
                        if (left is GenericParameter leftGP && right is GenericParameter rightGP &&
                            leftGP.Type == rightGP.Type && leftGP.Position == rightGP.Position)
                        {
                            // If they are generic parameters, then the types (Type or Method) and
                            // generic positions must match.
                            continue;
                        }
                        else if (left is GenericInstanceType leftGT && right is GenericInstanceType rightGT &&
                            leftGT.ElementType.FullName == rightGT.ElementType.FullName &&
                            leftGT.GenericArguments.Count == rightGT.GenericArguments.Count)
                        {
                            // If they are generic types, then the element type and number of arguments must match.
                            continue;
                        }
                        else if (!left.IsGenericParameter && !right.IsGenericParameter &&
                            left.FullName == right.FullName)
                        {
                            // If they are non-generic parameter, then the types must match.
                            continue;
                        }

                        isMatch = false;
                    }

                    if (isMatch)
                    {
                        match = method;
                        break;
                    }
                }
            }

            return match;
        }

        /// <summary>
        /// Creates a new generic method with the specified generic argument.
        /// </summary>
        protected static MethodReference MakeGenericMethod(MethodReference method, TypeReference argument)
        {
            if (method.GenericParameters.Count != 1)
            {
                throw new ArgumentException();
            }

            var instance = new GenericInstanceMethod(method);
            instance.GenericArguments.Add(argument);
            return instance;
        }

        /// <summary>
        /// Creates a new generic type with the specified generic argument.
        /// </summary>
        protected static TypeReference MakeGenericType(TypeReference type, TypeReference argument)
        {
            if (type.GenericParameters.Count != 1)
            {
                throw new ArgumentException();
            }

            var instance = new GenericInstanceType(type);
            instance.GenericArguments.Add(argument);
            return instance;
        }

        /// <summary>
        /// Fixes the instruction offsets of the specified method.
        /// </summary>
        internal static void FixInstructionOffsets(MethodDefinition method)
        {
            // By inserting new code into the visited method, it is possible some short branch
            // instructions are now out of range, and need to be switch to long branches. This
            // fixes that and it also recomputes instruction indexes which is also needed for
            // valid write assembly operation.
            method.Body.SimplifyMacros();
            method.Body.OptimizeMacros();
        }

        /// <summary>
        /// Replaces the existing instruction with a new instruction and fixes up any
        /// branch references to the old instruction so they point to the new instruction.
        /// </summary>
        /// <param name="instruction">The instruction to be replaced.</param>
        /// <param name="newInstruction">The new instruction to be inserted.</param>
        protected void Replace(Instruction instruction, Instruction newInstruction)
        {
            this.IsMethodBodyModified = true;
            this.Processor.Replace(instruction, newInstruction);
            foreach (var i in this.Processor.Body.Instructions)
            {
                if (this.Processor.Body.HasExceptionHandlers)
                {
                    foreach (var handler in this.Processor.Body.ExceptionHandlers)
                    {
                        if (handler.TryStart == instruction)
                        {
                            handler.TryStart = newInstruction;
                        }

                        if (handler.TryEnd == instruction)
                        {
                            handler.TryEnd = newInstruction;
                        }

                        if (handler.FilterStart == instruction)
                        {
                            handler.FilterStart = newInstruction;
                        }

                        if (handler.HandlerStart == instruction)
                        {
                            handler.HandlerStart = newInstruction;
                        }

                        if (handler.HandlerEnd == instruction)
                        {
                            handler.HandlerEnd = newInstruction;
                        }
                    }
                }

                // Fix up branch instructions so they branch to the new instruction instead.
                if (i.Operand is Instruction target && target == instruction)
                {
                    i.Operand = newInstruction;
                }
                else if (i.Operand is Instruction[] targets)
                {
                    for (var j = 0; j < targets.Length; ++j)
                    {
                        if (targets[j] == instruction)
                        {
                            targets[j] = newInstruction;
                        }
                    }
                }
            }
        }
    }
}
