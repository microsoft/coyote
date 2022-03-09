// Copyright (c) Microsoft Corporation.
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
    public class UncontrolledValueTaskAwaiter
    {
#if NET
#pragma warning disable CA1822 // Mark members as static
        public ValueTaskAwaiter GetAwaiter() => ValueTask.CompletedTask.GetAwaiter();
#pragma warning restore CA1822 // Mark members as static
#endif
    }

    /// <summary>
    /// Helper class for task rewriting tests.
    /// </summary>
    /// <remarks>
    /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
    /// </remarks>
    public class UncontrolledGenericValueTaskAwaiter
    {
#if NET
#pragma warning disable CA1822 // Mark members as static
        public ValueTaskAwaiter<int> GetAwaiter() => ValueTask.FromResult<int>(0).GetAwaiter();
#pragma warning restore CA1822 // Mark members as static
#endif
    }

    /// <summary>
    /// Helper class for task rewriting tests.
    /// </summary>
    /// <remarks>
    /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
    /// </remarks>
    public class UncontrolledValueTaskAwaiter<T>
    {
#if NET
        public ValueTaskAwaiter<T> GetAwaiter() => ValueTask.FromResult<T>(default).GetAwaiter();
#endif
    }
}
