// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;
#if NET
using HttpClient = Microsoft.Coyote.Rewriting.Types.Net.Http.HttpClient;
#endif
using NameCache = Microsoft.Coyote.Rewriting.Types.NameCache;
using TaskAwaiter = Microsoft.Coyote.Runtime.CompilerServices.TaskAwaiter;
using ValueTaskAwaiter = Microsoft.Coyote.Runtime.CompilerServices.ValueTaskAwaiter;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// Rewriting pass for invocations between assemblies.
    /// </summary>
    internal class InterAssemblyInvocationRewritingPass : RewritingPass
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InterAssemblyInvocationRewritingPass"/> class.
        /// </summary>
        internal InterAssemblyInvocationRewritingPass(IEnumerable<AssemblyInfo> visitedAssemblies, LogWriter logWriter)
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

            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                instruction.Operand is MethodReference methodReference &&
                this.IsForeignType(methodReference.DeclaringType))
            {
                TypeDefinition resolvedReturnType = methodReference.ReturnType.Resolve();
                if (IsTaskType(resolvedReturnType, NameCache.TaskName, NameCache.SystemTasksNamespace))
                {
                    string methodName = GetFullyQualifiedMethodName(methodReference);
                    Instruction nextInstruction = instruction.Next;
                    MethodReference interceptionMethod = this.CreateInterceptionMethod(
                        typeof(ExceptionProvider), methodReference,
                        nameof(ExceptionProvider.ThrowIfReturnedTaskNotControlled));
                    TypeReference interceptedReturnType = this.CreateInterceptedReturnType(methodReference);
                    var instructions = this.CreateInterceptionMethodCallInstructions(
                        interceptionMethod, nextInstruction, interceptedReturnType, methodName);
                    if (instructions.Count > 0)
                    {
                        this.LogWriter.LogDebug("............. [+] uncontrolled task assertion when invoking '{0}'", methodName);
                        instructions.ForEach(i => this.Processor.InsertBefore(nextInstruction, i));
                        this.IsMethodBodyModified = true;
                    }
                }
                else if (IsTaskType(resolvedReturnType, NameCache.ValueTaskName, NameCache.SystemTasksNamespace))
                {
                    string methodName = GetFullyQualifiedMethodName(methodReference);
                    Instruction nextInstruction = instruction.Next;
                    MethodReference interceptionMethod = this.CreateInterceptionMethod(
                        typeof(ExceptionProvider), methodReference,
                        nameof(ExceptionProvider.ThrowIfReturnedValueTaskNotControlled));
                    TypeReference interceptedReturnType = this.CreateInterceptedReturnType(methodReference);
                    var instructions = this.CreateInterceptionMethodCallInstructions(
                        interceptionMethod, nextInstruction, interceptedReturnType, methodName);
                    if (instructions.Count > 0)
                    {
                        this.LogWriter.LogDebug("............. [+] uncontrolled value task assertion when invoking '{0}'", methodName);
                        instructions.ForEach(i => this.Processor.InsertBefore(nextInstruction, i));
                        this.IsMethodBodyModified = true;
                    }
                }
                else if (methodReference.Name is "GetAwaiter" && IsTaskType(resolvedReturnType,
                    NameCache.TaskAwaiterName, NameCache.SystemCompilerNamespace))
                {
                    MethodReference interceptionMethod = this.CreateInterceptionMethod(
                        typeof(TaskAwaiter), methodReference,
                        nameof(TaskAwaiter.Wrap));
                    Instruction newInstruction = Instruction.Create(OpCodes.Call, interceptionMethod);
                    this.LogWriter.LogDebug("............. [+] {0}", newInstruction);

                    this.Processor.InsertAfter(instruction, newInstruction);
                    this.IsMethodBodyModified = true;
                }
                else if (methodReference.Name is "GetAwaiter" && IsTaskType(resolvedReturnType,
                    NameCache.ValueTaskAwaiterName, NameCache.SystemCompilerNamespace))
                {
                    MethodReference interceptionMethod = this.CreateInterceptionMethod(
                        typeof(ValueTaskAwaiter), methodReference,
                        nameof(ValueTaskAwaiter.Wrap));
                    Instruction newInstruction = Instruction.Create(OpCodes.Call, interceptionMethod);
                    this.LogWriter.LogDebug("............. [+] {0}", newInstruction);

                    this.Processor.InsertAfter(instruction, newInstruction);
                    this.IsMethodBodyModified = true;
                }
#if NET
                else if (IsSystemType(resolvedReturnType) && resolvedReturnType.FullName == NameCache.HttpClient)
                {
                    MethodReference interceptionMethod = this.CreateInterceptionMethod(
                        typeof(HttpClient), methodReference, nameof(HttpClient.Control));
                    Instruction newInstruction = Instruction.Create(OpCodes.Call, interceptionMethod);
                    this.LogWriter.LogDebug("............. [+] {0}", newInstruction);

                    this.Processor.InsertAfter(instruction, newInstruction);
                    this.IsMethodBodyModified = true;
                }
#endif
            }

            return instruction;
        }

        /// <summary>
        /// Creates the IL instructions for invoking the specified interception method.
        /// </summary>
        private List<Instruction> CreateInterceptionMethodCallInstructions(MethodReference interceptionMethod,
            Instruction nextInstruction, TypeReference returnType, string methodName)
        {
            var instructions = new List<Instruction>();
            bool isParamByReference = interceptionMethod.Parameters[0].ParameterType.IsByReference;
            if (nextInstruction.OpCode == OpCodes.Stsfld &&
                nextInstruction.Operand is FieldReference fieldReference)
            {
                OpCode loadOpCode = isParamByReference ? OpCodes.Ldsflda : OpCodes.Ldsfld;
                instructions.Add(this.Processor.Create(nextInstruction.OpCode, fieldReference));
                instructions.Add(this.Processor.Create(loadOpCode, fieldReference));
            }
            else if (nextInstruction.OpCode == OpCodes.Starg_S &&
                nextInstruction.Operand is ParameterDefinition parameterDefinition)
            {
                OpCode loadOpCode = isParamByReference ? OpCodes.Ldarga_S :
                    parameterDefinition.Index is 0 ? OpCodes.Ldarg_0 :
                    parameterDefinition.Index is 1 ? OpCodes.Ldarg_1 :
                    parameterDefinition.Index is 2 ? OpCodes.Ldarg_2 :
                    parameterDefinition.Index is 3 ? OpCodes.Ldarg_3 :
                    OpCodes.Ldarg_S;
                instructions.Add(this.Processor.Create(nextInstruction.OpCode, parameterDefinition));
                instructions.Add(loadOpCode == OpCodes.Ldarga_S || loadOpCode == OpCodes.Ldarg_S ?
                    this.Processor.Create(loadOpCode, parameterDefinition) :
                    this.Processor.Create(loadOpCode));
            }
            else if (nextInstruction.OpCode == OpCodes.Stloc_S &&
                nextInstruction.Operand is VariableDefinition variableDefinition)
            {
                OpCode loadOpCode = isParamByReference ? OpCodes.Ldloca_S : OpCodes.Ldloc_S;
                instructions.Add(this.Processor.Create(nextInstruction.OpCode, variableDefinition));
                instructions.Add(this.Processor.Create(loadOpCode, variableDefinition));
            }
            else if (nextInstruction.OpCode == OpCodes.Stloc_0)
            {
                OpCode loadOpCode = isParamByReference ? OpCodes.Ldloca_S : OpCodes.Ldloc_0;
                instructions.Add(this.Processor.Create(nextInstruction.OpCode));
                instructions.Add(isParamByReference ? this.Processor.Create(loadOpCode, (byte)0) :
                    this.Processor.Create(loadOpCode));
            }
            else if (nextInstruction.OpCode == OpCodes.Stloc_1)
            {
                OpCode loadOpCode = isParamByReference ? OpCodes.Ldloca_S : OpCodes.Ldloc_1;
                instructions.Add(this.Processor.Create(nextInstruction.OpCode));
                instructions.Add(isParamByReference ? this.Processor.Create(loadOpCode, (byte)1) :
                    this.Processor.Create(loadOpCode));
            }
            else if (nextInstruction.OpCode == OpCodes.Stloc_2)
            {
                OpCode loadOpCode = isParamByReference ? OpCodes.Ldloca_S : OpCodes.Ldloc_2;
                instructions.Add(this.Processor.Create(nextInstruction.OpCode));
                instructions.Add(isParamByReference ? this.Processor.Create(loadOpCode, (byte)2) :
                    this.Processor.Create(loadOpCode));
            }
            else if (nextInstruction.OpCode == OpCodes.Stloc_3)
            {
                OpCode loadOpCode = isParamByReference ? OpCodes.Ldloca_S : OpCodes.Ldloc_3;
                instructions.Add(this.Processor.Create(nextInstruction.OpCode));
                instructions.Add(isParamByReference ? this.Processor.Create(loadOpCode, (byte)3) :
                    this.Processor.Create(loadOpCode));
            }
            else
            {
                instructions.Add(this.Processor.Create(OpCodes.Ldstr, methodName));
                instructions.Add(this.Processor.Create(OpCodes.Call, interceptionMethod));
                return instructions;
            }

            instructions.Add(this.Processor.Create(OpCodes.Ldstr, methodName));
            instructions.Add(this.Processor.Create(OpCodes.Call, interceptionMethod));
            if (interceptionMethod.ReturnType.IsByReference)
            {
                instructions.Add(this.Processor.Create(OpCodes.Ldobj, returnType));
            }

            return instructions;
        }

        /// <summary>
        /// Creates an interception method from the specified method and type.
        /// </summary>
        private MethodReference CreateInterceptionMethod(Type type, MethodReference methodReference,
            string interceptionMethodName)
        {
            var returnType = methodReference.ReturnType;
            TypeDefinition providerType = this.Module.ImportReference(type).Resolve();
            MethodReference wrapMethod = null;
            if (returnType is GenericInstanceType genericType)
            {
                GenericInstanceType resolvedType = ResolveGenericTypeArguments(genericType, methodReference);
                TypeReference argType = resolvedType.HasGenericArguments ?
                    resolvedType.GenericArguments.FirstOrDefault() :
                    resolvedType;
                MethodDefinition genericMethod = providerType.Methods.FirstOrDefault(
                    m => m.Name == interceptionMethodName && m.HasGenericParameters);
                MethodReference wrapReference = this.Module.ImportReference(genericMethod);
                wrapMethod = MakeGenericMethod(wrapReference, argType);
            }
            else
            {
                wrapMethod = providerType.Methods.FirstOrDefault(m => m.Name == interceptionMethodName);
            }

            return this.Module.ImportReference(wrapMethod);
        }

        /// <summary>
        /// Creates an intercepted return type from the specified method.
        /// </summary>
        private TypeReference CreateInterceptedReturnType(MethodReference methodReference)
        {
            var returnType = methodReference.ReturnType;
            if (returnType is GenericInstanceType genericType)
            {
                GenericInstanceType resolvedType = ResolveGenericTypeArguments(genericType, methodReference);
                TypeReference argType = resolvedType.HasGenericArguments ?
                    resolvedType.GenericArguments.FirstOrDefault() :
                    resolvedType;
                returnType = MakeGenericType(genericType.ElementType, argType);
                returnType = this.Module.ImportReference(returnType);
            }

            return returnType;
        }

        /// <summary>
        /// Resolves the generic arguments of the specified type using the given method reference.
        /// </summary>
        private static GenericInstanceType ResolveGenericTypeArguments(GenericInstanceType genericType, MethodReference methodReference)
        {
            GenericInstanceType resolvedType = new GenericInstanceType(genericType.ElementType);
            foreach (var genericArgument in genericType.GenericArguments)
            {
                TypeReference argType = ResolveArgumentType(genericArgument, methodReference);
                resolvedType.GenericArguments.Add(argType);
            }

            return resolvedType;
        }

        /// <summary>
        /// Resolves the specified argument type using the given method reference.
        /// </summary>
        private static TypeReference ResolveArgumentType(TypeReference argType, MethodReference methodReference)
        {
            if (argType is GenericParameter gp)
            {
                if (gp.Type is GenericParameterType.Type &&
                    methodReference.DeclaringType is GenericInstanceType dgt)
                {
                    argType = dgt.GenericArguments[gp.Position];
                }
                else if (gp.Type is GenericParameterType.Method &&
                    methodReference is GenericInstanceMethod gim)
                {
                    argType = gim.GenericArguments[gp.Position];
                }
            }
            else if (argType is ArrayType at)
            {
                TypeReference newElementType = ResolveArgumentType(at.ElementType, methodReference);
                argType = new ArrayType(newElementType, at.Rank);
            }
            else if (argType is GenericInstanceType git)
            {
                argType = ResolveGenericTypeArguments(git, methodReference);
            }

            return argType;
        }

        /// <summary>
        /// Checks if the specified type is the expected task type.
        /// </summary>
        private static bool IsTaskType(TypeDefinition type, string expectedName, string expectedNamespace)
        {
            if (type != null)
            {
                if (IsSystemType(type) && type.Namespace == expectedNamespace &&
                    (type.Name == expectedName || type.Name.StartsWith(expectedName + "`")))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the specified type is a task-like type.
        /// </summary>
        private static bool IsTaskLikeType(TypeDefinition type)
        {
            if (type is null)
            {
                return false;
            }

            var interfaceTypes = type.Interfaces.Select(i => i.InterfaceType);
            if (!interfaceTypes.Any(
                i => i.FullName is "System.Runtime.CompilerServices.INotifyCompletion" ||
                i.FullName is "System.Runtime.CompilerServices.INotifyCompletion"))
            {
                return false;
            }

            if (type.Methods.Any(m => m.Name is "get_IsCompleted"))
            {
                return true;
            }

            return false;
        }
    }
}
