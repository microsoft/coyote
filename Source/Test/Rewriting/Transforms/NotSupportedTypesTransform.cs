// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.SystematicTesting.Interception;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// Rewriting pass that inserts checks for invocations of not supported types.
    /// </summary>
    internal class NotSupportedTypesTransform : AssemblyTransform
    {
        /// <summary>
        /// Cache of qualified names.
        /// </summary>
        private static readonly Dictionary<string, string> CachedQualifiedNames = new Dictionary<string, string>();

        /// <summary>
        /// The current method being transformed.
        /// </summary>
        private MethodDefinition Method;

        /// <summary>
        /// A helper class for editing method body.
        /// </summary>
        private ILProcessor Processor;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotSupportedTypesTransform"/> class.
        /// </summary>
        internal NotSupportedTypesTransform(ILogger log)
            : base(log)
        {
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
            if (method.IsAbstract)
            {
                return;
            }

            this.Method = method;
            this.Processor = method.Body.GetILProcessor();

            // Rewrite the method body instructions.
            this.VisitInstructions(method);
        }

        /// <inheritdoc/>
        protected override Instruction VisitInstruction(Instruction instruction)
        {
            if (this.Method is null)
            {
                return instruction;
            }

            try
            {
                bool isUnsupportedType = false;
                string invocationName = null;

                if (instruction.OpCode == OpCodes.Initobj && instruction.Operand is TypeReference typeReference)
                {
                    if (IsUnsupportedType(typeReference.Resolve()))
                    {
                        invocationName = GetFullyQualifiedTypeName(typeReference);
                        isUnsupportedType = true;
                    }
                }
                else if ((instruction.OpCode == OpCodes.Newobj || instruction.OpCode == OpCodes.Call ||
                    instruction.OpCode == OpCodes.Callvirt) && instruction.Operand is MethodReference methodReference)
                {
                    if (IsUnsupportedType(methodReference.DeclaringType.Resolve(), methodReference))
                    {
                        invocationName = GetFullyQualifiedMethodName(methodReference);
                        isUnsupportedType = true;
                    }
                }

                if (isUnsupportedType)
                {
                    Debug.WriteLine($"............. [+] throw exception on unsupported '{invocationName}' invocation");

                    var helperType = this.Method.Module.ImportReference(typeof(ExceptionHelpers)).Resolve();
                    MethodReference helperMethod = helperType.Methods.FirstOrDefault(m => m.Name is "ThrowNotSupportedException");
                    helperMethod = this.Method.Module.ImportReference(helperMethod);

                    this.Processor.InsertBefore(instruction, Instruction.Create(OpCodes.Ldstr, invocationName));
                    this.Processor.InsertBefore(instruction, Instruction.Create(OpCodes.Call, helperMethod));

                    FixInstructionOffsets(this.Method);
                }
            }
            catch (AssemblyResolutionException)
            {
                // Skip this instruction, we are only interested in types that can be resolved.
            }

            return instruction;
        }

        /// <summary>
        /// Checks if the specified type is not supported. If the optional method is specified,
        /// then it also checks if the method is not supported.
        /// </summary>
        private static bool IsUnsupportedType(TypeDefinition type, MethodReference method = null)
        {
            if (type is null)
            {
                return false;
            }

            string module = Path.GetFileName(type.Module.FileName);
            if (!(module is "System.Private.CoreLib.dll" || module is "mscorlib.dll"))
            {
                return false;
            }

            if (type.Namespace.StartsWith(typeof(System.Diagnostics.Process).Namespace))
            {
                if (type.Name is nameof(System.Diagnostics.Process) && method != null &&
                    (method.Name is nameof(System.Diagnostics.Process.Start) ||
                    method.Name is nameof(System.Diagnostics.Process.BeginErrorReadLine) ||
                    method.Name is nameof(System.Diagnostics.Process.BeginOutputReadLine) ||
                    method.Name is nameof(System.Diagnostics.Process.CancelErrorRead) ||
                    method.Name is nameof(System.Diagnostics.Process.CancelOutputRead) ||
                    method.Name is nameof(System.Diagnostics.Process.WaitForExit) ||
                    method.Name is nameof(System.Diagnostics.Process.WaitForInputIdle) ||
                    method.Name is nameof(System.Diagnostics.Process.EnterDebugMode) ||
                    method.Name is nameof(System.Diagnostics.Process.LeaveDebugMode) ||
                    method.Name is nameof(System.Diagnostics.Process.InitializeLifetimeService) ||
                    method.Name is nameof(System.Diagnostics.Process.Refresh) ||
                    method.Name is nameof(System.Diagnostics.Process.Close) ||
                    method.Name is nameof(System.Diagnostics.Process.Dispose) ||
                    method.Name is nameof(System.Diagnostics.Process.Kill)))
                {
                    return true;
                }
            }
            else if (type.Namespace.StartsWith(typeof(System.Threading.Tasks.Task).Namespace))
            {
                if (type.Name is nameof(System.Threading.Tasks.Task) && method != null &&
                    (method.Name is nameof(System.Threading.Tasks.Task.ContinueWith) ||
                    method.Name is nameof(System.Threading.Tasks.Task.Run) ||
                    method.Name is nameof(System.Threading.Tasks.Task.RunSynchronously)))
                {
                    return true;
                }
                else if (type.Name is nameof(System.Threading.Tasks.ValueTask))
                {
                    return true;
                }
            }
            else if (type.Namespace.StartsWith(typeof(System.Threading.Thread).Namespace))
            {
                if (type.Name is nameof(System.Threading.Thread) && method != null &&
                    (method.Name is nameof(System.Threading.Thread.Start) ||
                    method.Name is nameof(System.Threading.Thread.Join) ||
                    method.Name is nameof(System.Threading.Thread.SpinWait) ||
                    method.Name is nameof(System.Threading.Thread.Sleep) ||
                    method.Name is nameof(System.Threading.Thread.Yield) ||
                    method.Name is nameof(System.Threading.Thread.Interrupt) ||
                    method.Name is nameof(System.Threading.Thread.Suspend) ||
                    method.Name is nameof(System.Threading.Thread.Resume) ||
                    method.Name is nameof(System.Threading.Thread.BeginCriticalRegion) ||
                    method.Name is nameof(System.Threading.Thread.EndCriticalRegion) ||
                    method.Name is nameof(System.Threading.Thread.Abort) ||
                    method.Name is nameof(System.Threading.Thread.ResetAbort)))
                {
                    return true;
                }
                else if (type.Name is nameof(System.Threading.ThreadPool) && method != null &&
                    (method.Name is nameof(System.Threading.ThreadPool.QueueUserWorkItem) ||
                    method.Name is nameof(System.Threading.ThreadPool.UnsafeQueueUserWorkItem) ||
                    method.Name is nameof(System.Threading.ThreadPool.UnsafeQueueNativeOverlapped) ||
                    method.Name is nameof(System.Threading.ThreadPool.RegisterWaitForSingleObject) ||
                    method.Name is nameof(System.Threading.ThreadPool.UnsafeRegisterWaitForSingleObject)))
                {
                    return true;
                }
                else if (type.Name is nameof(System.Threading.EventWaitHandle) ||
                    type.Name is nameof(System.Threading.ExecutionContext) ||
                    type.Name is nameof(System.Threading.ManualResetEvent) ||
                    type.Name is nameof(System.Threading.ManualResetEventSlim) ||
                    type.Name is nameof(System.Threading.Mutex) ||
                    type.Name is nameof(System.Threading.ReaderWriterLock) ||
                    type.Name is nameof(System.Threading.ReaderWriterLockSlim) ||
                    type.Name is nameof(System.Threading.RegisteredWaitHandle) ||
                    type.Name is nameof(System.Threading.Semaphore) ||
                    type.Name is nameof(System.Threading.SemaphoreSlim) ||
                    type.Name is nameof(System.Threading.SpinLock) ||
                    type.Name is nameof(System.Threading.SpinWait) ||
                    type.Name is nameof(System.Threading.SynchronizationContext) ||
                    type.Name is nameof(System.Threading.Timer) ||
                    type.Name is nameof(System.Threading.WaitHandle))
                {
                    return true;
                }
            }
            else if (type.Namespace.StartsWith(typeof(System.Timers.Timer).Namespace))
            {
                if (type.Name is nameof(System.Timers.Timer))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetFullyQualifiedMethodName(MethodReference method)
        {
            if (!CachedQualifiedNames.TryGetValue(method.FullName, out string name))
            {
                string typeName = GetFullyQualifiedTypeName(method.DeclaringType);
                name = $"{typeName}.{method.Name}";
                CachedQualifiedNames.Add(method.FullName, name);
            }

            return name;
        }

        private static string GetFullyQualifiedTypeName(TypeReference type)
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
    }
}
