// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// Rewriting pass that fails invocations of uncontrolled types.
    /// </summary>
    internal class UncontrolledInvocationRewritingPass : RewritingPass
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UncontrolledInvocationRewritingPass"/> class.
        /// </summary>
        internal UncontrolledInvocationRewritingPass(IEnumerable<AssemblyInfo> visitedAssemblies, ILogger logger)
            : base(visitedAssemblies, logger)
        {
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
                bool isUncontrolledType = false;
                string invocationName = null;

                if (instruction.OpCode == OpCodes.Initobj && instruction.Operand is TypeReference typeReference)
                {
                    if (IsUncontrolledType(typeReference.Resolve()))
                    {
                        invocationName = GetFullyQualifiedTypeName(typeReference);
                        isUncontrolledType = true;
                    }
                }
                else if ((instruction.OpCode == OpCodes.Newobj || instruction.OpCode == OpCodes.Call ||
                    instruction.OpCode == OpCodes.Callvirt) && instruction.Operand is MethodReference methodReference)
                {
                    if (IsUncontrolledType(methodReference.DeclaringType.Resolve(), methodReference))
                    {
                        invocationName = GetFullyQualifiedMethodName(methodReference);
                        isUncontrolledType = true;
                    }
                }

                if (isUncontrolledType)
                {
                    Debug.WriteLine($"............. [+] injected uncontrolled '{invocationName}' invocation exception");

                    var providerType = this.Method.Module.ImportReference(typeof(ExceptionProvider)).Resolve();
                    MethodReference providerMethod = providerType.Methods.FirstOrDefault(
                        m => m.Name is nameof(ExceptionProvider.ThrowUncontrolledInvocationException));
                    providerMethod = this.Method.Module.ImportReference(providerMethod);

                    this.Processor.InsertBefore(instruction, Instruction.Create(OpCodes.Ldstr, invocationName));
                    this.Processor.InsertBefore(instruction, Instruction.Create(OpCodes.Call, providerMethod));

                    this.IsMethodBodyModified = true;
                }
            }
            catch (AssemblyResolutionException)
            {
                // Skip this instruction, we are only interested in types that can be resolved.
            }

            return instruction;
        }

        /// <summary>
        /// Checks if the specified type is not controlled. If the optional method is specified,
        /// then it also checks if the method is not controlled.
        /// </summary>
        private static bool IsUncontrolledType(TypeDefinition type, MethodReference method = null)
        {
            if (type is null || !IsSystemType(type))
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

                // else if (type.Name is nameof(System.Threading.Tasks.ValueTask))
                // {
                //     return true;
                // }
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
                    // type.Name is nameof(System.Threading.SynchronizationContext) ||
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
    }
}
