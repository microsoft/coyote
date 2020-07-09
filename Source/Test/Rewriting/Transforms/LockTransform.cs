// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Specifications;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Microsoft.Coyote.Rewriting
{
    internal class LockTransform : AssemblyTransform
    {
        private ModuleDefinition Module;
        private TypeDefinition TypeDef;
        private MethodDefinition Method;
        private ILProcessor Processor;
        private List<SyncObjectMapping> Mapping;
        private const string MonitorClassName = "System.Threading.Monitor";

        /// <summary>
        /// Maintains a mapping from the "syncobject" used in Monitor::Enter to the
        /// actual instance of SynchronizedBlock that is now wrapping that sync object.
        /// </summary>
        private class SyncObjectMapping
        {
            internal FieldDefinition SyncObjectField;
            internal VariableDefinition SyncObjectVariable;
            internal VariableDefinition SyncBlockVariable;
        }

        /// <inheritdoc/>
        internal override void VisitModule(ModuleDefinition module)
        {
            this.Module = module;
        }

        /// <inheritdoc/>
        internal override void VisitType(TypeDefinition typeDef)
        {
            this.TypeDef = typeDef;
        }

        /// <inheritdoc/>
        internal override void VisitField(FieldDefinition field)
        {
        }

        /// <inheritdoc/>
        internal override void VisitMethod(MethodDefinition method)
        {
            this.Method = method;
            this.Processor = method.Body.GetILProcessor();
            this.Mapping = new List<SyncObjectMapping>();
        }

        internal override void VisitVariable(VariableDefinition variable)
        {
        }

        /// <inheritdoc/>
        internal override Instruction VisitInstruction(Instruction instruction)
        {
            if (this.Method == null)
            {
                return instruction;
            }

            if (instruction.OpCode == OpCodes.Call && instruction.Operand is MethodReference method)
            {
                const string PulseMethod = nameof(System.Threading.Monitor.Pulse);
                const string PulseAllMethod = nameof(System.Threading.Monitor.PulseAll);
                const string WaitMethod = nameof(System.Threading.Monitor.Wait);

                if (method.DeclaringType.FullName == MonitorClassName && method.Name == PulseMethod)
                {
                    Debug.WriteLine($"......... [-] call '{method}'");
                    var pulseMethod = this.Module.ImportReference(typeof(Microsoft.Coyote.Tasks.SynchronizedBlock).GetMethod(PulseMethod));
                    this.ReplaceLoadSyncBlock(instruction);
                    var newInstruction = Instruction.Create(OpCodes.Callvirt, pulseMethod);
                    Debug.WriteLine($"......... [+] call '{pulseMethod}'");
                    this.Processor.Replace(instruction, newInstruction);
                    instruction = newInstruction;
                }
                else if (method.DeclaringType.FullName == MonitorClassName && method.Name == PulseAllMethod)
                {
                    Debug.WriteLine($"......... [-] call '{method}'");
                    var pulseAllMethod = this.Module.ImportReference(typeof(Microsoft.Coyote.Tasks.SynchronizedBlock).GetMethod(PulseAllMethod));
                    this.ReplaceLoadSyncBlock(instruction);
                    var newInstruction = Instruction.Create(OpCodes.Callvirt, pulseAllMethod);
                    Debug.WriteLine($"......... [+] call '{pulseAllMethod}'");
                    this.Processor.Replace(instruction, newInstruction);
                    instruction = newInstruction;
                }
                else if (method.DeclaringType.FullName == MonitorClassName && method.Name == "Wait")
                {
                    // public static bool Wait(object obj);
                    // public static bool Wait(object obj, int millisecondsTimeout);
                    // public static bool Wait(object obj, int millisecondsTimeout, bool exitContext);
                    // public static bool Wait(object obj, TimeSpan timeout);
                    // public static bool Wait(object obj, TimeSpan timeout, bool exitContext);
                    if (method.Parameters.Count == 1)
                    {
                        Debug.WriteLine($"......... [-] call '{method}'");
                        var waitMethod = this.Module.ImportReference(typeof(Microsoft.Coyote.Tasks.SynchronizedBlock).GetMethod(WaitMethod, Array.Empty<Type>()));
                        this.ReplaceLoadSyncBlock(instruction);
                        var newInstruction = Instruction.Create(OpCodes.Callvirt, waitMethod);
                        Debug.WriteLine($"......... [+] call '{waitMethod}'");
                        this.Processor.Replace(instruction, newInstruction);
                        instruction = newInstruction;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            return instruction;
        }

        /// <inheritdoc/>
        internal override void VisitExceptionHandler(ExceptionHandler handler)
        {
            // a C# lock statement uses try/finally block, where Monitor.Enter is at the beginning of the Try
            // and Monitor.Exit is in the finally block.  If the Monitor does not follow this pattern then it
            // is probably something else.

            // The C# lock statement only has finally block, so there should be no CatchType.
            if (handler.CatchType == null && MatchLockEnter(handler.TryStart, handler.TryEnd))
            {
                if (MatchLockExit(handler.HandlerStart, handler.HandlerEnd))
                {
                    this.RewriteLock(handler);
                }
            }
        }

        /// <summary>
        /// Resolve the FieldDefinition referenced in the given load instruction if any.
        /// </summary>
        /// <returns>A FieldDefinition or null.</returns>
        private static FieldDefinition GetFieldDefinition(Instruction loadInstruction)
        {
            FieldDefinition fd = null;
            if (loadInstruction.Operand is FieldReference fr)
            {
                fd = fr.Resolve();
            }
            else if (loadInstruction.Operand is FieldDefinition fdef)
            {
                fd = fdef;
            }

            return fd;
        }

        /// <summary>
        /// Rewrite "ldarg.0, ldfld syncobject" with "ldloc.n" to load the local SynchronizedBlock instead
        /// of the sync object, because the SynchronizedBlock methods (Pulse, Wait, etc) are instance methods
        /// not static methods and they do not take the syncobject as an argument because the SynchronizedBlock
        /// stores the sync object so it doesn't need it here.
        /// </summary>
        internal void ReplaceLoadSyncBlock(Instruction methodCall)
        {
            Instruction loadSyncObject = methodCall.Previous;
            if (loadSyncObject.OpCode == OpCodes.Ldfld)
            {
                var fd = GetFieldDefinition(loadSyncObject);
                if (fd != null)
                {
                    var syncBlock = this.FindSyncBlockVar(fd);
                    if (syncBlock != null)
                    {
                        Instruction loadThis = loadSyncObject.Previous;
                        if (loadThis.OpCode == OpCodes.Ldarg_0)
                        {
                            this.Processor.Remove(loadThis);
                        }
                        else
                        {
                            throw new InvalidOperationException("Expecting load 'this' instruction here...");
                        }

                        this.Processor.Replace(loadSyncObject, CreateLoadOp(syncBlock));
                    }
                    else
                    {
                        // TODO: this code only works if the Pulse, PulseAll, or Wait method is called inside
                        // the same method containing the C# lock statement.  This is normally the case but if
                        // someone decided to get clever and call a helper method and that helper method calls
                        // Wait then I have a problem because the SynchronizedBlock local variable will not be
                        // available in that new method...
                        throw new NotImplementedException(string.Format(
                            "Cannot find the matching SynchronizedBlock for the synchronizing object '{0}' in {1}",
                            loadSyncObject, this.Method.FullName));
                    }
                }
            }
            else if (IsLoadOp(loadSyncObject.OpCode))
            {
                // TODO: can this happen?  I haven't seen it yet...
                throw new NotImplementedException("Monitor method called using local variable instead of ldfld");
            }
        }

        internal static bool MatchLockEnter(Instruction start, Instruction end)
        {
            if (CountInstructions(start, end) >= 3)
            {
                Instruction a = start;
                Instruction b = a.Next;
                Instruction c = b.Next;

                if (IsLoadOp(a.OpCode) && // C# always creates local variable for the sync object
                    b.OpCode == OpCodes.Ldloca_S && // the LockTaken boolean flag should be local
                    c.OpCode == OpCodes.Call && c.Operand is MethodReference method && method.DeclaringType.FullName == MonitorClassName && method.Name == "Enter")
                {
                    // looks like a C# lock then!
                    return true;
                }
            }

            return false;
        }

        internal static bool MatchLockExit(Instruction start, Instruction end)
        {
            if (CountInstructions(start, end) >= 4)
            {
                Instruction a = start;
                Instruction b = a.Next;
                Instruction c = b.Next;
                Instruction d = c.Next;

                if (IsLoadOp(a.OpCode) && // checking the LockTaken boolean flag
                    b.OpCode == OpCodes.Brfalse_S && // no exit if we didn't get the lock!
                    IsLoadOp(c.OpCode) && // loading the sync object
                    d.OpCode == OpCodes.Call && d.Operand is MethodReference method && method.DeclaringType.FullName == MonitorClassName && method.Name == "Exit")
                {
                    // looks like a C# lock release then!
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Replaces Monitor.Enter with SynchronizedBlock.Lock and Monitor.Exit with SynchronizedBlock.Dispose().
        /// </summary>
        private void RewriteLock(ExceptionHandler handler)
        {
            // Then we can replace the LockTaken boolean local variable with a new SynchronizedBlock object and all this
            // happens outside the try statement, so InsertBefore the start of the try block.
            Instruction a = handler.TryStart; // local variable for the sync object
            Instruction b = a.Next; // the LockTaken boolean flag should be local
            Instruction c = b.Next; // the Monitor.Enter call.

            var syncObjectVar = this.GetLocalVariable(a.OpCode, a.Operand);
            var lockIndexVar = this.GetLocalVariable(b.OpCode, b.Operand);

            // creates: using(var m = SynchronizedBlock.Lock(syncObject)) { ...

            Debug.WriteLine($"......... [-] variable '{lockIndexVar.VariableType}'");
            Debug.WriteLine($"......... [-] call '{c}'");
            lockIndexVar.VariableType = this.Module.ImportReference(typeof(Microsoft.Coyote.Tasks.SynchronizedBlock)); // re-purpose this variable.
            this.InitializeNull(lockIndexVar);
            Debug.WriteLine($"......... [+] variable '{lockIndexVar.VariableType}'");

            Instruction load_sync_object = CreateLoadOp(syncObjectVar);
            this.Processor.InsertAfter(a.Previous, load_sync_object);

            var lockMethod = this.Module.ImportReference(typeof(Microsoft.Coyote.Tasks.SynchronizedBlock).GetMethod("Lock"));

            Instruction create_sync_block = Instruction.Create(OpCodes.Call, lockMethod);

            Debug.WriteLine($"......... [+] call '{create_sync_block}'");
            this.Processor.InsertAfter(load_sync_object, create_sync_block);

            Instruction stfield = CreateStoreOp(lockIndexVar);
            this.Processor.InsertAfter(create_sync_block, stfield);

            this.AddMapping(syncObjectVar, lockIndexVar);

            // and now we can remove the first 3 instructions of the try block that were calling Monitor.Enter.

            this.Processor.Remove(a);
            this.Processor.Remove(b);
            this.Processor.Remove(c);

            // fix the finally block.
            Instruction d = handler.HandlerStart; // ldloc LockTaken becomes ldloc SynchronizedBlock object
            Instruction e = d.Next; // brfalse, this remains the same
            Instruction f = e.Next; // ldloc sync object, becomes ldloc syncblock variable.
            Instruction g = f.Next; // call Monitor.Exit.

            // Create: m.Dispose()
            Instruction load_sync_block = CreateLoadOp(lockIndexVar); // remember lockIndexVar has been re-purposed to store the SynchronizedBlock object
            this.Processor.Replace(f, load_sync_block); // we need to load the object we are calling Dispose on. not the sync object
            var disposeMethod = this.Module.ImportReference(typeof(System.IDisposable).GetMethod("Dispose"));
            Instruction dispose = Instruction.Create(OpCodes.Callvirt, disposeMethod);

            Debug.WriteLine($"......... [-] call '{g}'");
            Debug.WriteLine($"......... [+] call '{dispose}'");
            this.Processor.Replace(g, dispose);
        }

        /// <summary>
        /// Make sure a variable is initialized to null (when variable was a bool it was initialized to integer 0 instead).
        /// </summary>
        private void InitializeNull(VariableDefinition v)
        {
            Instruction instruction = this.Method.Body.Instructions.FirstOrDefault();
            while (instruction != null)
            {
                var op = instruction.OpCode;
                if (IsStoreOp(op))
                {
                    VariableDefinition v2 = this.GetLocalVariable(op, instruction.Operand);
                    if (v2.Index == v.Index)
                    {
                        // found the store operation for this local variable, so make sure it is initialized to null
                        if (instruction.Previous != null && instruction.Previous.OpCode != OpCodes.Ldnull)
                        {
                            this.Processor.Replace(instruction.Previous, Instruction.Create(OpCodes.Ldnull));
                            break;
                        }
                    }
                }

                instruction = instruction.Next;
            }
        }

        /// <summary>
        /// Remember the connection between syncObject and SynchronizedBlock and find which
        /// FieldDefinition stores the syncObject.
        /// </summary>
        private void AddMapping(VariableDefinition syncObjectVar, VariableDefinition syncBlockVar)
        {
            // find the ldarg, ldfld, stloc.x for the syncObject so we know what it's FieldDefinition is.
            var mapping = new SyncObjectMapping()
            {
                SyncObjectVariable = syncObjectVar,
                SyncBlockVariable = syncBlockVar
            };

            Instruction instruction = this.Method.Body.Instructions.FirstOrDefault();
            while (instruction != null)
            {
                var op = instruction.OpCode;
                if (IsStoreOp(op))
                {
                    VariableDefinition v = this.GetLocalVariable(op, instruction.Operand);
                    if (v.Index == syncObjectVar.Index)
                    {
                        // found the store operation for this local variable!
                        if (instruction.Previous != null && instruction.Previous.OpCode == OpCodes.Ldfld)
                        {
                            var fd = GetFieldDefinition(instruction.Previous);
                            if (fd != null)
                            {
                                mapping.SyncObjectField = fd;
                                break;
                            }
                        }
                    }
                }

                instruction = instruction.Next;
            }

            if (mapping.SyncObjectField == null)
            {
                // TODO: hmmm, perhaps the location of the sync object is more complicated...
            }

            this.Mapping.Add(mapping);
        }

        private VariableDefinition FindSyncBlockVar(FieldDefinition fd)
        {
            foreach (var item in this.Mapping)
            {
                if (item.SyncObjectField == fd)
                {
                    return item.SyncBlockVariable;
                }
            }

            Debug.WriteLine("### Cannot find SynchronizedBlock created for this Synchronizing Object");
            return null;
        }

        internal static bool IsLoadOp(OpCode op)
        {
            return op == OpCodes.Ldloc_S || op == OpCodes.Ldloc_0 || op == OpCodes.Ldloc_1 || op == OpCodes.Ldloc_2 || op == OpCodes.Ldloc_3;
        }

        internal static bool IsStoreOp(OpCode op)
        {
            return op == OpCodes.Stloc_S || op == OpCodes.Stloc_0 || op == OpCodes.Stloc_1 || op == OpCodes.Stloc_2 || op == OpCodes.Stloc_3;
        }

        internal VariableDefinition GetLocalVariable(OpCode op, object operand)
        {
            if ((op == OpCodes.Ldloc || op == OpCodes.Ldloc_S || op == OpCodes.Ldloca_S || op == OpCodes.Stloc_S || op == OpCodes.Stloc) && operand is VariableDefinition vdef)
            {
                return vdef;
            }

            if (op == OpCodes.Ldloc_0 || op == OpCodes.Stloc_0)
            {
                return this.Method.Body.Variables[0];
            }

            if (op == OpCodes.Ldloc_1 || op == OpCodes.Stloc_1)
            {
                return this.Method.Body.Variables[1];
            }

            if (op == OpCodes.Ldloc_2 || op == OpCodes.Stloc_2)
            {
                return this.Method.Body.Variables[2];
            }

            if (op == OpCodes.Ldloc_3 || op == OpCodes.Stloc_3)
            {
                return this.Method.Body.Variables[3];
            }

            throw new InvalidOperationException();
        }

        private static Instruction CreateLoadOp(VariableDefinition var)
        {
            switch (var.Index)
            {
                case 0:
                    return Instruction.Create(OpCodes.Ldloc_0);
                case 1:
                    return Instruction.Create(OpCodes.Ldloc_1);
                case 2:
                    return Instruction.Create(OpCodes.Ldloc_2);
                case 3:
                    return Instruction.Create(OpCodes.Ldloc_3);
                default:
                    return Instruction.Create(OpCodes.Ldloc, var);
            }
        }

        internal static Instruction CreateStoreOp(VariableDefinition var)
        {
            switch (var.Index)
            {
                case 0:
                    return Instruction.Create(OpCodes.Stloc_0);
                case 1:
                    return Instruction.Create(OpCodes.Stloc_1);
                case 2:
                    return Instruction.Create(OpCodes.Stloc_2);
                case 3:
                    return Instruction.Create(OpCodes.Stloc_3);
                default:
                    return Instruction.Create(OpCodes.Stloc_S, var);
            }
        }

        internal static int CountInstructions(Instruction start, Instruction end)
        {
            int count = 0;
            while (start != end)
            {
                count++;
                start = start.Next;
            }

            return count;
        }
    }
}
