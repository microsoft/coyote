// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using SystemCompiler = System.Runtime.CompilerServices;

namespace Microsoft.Coyote.Rewriting.Tests.Helpers
{
    /// <summary>
    /// Helper class for task rewriting tests.
    /// </summary>
    public class TaskAwaiter
    {
#pragma warning disable CA1822 // Mark members as static
        public SystemCompiler.TaskAwaiter GetAwaiter() => Task.CompletedTask.GetAwaiter();
#pragma warning restore CA1822 // Mark members as static
    }

    /// <summary>
    /// Helper class for task rewriting tests.
    /// </summary>
    public class GenericTaskAwaiter
    {
#pragma warning disable CA1822 // Mark members as static
        public SystemCompiler.TaskAwaiter<int> GetAwaiter() => Task.FromResult<int>(0).GetAwaiter();
#pragma warning restore CA1822 // Mark members as static
    }

    /// <summary>
    /// Helper class for task rewriting tests.
    /// </summary>
    public class TaskAwaiter<T>
    {
        public SystemCompiler.TaskAwaiter<T> GetAwaiter() => Task.FromResult<T>(default).GetAwaiter();
    }
}
