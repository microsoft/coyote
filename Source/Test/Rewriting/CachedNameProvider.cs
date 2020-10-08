﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using SystematicTesting = Microsoft.Coyote.SystematicTesting.Interception;
using SystemCompiler = System.Runtime.CompilerServices;
using SystemTasks = System.Threading.Tasks;
using SystemThreading = System.Threading;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// Provider of cached names for known system types.
    /// </summary>
    internal static class CachedNameProvider
    {
        internal static string TaskFullName { get; } = typeof(SystemTasks.Task).FullName;
        internal static string GenericTaskFullName { get; } = typeof(SystemTasks.Task<>).FullName;
        internal static string AsyncTaskMethodBuilderName { get; } = typeof(SystemCompiler.AsyncTaskMethodBuilder).Name;
        internal static string AsyncTaskMethodBuilderFullName { get; } = typeof(SystemCompiler.AsyncTaskMethodBuilder).FullName;
        internal static string GenericAsyncTaskMethodBuilderName { get; } = typeof(SystemCompiler.AsyncTaskMethodBuilder<>).Name;
        internal static string GenericAsyncTaskMethodBuilderFullName { get; } = typeof(SystemCompiler.AsyncTaskMethodBuilder<>).FullName;
        internal static string TaskAwaiterFullName { get; } = typeof(SystemCompiler.TaskAwaiter).FullName;
        internal static string GenericTaskAwaiterName { get; } = typeof(SystemCompiler.TaskAwaiter<>).Name;
        internal static string GenericTaskAwaiterFullName { get; } = typeof(SystemCompiler.TaskAwaiter<>).FullName;
        internal static string YieldAwaitableFullName { get; } = typeof(SystemCompiler.YieldAwaitable).FullName;
        internal static string YieldAwaiterFullName { get; } = typeof(SystemCompiler.YieldAwaitable).FullName + "/YieldAwaiter";
        internal static string TaskExtensionsFullName { get; } = typeof(SystemTasks.TaskExtensions).FullName;
        internal static string TaskFactoryFullName { get; } = typeof(SystemTasks.TaskFactory).FullName;
        internal static string GenericTaskFactoryFullName { get; } = typeof(SystemTasks.TaskFactory<>).FullName;
        internal static string TaskParallelFullName { get; } = typeof(SystemTasks.Parallel).FullName;
        internal static string ThreadPoolFullName { get; } = typeof(SystemThreading.ThreadPool).FullName;
        internal static string SystematicTestingNamespace { get; } = typeof(SystematicTesting.Interception.ControlledTask).Namespace;
        internal static string SystemCompilerNamespace { get; } = typeof(SystemCompiler.AsyncTaskMethodBuilder).Namespace;
        internal static string SystemTasksNamespace { get; } = typeof(SystemTasks.Task).Namespace;
    }
}
