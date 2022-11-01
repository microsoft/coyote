// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote.Logging;
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
        internal TypeRewritingPass(RewritingOptions options, IEnumerable<AssemblyInfo> visitedAssemblies, LogWriter logWriter)
            : base(visitedAssemblies, logWriter)
        {
            this.KnownTypes = new Dictionary<string, Type>();

            // Populate the map with the known compiler types.
            this.KnownTypes[NameCache.AsyncTaskMethodBuilder] =
                typeof(Types.Runtime.CompilerServices.AsyncTaskMethodBuilder);
            this.KnownTypes[NameCache.GenericAsyncTaskMethodBuilder] =
                typeof(Types.Runtime.CompilerServices.AsyncTaskMethodBuilder<>);
            this.KnownTypes[NameCache.AsyncValueTaskMethodBuilder] =
                typeof(Types.Runtime.CompilerServices.AsyncValueTaskMethodBuilder);
            this.KnownTypes[NameCache.GenericAsyncValueTaskMethodBuilder] =
                typeof(Types.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<>);
            this.KnownTypes[NameCache.TaskAwaiter] = typeof(Runtime.CompilerServices.TaskAwaiter);
            this.KnownTypes[NameCache.GenericTaskAwaiter] = typeof(Runtime.CompilerServices.TaskAwaiter<>);
            this.KnownTypes[NameCache.ValueTaskAwaiter] = typeof(Runtime.CompilerServices.ValueTaskAwaiter);
            this.KnownTypes[NameCache.GenericValueTaskAwaiter] = typeof(Runtime.CompilerServices.ValueTaskAwaiter<>);
            this.KnownTypes[NameCache.ConfiguredTaskAwaitable] =
                typeof(Types.Runtime.CompilerServices.ConfiguredTaskAwaitable);
            this.KnownTypes[NameCache.ConfiguredTaskAwaiter] =
                typeof(Types.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter);
            this.KnownTypes[NameCache.GenericConfiguredTaskAwaitable] =
                typeof(Types.Runtime.CompilerServices.ConfiguredTaskAwaitable<>);
            this.KnownTypes[NameCache.GenericConfiguredTaskAwaiter] =
                typeof(Types.Runtime.CompilerServices.ConfiguredTaskAwaitable<>.ConfiguredTaskAwaiter);
            this.KnownTypes[NameCache.ConfiguredValueTaskAwaitable] =
                typeof(Types.Runtime.CompilerServices.ConfiguredValueTaskAwaitable);
            this.KnownTypes[NameCache.ConfiguredValueTaskAwaiter] =
                typeof(Types.Runtime.CompilerServices.ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter);
            this.KnownTypes[NameCache.GenericConfiguredValueTaskAwaitable] =
                typeof(Types.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<>);
            this.KnownTypes[NameCache.GenericConfiguredValueTaskAwaiter] =
                typeof(Types.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<>.ConfiguredValueTaskAwaiter);
            this.KnownTypes[NameCache.YieldAwaitable] =
                typeof(Types.Runtime.CompilerServices.YieldAwaitable);
            this.KnownTypes[NameCache.YieldAwaiter] =
                typeof(Types.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter);

            // Populate the map with the default task-based types.
            this.KnownTypes[NameCache.Task] = typeof(Types.Threading.Tasks.Task);
            this.KnownTypes[NameCache.GenericTask] = typeof(Types.Threading.Tasks.Task<>);
            this.KnownTypes[NameCache.ValueTask] = typeof(Types.Threading.Tasks.ValueTask);
            this.KnownTypes[NameCache.GenericValueTask] = typeof(Types.Threading.Tasks.ValueTask<>);
#if NET
            this.KnownTypes[NameCache.TaskCompletionSource] = typeof(Types.Threading.Tasks.TaskCompletionSource);
#endif
            this.KnownTypes[NameCache.GenericTaskCompletionSource] = typeof(Types.Threading.Tasks.TaskCompletionSource<>);
            this.KnownTypes[NameCache.TaskExtensions] = typeof(Types.TaskExtensions);
            this.KnownTypes[NameCache.TaskFactory] = typeof(Types.Threading.Tasks.TaskFactory);
            this.KnownTypes[NameCache.GenericTaskFactory] = typeof(Types.Threading.Tasks.TaskFactory<>);
            this.KnownTypes[NameCache.TaskParallel] = typeof(Types.Threading.Tasks.Parallel);

            // Populate the map with the default thread-based types.
            this.KnownTypes[NameCache.Thread] = typeof(Types.Threading.Thread);

            // Populate the map with the known synchronization types.
            this.KnownTypes[NameCache.Monitor] = typeof(Types.Threading.Monitor);
            this.KnownTypes[NameCache.SemaphoreSlim] = typeof(Types.Threading.SemaphoreSlim);

#if NET || NETCOREAPP3_1
            // Populate the map with the known HTTP and web-related types.
            this.KnownTypes[NameCache.HttpClient] = typeof(Types.Net.Http.HttpClient);
            this.KnownTypes[NameCache.HttpRequestMessage] = typeof(Types.Net.Http.HttpRequestMessage);
#endif

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
        /// Returns the rewritten type for the specified type and with the specified rewriting
        /// options, or returns the original type if there is nothing to rewrite.
        /// </summary>
        protected TypeReference RewriteType(TypeReference type, Options options)
        {
            this.TryRewriteType(type, options, out TypeReference result);
            return result;
        }

        /// <summary>
        /// Returns the rewritten type for the specified type and with the specified rewriting
        /// options, or returns the original type if there is nothing to rewrite.
        /// </summary>
        protected TypeReference RewriteType(TypeReference type, Options options, ref bool isRewritten)
        {
            isRewritten |= this.TryRewriteType(type, options, out TypeReference result);
            return result;
        }

        /// <summary>
        /// Tries to return the rewritten type for the specified type, or returns false
        /// if there is nothing to rewrite.
        /// </summary>
        protected bool TryRewriteType(TypeReference type, out TypeReference result) =>
            this.TryRewriteType(type, Options.None, out result);

        /// <summary>
        /// Tries to return the rewritten type for the specified type and with the specified
        /// rewriting options, or returns false if there is nothing to rewrite.
        /// </summary>
        private bool TryRewriteType(TypeReference type, Options options, out TypeReference result)
        {
            bool isRewritten = false;
            result = this.RewriteType(type, options, options.HasFlag(Options.SkipRootType), ref isRewritten);
            return isRewritten;
        }

        /// <summary>
        /// Returns the rewritten type for the specified type, or returns the original
        /// if there is nothing to rewrite.
        /// </summary>
        private TypeReference RewriteType(TypeReference type, Options options, bool onlyImport, ref bool isRewritten)
        {
            TypeReference result = type;
            if (type is ArrayType arrayType)
            {
                TypeReference newElementType = this.RewriteType(arrayType.ElementType, options, onlyImport, ref isRewritten);
                return new ArrayType(newElementType, arrayType.Rank);
            }
            else if (type is ByReferenceType refType)
            {
                TypeReference newElementType = this.RewriteType(refType.ElementType, options, onlyImport, ref isRewritten);
                return new ByReferenceType(newElementType);
            }
            else if (type is RequiredModifierType reqModType)
            {
                TypeReference newModifierType = this.RewriteType(reqModType.ModifierType, options, onlyImport, ref isRewritten);
                TypeReference newElementType = this.RewriteType(reqModType.ElementType, options, onlyImport, ref isRewritten);
                return new RequiredModifierType(newModifierType, newElementType);
            }
            else if (type is GenericInstanceType genericType)
            {
                TypeReference newElementType = this.RewriteAndImportType(genericType.ElementType, options,
                    onlyImport, ref isRewritten);
                GenericInstanceType newGenericType = newElementType as GenericInstanceType ??
                     new GenericInstanceType(newElementType);
                for (int idx = 0; idx < genericType.GenericArguments.Count; idx++)
                {
                    newGenericType.GenericArguments.Add(this.RewriteType(genericType.GenericArguments[idx],
                        options & ~Options.AllowStaticRewrittenType, false, ref isRewritten));
                }

                return newGenericType;
            }

            return this.RewriteAndImportType(type, options, onlyImport, ref isRewritten);
        }

        /// <summary>
        /// Returns the rewritten type for the specified type, or returns the original
        /// if there is nothing to rewrite.
        /// </summary>
        private TypeReference RewriteAndImportType(TypeReference type, Options options, bool onlyImport,
            ref bool isRewritten)
        {
            TypeReference result = type;
            if (!type.IsGenericParameter && !type.IsByReference)
            {
                // Rewrite the type if it is a known type. If the rewritten type is static,
                // then only allow if this is a declaring type.
                if (!onlyImport && this.KnownTypes.TryGetValue(type.FullName, out Type newType) &&
                    IsRewrittenTypeAllowed(newType, options))
                {
                    result = this.Module.ImportReference(newType);
                    isRewritten = true;
                }
                else if (type.Module != this.Module)
                {
                    // Import the type to the current module.
                    result = this.Module.ImportReference(type);
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if the specified rewritten type is allowed.
        /// </summary>
        private static bool IsRewrittenTypeAllowed(Type type, Options options) =>
            type.IsSealed && type.IsAbstract ? options.HasFlag(Options.AllowStaticRewrittenType) :
            true;

        /// <summary>
        /// Checks if the specified type is a rewritable type.
        /// </summary>
        protected virtual bool IsRewritableType(TypeDefinition type) =>
            IsSystemType(type) || this.IsSupportedType(type) || this.IsVisitedType(type);

        /// <summary>
        /// Checks if the specified type is a supported type.
        /// </summary>
        protected virtual bool IsSupportedType(TypeDefinition type) => false;

        /// <summary>
        /// Options for rewriting a type.
        /// </summary>
        [Flags]
        protected enum Options
        {
            None = 0,
            AllowStaticRewrittenType = 1 << 0,
            SkipRootType = 1 << 1
        }
    }
}
