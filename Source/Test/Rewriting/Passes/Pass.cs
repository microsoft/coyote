// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Coyote.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// An abstract implementation of a pass that traverses IL using a visitor pattern.
    /// </summary>
    internal abstract class Pass
    {
        /// <summary>
        /// The set of assemblies that are being visited.
        /// </summary>
        protected IEnumerable<AssemblyInfo> VisitedAssemblies { get; private set; }

        /// <summary>
        /// The current assembly being visited.
        /// </summary>
        protected AssemblyDefinition Assembly { get; private set; }

        /// <summary>
        /// The current module being visited.
        /// </summary>
        protected ModuleDefinition Module { get; private set; }

        /// <summary>
        /// The type being visited.
        /// </summary>
        protected TypeDefinition TypeDef { get; private set; }

        /// <summary>
        /// The current method being visited.
        /// </summary>
        protected MethodDefinition Method { get; private set; }

        /// <summary>
        /// A helper for transforming method bodies.
        /// </summary>
        protected ILProcessor Processor { get; private set; }

        /// <summary>
        /// Cache of qualified names.
        /// </summary>
        private static readonly Dictionary<string, string> CachedQualifiedNames = new Dictionary<string, string>();

        /// <summary>
        /// The installed logger.
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pass"/> class.
        /// </summary>
        protected Pass(IEnumerable<AssemblyInfo> visitedAssemblies, ILogger logger)
        {
            this.VisitedAssemblies = visitedAssemblies;
            this.Logger = logger;
        }

        /// <summary>
        /// Visits the specified <see cref="AssemblyDefinition"/>.
        /// </summary>
        /// <param name="assembly">The assembly definition to visit.</param>
        protected internal virtual void VisitAssembly(AssemblyDefinition assembly)
        {
            this.Assembly = assembly;
            this.Module = null;
            this.TypeDef = null;
            this.Method = null;
            this.Processor = null;
        }

        /// <summary>
        /// Visits the specified <see cref="ModuleDefinition"/> inside the currently
        /// visited <see cref="AssemblyDefinition"/>.
        /// </summary>
        /// <param name="module">The module definition to visit.</param>
        protected internal virtual void VisitModule(ModuleDefinition module)
        {
            this.Module = module;
            this.TypeDef = null;
            this.Method = null;
            this.Processor = null;
        }

        /// <summary>
        /// Visits the specified <see cref="TypeDefinition"/> inside the currently
        /// visited <see cref="ModuleDefinition"/>.
        /// </summary>
        /// <param name="type">The type definition to visit.</param>
        protected internal virtual void VisitType(TypeDefinition type)
        {
            this.TypeDef = type;
            this.Method = null;
            this.Processor = null;
        }

        /// <summary>
        /// Visits the specified <see cref="FieldDefinition"/> inside the currently
        /// visited <see cref="TypeDefinition"/>.
        /// </summary>
        /// <param name="field">The field definition to visit.</param>
        protected internal virtual void VisitField(FieldDefinition field)
        {
        }

        /// <summary>
        /// Visits the specified <see cref="MethodDefinition"/> inside the currently
        /// visited <see cref="TypeDefinition"/>.
        /// </summary>
        /// <param name="method">The method definition to visit.</param>
        protected internal virtual void VisitMethod(MethodDefinition method)
        {
            this.Method = method;

            // Only non-abstract method bodies can be visited.
            if (!method.IsAbstract)
            {
                this.Processor = method.Body.GetILProcessor();

                // Visit the method body variables.
                foreach (var variable in method.Body.Variables.ToArray())
                {
                    this.VisitVariable(variable);
                }

                // Visit the method body instructions.
                Instruction instruction = method.Body.Instructions.FirstOrDefault();
                while (instruction != null)
                {
                    instruction = this.VisitInstruction(instruction);
                    instruction = instruction.Next;
                }
            }
        }

        /// <summary>
        /// Visits the specified <see cref="VariableDefinition"/> inside the currently
        /// visited <see cref="MethodDefinition"/>.
        /// </summary>
        /// <param name="variable">The variable definition to visit.</param>
        protected virtual void VisitVariable(VariableDefinition variable)
        {
        }

        /// <summary>
        /// Visits the specified IL <see cref="Instruction"/> inside the body of the currently
        /// visited <see cref="MethodDefinition"/>.
        /// </summary>
        /// <param name="instruction">The instruction to visit.</param>
        /// <returns>The last modified instruction, or the original if it was not changed.</returns>
        protected virtual Instruction VisitInstruction(Instruction instruction)
        {
            return instruction;
        }

        /// <summary>
        /// Completes the visit over the current assembly.
        /// </summary>
        protected internal virtual void CompleteVisit()
        {
        }

        /// <summary>
        /// Finds the matching method in the specified declaring type, if any.
        /// </summary>
        protected static MethodDefinition FindMatchingMethodInDeclaringType(TypeDefinition declaringType,
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
        /// Finds the matching method in the specified declaring type, if any.
        /// </summary>
        protected static MethodDefinition FindMatchingMethodInDeclaringType(TypeDefinition declaringType,
            string name, params TypeReference[] parameterTypes)
        {
            foreach (var match in declaringType.Methods)
            {
                if (match.Name == name && match.Parameters.Count == parameterTypes.Length)
                {
                    bool matches = true;
                    // Check if the parameters match.
                    for (int i = 0, n = match.Parameters.Count; matches && i < n; i++)
                    {
                        var p = match.Parameters[i];
                        var q = parameterTypes[i];
                        if (p.ParameterType.FullName != q.FullName)
                        {
                            matches = false;
                        }
                    }

                    if (matches)
                    {
                        return match;
                    }
                }
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
        /// Returns true if the specified <see cref="MethodReference"/> can be resolved,
        /// as well as the resolved method definition, else return false.
        /// </summary>
        protected bool TryResolve(MethodReference method, out MethodDefinition resolved)
        {
            try
            {
                resolved = method.Resolve();
            }
            catch
            {
                resolved = null;
            }

            if (resolved is null)
            {
                this.Logger.WriteLine(LogSeverity.Warning, $"Unable to resolve '{method.FullName}' method. " +
                    "The method is either unsupported by Coyote, an external method not being rewritten, " +
                    "or the .NET platform of Coyote and the target assembly do not match.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the resolved definition of the specified <see cref="TypeReference"/>.
        /// </summary>
        protected static TypeDefinition Resolve(TypeReference type)
        {
            TypeDefinition result = type?.Resolve();
            if (result is null)
            {
                throw new InvalidOperationException($"Error resolving '{type.FullName}' type. Please check that " +
                    "the .NET platform of coyote and the target assembly match.");
            }

            return result;
        }

        /// <summary>
        /// Returns the parameter type that is at the specified index of the given <see cref="GenericInstanceType"/>.
        /// </summary>
        /// <remarks>
        /// The index is of the form '!N' where N is the index of the parameter in the generic type.
        /// </remarks>
        protected static TypeReference GetGenericParameterTypeFromNamedIndex(GenericInstanceType genericType, string namedIndex)
        {
            int index = int.Parse(namedIndex.Split('!')[1]);
            return genericType.GenericArguments[index].GetElementType();
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

        /// <summary>
        /// Checks if the specified type is a foreign type.
        /// </summary>
        protected bool IsForeignType(TypeDefinition type)
        {
            if (type != null)
            {
                // Any type from an assembly being visited is not a foreign type.
                if (type.Module == this.Module ||
                    this.VisitedAssemblies.Any(assembly => assembly.FilePath == type.Module.FileName))
                {
                    return false;
                }

                // Any type from the Coyote assemblies is not a foreign type.
                string modulePath = Path.GetFileName(type.Module.FileName);
                if (modulePath is "Microsoft.Coyote.dll" || modulePath is "Microsoft.Coyote.Test.dll")
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the specified type is a system type.
        /// </summary>
        protected static bool IsSystemType(TypeDefinition type)
        {
            if (type != null)
            {
                TypeDefinition declaringType = type;
                while (declaringType.IsNested)
                {
                    declaringType = declaringType.DeclaringType;
                }

                // Any type in the 'System' namespace is a system type.
                if (declaringType.Namespace is "System" || declaringType.Namespace.StartsWith("System."))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
