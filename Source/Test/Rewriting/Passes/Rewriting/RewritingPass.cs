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
        /// Rewrites the specified <see cref="MethodReference"/>.
        /// </summary>
        /// <param name="method">The method reference to rewrite.</param>
        /// <param name="module">The module definition that is being visited.</param>
        /// <param name="matchName">Optional method name to match.</param>
        /// <returns>The rewritten method, or the original if it was not changed.</returns>
        protected MethodReference RewriteMethodReference(MethodReference method, ModuleDefinition module, string matchName = null)
        {
            MethodReference result = method;
            TypeDefinition resolvedDeclaringType = method.DeclaringType.Resolve();
            if (!this.IsRewritableType(resolvedDeclaringType))
            {
                return result;
            }

            if (!this.TryResolve(method, out MethodDefinition resolvedMethod))
            {
                // Check if this method signature has been rewritten and, if it has, find the
                // rewritten method. The signature does not include the return type according
                // to C# rules, but the return type may have also been rewritten which is why
                // it is imperative here that we find the correct new definition.
                List<TypeReference> paramTypes = new List<TypeReference>();
                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    var p = method.Parameters[i];
                    paramTypes.Add(this.RewriteTypeReference(p.ParameterType));
                }

                var newMethod = FindMatchingMethodInDeclaringType(resolvedDeclaringType, method.Name, paramTypes.ToArray());
                if (!this.TryResolve(newMethod, out resolvedMethod))
                {
                    // Unable to resolve the method or a rewritten version of this method.
                    return result;
                }
            }

            // Try to rewrite the declaring type.
            TypeReference newDeclaringType = this.RewriteMethodDeclaringTypeReference(method);
            resolvedDeclaringType = Resolve(newDeclaringType);

            MethodDefinition match = FindMatchingMethodInDeclaringType(resolvedDeclaringType, resolvedMethod, matchName);
            if (match is null)
            {
                // No matching method found.
                return result;
            }

            result = module.ImportReference(match);
            if (!result.HasThis && !newDeclaringType.IsGenericInstance &&
                method.HasThis && method.DeclaringType.IsGenericInstance)
            {
                // We are converting from a generic type to a non generic static type, and from a non-generic
                // method to a generic method, so we need to instantiate the generic method.
                GenericInstanceMethod genericMethodInstance = new GenericInstanceMethod(result);

                var genericArgs = new List<TypeReference>();
                if (method.DeclaringType is GenericInstanceType genericDeclaringType)
                {
                    // Populate the generic arguments with the generic declaring type arguments.
                    genericArgs.AddRange(genericDeclaringType.GenericArguments);
                    foreach (var genericArg in genericArgs)
                    {
                        genericMethodInstance.GenericArguments.Add(genericArg);
                    }
                }

                result = genericMethodInstance;
            }
            else
            {
                // This is an extra initial parameter that we have when converting an instance to a static method.
                // For example, `task.GetAwaiter()` is converted to `ControlledTask.GetAwaiter(task)`.
                ParameterDefinition instanceParameter = null;
                if (resolvedMethod.Parameters.Count != match.Parameters.Count)
                {
                    // We are converting from an instance method to a static method, so store the instance parameter.
                    instanceParameter = result.Parameters[0];
                }

                // Try to rewrite the return type only if it matches the original method return type.
                // TypeReference newReturnType = (result.ReturnType is GenericInstanceType genericReturnType &&
                //     method.ReturnType is GenericInstanceType genericMethodReturnType &&
                //     genericReturnType.ElementType.FullName == genericMethodReturnType.ElementType.FullName) ||
                //     result.ReturnType.FullName == method.ReturnType.FullName ?
                //     this.RewriteTypeReference(method.ReturnType) : result.ReturnType;

                // Console.WriteLine($"Match: {result.FullName}");
                TypeReference newReturnType = this.RewriteTypeReference(method.ReturnType);

                // Instantiate the method reference to set its generic arguments and parameters, if any.
                result = new MethodReference(result.Name, newReturnType, newDeclaringType)
                {
                    HasThis = result.HasThis,
                    ExplicitThis = result.ExplicitThis,
                    CallingConvention = result.CallingConvention
                };

                if (resolvedMethod.HasGenericParameters)
                {
                    // We need to instantiate the generic method.
                    GenericInstanceMethod genericMethodInstance = new GenericInstanceMethod(result);

                    var genericArgs = new List<TypeReference>();
                    int genericArgOffset = 0;

                    if (newDeclaringType is GenericInstanceType genericDeclaringType)
                    {
                        // Populate the generic arguments with the generic declaring type arguments.
                        genericArgs.AddRange(genericDeclaringType.GenericArguments);
                        genericArgOffset = genericDeclaringType.GenericArguments.Count;
                    }

                    if (method is GenericInstanceMethod genericInstanceMethod)
                    {
                        // Populate the generic arguments with the generic instance method arguments.
                        genericArgs.AddRange(genericInstanceMethod.GenericArguments);
                    }

                    for (int i = 0; i < resolvedMethod.GenericParameters.Count; i++)
                    {
                        var p = resolvedMethod.GenericParameters[i];
                        var j = p.Position + genericArgOffset;
                        if (j >= genericArgs.Count)
                        {
                            throw new InvalidOperationException($"Not enough generic arguments to instantiate method {method}");
                        }

                        GenericParameter parameter = new GenericParameter(p.Name, genericMethodInstance);
                        result.GenericParameters.Add(parameter);
                        genericMethodInstance.GenericParameters.Add(parameter);
                        genericMethodInstance.GenericArguments.Add(this.RewriteTypeReference(genericArgs[j]));
                    }

                    result = genericMethodInstance;
                }

                // Set the instance parameter of the method, if any.
                if (instanceParameter != null)
                {
                    result.Parameters.Add(instanceParameter);
                }

                // Set the remaining parameters of the method, if any.
                foreach (var parameter in method.Parameters)
                {
                    result.Parameters.Add(this.RewriteParameterDefinition(parameter));
                }
            }

            return module.ImportReference(result);
        }

        /// <summary>
        /// Rewrites the specified <see cref="ParameterDefinition"/>.
        /// </summary>
        /// <param name="parameter">The parameter definition to rewrite.</param>
        /// <returns>The rewritten parameter definition, or the original if it was not changed.</returns>
        protected virtual ParameterDefinition RewriteParameterDefinition(ParameterDefinition parameter) => parameter;

        /// <summary>
        /// Rewrites the declaring <see cref="TypeReference"/> of the specified <see cref="MethodReference"/>.
        /// </summary>
        /// <param name="method">The method with the declaring type to rewrite.</param>
        /// <returns>The rewritten declaring type, or the original if it was not changed.</returns>
        protected virtual TypeReference RewriteMethodDeclaringTypeReference(MethodReference method) => method.DeclaringType;

        /// <summary>
        /// Rewrites the specified <see cref="TypeReference"/>.
        /// </summary>
        /// <param name="type">The type reference to rewrite.</param>
        /// <returns>The rewritten type reference, or the original if it was not changed.</returns>
        protected virtual TypeReference RewriteTypeReference(TypeReference type) => type;

        /// <summary>
        /// Checks if the specified type is a rewritable type.
        /// </summary>
        protected virtual bool IsRewritableType(TypeDefinition type) => IsSystemType(type) || !this.IsForeignType(type);

        protected static TypeReference MakeGenericType(TypeReference self, params TypeReference[] arguments)
        {
            if (self.GenericParameters.Count != arguments.Length)
            {
                throw new ArgumentException();
            }

            var instance = new GenericInstanceType(self);
            foreach (var argument in arguments)
            {
                instance.GenericArguments.Add(argument);
            }

            return instance;
        }

        protected static MethodReference MakeGenericMethod(MethodReference self, params TypeReference[] arguments)
        {
            if (self.GenericParameters.Count != arguments.Length)
            {
                throw new ArgumentException();
            }

            var instance = new GenericInstanceMethod(self);
            foreach (var argument in arguments)
            {
                instance.GenericArguments.Add(argument);
            }

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

                // fix up branch instructions so they branch to the new instruction instead.
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
