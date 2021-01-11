// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        /// <inheritdoc/>
        protected override Instruction VisitInstruction(Instruction instruction)
        {
            if (this.Method is null)
            {
                return instruction;
            }

            if ( (instruction.OpCode == OpCodes.Callvirt || instruction.OpCode == OpCodes.Call) &&
                instruction.Operand is MethodReference method &&
                method.DeclaringType.FullName.Contains(this.CollectionClassName) &&
                (!method.DeclaringType.FullName.Contains("Enumerator")))
            {
                var newMethod = GetMockDictionaryMethod(method, method.Module);

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
                    // Since the method parameters match there's no need to modify the parameter setup code
                    // we can simply switch out the call.
                    Debug.WriteLine($"............. [-] call '{method}'");

                    // ParameterDefinition instance = method.Parameters[0];

                    // newMethod.Parameters.Add(instance);
                    var newInstruction = Instruction.Create(OpCodes.Call, genInst_2_Convert8);
                    // var newInstruction2 = Instruction.Create(OpCodes.Ldarg_0);
                    // var newInstruction1 = Instruction.Create(OpCodes.Ldc_I4, 0);

                    // this.Processor.InsertAfter(instruction, newInstruction);
                    // this.Processor.InsertAfter(instruction, newInstruction1);
                    // this.Processor.InsertAfter(instruction, newInstruction2);
                    // Debug.WriteLine($"............. [+] call '{newMethod}'");
                    this.Processor.Replace(instruction, newInstruction);
                    instruction = newInstruction;
                }
            }

            return instruction;
        }

#pragma warning disable CA1801 // Review unused parameters
        private static MethodReference GetMockDictionaryMethod(MethodReference method, ModuleDefinition mod)
#pragma warning restore CA1801 // Review unused parameters
        {
            var tt = typeof(SystematicTesting.Interception.MockDictionary);
            // Type[] typeArgs = { typeof(int), typeof(int) };
            // tt = tt.MakeGenericType(typeArgs);

            foreach (var ttypes in mod.Types)
            {
                _ = ttypes.Resolve();
            }

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
    }
}
