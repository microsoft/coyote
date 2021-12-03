// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Rewriting.Types;
using Mono.Cecil;
using Mono.Cecil.Cil;
using RuntimeCompiler = Microsoft.Coyote.Runtime.CompilerServices;
using SystemCompiler = System.Runtime.CompilerServices;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Rewriting
{
    internal sealed class MethodBodyTypeRewritingPass : TypeRewritingPass
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MethodBodyTypeRewritingPass"/> class.
        /// </summary>
        internal MethodBodyTypeRewritingPass(RewritingOptions options, IEnumerable<AssemblyInfo> visitedAssemblies,
            ILogger logger)
            : base(options, visitedAssemblies, logger)
        {
        }

        /// <inheritdoc/>
        protected override void VisitVariable(VariableDefinition variable)
        {
            if (this.Method is null)
            {
                return;
            }

            if (this.TryRewriteType(variable.VariableType, out TypeReference newVariableType) &&
                this.TryResolve(newVariableType, out TypeDefinition newVariableDefinition) &&
                !IsStaticType(newVariableDefinition))
            {
                Debug.WriteLine($"............. [-] variable '{variable.VariableType}'");
                variable.VariableType = newVariableType;
                Debug.WriteLine($"............. [+] variable '{variable.VariableType}'");
            }
        }

        /// <inheritdoc/>
        protected override Instruction VisitInstruction(Instruction instruction)
        {
            if (this.Method is null)
            {
                return instruction;
            }

            // Note that the C# compiler is not generating `OpCodes.Calli` instructions:
            // https://docs.microsoft.com/en-us/archive/blogs/shawnfa/calli-is-not-verifiable.
            // TODO: what about ldsfld, for static fields?
            if (instruction.OpCode == OpCodes.Stfld || instruction.OpCode == OpCodes.Ldfld || instruction.OpCode == OpCodes.Ldflda)
            {
                if (instruction.Operand is FieldDefinition fd &&
                    this.TryRewriteType(fd.FieldType, out TypeReference newFieldType) &&
                    this.TryResolve(newFieldType, out TypeDefinition _))
                {
                    Debug.WriteLine($"............. [-] {instruction}");
                    fd.FieldType = newFieldType;
                    this.IsMethodBodyModified = true;
                    Debug.WriteLine($"............. [+] {instruction}");
                }
                else if (instruction.Operand is FieldReference fr &&
                    this.TryRewriteType(fr.FieldType, out newFieldType) &&
                    this.TryResolve(newFieldType, out TypeDefinition _))
                {
                    Debug.WriteLine($"............. [-] {instruction}");
                    fr.FieldType = newFieldType;
                    this.IsMethodBodyModified = true;
                    Debug.WriteLine($"............. [+] {instruction}");
                }
            }
            else if (instruction.OpCode == OpCodes.Initobj)
            {
                instruction = this.VisitInitobjInstruction(instruction);
            }
            else if (instruction.OpCode == OpCodes.Newobj)
            {
                instruction = this.VisitNewobjInstruction(instruction);
            }
            else if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                instruction.Operand is MethodReference methodReference)
            {
                instruction = this.VisitCallInstruction(instruction, methodReference);
            }

            return instruction;
        }

        /// <summary>
        /// Rewrites the specified <see cref="OpCodes.Initobj"/> instruction.
        /// </summary>
        /// <returns>The unmodified instruction, or the newly replaced instruction.</returns>
        private Instruction VisitInitobjInstruction(Instruction instruction)
        {
            TypeReference type = instruction.Operand as TypeReference;
            if (this.TryRewriteType(type, out TypeReference newType) &&
                this.TryResolve(newType, out TypeDefinition _))
            {
                var newInstruction = Instruction.Create(instruction.OpCode, newType);
                newInstruction.Offset = instruction.Offset;

                Debug.WriteLine($"............. [-] {instruction}");
                this.Replace(instruction, newInstruction);
                Debug.WriteLine($"............. [+] {newInstruction}");

                instruction = newInstruction;
            }

            return instruction;
        }

        /// <summary>
        /// Rewrites the specified <see cref="OpCodes.Newobj"/> instruction.
        /// </summary>
        /// <returns>The unmodified instruction, or the newly replaced instruction.</returns>
        private Instruction VisitNewobjInstruction(Instruction instruction)
        {
            MethodReference constructor = instruction.Operand as MethodReference;
            MethodReference newMethod = this.RewriteMethodReference(constructor, this.Module, "Create");
            if (constructor.FullName != newMethod.FullName &&
                this.TryResolve(constructor, out MethodDefinition _))
            {
                // Create and return the new instruction.
                Instruction newInstruction = Instruction.Create(OpCodes.Call, newMethod);
                newInstruction.Offset = instruction.Offset;

                Debug.WriteLine($"............. [-] {instruction}");
                this.Replace(instruction, newInstruction);
                Debug.WriteLine($"............. [+] {newInstruction}");

                instruction = newInstruction;
            }

            return instruction;
        }

        /// <summary>
        /// Rewrites the specified non-generic <see cref="OpCodes.Call"/> or <see cref="OpCodes.Callvirt"/> instruction.
        /// </summary>
        /// <returns>The unmodified instruction, or the newly replaced instruction.</returns>
        private Instruction VisitCallInstruction(Instruction instruction, MethodReference method)
        {
            MethodReference newMethod = this.RewriteMethodReference(method, this.Module);
            if (method.FullName != newMethod.FullName &&
                this.TryResolve(newMethod, out MethodDefinition resolvedMethod))
            {
                // Create and return the new instruction.
                Instruction newInstruction = Instruction.Create(resolvedMethod.IsVirtual ?
                    OpCodes.Callvirt : OpCodes.Call, newMethod);
                newInstruction.Offset = instruction.Offset;

                Debug.WriteLine($"............. [-] {instruction}");
                this.Replace(instruction, newInstruction);
                Debug.WriteLine($"............. [+] {newInstruction}");

                instruction = newInstruction;
            }

            return instruction;
        }

        /// <summary>
        /// Rewrites the specified <see cref="MethodReference"/>.
        /// </summary>
        private MethodReference RewriteMethodReference(MethodReference method, ModuleDefinition module, string matchName = null)
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
            TypeReference newDeclaringType = this.RewriteDeclaringTypeReference(method);
            if (!this.TryResolve(newDeclaringType, out TypeDefinition resolvedNewDeclaringType))
            {
                // Unable to resolve the declaring type of the method.
                return result;
            }

            // if (resolvedNewDeclaringType.FullName == resolvedDeclaringType.FullName &&
            //     IsSystemType(resolvedDeclaringType))
            // {
            //     // Cannot rewrite the signature of a system method.
            //     return result;
            // }

            MethodDefinition match = FindMatchingMethodInDeclaringType(resolvedNewDeclaringType, resolvedMethod, matchName);
            if (match is null)
            {
                // No matching method found.
                return result;
            }

            result = module.ImportReference(match);
            // Console.WriteLine($"Method: {method.FullName}");
            // Console.WriteLine($"Match: {result.FullName}");
            // Console.WriteLine($">> Method: {method.Name} ({method.DeclaringType.FullName})");
            // Console.WriteLine($">> Match: {result.Name} ({result.DeclaringType.FullName})");

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
                ParameterDefinition instanceParameter = null;
                if (resolvedMethod.Parameters.Count != match.Parameters.Count)
                {
                    // We are converting from an instance method to a static method, so store the instance parameter.
                    instanceParameter = result.Parameters[0];
                }

                // Console.WriteLine($">> Return: {method.ReturnType} ({method.ReturnType.IsGenericParameter})");
                // Console.WriteLine($">> Return: {result.ReturnType} ({result.ReturnType.IsGenericParameter})");
                // Console.WriteLine($">> Return: {method.ReturnType} ({method.ReturnType.IsGenericInstance})");
                // Console.WriteLine($">> Return: {result.ReturnType} ({result.ReturnType.IsGenericInstance})");

                // Try to rewrite the return type only if it matches the original method return type.
                // TypeReference newReturnType = (result.ReturnType is GenericInstanceType genericReturnType &&
                //     method.ReturnType is GenericInstanceType genericMethodReturnType &&
                //     genericReturnType.ElementType.FullName == genericMethodReturnType.ElementType.FullName) ||
                //     result.ReturnType.FullName == method.ReturnType.FullName ?
                //     this.RewriteTypeReference(method.ReturnType) : result.ReturnType;

                TypeReference newReturnType = this.RewriteTypeReference(method.ReturnType);
                // TypeReference newReturnType = this.RewriteTypeReference(result.ReturnType);

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
        private ParameterDefinition RewriteParameterDefinition(ParameterDefinition parameter) =>
            new ParameterDefinition(parameter.Name, parameter.Attributes, this.RewriteTypeReference(parameter.ParameterType));

        /// <summary>
        /// Rewrites the declaring <see cref="TypeReference"/> of the specified <see cref="MethodReference"/>.
        /// </summary>
        private TypeReference RewriteDeclaringTypeReference(MethodReference method)
        {
            TypeReference type = method.DeclaringType;
            if (this.TryRewriteType(type, out TypeReference newDeclaringType) &&
                this.TryResolve(newDeclaringType, out TypeDefinition _))
            {
                type = newDeclaringType;
            }

            return type;
        }

        /// <summary>
        /// Rewrites the specified <see cref="TypeReference"/>.
        /// </summary>
        private TypeReference RewriteTypeReference(TypeReference type)
        {
            if (this.TryRewriteType(type, out TypeReference newType) &&
                this.TryResolve(newType, out TypeDefinition newTypeDefinition) &&
                !IsStaticType(newTypeDefinition))
            {
                type = newType;
            }

            return type;
        }

        /// <summary>
        /// Finds the matching method in the specified declaring type, if any.
        /// </summary>
        private static MethodDefinition FindMatchingMethodInDeclaringType(TypeDefinition declaringType,
            MethodDefinition method, string matchName = null)
        {
            foreach (var match in declaringType.Methods)
            {
                if (match.Name == matchName && CheckMethodParametersMatch(method, match))
                {
                    return match;
                }
                else if (CheckMethodSignaturesMatch(method, match))
                {
                    return match;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if the parameters of the two specified methods match.
        /// </summary>
        private static bool CheckMethodParametersMatch(MethodDefinition left, MethodDefinition right)
        {
            if (left.Parameters.Count != right.Parameters.Count)
            {
                return false;
            }

            for (int idx = 0; idx < right.Parameters.Count; idx++)
            {
                var leftParam = left.Parameters[idx];
                var rightParam = right.Parameters[idx];
                // TODO: make sure all necessary checks are in place!
                if ((leftParam.ParameterType.FullName != rightParam.ParameterType.FullName) ||
                    (leftParam.Name != rightParam.Name) ||
                    (leftParam.IsIn && !rightParam.IsIn) ||
                    (leftParam.IsOut && !rightParam.IsOut))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the signatures of the original and the replacement methods match.
        /// </summary>
        /// <remarks>
        /// This method also checks the use case where we are converting an instance method into a static method.
        /// In such a case case, we are inserting a first parameter that has the same type as the declaring type
        /// of the original method.
        /// </remarks>
        private static bool CheckMethodSignaturesMatch(MethodDefinition originalMethod, MethodDefinition newMethod)
        {
            // TODO: This method should be now generic enough that we can also use in future transformers, for similar checks.
            // This static method conversion is not specific to Tasks, and we can move it in a new helper API in the future.

            // Check if the method properties match. We check 'IsStatic' later as we need to do additional checks
            // in cases where we are replacing an instance method with a static method.
            // TODO: make sure all necessary checks are in place!
            if (originalMethod.Name != newMethod.Name ||
                originalMethod.IsConstructor != newMethod.IsConstructor ||
                originalMethod.ReturnType.IsGenericInstance != newMethod.ReturnType.IsGenericInstance ||
                originalMethod.IsPublic != newMethod.IsPublic ||
                originalMethod.IsPrivate != newMethod.IsPrivate ||
                originalMethod.IsAssembly != newMethod.IsAssembly ||
                originalMethod.IsFamilyAndAssembly != newMethod.IsFamilyAndAssembly)
            {
                return false;
            }

            // Check if we are converting the original method into a static method.
            bool isConvertedToStatic = !originalMethod.IsStatic && newMethod.IsStatic;
            int parameterCountDiff = newMethod.Parameters.Count - originalMethod.Parameters.Count;
            if (isConvertedToStatic)
            {
                // We are expecting one extra parameter in the static method in index '0', and the type
                // of this parameter must be the same as the declaring type of the original method.
                if (parameterCountDiff != 1 || newMethod.Parameters[0].ParameterType == originalMethod.DeclaringType)
                {
                    return false;
                }
            }
            else if (originalMethod.IsStatic != newMethod.IsStatic || parameterCountDiff != 0)
            {
                // The static properties or the parameter counts do not match.
                return false;
            }

            // Check if the parameters match.
            for (int idx = 0; idx < originalMethod.Parameters.Count; idx++)
            {
                // If we are converting to static, we have one extra parameter, so skip it.
                var newParameter = newMethod.Parameters[isConvertedToStatic ? idx + 1 : idx];
                var originalParameter = originalMethod.Parameters[idx];

                // TODO: make sure all necessary checks are in place!
                if ((newParameter.ParameterType.FullName != originalParameter.ParameterType.FullName) ||
                    (newParameter.Name != originalParameter.Name) ||
                    (newParameter.IsIn && !originalParameter.IsIn) ||
                    (newParameter.IsOut && !originalParameter.IsOut))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
