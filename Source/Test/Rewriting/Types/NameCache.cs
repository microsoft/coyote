// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using RuntimeCompiler = Microsoft.Coyote.Runtime.CompilerServices;
using SystemCompiler = System.Runtime.CompilerServices;
using SystemConcurrentCollections = System.Collections.Concurrent;
using SystemGenericCollections = System.Collections.Generic;
#if NET || NETCOREAPP3_1
using SystemNetHttp = System.Net.Http;
#endif
using SystemTasks = System.Threading.Tasks;
using SystemThreading = System.Threading;

namespace Microsoft.Coyote.Rewriting.Types
{
    /// <summary>
    /// Cache of known types that can be rewritten.
    /// </summary>
    internal static class NameCache
    {
        internal static string SystemCompilerNamespace { get; } = typeof(SystemCompiler.AsyncTaskMethodBuilder).Namespace;
        internal static string SystemTasksNamespace { get; } = typeof(SystemTasks.Task).Namespace;

        internal static string Task { get; } = typeof(SystemTasks.Task).FullName;
        internal static string TaskName { get; } = typeof(SystemTasks.Task).Name;
        internal static string GenericTask { get; } = typeof(SystemTasks.Task<>).FullName;
        internal static string ValueTask { get; } = typeof(SystemTasks.ValueTask).FullName;
        internal static string ValueTaskName { get; } = typeof(SystemTasks.ValueTask).Name;
        internal static string GenericValueTask { get; } = typeof(SystemTasks.ValueTask<>).FullName;
#if NET
        internal static string TaskCompletionSource { get; } = typeof(SystemTasks.TaskCompletionSource).FullName;
#endif
        internal static string GenericTaskCompletionSource { get; } = typeof(SystemTasks.TaskCompletionSource<>).FullName;

        internal static string AsyncTaskMethodBuilder { get; } = typeof(SystemCompiler.AsyncTaskMethodBuilder).FullName;
        internal static string AsyncTaskMethodBuilderName { get; } = typeof(SystemCompiler.AsyncTaskMethodBuilder).Name;
        internal static string GenericAsyncTaskMethodBuilder { get; } = typeof(SystemCompiler.AsyncTaskMethodBuilder<>).FullName;
        internal static string AsyncValueTaskMethodBuilder { get; } = typeof(SystemCompiler.AsyncValueTaskMethodBuilder).FullName;
        internal static string AsyncValueTaskMethodBuilderName { get; } = typeof(SystemCompiler.AsyncValueTaskMethodBuilder).Name;
        internal static string GenericAsyncValueTaskMethodBuilder { get; } = typeof(SystemCompiler.AsyncValueTaskMethodBuilder<>).FullName;

        internal static string TaskAwaiter { get; } = typeof(SystemCompiler.TaskAwaiter).FullName;
        internal static string TaskAwaiterName { get; } = typeof(SystemCompiler.TaskAwaiter).Name;
        internal static string GenericTaskAwaiter { get; } = typeof(SystemCompiler.TaskAwaiter<>).FullName;
        internal static string ValueTaskAwaiter { get; } = typeof(SystemCompiler.ValueTaskAwaiter).FullName;
        internal static string ValueTaskAwaiterName { get; } = typeof(SystemCompiler.ValueTaskAwaiter).Name;
        internal static string GenericValueTaskAwaiter { get; } = typeof(SystemCompiler.ValueTaskAwaiter<>).FullName;

        internal static string ConfiguredTaskAwaitable { get; } = typeof(SystemCompiler.ConfiguredTaskAwaitable).FullName;
        internal static string ConfiguredTaskAwaiter { get; } =
            typeof(SystemCompiler.ConfiguredTaskAwaitable).FullName + "/ConfiguredTaskAwaiter";
        internal static string GenericConfiguredTaskAwaitable { get; } = typeof(SystemCompiler.ConfiguredTaskAwaitable<>).FullName;
        internal static string GenericConfiguredTaskAwaiter { get; } =
            typeof(SystemCompiler.ConfiguredTaskAwaitable<>).FullName + "/ConfiguredTaskAwaiter";
        internal static string ConfiguredValueTaskAwaitable { get; } = typeof(SystemCompiler.ConfiguredValueTaskAwaitable).FullName;
        internal static string ConfiguredValueTaskAwaiter { get; } =
            typeof(SystemCompiler.ConfiguredValueTaskAwaitable).FullName + "/ConfiguredValueTaskAwaiter";
        internal static string GenericConfiguredValueTaskAwaitable { get; } =
                typeof(SystemCompiler.ConfiguredValueTaskAwaitable<>).FullName;
        internal static string GenericConfiguredValueTaskAwaiter { get; } =
            typeof(SystemCompiler.ConfiguredValueTaskAwaitable<>).FullName + "/ConfiguredValueTaskAwaiter";
        internal static string YieldAwaitable { get; } = typeof(SystemCompiler.YieldAwaitable).FullName;
        internal static string YieldAwaiter { get; } =
            typeof(SystemCompiler.YieldAwaitable).FullName + "/YieldAwaiter";

        internal static string TaskExtensions { get; } = typeof(SystemTasks.TaskExtensions).FullName;
        internal static string TaskFactory { get; } = typeof(SystemTasks.TaskFactory).FullName;
        internal static string GenericTaskFactory { get; } = typeof(SystemTasks.TaskFactory<>).FullName;
        internal static string TaskParallel { get; } = typeof(SystemTasks.Parallel).FullName;

        internal static string Thread { get; } = typeof(SystemThreading.Thread).FullName;

        internal static string Monitor { get; } = typeof(SystemThreading.Monitor).FullName;
        internal static string SemaphoreSlim { get; } = typeof(SystemThreading.SemaphoreSlim).FullName;
        internal static string Interlocked { get; } = typeof(SystemThreading.Interlocked).FullName;
        internal static string AutoResetEvent { get; } = typeof(SystemThreading.AutoResetEvent).FullName;
        internal static string ManualResetEvent { get; } = typeof(SystemThreading.ManualResetEvent).FullName;
        internal static string EventWaitHandle { get; } = typeof(SystemThreading.EventWaitHandle).FullName;
        internal static string WaitHandle { get; } = typeof(SystemThreading.WaitHandle).FullName;

        internal static string GenericList { get; } = typeof(SystemGenericCollections.List<>).FullName;
        internal static string GenericDictionary { get; } = typeof(SystemGenericCollections.Dictionary<,>).FullName;
        internal static string GenericHashSet { get; } = typeof(SystemGenericCollections.HashSet<>).FullName;

        internal static string ConcurrentBag { get; } = typeof(SystemConcurrentCollections.ConcurrentBag<>).FullName;
        internal static string ConcurrentDictionary { get; } = typeof(SystemConcurrentCollections.ConcurrentDictionary<,>).FullName;
        internal static string ConcurrentQueue { get; } = typeof(SystemConcurrentCollections.ConcurrentQueue<>).FullName;
        internal static string ConcurrentStack { get; } = typeof(SystemConcurrentCollections.ConcurrentStack<>).FullName;

#if NET || NETCOREAPP3_1
        internal static string HttpClient { get; } = typeof(SystemNetHttp.HttpClient).FullName;
        internal static string HttpRequestMessage { get; } = typeof(SystemNetHttp.HttpRequestMessage).FullName;
#endif
    }
}
