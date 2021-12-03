// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Rewriting.Types;
using Mono.Cecil;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// A pass that rewrites types.
    /// </summary>
    internal abstract class TypeRewritingPass : RewritingPass
    {
        /// <summary>
        /// Map from known full type names to types.
        /// </summary>
        private readonly Dictionary<string, Type> KnownTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeRewritingPass"/> class.
        /// </summary>
        internal TypeRewritingPass(RewritingOptions options, IEnumerable<AssemblyInfo> visitedAssemblies, ILogger logger)
            : base(visitedAssemblies, logger)
        {
            this.KnownTypes = new Dictionary<string, Type>();

            // Populate the map with the known compiler types.
            this.KnownTypes[NameCache.AsyncTaskMethodBuilderFullName] =
                typeof(Runtime.CompilerServices.AsyncTaskMethodBuilder);
            this.KnownTypes[NameCache.GenericAsyncTaskMethodBuilderFullName] =
                typeof(Runtime.CompilerServices.AsyncTaskMethodBuilder<>);
            this.KnownTypes[NameCache.TaskAwaiterFullName] = typeof(Runtime.CompilerServices.TaskAwaiter);
            this.KnownTypes[NameCache.GenericTaskAwaiterFullName] = typeof(Runtime.CompilerServices.TaskAwaiter<>);
            this.KnownTypes[NameCache.ConfiguredTaskAwaitableFullName] =
                typeof(Runtime.CompilerServices.ConfiguredTaskAwaitable);
            this.KnownTypes[NameCache.GenericConfiguredTaskAwaitableFullName] =
                typeof(Runtime.CompilerServices.ConfiguredTaskAwaitable<>);
            this.KnownTypes[NameCache.ConfiguredTaskAwaiterFullName] =
                typeof(Runtime.CompilerServices.ConfiguredTaskAwaitable);
            this.KnownTypes[NameCache.GenericConfiguredTaskAwaiterFullName] =
                typeof(Runtime.CompilerServices.ConfiguredTaskAwaitable<>);

            // Populate the map with the default task-based types.
            this.KnownTypes[NameCache.TaskFullName] = typeof(Types.Threading.Tasks.Task);
            this.KnownTypes[NameCache.GenericTaskFullName] = typeof(Types.Threading.Tasks.Task<>);
            this.KnownTypes[NameCache.GenericTaskCompletionSourceFullName] =
                typeof(Types.Threading.Tasks.TaskCompletionSource<>);
            this.KnownTypes[NameCache.TaskExtensionsFullName] = typeof(Types.TaskExtensions);
            this.KnownTypes[NameCache.TaskFactoryFullName] = typeof(Types.Threading.Tasks.TaskFactory);
            this.KnownTypes[NameCache.GenericTaskFactoryFullName] = typeof(Types.Threading.Tasks.TaskFactory<>);
            this.KnownTypes[NameCache.TaskParallelFullName] = typeof(Types.Threading.Tasks.Parallel);

            // Populate the map with the known synchronization types.
            this.KnownTypes[NameCache.MonitorFullName] = typeof(Types.Threading.Monitor);

            if (options.IsRewritingConcurrentCollections)
            {
                this.KnownTypes[NameCache.ConcurrentBagFullName] = typeof(Types.Collections.Concurrent.ControlledConcurrentBag);
                this.KnownTypes[NameCache.ConcurrentDictionaryFullName] =
                    typeof(Types.Collections.Concurrent.ControlledConcurrentDictionary);
                this.KnownTypes[NameCache.ConcurrentQueueFullName] = typeof(Types.Collections.Concurrent.ControlledConcurrentQueue);
                this.KnownTypes[NameCache.ConcurrentStackFullName] = typeof(Types.Collections.Concurrent.ControlledConcurrentStack);
            }

            if (options.IsDataRaceCheckingEnabled)
            {
                this.KnownTypes[NameCache.GenericListFullName] = typeof(Types.Collections.Generic.List);
                this.KnownTypes[NameCache.GenericDictionaryFullName] = typeof(Types.Collections.Generic.Dictionary);
                this.KnownTypes[NameCache.GenericHashSetFullName] = typeof(Types.Collections.Generic.HashSet);
            }
        }

        /// <summary>
        /// Tries to return the rewritten type for the specified type, or returns false
        /// if there is nothing to rewrite.
        /// </summary>
        protected bool TryRewriteType(TypeReference type, out TypeReference result)
        {
            result = this.RewriteType(type);
            return result.FullName != type.FullName;
        }

        /// <summary>
        /// Returns the rewritten type for the specified type, or returns the original
        /// if there is nothing to rewrite.
        /// </summary>
        private TypeReference RewriteType(TypeReference type)
        {
            TypeReference result = type;

            string fullName = type.FullName;
            if (type is GenericInstanceType genericType)
            {
                TypeReference elementType = this.RewriteType(genericType.ElementType);
                result = this.RewriteType(genericType, elementType);
                result = this.Module.ImportReference(result);
            }
            else
            {
                if (this.KnownTypes.TryGetValue(fullName, out Type newType))
                {
                    result = newType.IsGenericType ?
                        this.Module.ImportReference(newType, type) :
                        this.Module.ImportReference(newType);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the rewritten type for the specified generic type, or returns the original
        /// if there is nothing to rewrite.
        /// </summary>
        private TypeReference RewriteType(GenericInstanceType type, TypeReference elementType)
        {
            GenericInstanceType result = type;
            if (type.ElementType.FullName != elementType.FullName)
            {
                // Try to rewrite the arguments of the generic type.
                result = this.Module.ImportReference(elementType) as GenericInstanceType;
                for (int idx = 0; idx < type.GenericArguments.Count; idx++)
                {
                    result.GenericArguments[idx] = this.RewriteType(type.GenericArguments[idx]);
                }
            }

            return this.Module.ImportReference(result);
        }

        /// <summary>
        /// Checks if the specified type is a rewritable type.
        /// </summary>
        protected virtual bool IsRewritableType(TypeDefinition type) =>
            IsSystemType(type) || this.IsSupportedType(type) || this.IsVisitedType(type);

        /// <summary>
        /// Checks if the specified type is a supported type.
        /// </summary>
        protected virtual bool IsSupportedType(TypeDefinition type)
        {
            if (type != null)
            {
                string modulePath = Path.GetFileName(type.Module.FileName);
                if (modulePath is "Microsoft.AspNetCore.Mvc.Testing.dll")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the specified type is a static type.
        /// </summary>
        protected static bool IsStaticType(TypeDefinition type) => type.IsSealed && type.IsAbstract;
    }
}
