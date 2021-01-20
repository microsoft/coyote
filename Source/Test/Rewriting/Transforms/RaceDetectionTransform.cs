// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Coyote.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// Rewrites Monitor.Enter, Monitor.Wait, Monitor.Pulse, Monitor.Exit to use the
    /// Coyote ControlledMonitor instead which allows systematic testing of code that
    /// uses monitors.
    /// </summary>
    internal class RaceDetectionTransform : AssemblyTransform
    {
        /// <summary>
        /// The current module being transformed.
        /// </summary>
        private ModuleDefinition Module;

        /// <summary>
        /// The current method being transformed.
        /// </summary>
        private MethodDefinition Method;

        /// <summary>
        /// A helper class for editing method body.
        /// </summary>
        private ILProcessor Processor;
#pragma warning restore IDE0052 // Remove unread private members

        public string CollectionClassName = "System.Collections.Generic.Dictionary";

        /// <summary>
        /// Initializes a new instance of the <see cref="RaceDetectionTransform"/> class.
        /// </summary>
        internal RaceDetectionTransform(ILogger logger)
            : base(logger)
        {
        }

        /// <inheritdoc/>
        internal override void VisitModule(ModuleDefinition module)
        {
            this.Module = module;
            // this.ControlledCollectionType = null;
        }

        /// <inheritdoc/>
        internal override void VisitMethod(MethodDefinition method)
        {
            this.Method = null;

            // Only non-abstract method bodies can be rewritten.
            if (!method.IsAbstract)
            {
                this.Method = method;
                this.Processor = method.Body.GetILProcessor();
                this.VisitInstructions(method);
                FixInstructionOffsets(method);
            }
        }

        protected static MethodDefinition FindMatchingMethod(MethodDefinition method, TypeDefinition declaringType)
        {
            foreach (var match in declaringType.Methods)
            {
                if (method.Name == match.Name && match.Parameters.Count == (method.Parameters.Count + 1))
                {
                    return match;
                }
            }

            return null;
        }

        protected Instruction VisitInitObjInstruction(Instruction instruction)
        {
            var method = instruction.Operand as MethodReference;

            var newMethod = GetStaticMockDictionaryWrapperMethod(method.Resolve(), this.Module, "Create");

            if (newMethod == null)
            {
                return instruction;
            }

            var genInst_2_Convert8 = new GenericInstanceMethod(newMethod);

            if (method.DeclaringType.IsGenericInstance)
            {
                GenericInstanceType instance = (GenericInstanceType)method.DeclaringType;
                IList<TypeReference> genericArguments = instance.GenericArguments;
                foreach (var arg in genericArguments)
                {
                    genInst_2_Convert8.GenericArguments.Add(arg);
                }
            }

            if (newMethod != null)
            {
                var newInstruction = Instruction.Create(OpCodes.Call, genInst_2_Convert8);

                this.Processor.Replace(instruction, newInstruction);
                instruction = newInstruction;
            }

            return instruction;
        }

        /// <inheritdoc/>
        protected override Instruction VisitInstruction(Instruction instruction)
        {
            if (this.Method is null)
            {
                return instruction;
            }

            if ((instruction.OpCode == OpCodes.Newobj) &&
                instruction.Operand is MethodReference meth &&
                meth.DeclaringType.FullName.Contains(this.CollectionClassName) &&
                (!meth.DeclaringType.FullName.Contains("Enumerator")))
            {
                return this.VisitInitObjInstruction(instruction);
            }

            if ( (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                instruction.Operand is MethodReference method &&
                method.DeclaringType.FullName.Contains(this.CollectionClassName) &&
                (!method.DeclaringType.FullName.Contains("Enumerator")))
            {
                var newMethod = GetMockDictionaryMethod(method, this.Module);

                if (newMethod == null)
                {
                    return instruction;
                }

                var genInst_2_Convert8 = new GenericInstanceMethod(newMethod);

                if (method.DeclaringType.IsGenericInstance)
                {
                    GenericInstanceType instance = (GenericInstanceType)method.DeclaringType;
                    IList<TypeReference> genericArguments = instance.GenericArguments;
                    foreach (var arg in genericArguments)
                    {
                        genInst_2_Convert8.GenericArguments.Add(arg);
                    }
                }

                if (newMethod != null)
                {
                    var newInstruction = Instruction.Create(OpCodes.Call, genInst_2_Convert8);

                    this.Processor.Replace(instruction, newInstruction);
                    instruction = newInstruction;
                }
            }

            return instruction;
        }

        private static MethodReference GetMockDictionaryMethod(MethodReference method, ModuleDefinition mod)
        {
            var tt = typeof(SystematicTesting.Interception.StaticMockDictionaryWrapper);

            TypeReference declaringType = mod.ImportReference(tt);

            TypeDefinition resolvedDeclaringType = Resolve(declaringType);

            foreach (var match in resolvedDeclaringType.Methods)
            {
                if (match.Name.Contains(method.Name))
                {
                    return mod.ImportReference(match);
                }
            }

            return null;
        }

        private static MethodReference GetStaticMockDictionaryWrapperMethod(MethodDefinition meth, ModuleDefinition mod, string methName)
        {
            var tt = typeof(SystematicTesting.Interception.StaticMockDictionaryWrapper);

            TypeReference declaringType = mod.ImportReference(tt);

            /*
            Mono.Cecil.GenericInstanceType gi = null;

            if (method.DeclaringType.IsGenericInstance)
            {
                GenericInstanceType instance = (GenericInstanceType)method.DeclaringType;
                IList<TypeReference> genericArguments = instance.GenericArguments;

                gi = declaringType.MakeGenericInstanceType(mod.ImportReference(genericArguments[0]), mod.ImportReference(genericArguments[1]));
            }

            this.Module.ImportReference(gi);

            return DefaultCtorFor(gi);
            */

            TypeDefinition resolvedDeclaringType = declaringType.Resolve();

            foreach (var match in resolvedDeclaringType.Methods)
            {
                if (match.Name.Contains(methName) && meth.Parameters.Count == match.Parameters.Count)
                {
                    bool isMatch = true;
                    for (int idx = 0; idx < match.Parameters.Count; idx++)
                    {
                        var originalParam = meth.Parameters[idx];
                        var replacementParam = match.Parameters[idx];
                        if ((replacementParam.ParameterType.FullName != originalParam.ParameterType.FullName) ||
                            (replacementParam.IsIn && !originalParam.IsIn) ||
                            (replacementParam.IsOut && !originalParam.IsOut))
                        {
                            isMatch = false;
                            break;
                        }
                    }

                    if (isMatch)
                    {
                        return mod.ImportReference(match);
                    }
                }
            }

            return null;
        }

        public static MethodReference DefaultCtorFor(TypeReference type)
        {
            var resolved = type.Resolve();
            if (resolved == null)
            {
                return null;
            }

            var ctor = resolved.Methods.SingleOrDefault(m => m.IsConstructor && m.Parameters.Count == 0 && !m.IsStatic);
            if (ctor == null)
            {
                return DefaultCtorFor(resolved.BaseType);
            }

            return new MethodReference(".ctor", type.Module.TypeSystem.Void, type) { HasThis = true };
        }
    }
}
