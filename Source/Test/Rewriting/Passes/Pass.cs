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
        protected AssemblyInfo Assembly { get; private set; }

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
        /// Visits the specified <see cref="AssemblyInfo"/>.
        /// </summary>
        /// <param name="assembly">The assembly to visit.</param>
        protected internal virtual void VisitAssembly(AssemblyInfo assembly)
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
        /// Returns true if the specified <see cref="MethodReference"/> can be resolved,
        /// as well as return the resolved method definition, else return false.
        /// </summary>
        protected bool TryResolve(MethodReference method, out MethodDefinition resolved, bool logError = true)
        {
            try
            {
                resolved = method?.Resolve();
            }
            catch
            {
                if (logError)
                {
                    this.Logger.WriteLine(LogSeverity.Warning, $"Unable to resolve the '{method.FullName}' method. " +
                        "The method is either unsupported by Coyote, an external method not being rewritten, or the " +
                        ".NET platform of Coyote and the target assembly do not match.");
                }

                resolved = null;
            }

            return resolved != null;
        }

        /// <summary>
        /// Returns true if the specified <see cref="TypeReference"/> can be resolved,
        /// as well as return the resolved type definition, else return false.
        /// </summary>
        protected bool TryResolve(TypeReference type, out TypeDefinition resolved, bool logError = true)
        {
            try
            {
                resolved = type?.Resolve();
            }
            catch
            {
                if (logError)
                {
                    this.Logger.WriteLine(LogSeverity.Warning, $"Unable to resolve the '{type.FullName}' type. " +
                        "The type is either unsupported by Coyote, an external type not being rewritten, or the " +
                        ".NET platform of Coyote and the target assembly do not match.");
                }

                resolved = null;
            }

            return resolved != null;
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
        /// Checks if the specified type is a visited type.
        /// </summary>
        /// <remarks>
        /// Any type from an assembly being visited is a visited type.
        /// </remarks>
        protected bool IsVisitedType(TypeDefinition type)
        {
            if (type != null)
            {
                if (type.Module == this.Module ||
                    this.VisitedAssemblies.Any(assembly => assembly.FilePath == type.Module.FileName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the specified type is a foreign type.
        /// </summary>
        /// <remarks>
        /// Any type not visited that is not a runtime type is a foreign type.
        /// </remarks>
        protected bool IsForeignType(TypeDefinition type) =>
            !this.IsVisitedType(type) && !IsRuntimeType(type);

        /// <summary>
        /// Checks if the specified type is a runtime type.
        /// </summary>
        /// <remarks>
        /// Any type from the Coyote assemblies is a runtime type.
        /// </remarks>
        protected static bool IsRuntimeType(TypeDefinition type)
        {
            if (type != null)
            {
                string modulePath = Path.GetFileName(type.Module.FileName);
                if (modulePath is "Microsoft.Coyote.dll" || modulePath is "Microsoft.Coyote.Test.dll")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the specified type is a system type.
        /// </summary>
        /// <remarks>
        /// Any type in the system namespace is a system type.
        /// </remarks>
        protected static bool IsSystemType(TypeDefinition type)
        {
            if (type != null)
            {
                TypeDefinition declaringType = type;
                while (declaringType.IsNested)
                {
                    declaringType = declaringType.DeclaringType;
                }

                if (declaringType.Namespace is "System" || declaringType.Namespace.StartsWith("System."))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
