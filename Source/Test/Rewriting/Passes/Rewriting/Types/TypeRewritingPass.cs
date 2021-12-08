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
                typeof(Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter);
            this.KnownTypes[NameCache.GenericConfiguredTaskAwaiter] =
                typeof(Runtime.CompilerServices.ConfiguredTaskAwaitable<>.ConfiguredTaskAwaiter);

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
        protected bool TryRewriteType(TypeReference type, out TypeReference result, bool allowStatic = false)
        {
            result = this.RewriteType(type, allowStatic);
            return result.FullName != type.FullName || result.Module != type.Module;
        }

        /// <summary>
        /// Returns the rewritten type for the specified type, or returns the original
        /// if there is nothing to rewrite.
        /// </summary>
        private TypeReference RewriteType(TypeReference type, bool allowStatic)
        {
            TypeReference result = type;
            // Console.WriteLine($"Rewriting type {type.FullName}");
            if (type is GenericInstanceType genericType)
            {
                // Console.WriteLine($"1-1: {genericType} ({genericType.Module})");
                TypeReference newElementType = this.RewriteType(genericType.ElementType, allowStatic);
                // Console.WriteLine($"1-2: {newElementType} ({newElementType.GenericParameters.Count})");
                GenericInstanceType newGenericType = newElementType as GenericInstanceType ??
                     new GenericInstanceType(newElementType);
                // GenericInstanceType newGenericType = newElementType.FullName == genericType.ElementType.FullName ?
                //     genericType : this.Module.ImportReference(newElementType) as GenericInstanceType;
                // GenericInstanceType newGenericType = this.MakeGenericType(newElementType, genericType.GenericArguments, genericType);
                // GenericInstanceType newGenericType = new GenericInstanceType(newElementType);
                // Console.WriteLine($"1-3: {newGenericType} ({newGenericType.Module})");
                // Console.WriteLine($"GenericParameters: {newGenericType.GenericParameters.Count}");
                // Console.WriteLine($"GenericArguments: {newGenericType.GenericArguments.Count}");

                for (int idx = 0; idx < genericType.GenericArguments.Count; idx++)
                {
                    newGenericType.GenericArguments.Add(this.RewriteType(genericType.GenericArguments[idx], allowStatic));
                }

                // Console.WriteLine($"1-4: {newGenericType} ({newGenericType.Module})");
                result = newGenericType;
                // result = this.Module.ImportReference(newGenericType, genericType);
                // result = newGenericType.Module != this.Module ?
                //     this.Module.ImportReference(newGenericType, genericType) :
                //     newGenericType;
                // Console.WriteLine($"1-5: {result} ({result.Module})");
            }
            else if (type is ArrayType arrayType)
            {
                // Console.WriteLine($"3-1: {arrayType} ({arrayType.Dimensions.Count})");
                foreach (var dimension in arrayType.Dimensions)
                {
                    // Console.WriteLine($"3-2: {dimension.IsSized} {dimension.LowerBound} {dimension.UpperBound}");
                }

                TypeReference newElementType = this.RewriteType(arrayType.ElementType, allowStatic);
                // Console.WriteLine($"3-2: {newElementType} ({newElementType.GenericParameters.Count})");
                ArrayType newArrayType = new ArrayType(newElementType, arrayType.Rank);
                foreach (var dimension in newArrayType.Dimensions)
                {
                    // Console.WriteLine($"3-3: {dimension.IsSized} {dimension.LowerBound} {dimension.UpperBound}");
                }

                // Console.WriteLine($"3-4: {newArrayType} ({newArrayType?.Module}) ({newArrayType.Dimensions.Count})");
                result = newArrayType;
            }
            else if (!type.IsGenericParameter && !type.IsByReference)
            {
                // Console.WriteLine($"2-1: {type.GetType()}");
                if (this.KnownTypes.TryGetValue(type.FullName, out Type newType) &&
                    (allowStatic || !newType.IsSealed || !newType.IsAbstract))
                {
                    // Console.WriteLine($"2-2 {newType} ({newType.Module})");
                    result = this.Module.ImportReference(newType);
                    // Console.WriteLine($"2-3: {result} ({result.Module})");
                }
                else if (type.Module != this.Module)
                {
                    // Console.WriteLine($"2-4 {type} ({type.Module})");
                    result = this.Module.ImportReference(type);
                    // Console.WriteLine($"2-5: {result} ({result.Module})");
                }
            }

            // Console.WriteLine($"4: {result} ({result.Module})");
            return result;
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
    }
}
