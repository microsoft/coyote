// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// An abstract interface for transforming code using a visitor pattern.
    /// This is used by the <see cref="RewritingEngine"/> to manage multiple different
    /// transforms in a single pass over an assembly.
    /// </summary>
    internal abstract class AssemblyTransform
    {
        /// <summary>
        /// Cache of qualified names.
        /// </summary>
        private static readonly Dictionary<string, string> CachedQualifiedNames = new Dictionary<string, string>();

        /// <summary>
        /// The installed logger.
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyTransform"/> class.
        /// </summary>
        protected AssemblyTransform(ILogger logger)
        {
            this.Logger = logger;
        }

        /// <summary>
        /// Visits the specified <see cref="ModuleDefinition"/> inside the <see cref="AssemblyDefinition"/>
        /// that was visited by the <see cref="RewritingEngine"/>.
        /// </summary>
        /// <param name="module">The module definition to visit.</param>
        internal virtual void VisitModule(ModuleDefinition module)
        {
        }

        /// <summary>
        /// Visits the specified <see cref="TypeDefinition"/> inside the <see cref="ModuleDefinition"/>
        /// that was visited by the last <see cref="VisitModule"/>.
        /// </summary>
        /// <param name="type">The type definition to visit.</param>
        internal virtual void VisitType(TypeDefinition type)
        {
        }

        /// <summary>
        /// Visits the specified <see cref="FieldDefinition"/> inside the <see cref="TypeDefinition"/> that was visited
        /// by the last <see cref="VisitType"/>.
        /// </summary>
        /// <param name="field">The field definition to visit.</param>
        internal virtual void VisitField(FieldDefinition field)
        {
        }

        /// <summary>
        /// Visits the specified <see cref="MethodDefinition"/> inside the <see cref="TypeDefinition"/> that was visited
        /// by the last <see cref="VisitType"/>.
        /// </summary>
        /// <param name="method">The method definition to visit.</param>
        internal virtual void VisitMethod(MethodDefinition method)
        {
        }

        /// <summary>
        /// If you want to transform individual method variables, then call this method.
        /// </summary>
        /// <param name="method">The method whose variables are being transformed.</param>
        protected virtual void VisitVariables(MethodDefinition method)
        {
            foreach (var variable in method.Body.Variables.ToArray())
            {
                this.VisitVariable(variable);
            }
        }

        /// <summary>
        /// Visits the specified <see cref="VariableDefinition"/> inside the <see cref="MethodDefinition"/> that was visited
        /// by the last <see cref="VisitMethod"/>.
        /// </summary>
        /// <param name="variable">The variable definition to visit.</param>
        protected virtual void VisitVariable(VariableDefinition variable)
        {
        }

        /// <summary>
        /// If you want to transform individual instructions, then call this method.
        /// </summary>
        /// <param name="method">The method whose instructions will be transformed.</param>
        protected void VisitInstructions(MethodDefinition method)
        {
            // Rewrite the method body instructions.
            Instruction instruction = method.Body.Instructions.FirstOrDefault();
            while (instruction != null)
            {
                instruction = this.VisitInstruction(instruction);
                instruction = instruction.Next;
            }
        }

        /// <summary>
        /// Visits the specified IL <see cref="Instruction"/> inside the body of the <see cref="MethodDefinition"/>
        /// that was visited by the last <see cref="VisitMethod"/>.
        /// </summary>
        /// <param name="instruction">The instruction to visit.</param>
        /// <returns>The last modified instruction, or the original if it was not changed.</returns>
        protected virtual Instruction VisitInstruction(Instruction instruction)
        {
            return instruction;
        }

        /// <summary>
        /// Rewrites the specified <see cref="MethodReference"/>.
        /// </summary>
        /// <param name="method">The method reference to rewrite.</param>
        /// <param name="module">The module definition that is being visited.</param>
        /// <returns>The rewritten method, or the original if it was not changed.</returns>
        protected MethodReference RewriteMethodReference(MethodReference method, ModuleDefinition module)
        {
            MethodReference result = method;

            TypeReference declaringType = this.RewriteDeclaringTypeReference(method);
            if (method.DeclaringType == declaringType ||
                !this.TryResolve(method, out MethodDefinition resolvedMethod))
            {
                // We are not rewriting this method.
                return result;
            }

            TypeDefinition resolvedDeclaringType = Resolve(declaringType);

            // This is an extra initial parameter that we have when converting an instance to a static method.
            // For example, `task.GetAwaiter()` is converted to `ControlledTask.GetAwaiter(task)`.
            ParameterDefinition instanceParameter = null;
            MethodDefinition match = FindMatchingMethodInDeclaringType(resolvedMethod, resolvedDeclaringType);
            if (match != null)
            {
                result = module.ImportReference(match);
                if (resolvedMethod.Parameters.Count != match.Parameters.Count)
                {
                    // We are converting from an instance method to a static method, so store the instance parameter.
                    instanceParameter = result.Parameters[0];
                }
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
                        throw new InvalidOperationException(string.Format("Not enough generic arguments to instantiate method {0}", method));
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
        protected virtual TypeReference RewriteDeclaringTypeReference(MethodReference method) => method.DeclaringType;

        /// <summary>
        /// Rewrites the specified <see cref="TypeReference"/>.
        /// </summary>
        /// <param name="type">The type reference to rewrite.</param>
        /// <returns>The rewritten type reference, or the original if it was not changed.</returns>
        protected virtual TypeReference RewriteTypeReference(TypeReference type) => type;

        /// <summary>
        /// Finds the matching method in the specified declaring type, if any.
        /// </summary>
        protected static MethodDefinition FindMatchingMethodInDeclaringType(MethodDefinition method, TypeDefinition declaringType)
        {
            foreach (var match in declaringType.Methods)
            {
                if (!CheckMethodSignaturesMatch(method, match))
                {
                    continue;
                }

                return match;
            }

            return null;
        }

        /// <summary>
        /// Checks if the signatures of the original and the replacement methods match.
        /// </summary>
        /// <remarks>
        /// This method also checks the use case where we are converting an instance method into a static method.
        /// In such a case case, we are inserting a first parameter that has the same type as the declaring type
        /// of the original method. For example we can convert `task.Wait()` to `ControlledTask.Wait(task)`.
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

        /// <summary>
        /// Find a matching method declaration in the given declaring type.
        /// </summary>
        /// <returns>The matching method or null if it is not found.</returns>
        protected static MethodDefinition FindMatchingMethod(TypeDefinition declaringType, string name, params TypeReference[] parameterTypes)
        {
            foreach (var method in declaringType.Methods)
            {
                if (method.Name == name && method.Parameters.Count == parameterTypes.Length)
                {
                    bool matches = true;
                    // Check if the parameters match.
                    for (int i = 0, n = method.Parameters.Count; matches && i < n; i++)
                    {
                        var p = method.Parameters[i];
                        var q = parameterTypes[i];
                        if (p.ParameterType.FullName != q.FullName)
                        {
                            matches = false;
                        }
                    }

                    if (matches)
                    {
                        return method;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if the parameters of the two specified methods match.
        /// </summary>
        protected static bool CheckMethodParametersMatch(MethodDefinition left, MethodDefinition right)
        {
            if (left.Parameters.Count != right.Parameters.Count)
            {
                return false;
            }

            for (int idx = 0; idx < right.Parameters.Count; idx++)
            {
                var originalParam = right.Parameters[idx];
                var replacementParam = left.Parameters[idx];
                // TODO: make sure all necessary checks are in place!
                if ((replacementParam.ParameterType.FullName != originalParam.ParameterType.FullName) ||
                    (replacementParam.Name != originalParam.Name) ||
                    (replacementParam.IsIn && !originalParam.IsIn) ||
                    (replacementParam.IsOut && !originalParam.IsOut))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns true if the specified <see cref="MethodReference"/> can be resolved,
        /// as well as the resolved method definition, else return false.
        /// </summary>
        protected bool TryResolve(MethodReference method, out MethodDefinition resolved)
        {
            resolved = method.Resolve();
            if (resolved is null)
            {
                this.Logger.WriteLine(LogSeverity.Warning, $"Unable to resolve '{method.FullName}' method. " +
                    "The method is either unsupported by Coyote, or a user-defined extension method, or the " +
                    ".NET platform of Coyote and the target assembly do not match.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the resolved definition of the specified <see cref="TypeReference"/>.
        /// </summary>
        protected static TypeDefinition Resolve(TypeReference type)
        {
            TypeDefinition result = type.Resolve();
            if (result is null)
            {
                throw new Exception($"Error resolving '{type.FullName}' type. Please check that " +
                    "the .NET platform of coyote and the target assembly match.");
            }

            return result;
        }

        /// <summary>
        /// Create a GenericInstanceType for the given generic type instantiated with the given generic arguments.
        /// </summary>
        /// <param name="module">The module we are operating on.</param>
        /// <param name="genericType">The generic type to instantiate.</param>
        /// <param name="genericArgs">The generic arguments needed to instantiate the generic type.</param>
        /// <returns>The new generic instance type.</returns>
        protected static GenericInstanceType ImportGenericTypeInstance(ModuleDefinition module, TypeReference genericType,
            params TypeReference[] genericArgs)
        {
            TypeReference typeDef = genericType.Resolve();
            if (!typeDef.HasGenericParameters)
            {
                throw new InvalidOperationException(string.Format("Type {0} is not generic", genericType));
            }

            typeDef = module.ImportReference(typeDef);
            var instance = new GenericInstanceType(typeDef);
            for (int i = 0; i < typeDef.GenericParameters.Count; i++)
            {
                var p = typeDef.GenericParameters[i];
                if (p.Position < genericArgs.Length)
                {
                    GenericParameter parameter = new GenericParameter(p.Name, typeDef);
                    instance.GenericParameters.Add(parameter);
                    instance.GenericArguments.Add(genericArgs[p.Position]);
                }
                else
                {
                    throw new InvalidOperationException(string.Format("Not enough generic arguments to instantiate type {0}", genericType));
                }
            }

            return instance;
        }

        /// <summary>
        /// Create a GenericInstanceMethod from the given generic method and the given generic arguments.
        /// Note: this can also handle the case where the DeclaringType is also generic.  Simply pass the combined
        /// generic args for the declaring type and the method.
        /// </summary>
        /// <param name="module">The module we are operating on.</param>
        /// <param name="genericMethod">A generic method to instantiate.</param>
        /// <param name="genericArgs">The combined generic arguments for the declaring type (if it is generic) and for the method.</param>
        /// <returns>The new method reference.</returns>
        protected static MethodReference ImportGenericMethodInstance(ModuleDefinition module, MethodReference genericMethod,
            params TypeReference[] genericArgs)
        {
            var methodDef = genericMethod.Resolve();

            TypeReference typeDef = methodDef.DeclaringType;
            var genericArgOffset = 0;
            GenericInstanceType typeInstance = null;

            if (typeDef.HasGenericParameters)
            {
                typeInstance = ImportGenericTypeInstance(module, typeDef, genericArgs);
                typeDef = typeInstance;
                genericArgOffset = typeInstance.GenericArguments.Count;
            }

            TypeReference returnType = methodDef.ReturnType;

            // create a new MethodReference with the instantiated generic type as the DeclaringType.
            MethodReference result = new MethodReference(genericMethod.Name, returnType, typeDef)
            {
                HasThis = genericMethod.HasThis,
                ExplicitThis = genericMethod.ExplicitThis,
                CallingConvention = genericMethod.CallingConvention
            };

            GenericInstanceMethod genericMethodInstance = null;

            if (methodDef.HasGenericParameters)
            {
                // Then we also need to instantiate the generic method!
                genericMethodInstance = new GenericInstanceMethod(result);

                for (int i = 0; i < methodDef.GenericParameters.Count; i++)
                {
                    var p = methodDef.GenericParameters[i];
                    var j = p.Position + genericArgOffset;
                    if (j > genericArgs.Length)
                    {
                        throw new InvalidOperationException(string.Format("Not enough generic arguments to instantiate method {0}", genericMethod));
                    }

                    GenericParameter parameter = new GenericParameter(p.Name, genericMethodInstance);
                    result.GenericParameters.Add(parameter);
                    genericMethodInstance.GenericParameters.Add(parameter);
                    genericMethodInstance.GenericArguments.Add(genericArgs[j]);
                }

                result = genericMethodInstance;
            }

            foreach (var arg in genericMethod.Parameters)
            {
                ParameterDefinition p = new ParameterDefinition(arg.Name, arg.Attributes,
                           module.ImportReference(arg.ParameterType, typeDef));

                if (arg.ParameterType is GenericParameter gp)
                {
                    if (gp.DeclaringType != null)
                    {
                        p.ParameterType = typeInstance.GenericParameters[gp.Position];
                    }
                    else if (gp.DeclaringMethod != null)
                    {
                        p.ParameterType = genericMethodInstance.GenericParameters[gp.Position];
                    }
                }

                result.Parameters.Add(p);
            }

            return result;
        }

        /// <summary>
        /// Fixes the instruction offsets of the specified method.
        /// </summary>
        protected static void FixInstructionOffsets(MethodDefinition method)
        {
            // By inserting new code into the visited method, it is possible some short branch
            // instructions are now out of range, and need to be switch to long branches. This
            // fixes that and it also recomputes instruction indexes which is also needed for
            // valid write assembly operation.
            method.Body.SimplifyMacros();
            method.Body.OptimizeMacros();
        }

        /// <summary>
        /// Gets the fully qualified name of the specified type.
        /// </summary>
        protected static string GetFullyQualifiedTypeName(TypeReference type)
        {
            if (!CachedQualifiedNames.TryGetValue(type.FullName, out string name))
            {
                if (type is GenericInstanceType genericType)
                {
                    name = $"{genericType.ElementType.FullName.Split('`')[0]}";
                }
                else
                {
                    name = type.FullName;
                }

                CachedQualifiedNames.Add(type.FullName, name);
            }

            return name;
        }

        /// <summary>
        /// Gets the fully qualified name of the specified method.
        /// </summary>
        protected static string GetFullyQualifiedMethodName(MethodReference method)
        {
            if (!CachedQualifiedNames.TryGetValue(method.FullName, out string name))
            {
                string typeName = GetFullyQualifiedTypeName(method.DeclaringType);
                name = $"{typeName}.{method.Name}";
                CachedQualifiedNames.Add(method.FullName, name);
            }

            return name;
        }
    }
}
