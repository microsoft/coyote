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
            if (!this.TryResolve(method, out MethodDefinition resolvedMethod))
            {
                // Can't rewrite external method reference since we are not rewriting this external assembly.
                Console.WriteLine($"6");
                return method;
            }

            TypeReference declaringType = this.RewriteDeclaringTypeReference(method);
            var resolvedType = Resolve(declaringType);

            if (resolvedMethod is null)
            {
                // Check if this method signature has been rewritten, find the method by same name,
                // but with newly rewritten parameter types (note: signature does not include return type
                // according to C# rules, but the return type may have also been rewritten which is why
                // it is imperative here that we find the correct new MethodDefinition.
                List<TypeReference> parameterTypes = new List<TypeReference>();
                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    var p = method.Parameters[i];
                    parameterTypes.Add(this.RewriteTypeReference(p.ParameterType));
                }

                var newMethod = FindMatchingMethodInDeclaringType(resolvedType, method.Name, parameterTypes.ToArray());
                Console.WriteLine($"FindMatchingMethodInDeclaringType: {newMethod}");
                if (newMethod != null)
                {
                    if (!this.TryResolve(newMethod, out resolvedMethod))
                    {
                        Console.WriteLine($"5");
                        return newMethod;
                    }
                }
            }

            if (method.DeclaringType == declaringType && result.Resolve() == resolvedMethod)
            {
                // We are not rewriting this method.
                Console.WriteLine($"4");
                return result;
            }

            if (resolvedMethod is null)
            {
                // TODO: do we need to return the resolved method here?
                this.TryResolve(method, out resolvedMethod);
                Console.WriteLine($"3");
                return method;
            }

            MethodDefinition match = FindMatchingMethodInDeclaringType(resolvedType, resolvedMethod, matchName);
            if (match != null)
            {
                result = module.ImportReference(match);
            }

            if (match != null && !result.HasThis && !declaringType.IsGenericInstance &&
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
                if (match != null && resolvedMethod.Parameters.Count != match.Parameters.Count)
                {
                    // We are converting from an instance method to a static method, so store the instance parameter.
                    instanceParameter = result.Parameters[0];
                }

                TypeReference returnType = this.RewriteTypeReference(method.ReturnType);

                // Instantiate the method reference to set its generic arguments and parameters, if any.
                result = new MethodReference(result.Name, returnType, declaringType)
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

                    if (declaringType is GenericInstanceType genericDeclaringType)
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
                        if (j > genericArgs.Count)
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

            result = module.ImportReference(result);
            if (!this.TryResolve(result, out _))
            {
                Console.WriteLine($"1");
                return method;
            }

            Console.WriteLine($"2: {result}");
            return result;
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
        protected virtual TypeReference RewriteDeclaringTypeReference(MethodReference method) => method.DeclaringType;

        /// <summary>
        /// Rewrites the specified <see cref="TypeReference"/>.
        /// </summary>
        /// <param name="type">The type reference to rewrite.</param>
        /// <returns>The rewritten type reference, or the original if it was not changed.</returns>
        protected virtual TypeReference RewriteTypeReference(TypeReference type) => type;

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
