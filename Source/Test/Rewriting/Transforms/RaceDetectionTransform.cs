// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

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

        public static string DictionaryClassName = "System.Collections.Generic.Dictionary`2";
        public static string ListClassName = "System.Collections.Generic.List`1";

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

        protected Instruction VisitInitObjInstruction(Instruction instruction)
        {
            var method = instruction.Operand as MethodReference;
            MethodReference newMethod = null;

            if (method.DeclaringType.GetElementType().FullName.Contains(DictionaryClassName))
            {
                newMethod = GetStaticMockCollectionWrapperMethod(method.Resolve(), typeof(SystematicTesting.Interception.StaticMockDictionaryWrapper), this.Module, "Create");
            }
            else if (method.Name == ".ctor" && method.DeclaringType.GetElementType().FullName.Contains(ListClassName))
            {
                newMethod = GetStaticMockCollectionWrapperMethod(method.Resolve(), typeof(SystematicTesting.Interception.StaticMockListWrapper), this.Module, "Create");
            }

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
                (meth.DeclaringType.GetElementType().FullName.Contains(DictionaryClassName) || meth.DeclaringType.GetElementType().FullName.Contains(ListClassName)) &&
                (!meth.DeclaringType.FullName.Contains("Enumerator")))
            {
                return this.VisitInitObjInstruction(instruction);
            }

            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                instruction.Operand is MethodReference method &&
                (method.DeclaringType.GetElementType().FullName.Contains(DictionaryClassName) || method.DeclaringType.GetElementType().FullName.Contains(ListClassName)) &&
                (!method.DeclaringType.FullName.Contains("Enumerator")))
            {
                var newMethod = GetMockCollectionMethod(method, this.Module);

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

        private static MethodReference GetMockCollectionMethod(MethodReference method, ModuleDefinition mod)
        {
            Type tt = null;

            if (method.DeclaringType.FullName.Contains(DictionaryClassName))
            {
                tt = typeof(SystematicTesting.Interception.StaticMockDictionaryWrapper);
            }
            else if (method.DeclaringType.FullName.Contains(ListClassName))
            {
                tt = typeof(SystematicTesting.Interception.StaticMockListWrapper);
            }

            if (tt != null)
            {
                TypeReference declaringType = mod.ImportReference(tt);

                TypeDefinition resolvedDeclaringType = Resolve(declaringType);

                foreach (var match in resolvedDeclaringType.Methods)
                {
                    // TODO: Check the type of parameters also.
                    if (match.Name.Contains(method.Name) && match.Parameters.Count == (method.Parameters.Count + 1))
                    {
                        return mod.ImportReference(match);
                    }
                }
            }

            return null;
        }

        private static MethodReference GetStaticMockCollectionWrapperMethod(MethodDefinition meth, Type tt, ModuleDefinition mod, string methName)
        {
            TypeReference declaringType = mod.ImportReference(tt);
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
    }
}
