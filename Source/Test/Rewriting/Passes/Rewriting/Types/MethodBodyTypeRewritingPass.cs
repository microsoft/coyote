// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Microsoft.Coyote.Rewriting
{
    internal sealed class MethodBodyTypeRewritingPass : TypeRewritingPass
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MethodBodyTypeRewritingPass"/> class.
        /// </summary>
        internal MethodBodyTypeRewritingPass(RewritingOptions options, IEnumerable<AssemblyInfo> visitedAssemblies, LogWriter logWriter)
            : base(options, visitedAssemblies, logWriter)
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
                this.TryResolve(newVariableType, out TypeDefinition _))
            {
                this.LogWriter.LogDebug("............. [-] variable '{0}'", variable.VariableType);
                variable.VariableType = newVariableType;
                this.LogWriter.LogDebug("............. [+] variable '{0}'", variable.VariableType);
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
                    this.LogWriter.LogDebug("............. [-] {0}", instruction);
                    fd.FieldType = newFieldType;
                    this.IsMethodBodyModified = true;
                    this.LogWriter.LogDebug("............. [+] {0}", instruction);
                }
                else if (instruction.Operand is FieldReference fr &&
                    this.TryRewriteType(fr.FieldType, out newFieldType) &&
                    this.TryResolve(newFieldType, out TypeDefinition _))
                {
                    this.LogWriter.LogDebug("............. [-] {0}", instruction);
                    fr.FieldType = newFieldType;
                    this.IsMethodBodyModified = true;
                    this.LogWriter.LogDebug("............. [+] {0}", instruction);
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

                this.LogWriter.LogDebug("............. [-] {0}", instruction);
                this.Replace(instruction, newInstruction);
                this.LogWriter.LogDebug("............. [+] {0}", newInstruction);

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
            if (this.TryRewriteMethodReference(constructor, "Create", out MethodReference newMethod) &&
                this.TryResolve(newMethod, out MethodDefinition _))
            {
                // Create and return the new instruction.
                Instruction newInstruction = Instruction.Create(OpCodes.Call, newMethod);
                newInstruction.Offset = instruction.Offset;

                this.LogWriter.LogDebug("............. [-] {0}", instruction);
                this.Replace(instruction, newInstruction);
                this.LogWriter.LogDebug("............. [+] {0}", newInstruction);

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
            if (this.TryRewriteMethodReference(method, out MethodReference newMethod) &&
                this.TryResolve(newMethod, out MethodDefinition resolvedMethod))
            {
                // Create and return the new instruction.
                Instruction newInstruction = Instruction.Create(resolvedMethod.IsVirtual ?
                    OpCodes.Callvirt : OpCodes.Call, newMethod);

                newInstruction.Offset = instruction.Offset;
                this.LogWriter.LogDebug("............. [-] {0}", instruction);
                this.Replace(instruction, newInstruction);
                this.LogWriter.LogDebug("............. [+] {0}", newInstruction);

                instruction = newInstruction;
            }

            return instruction;
        }

        /// <summary>
        /// Tries to rewrite the specified <see cref="MethodReference"/>.
        /// </summary>
        private bool TryRewriteMethodReference(MethodReference method, out MethodReference result) =>
            this.TryRewriteMethodReference(method, null, out result);

        /// <summary>
        /// Tries to rewrite the specified <see cref="MethodReference"/>.
        /// </summary>
        private bool TryRewriteMethodReference(MethodReference method, string matchName, out MethodReference result)
        {
            result = method;
            TypeDefinition resolvedDeclaringType = method.DeclaringType.Resolve();
            if (!this.IsRewritableType(resolvedDeclaringType))
            {
                return false;
            }

            // Variable that is passed by reference to rewriting methods keeping
            // track if any type in the method was rewritten.
            bool isRewritten = false;
            if (!this.TryResolve(method, out MethodDefinition resolvedMethod, false))
            {
                // Check if this method signature has been rewritten in the dependency assembly and,
                // if it has, find the rewritten method. The signature does not include the return
                // type according to C# rules, so we do not take it into account.
                List<TypeReference> paramTypes = new List<TypeReference>();
                for (int i = 0; i < method.Parameters.Count; ++i)
                {
                    var p = method.Parameters[i];
                    paramTypes.Add(this.RewriteType(p.ParameterType, Options.None));
                }

                MethodDefinition match = FindMethod(method.Name, resolvedDeclaringType, paramTypes.ToArray());
                if (!this.TryResolve(match, out resolvedMethod))
                {
                    // Unable to resolve the method or a rewritten version of this method.
                    return false;
                }

                isRewritten = true;
            }

            // Try to rewrite the declaring type.
            TypeReference newDeclaringType = this.RewriteType(method.DeclaringType,
                Options.AllowStaticRewrittenType, ref isRewritten);
            if (!this.TryResolve(newDeclaringType, out TypeDefinition resolvedNewDeclaringType))
            {
                // Unable to resolve the declaring type of the method.
                return false;
            }

            bool isDeclaringTypeRewritten = IsRuntimeType(resolvedNewDeclaringType);
            if (isDeclaringTypeRewritten)
            {
                // The declaring type is being rewritten, so only rewrite the return and
                // parameter types if they are generic.
                // resolvedMethod = FindMatchingMethodInDeclaringType(resolvedNewDeclaringType, resolvedMethod, matchName);
                if (!TryFindMethod(resolvedNewDeclaringType, resolvedMethod, matchName, out resolvedMethod))
                {
                    // No matching method found.
                    return false;
                }

                result = resolvedMethod;
            }

            if (!result.HasThis && !newDeclaringType.IsGenericInstance &&
                method.HasThis && method.DeclaringType.IsGenericInstance)
            {
                // TODO: is this needed?

                // We are converting from a generic type to a non generic static type, and from a non-generic
                // method to a generic method, so we need to instantiate the generic method.
                GenericInstanceMethod genericInstanceMethod = new GenericInstanceMethod(result);

                var genericArgs = new List<TypeReference>();
                if (method.DeclaringType is GenericInstanceType genericDeclaringType)
                {
                    // Populate the generic arguments with the generic declaring type arguments.
                    genericArgs.AddRange(genericDeclaringType.GenericArguments);
                    foreach (var genericArg in genericArgs)
                    {
                        genericInstanceMethod.GenericArguments.Add(genericArg);
                    }
                }

                result = genericInstanceMethod;
            }
            else
            {
                // Try rewrite the return only if the declaring type is a non-runtime type,
                // else assign the generic arguments if the parameter is generic.
                TypeReference newReturnType = this.RewriteType(resolvedMethod.ReturnType,
                    isDeclaringTypeRewritten ? Options.SkipRootType : Options.None, ref isRewritten);

                // Instantiate the method reference to set its generic arguments and parameters, if any.
                result = new MethodReference(result.Name, newReturnType, newDeclaringType)
                {
                    HasThis = result.HasThis,
                    ExplicitThis = result.ExplicitThis,
                    CallingConvention = result.CallingConvention
                };

                if (resolvedMethod.HasGenericParameters && method is GenericInstanceMethod genericInstanceMethod)
                {
                    // Need to rewrite the generic method to instantiate the correct generic parameter types.
                    result = this.RewriteGenericArguments(result, resolvedMethod.GenericParameters,
                        genericInstanceMethod.GenericArguments, ref isRewritten);
                }

                // Rewrite the parameters of the method, if any.
                result = this.RewriteParameters(result, resolvedMethod.Parameters, ref isRewritten);
            }

            result = this.Module.ImportReference(result);
            return isRewritten;
        }

        /// <summary>
        /// Rewrites the generic arguments of the specified <see cref="MethodReference"/>.
        /// </summary>
        private MethodReference RewriteGenericArguments(MethodReference method, Collection<GenericParameter> genericParameters,
            Collection<TypeReference> genericArguments, ref bool isRewritten)
        {
            var genericMethod = new GenericInstanceMethod(method);
            for (int i = 0; i < genericArguments.Count; ++i)
            {
                GenericParameter parameter = new GenericParameter(genericParameters[i].Name, genericMethod);
                method.GenericParameters.Add(parameter);
                genericMethod.GenericParameters.Add(parameter);

                TypeReference newArgType = this.RewriteType(genericArguments[i], Options.None, ref isRewritten);
                genericMethod.GenericArguments.Add(newArgType);
            }

            return genericMethod;
        }

        /// <summary>
        /// Rewrites the parameters of the specified <see cref="MethodReference"/>.
        /// </summary>
        private MethodReference RewriteParameters(MethodReference method, Collection<ParameterDefinition> parameters,
            ref bool isRewritten)
        {
            for (int i = 0; i < parameters.Count; ++i)
            {
                // Try rewrite the parameter only if the declaring type is a non-runtime type,
                // else assign the generic arguments if the parameter is generic.
                ParameterDefinition parameter = parameters[i];
                bool isDeclaringTypeRewritten = IsRuntimeType(method.DeclaringType);
                TypeReference newParameterType = this.RewriteType(parameter.ParameterType,
                    IsRuntimeType(method.DeclaringType) ? Options.SkipRootType : Options.None, ref isRewritten);
                ParameterDefinition newParameter = new ParameterDefinition(parameter.Name,
                    parameter.Attributes, newParameterType);
                method.Parameters.Add(newParameter);
            }

            return method;
        }

        /// <summary>
        /// Finds the matching method in the specified declaring type, if any.
        /// </summary>
        private static bool TryFindMethod(TypeDefinition declaringType, MethodDefinition originalMethod,
            string matchName, out MethodDefinition match)
        {
            match = null;
            foreach (var method in declaringType.Methods)
            {
                if ((method.Name == matchName && CheckMethodParametersMatch(originalMethod, method)) ||
                    CheckMethodSignaturesMatch(originalMethod, method))
                {
                    match = method;
                    break;
                }
            }

            return match != null;
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

            for (int idx = 0; idx < right.Parameters.Count; ++idx)
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
            // TODO: make sure all necessary checks are in place!
            // Check if the method properties match. We check 'IsStatic' later as we need to do additional checks
            // in cases where we are replacing an instance method with a static method.
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
            for (int idx = 0; idx < originalMethod.Parameters.Count; ++idx)
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
