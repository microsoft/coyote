// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Interception;
using Microsoft.Coyote.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Microsoft.Coyote.Rewriting
{
    internal class ThreadingRewriter : AssemblyRewriter
    {
        /// <summary>
        /// The cached imported <see cref="ControlledThread"/> type.
        /// </summary>
        private TypeDefinition ControlledThreadType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadingRewriter"/> class.
        /// </summary>
        internal ThreadingRewriter(ILogger logger)
            : base(logger)
        {
        }

        /// <inheritdoc/>
        internal override void VisitModule(ModuleDefinition module)
        {
            this.ControlledThreadType = null;
            base.VisitModule(module);
        }

        /// <inheritdoc/>
        internal override void VisitType(TypeDefinition type)
        {
            this.Method = null;
            this.Processor = null;
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

                // Rewrite the method body instructions.
                this.VisitInstructions(method);
            }
        }

        /// <inheritdoc/>
        protected override Instruction VisitInstruction(Instruction instruction)
        {
            if (this.Method is null)
            {
                return instruction;
            }

            if (instruction.OpCode == OpCodes.Newobj)
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
        private Instruction VisitNewobjInstruction(Instruction instruction)
        {
            MethodReference constructor = instruction.Operand as MethodReference;
            if (constructor.DeclaringType.FullName == "System.Threading.Thread")
            {
                Instruction newInstruction;

                // Invoke "public static Thread ControlledThread.Create(ThreadStart start)" instead.
                TypeDefinition controlledThread = this.GetOrImportControlledThreadType();
                MethodDefinition resolvedConstructor = constructor.Resolve();
                MethodReference createMethod = FindMatchingMethodInDeclaringType(controlledThread, resolvedConstructor, "Create");
                if (createMethod != null)
                {
                    createMethod = this.Module.ImportReference(createMethod);
                    newInstruction = Instruction.Create(OpCodes.Call, createMethod);
                    newInstruction.Offset = instruction.Offset;
                    this.Replace(instruction, newInstruction);
                    this.ModifiedMethodBody = true;
                }
                else
                {
                    // TODO: report unsupported thread construction error.
                    return instruction;
                }

                Debug.WriteLine($"............. [-] {instruction}");
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
            if (method.DeclaringType.FullName == "System.Threading.Thread")
            {
                // Some thread method calls need to change to static methods on ControlledThread.
                Instruction newInstruction;
                TypeDefinition controlledThread = this.GetOrImportControlledThreadType();
                var intercept = FindMatchingStaticMethod(controlledThread, method.Resolve());
                if (intercept != null)
                {
                    intercept = this.Module.ImportReference(intercept);
                    newInstruction = Instruction.Create(OpCodes.Call, intercept);
                    newInstruction.Offset = instruction.Offset;
                    this.Replace(instruction, newInstruction);
                    this.ModifiedMethodBody = true;
                }
                else
                {
                    // todo: some methods don't matter, but we should report errors for methods that are unsupported...
                    return instruction;
                }

                Debug.WriteLine($"............. [-] {instruction}");
                Debug.WriteLine($"............. [+] {newInstruction}");
                instruction = newInstruction;
            }

            return instruction;
        }

        /// <summary>
        /// Find a static method that matches the instance method, where the first parameter to the static
        /// is the instance parameter on the other method.
        /// </summary>
        private static MethodReference FindMatchingStaticMethod(TypeDefinition newType, MethodDefinition right)
        {
            foreach (var method in newType.Methods)
            {
                if (method.Name == right.Name && method.Parameters.Count == right.Parameters.Count + 1 &&
                    method.Parameters[0].ParameterType.FullName == right.DeclaringType.FullName)
                {
                    bool match = true;
                    for (int idx = 0; idx < right.Parameters.Count; idx++)
                    {
                        var originalParam = right.Parameters[idx];
                        var replacementParam = method.Parameters[idx + 1];
                        if ((replacementParam.ParameterType.FullName != originalParam.ParameterType.FullName) ||
                            (replacementParam.Name != originalParam.Name) ||
                            (replacementParam.IsIn && !originalParam.IsIn) ||
                            (replacementParam.IsOut && !originalParam.IsOut))
                        {
                            match = false;
                        }
                    }

                    if (match)
                    {
                        return method;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the imported <see cref="ControlledThread"/> type definition.
        /// </summary>
        private TypeDefinition GetOrImportControlledThreadType()
        {
            if (this.ControlledThreadType is null)
            {
                this.ControlledThreadType = this.Module.ImportReference(typeof(ControlledThread)).Resolve();
            }

            return this.ControlledThreadType;
        }
    }
}
