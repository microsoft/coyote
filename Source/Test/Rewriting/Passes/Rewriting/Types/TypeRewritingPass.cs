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
            this.KnownTypes[NameCache.AsyncTaskMethodBuilder] =
                typeof(Runtime.CompilerServices.AsyncTaskMethodBuilder);
            this.KnownTypes[NameCache.GenericAsyncTaskMethodBuilder] =
                typeof(Runtime.CompilerServices.AsyncTaskMethodBuilder<>);
            this.KnownTypes[NameCache.TaskAwaiter] = typeof(Runtime.CompilerServices.TaskAwaiter);
            this.KnownTypes[NameCache.GenericTaskAwaiter] = typeof(Runtime.CompilerServices.TaskAwaiter<>);
            this.KnownTypes[NameCache.ConfiguredTaskAwaitable] =
                typeof(Runtime.CompilerServices.ConfiguredTaskAwaitable);
            this.KnownTypes[NameCache.GenericConfiguredTaskAwaitable] =
                typeof(Runtime.CompilerServices.ConfiguredTaskAwaitable<>);
            this.KnownTypes[NameCache.ConfiguredTaskAwaiter] =
                typeof(Runtime.CompilerServices.ConfiguredTaskAwaitable);
            this.KnownTypes[NameCache.GenericConfiguredTaskAwaiter] =
                typeof(Runtime.CompilerServices.ConfiguredTaskAwaitable<>);

            // Populate the map with the default task-based types.
            this.KnownTypes[NameCache.Task] = typeof(Types.Threading.Tasks.Task);
            this.KnownTypes[NameCache.GenericTask] = typeof(Types.Threading.Tasks.Task<>);
            this.KnownTypes[NameCache.GenericTaskCompletionSource] =
                typeof(Types.Threading.Tasks.TaskCompletionSource<>);
            this.KnownTypes[NameCache.TaskExtensions] = typeof(Types.TaskExtensions);
            this.KnownTypes[NameCache.TaskFactory] = typeof(Types.Threading.Tasks.TaskFactory);
            this.KnownTypes[NameCache.GenericTaskFactory] = typeof(Types.Threading.Tasks.TaskFactory<>);
            this.KnownTypes[NameCache.TaskParallel] = typeof(Types.Threading.Tasks.Parallel);

            // Populate the map with the known synchronization types.
            this.KnownTypes[NameCache.Monitor] = typeof(Types.Threading.Monitor);

            if (options.IsRewritingConcurrentCollections)
            {
                this.KnownTypes[NameCache.ConcurrentBag] = typeof(Types.Collections.Concurrent.ConcurrentBag<>);
                this.KnownTypes[NameCache.ConcurrentDictionary] = typeof(Types.Collections.Concurrent.ConcurrentDictionary<,>);
                this.KnownTypes[NameCache.ConcurrentQueue] = typeof(Types.Collections.Concurrent.ConcurrentQueue<>);
                this.KnownTypes[NameCache.ConcurrentStack] = typeof(Types.Collections.Concurrent.ConcurrentStack<>);
            }

            if (options.IsDataRaceCheckingEnabled)
            {
                this.KnownTypes[NameCache.GenericList] = typeof(Types.Collections.Generic.List<>);
                this.KnownTypes[NameCache.GenericDictionary] = typeof(Types.Collections.Generic.Dictionary<,>);
                this.KnownTypes[NameCache.GenericHashSet] = typeof(Types.Collections.Generic.HashSet<>);
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
