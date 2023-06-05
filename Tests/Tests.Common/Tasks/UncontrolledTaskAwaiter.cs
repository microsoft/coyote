﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Tests.Common.Tasks
{
    /// <summary>
    /// Helper class for task rewriting tests.
    /// </summary>
    /// <remarks>
    /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
    /// </remarks>
    public class UncontrolledTaskAwaiter
    {
#pragma warning disable CA1822 // Mark members as static
        public TaskAwaiter GetAwaiter() => Task.CompletedTask.GetAwaiter();
#pragma warning restore CA1822 // Mark members as static
    }

    /// <summary>
    /// Helper class for task rewriting tests.
    /// </summary>
    /// <remarks>
    /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
    /// </remarks>
    public class UncontrolledGenericTaskAwaiter
    {
#pragma warning disable CA1822 // Mark members as static
        public TaskAwaiter<int> GetAwaiter() => Task.FromResult<int>(0).GetAwaiter();
#pragma warning restore CA1822 // Mark members as static
    }

    /// <summary>
    /// Helper class for task rewriting tests.
    /// </summary>
    /// <remarks>
    /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
    /// </remarks>
    public class UncontrolledTaskAwaiter<T>
    {
        public TaskAwaiter<T> GetAwaiter() => Task.FromResult<T>(default).GetAwaiter();
    }
}
