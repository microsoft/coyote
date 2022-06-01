// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Microsoft.Coyote.Tests.Common.Tasks
{
#pragma warning disable CA1000 // Do not declare static members on generic types
    /// <summary>
    /// Helper class for task rewriting tests.
    /// </summary>
    /// <remarks>
    /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
    /// </remarks>
    public static class TaskProvider
    {
        public static Task GetTask() => Task.CompletedTask;

        public static Task<T> GetGenericTask<T>() => Task.FromResult<T>(default(T));

        public static Task<(int, bool)> GetValueTupleTask() => Task.FromResult((0, true));

        public static Task<(TLeft, TRight)> GetGenericValueTupleTask<TLeft, TRight>() =>
            Task.FromResult((default(TLeft), default(TRight)));
    }

    /// <summary>
    /// Helper class for task rewriting tests.
    /// </summary>
    /// <remarks>
    /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
    /// </remarks>
    public static class GenericTaskProvider<TLeft, TRight>
    {
        public static class Nested<TNested>
        {
            public static Task GetTask() => Task.CompletedTask;

            public static Task<T> GetGenericMethodTask<T>() => Task.FromResult<T>(default(T));

            public static Task<TRight> GetGenericTypeTask<T>() => Task.FromResult<TRight>(default(TRight));

            public static Task<(T, TRight, TNested)> GetGenericValueTupleTask<T>() =>
                Task.FromResult((default(T), default(TRight), default(TNested)));
        }
    }

#if NET
    /// <summary>
    /// Helper class for value task rewriting tests.
    /// </summary>
    /// <remarks>
    /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
    /// </remarks>
    public static class ValueTaskProvider
    {
        public static ValueTask GetTask() => ValueTask.CompletedTask;

        public static ValueTask<T> GetGenericTask<T>() => ValueTask.FromResult<T>(default(T));
    }

    /// <summary>
    /// Helper class for value task rewriting tests.
    /// </summary>
    /// <remarks>
    /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
    /// </remarks>
    public static class GenericValueTaskProvider<TLeft, TRight>
    {
        public static class Nested<TNested>
        {
            public static ValueTask GetTask() => ValueTask.CompletedTask;

            public static ValueTask<T> GetGenericMethodTask<T>() => ValueTask.FromResult<T>(default(T));

            public static ValueTask<TRight> GetGenericTypeTask<T>() => ValueTask.FromResult<TRight>(default(TRight));
        }
    }
#endif
#pragma warning restore CA1000 // Do not declare static members on generic types
}
