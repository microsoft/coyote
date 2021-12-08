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
                this.TryResolve(newVariableType, out TypeDefinition _))
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
            // Console.WriteLine($"\n\n[ENTRY] Method: {method.FullName}");
            // Console.WriteLine($"  >>> Method: {method.Name} ({method.DeclaringType}) ({method.DeclaringType.Module})");
            MethodReference result = method;
            TypeDefinition resolvedDeclaringType = method.DeclaringType.Resolve();
            if (!this.IsRewritableType(resolvedDeclaringType))
            {
                return result;
            }

            // Console.WriteLine($"  >>> resolvedDeclaringType {resolvedDeclaringType} ({resolvedDeclaringType.Module})");
            if (!this.TryResolve(method, out MethodDefinition resolvedMethod, false))
            {
                // Console.WriteLine($"  >>> CANNOT RESOLVE -- is it rewritten?");

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
                // Console.WriteLine($"  >>> match: {match}");
                if (!this.TryResolve(match, out resolvedMethod))
                {
                    // Unable to resolve the method or a rewritten version of this method.
                    return result;
                }
            }

            // Console.WriteLine($"  >>> resolvedMethod: {resolvedMethod} ({resolvedMethod.HasGenericParameters})");

            // Try to rewrite the declaring type.
            TypeReference newDeclaringType = this.RewriteTypeReference(method.DeclaringType, true);
            if (!this.TryResolve(newDeclaringType, out TypeDefinition resolvedNewDeclaringType))
            {
                // Unable to resolve the declaring type of the method.
                return result;
            }

            // Console.WriteLine($"  >>> newDeclaringType: {newDeclaringType}");

            bool isRewritingDeclaringType = IsRuntimeType(resolvedNewDeclaringType);
            // Console.WriteLine($"  >>> isRewritingDeclaringType: {isRewritingDeclaringType}");
            if (isRewritingDeclaringType)
            {
                // The declaring type is being rewritten, so only rewrite the return and
                // parameter types if they are generic.
                // resolvedMethod = FindMatchingMethodInDeclaringType(resolvedNewDeclaringType, resolvedMethod, matchName);
                if (!TryFindMethod(resolvedNewDeclaringType, resolvedMethod, matchName, out resolvedMethod))
                {
                    // No matching method found.
                    return result;
                }

                // TODO: is this needed?
                // result = this.Module.ImportReference(resolvedMethod);
                result = resolvedMethod;
                // Console.WriteLine($"Match: {result.FullName}");
                // Console.WriteLine($">> Match: {result.Name} ({result.DeclaringType.FullName})");
                // Console.WriteLine($"resolvedMethod: {resolvedMethod}");
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
                // Console.WriteLine($">> result.ReturnType: {result.ReturnType}");
                // Console.WriteLine($">> Return: {method.ReturnType} ({method.ReturnType.IsGenericParameter}) ({method.ReturnType.GenericParameters.Count})");
                // Console.WriteLine($">> Return: {method.ReturnType} ({method.ReturnType.HasGenericParameters}) ({method.ReturnType.GenericParameters.Count})");
                // Console.WriteLine($">> Return: {result.ReturnType} ({result.ReturnType.IsGenericParameter}) ({result.ReturnType.GenericParameters.Count})");
                // Console.WriteLine($">> Return: {result.ReturnType} ({result.ReturnType.HasGenericParameters}) ({result.ReturnType.GenericParameters.Count})");
                // Console.WriteLine($">> Return: {method.ReturnType} ({method.ReturnType.IsGenericInstance})");
                // Console.WriteLine($">> Return: {result.ReturnType} ({result.ReturnType.IsGenericInstance})");

                // Try rewrite the return type.
                TypeReference newReturnType = this.RewriteTypeReference(resolvedMethod.ReturnType);

                // Console.WriteLine($">> newReturnType: {newReturnType} ({newReturnType.Module})");
                // Console.WriteLine($">> method.Parameters: {method.HasParameters}");
                // Console.WriteLine($">> method.GenParameters: {method.HasParameters}");
                // Console.WriteLine($">> result.Parameters: {result.HasParameters}");
                // Console.WriteLine($">> result.GenParameters: {result.HasGenericParameters}");
                // Console.WriteLine($">> resolvedMethod.Parameters: {resolvedMethod.HasParameters}");
                // Console.WriteLine($">> resolvedMethod.GenParameters: {resolvedMethod.HasGenericParameters}");
                // Console.WriteLine($">> method.Parameters: {method.Parameters.Count}");
                // Console.WriteLine($">> result.Parameters: {result.Parameters.Count}");
                // Console.WriteLine($">> resolvedMethod.Parameters: {resolvedMethod.Parameters.Count}");
                // Console.WriteLine($">> method.GenericParameters: {method.GenericParameters.Count}");
                // Console.WriteLine($">> result.GenericParameters: {result.GenericParameters.Count}");
                // Console.WriteLine($">> resolvedMethod.GenericParameters: {resolvedMethod.GenericParameters.Count}");

                // Rewrite the parameters of the method, if any.
                // Collection<ParameterDefinition> newParameters = this.RewriteMethodParameters(result,
                //     method.Parameters, isRewritingDeclaringType, method);
                // Console.WriteLine($"1: {result.FullName}");

                // Instantiate the method reference to set its generic arguments and parameters, if any.
                result = new MethodReference(result.Name, newReturnType, newDeclaringType)
                {
                    HasThis = result.HasThis,
                    ExplicitThis = result.ExplicitThis,
                    CallingConvention = result.CallingConvention
                };

                // Console.WriteLine($"2: {result.FullName}");

                if (resolvedMethod.HasGenericParameters)
                {
                    // Console.WriteLine($"\n\n\n3: =======================================");
                    // Console.WriteLine($"3: HasGenericParameters: {result.FullName}");
                    // Console.WriteLine($"3: method: {method.GenericParameters.Count}");
                    // Console.WriteLine($"3: resolvedMethod: {resolvedMethod.GenericParameters.Count}");
                    // Console.WriteLine($"3: result: {result.GenericParameters.Count}");
                    foreach (var gp in method.GenericParameters)
                    {
                        // Console.WriteLine($"x-gp: {gp.FullName}");
                    }

                    foreach (var ga in (method as GenericInstanceMethod)?.GenericArguments)
                    {
                        // Console.WriteLine($"x-ga: {ga.FullName}");
                    }

                    foreach (var gp in resolvedMethod.GenericParameters)
                    {
                        // Console.WriteLine($"y-gp: {gp.FullName}");
                    }

                    // Need to rewrite the generic method to instantiate the correct generic parameter types.
                    result = this.RewriteGenericArguments(result, resolvedMethod.GenericParameters,
                        (method as GenericInstanceMethod)?.GenericArguments);
                }

                // Rewrite the parameters of the method, if any.
                result = this.RewriteParameters(result, resolvedMethod.Parameters);
                // Console.WriteLine($"4: {result.FullName}");
            }

            var x = this.Module.ImportReference(result);
            // Console.WriteLine($"=== declaring: {x.DeclaringType} ({x.DeclaringType.Module})");
            // Console.WriteLine($"=== return: {x.ReturnType} ({x.ReturnType.Module})");
            foreach (var k in x.Parameters)
            {
                // Console.WriteLine($"  === param: {k.ParameterType} ({k.ParameterType.Module})");
            }

            if (x is GenericInstanceMethod xx)
            {
                foreach (var k in xx.GenericParameters)
                {
                    // Console.WriteLine($"  === genparam: {k} ({k.Module})");
                }

                foreach (var k in xx.GenericArguments)
                {
                    // Console.WriteLine($"  === genarg: {k} ({k.Module})");
                }
            }

            // Console.WriteLine($"  === DONE");
            TypeDefinition resultDeclaringType = x.DeclaringType.Resolve();
            // Console.WriteLine($"  === {resultDeclaringType}");
            return x;
        }

        /// <summary>
        /// Rewrites the generic arguments of the specified <see cref="MethodReference"/>.
        /// </summary>
        private MethodReference RewriteGenericArguments(MethodReference method,
            Collection<GenericParameter> genericParameters, Collection<TypeReference> genericArguments)
        {
            // Console.WriteLine($">> HI?!");
            var genericMethod = new GenericInstanceMethod(method);

            // Console.WriteLine($">> method: {method}");
            // Console.WriteLine($">> method-type: {method.DeclaringType}");
            // Console.WriteLine($">> old-gen-parameter-count: {method.GenericParameters.Count}");
            // Console.WriteLine($">> gen-parameter-count: {genericParameters.Count}");
            // Console.WriteLine($">> old-gen-arg-count: {genericMethod.GenericArguments.Count}");
            // Console.WriteLine($">> gen-arg-count: {genericArguments.Count}");

            for (int i = 0; i < genericArguments.Count; i++)
            {
                GenericParameter parameter = new GenericParameter(genericParameters[i].Name, genericMethod);
                method.GenericParameters.Add(parameter);
                genericMethod.GenericParameters.Add(parameter);

                TypeReference newArgType = this.RewriteTypeReference(genericArguments[i]);
                genericMethod.GenericArguments.Add(newArgType);
                // Console.WriteLine($">> gen-arg: {genericMethod.GenericArguments[i]}");
            }

            return genericMethod;
        }

        /// <summary>
        /// Rewrites the parameters of the specified <see cref="MethodReference"/>.
        /// </summary>
        private MethodReference RewriteParameters(MethodReference method, Collection<ParameterDefinition> parameters)
        {
            // Console.WriteLine($">> HELLO?!");
            // Console.WriteLine($">> method: {method}");
            // Console.WriteLine($">> method-type: {method.DeclaringType}");
            // Console.WriteLine($">> old-parameter-count: {method.Parameters.Count}");
            // Console.WriteLine($">> old-gen-parameter-count: {method.GenericParameters.Count}");
            // Console.WriteLine($">> parameter-count: {parameters.Count}");

            for (int i = 0; i < parameters.Count; i++)
            {
                ParameterDefinition parameter = parameters[i];
                // Console.WriteLine($">> parameter: {parameter}");
                // Console.WriteLine($">> parameter-name: {parameter.Name}");
                // Console.WriteLine($">> parameter-type: {parameter.ParameterType}");
                // Console.WriteLine($">> parameter-type-has-gen: {parameter.ParameterType.HasGenericParameters}");
                // Console.WriteLine($">> parameter-type-contains-gen: {parameter.ParameterType.ContainsGenericParameter}");
                // Console.WriteLine($">> parameter-type-gen-count: {parameter.ParameterType.GenericParameters.Count}");

                // Try rewrite the parameter type.
                TypeReference newParameterType = this.RewriteTypeReference(parameter.ParameterType);
                // Console.WriteLine($">> newParameterType: {newParameterType} ({newParameterType.Module})");

                ParameterDefinition newParameter = new ParameterDefinition(parameter.Name,
                    parameter.Attributes, newParameterType);
                // Console.WriteLine($">> newParameter: {newParameter.ParameterType} ({newParameter.ParameterType.Module})");
                method.Parameters.Add(newParameter);
            }

            return method;
        }

        /// <summary>
        /// Rewrites the specified <see cref="TypeReference"/>.
        /// </summary>
        private TypeReference RewriteTypeReference(TypeReference type, bool allowStatic = false)
        {
            if (this.TryRewriteType(type, out TypeReference newType, allowStatic) &&
                this.TryResolve(newType, out TypeDefinition _))
            {
                // Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>> type1: {type} ({type.Module})");
                type = newType;
            }

            // Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>> type2: {type} ({type.Module})");
            return type;
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
