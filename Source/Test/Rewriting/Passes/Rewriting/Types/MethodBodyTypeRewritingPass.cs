// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Rewriting.Types;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
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
            MethodReference newMethod = this.RewriteMethodReference(constructor, "Create");
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
            MethodReference newMethod = this.RewriteMethodReference(method);
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
        private MethodReference RewriteMethodReference(MethodReference method, string matchName = null)
        {
            Console.WriteLine($"Method: {method.FullName}");
            Console.WriteLine($"  >>> Method: {method.Name} ({method.DeclaringType.FullName})");
            MethodReference result = method;
            TypeDefinition resolvedDeclaringType = method.DeclaringType.Resolve();
            if (!this.IsRewritableType(resolvedDeclaringType))
            {
                return result;
            }

            if (!this.TryResolve(method, out MethodDefinition resolvedMethod, false))
            {
                Console.WriteLine($"  >>> CANNOT RESOLVE -- is it rewritten?");

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

                MethodDefinition match = FindMethod(method.Name, resolvedDeclaringType, paramTypes.ToArray());
                if (!this.TryResolve(match, out resolvedMethod))
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

            bool isRewritingDeclaringType = resolvedDeclaringType.FullName != resolvedNewDeclaringType.FullName;

            // if (resolvedNewDeclaringType.FullName == resolvedDeclaringType.FullName &&
            //     IsSystemType(resolvedDeclaringType))
            // {
            //     // Cannot rewrite the signature of a system method.
            //     return result;
            // }

            Console.WriteLine($"  >>> isRewritingDeclaringType: {isRewritingDeclaringType}");

            if (isRewritingDeclaringType)
            {
                // The declaring type is being rewritten, so only rewrite the return and
                // parameter types if they are generic.
                MethodDefinition resolvedNewMethod = FindMatchingMethodInDeclaringType(resolvedNewDeclaringType,
                    resolvedMethod, matchName);
                if (resolvedNewMethod is null)
                {
                    // No matching method found.
                    return result;
                }

                result = this.Module.ImportReference(resolvedNewMethod);
                Console.WriteLine($"Match: {result.FullName}");
                Console.WriteLine($">> Match: {result.Name} ({result.DeclaringType.FullName})");

                Console.WriteLine($"resolvedMethod: {resolvedMethod}");
                Console.WriteLine($"resolvedNewMethod: {resolvedNewMethod}");
            }

            if (!result.HasThis && !newDeclaringType.IsGenericInstance &&
                method.HasThis && method.DeclaringType.IsGenericInstance)
            {
                // TODO: is this ever called?

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
                Console.WriteLine($">> result.ReturnType: {result.ReturnType}");
                Console.WriteLine($">> Return: {method.ReturnType} ({method.ReturnType.IsGenericParameter}) ({method.ReturnType.GenericParameters.Count})");
                Console.WriteLine($">> Return: {method.ReturnType} ({method.ReturnType.HasGenericParameters}) ({method.ReturnType.GenericParameters.Count})");
                Console.WriteLine($">> Return: {result.ReturnType} ({result.ReturnType.IsGenericParameter}) ({result.ReturnType.GenericParameters.Count})");
                Console.WriteLine($">> Return: {result.ReturnType} ({result.ReturnType.HasGenericParameters}) ({result.ReturnType.GenericParameters.Count})");
                Console.WriteLine($">> Return: {method.ReturnType} ({method.ReturnType.IsGenericInstance})");
                Console.WriteLine($">> Return: {result.ReturnType} ({result.ReturnType.IsGenericInstance})");

                // Try rewrite the return type only if the declaring type is not being rewritten,
                // else assign the generic arguments if the return type is generic.
                TypeReference newReturnType = isRewritingDeclaringType ?
                    this.TryMakeGenericType(result.ReturnType, method.ReturnType, method) :
                    this.RewriteTypeReference(method.ReturnType);

                Console.WriteLine($">> newReturnType: {newReturnType}");
                Console.WriteLine($">> method.Parameters: {method.HasParameters}");
                Console.WriteLine($">> result.Parameters: {result.HasParameters}");
                Console.WriteLine($">> method.Parameters: {method.Parameters.Count}");
                Console.WriteLine($">> result.Parameters: {result.Parameters.Count}");
                Console.WriteLine($">> newReturnType: {newReturnType}");

                // Rewrite the parameters of the method, if any.
                // Collection<ParameterDefinition> newParameters = this.RewriteMethodParameters(result,
                //     method.Parameters, isRewritingDeclaringType, method);
                Console.WriteLine($"1: {result.FullName}");

                // Instantiate the method reference to set its generic arguments and parameters, if any.
                result = new MethodReference(result.Name, newReturnType, newDeclaringType)
                {
                    HasThis = result.HasThis,
                    ExplicitThis = result.ExplicitThis,
                    CallingConvention = result.CallingConvention
                };

                if (resolvedMethod.HasGenericParameters)
                {
                    // Need to rewrite the generic method to instantiate the correct generic parameter types.
                    result = this.RewriteGenericMethodArguments(result, method.Parameters, resolvedMethod.GenericParameters,
                        (method as GenericInstanceMethod)?.GenericArguments, isRewritingDeclaringType, method);
                }
                else
                {
                    // Rewrite the parameters of the method, if any.
                    Collection<ParameterDefinition> newParameters = this.RewriteMethodParameters(result,
                        method.Parameters, isRewritingDeclaringType, method);

                    // Set the remaining parameters of the method, if any.
                    foreach (var parameter in newParameters)
                    {
                        result.Parameters.Add(parameter);
                    }
                }

                Console.WriteLine($"2: {result.FullName}");
            }

            return this.Module.ImportReference(result);
        }

        /// <summary>
        /// Rewrites the generic arguments of the specified <see cref="MethodReference"/>.
        /// </summary>
        private GenericInstanceMethod RewriteGenericMethodArguments(MethodReference method,
            Collection<ParameterDefinition> originalParameters, Collection<GenericParameter> originalGenericParameters,
            Collection<TypeReference> extraGenericArguments, bool rewriteTypes,
            IGenericParameterProvider context = null)
        {
            // Rewrite the parameters of the method, if any.
            Collection<ParameterDefinition> newParameters = this.RewriteMethodParameters(method,
                originalParameters, rewriteTypes, context);

            var genericMethodInstance = new GenericInstanceMethod(method);
            var genericArgs = new List<TypeReference>();
            int offset = 0;

            if (method.DeclaringType is GenericInstanceType genericDeclaringType)
            {
                // Populate the generic arguments with the generic declaring type arguments.
                genericArgs.AddRange(genericDeclaringType.GenericArguments);
                offset = genericDeclaringType.GenericArguments.Count;
            }

            if (extraGenericArguments != null)
            {
                // Populate the generic arguments with the extra generic arguments.
                genericArgs.AddRange(extraGenericArguments);
            }

            for (int i = 0; i < originalGenericParameters.Count; i++)
            {
                var p = originalGenericParameters[i];
                var j = p.Position + offset;
                if (j >= genericArgs.Count)
                {
                    throw new InvalidOperationException($"Not enough generic arguments to instantiate '{method}'.");
                }

                GenericParameter parameter = new GenericParameter(p.Name, genericMethodInstance);
                method.GenericParameters.Add(parameter);
                genericMethodInstance.GenericParameters.Add(parameter);
                genericMethodInstance.GenericArguments.Add(this.RewriteTypeReference(genericArgs[j]));
            }

            // Set the remaining parameters of the method, if any.
            foreach (var parameter in newParameters)
            {
                method.Parameters.Add(parameter);
            }

            return genericMethodInstance;
        }

        /// <summary>
        /// Rewrites the parameters of the specified <see cref="MethodReference"/>.
        /// </summary>
        private Collection<ParameterDefinition> RewriteMethodParameters(MethodReference method,
            Collection<ParameterDefinition> originalParameters, bool rewriteTypes,
            IGenericParameterProvider context = null)
        {
            var result = new Collection<ParameterDefinition>();
            Console.WriteLine($">> parameter-count: {originalParameters.Count}");
            Console.WriteLine($">> parameter-new-count: {method.Parameters.Count}");

            // If there is an offset, then we are rewriting a static method that includes
            // one extra parameter for passing the owner of the instance method.
            int offset = method.Parameters.Count - originalParameters.Count;
            for (int i = 0; i < offset; i++)
            {
                ParameterDefinition parameter = method.Parameters[i];
                Console.WriteLine($">> parameter-special: {parameter.ParameterType} {parameter.Name}");
                if (parameter.ParameterType is GenericInstanceType genericInstanceType)
                {
                    Console.WriteLine($">> parameter-special-generic: {genericInstanceType.ElementType} {genericInstanceType.GenericArguments.Count} {genericInstanceType.GenericParameters.Count}");
                    foreach (var genericArg in genericInstanceType.GenericArguments)
                    {
                        Console.WriteLine($"  >>> {genericArg}");
                    }
                }

                result.Add(parameter);
            }

            for (int i = 0; i < originalParameters.Count; i++)
            {
                ParameterDefinition newParameter = method.Parameters[i + offset];
                ParameterDefinition parameter = originalParameters[i];

                Console.WriteLine($">> parameter-new: {newParameter}");
                Console.WriteLine($">> parameter-old: {parameter}");

                // Try rewrite the parameter only if the declaring type is not being rewritten,
                // else assign the generic arguments if the parameter is generic.
                TypeReference newParameterType = rewriteTypes ?
                    this.RewriteTypeReference(parameter.ParameterType) :
                    this.TryMakeGenericType(newParameter.ParameterType, parameter.ParameterType, context);

                result.Add(new ParameterDefinition(newParameter.Name, newParameter.Attributes, newParameterType));
            }

            foreach (var parameter in result)
            {
                Console.WriteLine($">> newParameters: {parameter.ParameterType}");
            }

            return result;
        }

        /// <summary>
        /// Rewrites the generic arguments of the specified <see cref="TypeReference"/>.
        /// </summary>
        private TypeReference RewriteTypeReferenceGenericArguments(TypeReference type,
            Collection<TypeReference> genericArguments, IGenericParameterProvider context = null)
        {
            if (this.TryResolve(type, out TypeDefinition typeDefinition))
            {
                TypeReference importedType = this.Module.ImportReference(typeDefinition, context);
                return MakeGenericType(importedType, genericArguments);
            }
            else
            {
                return type;
            }
        }

        /// <summary>
        /// Rewrites the specified <see cref="ParameterDefinition"/>.
        /// </summary>
        /// <remarks>
        /// If a generic provider is specified, the method only rewrites the parameter type arguments.
        /// </remarks>
        private ParameterDefinition RewriteParameterDefinition(ParameterDefinition parameter,
            IGenericParameterProvider context = null) =>
            new ParameterDefinition(parameter.Name, parameter.Attributes,
                context is null ? this.RewriteTypeReference(parameter.ParameterType) :
                this.Module.ImportReference(parameter.ParameterType, context));

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
