// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Logging;
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
        internal UncontrolledInvocationRewritingPass(IEnumerable<AssemblyInfo> visitedAssemblies, LogWriter logWriter)
            : base(visitedAssemblies, logWriter)
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
                bool isDataNondeterministic = false;
                string invocationName = null;

                if (instruction.OpCode == OpCodes.Initobj && instruction.Operand is TypeReference typeReference)
                {
                    if (IsUncontrolledType(typeReference.Resolve(), null, out isDataNondeterministic))
                    {
                        invocationName = GetFullyQualifiedTypeName(typeReference);
                        isUncontrolledType = true;
                    }
                }
                else if ((instruction.OpCode == OpCodes.Newobj || instruction.OpCode == OpCodes.Call ||
                    instruction.OpCode == OpCodes.Callvirt) && instruction.Operand is MethodReference methodReference)
                {
                    if (IsUncontrolledType(methodReference.DeclaringType.Resolve(), methodReference, out isDataNondeterministic))
                    {
                        invocationName = GetFullyQualifiedMethodName(methodReference);
                        isUncontrolledType = true;
                    }
                }

                if (isUncontrolledType)
                {
                    this.LogWriter.LogDebug("............. [+] injected uncontrolled '{0}' invocation exception", invocationName);

                    var providerType = this.Method.Module.ImportReference(typeof(ExceptionProvider)).Resolve();
                    MethodReference providerMethod = isDataNondeterministic ?
                        providerType.Methods.FirstOrDefault(m => m.Name is nameof(ExceptionProvider.ThrowUncontrolledDataInvocationException)) :
                        providerType.Methods.FirstOrDefault(m => m.Name is nameof(ExceptionProvider.ThrowUncontrolledInvocationException));
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
        /// Checks if the specified type is not controlled. If the optional member is specified,
        /// then it also checks if the member is not controlled.
        /// </summary>
        private static bool IsUncontrolledType(TypeDefinition type, MemberReference member, out bool isDataNondeterministic)
        {
            isDataNondeterministic = false;
            if (type is null || !IsSystemType(type))
            {
                return false;
            }

            if (type.Namespace.StartsWith(typeof(System.Diagnostics.Process).Namespace))
            {
                if (type.Name is nameof(System.Diagnostics.Process) && member != null &&
                    (member.Name is nameof(System.Diagnostics.Process.Start) ||
                    member.Name is nameof(System.Diagnostics.Process.BeginErrorReadLine) ||
                    member.Name is nameof(System.Diagnostics.Process.BeginOutputReadLine) ||
                    member.Name is nameof(System.Diagnostics.Process.CancelErrorRead) ||
                    member.Name is nameof(System.Diagnostics.Process.CancelOutputRead) ||
                    member.Name is nameof(System.Diagnostics.Process.WaitForExit) ||
                    member.Name is nameof(System.Diagnostics.Process.WaitForInputIdle) ||
                    member.Name is nameof(System.Diagnostics.Process.EnterDebugMode) ||
                    member.Name is nameof(System.Diagnostics.Process.LeaveDebugMode) ||
                    member.Name is nameof(System.Diagnostics.Process.InitializeLifetimeService) ||
                    member.Name is nameof(System.Diagnostics.Process.Refresh) ||
                    member.Name is nameof(System.Diagnostics.Process.Close) ||
                    member.Name is nameof(System.Diagnostics.Process.Dispose) ||
                    member.Name is nameof(System.Diagnostics.Process.Kill)))
                {
                    return true;
                }
            }
            else if (type.Namespace.StartsWith(typeof(System.Threading.Tasks.Task).Namespace))
            {
                if (type.Name is nameof(System.Threading.Tasks.Task) && member != null &&
                    (member.Name is nameof(System.Threading.Tasks.Task.ContinueWith) ||
                    member.Name is nameof(System.Threading.Tasks.Task.Run) ||
                    member.Name is nameof(System.Threading.Tasks.Task.RunSynchronously)))
                {
                    return true;
                }
            }
            else if (type.Namespace.StartsWith(typeof(System.Threading.Thread).Namespace))
            {
                if (type.Name is nameof(System.Threading.Thread) && member != null &&
                    (member.Name is nameof(System.Threading.Thread.Interrupt) ||
                    member.Name is nameof(System.Threading.Thread.Suspend) ||
                    member.Name is nameof(System.Threading.Thread.Resume) ||
                    member.Name is nameof(System.Threading.Thread.BeginCriticalRegion) ||
                    member.Name is nameof(System.Threading.Thread.EndCriticalRegion) ||
                    member.Name is nameof(System.Threading.Thread.Abort) ||
                    member.Name is nameof(System.Threading.Thread.ResetAbort)))
                {
                    return true;
                }
                else if (type.Name is nameof(System.Threading.ThreadPool) && member != null &&
                    (member.Name is nameof(System.Threading.ThreadPool.QueueUserWorkItem) ||
                    member.Name is nameof(System.Threading.ThreadPool.UnsafeQueueUserWorkItem) ||
                    member.Name is nameof(System.Threading.ThreadPool.UnsafeQueueNativeOverlapped) ||
                    member.Name is nameof(System.Threading.ThreadPool.RegisterWaitForSingleObject) ||
                    member.Name is nameof(System.Threading.ThreadPool.UnsafeRegisterWaitForSingleObject)))
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
            else if (type.Namespace.StartsWith(typeof(System.DateTime).Namespace))
            {
#if NET
                if (type.Name is nameof(System.DateTime) && member != null &&
                    (member.Name is $"get_{nameof(System.DateTime.Now)}" ||
                    member.Name is $"get_{nameof(System.DateTime.Today)}" ||
                    member.Name is $"get_{nameof(System.DateTime.UtcNow)}"))
                {
#else
                if (type.Name is nameof(System.DateTime) && member != null &&

                    (member.Name == $"get_{nameof(System.DateTime.Now)}" ||
                    member.Name == $"get_{nameof(System.DateTime.Today)}" ||
                    member.Name == $"get_{nameof(System.DateTime.UtcNow)}"))
                {
#endif
                    isDataNondeterministic = true;
                    return true;
                }
                else if (type.Name is nameof(System.Guid) && member != null &&
                    member.Name is nameof(System.Guid.NewGuid))
                {
                    isDataNondeterministic = true;
                    return true;
                }
            }

            return false;
        }
    }
}
