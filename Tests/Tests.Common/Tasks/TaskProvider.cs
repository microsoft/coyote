// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Microsoft.Coyote.Tests.Common.Tasks
{
    /// <summary>
    /// Helper class for task rewriting tests.
    /// </summary>
    /// <remarks>
    /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
    /// </remarks>
    public class TaskProvider
    {
        public static Task GetTask() => Task.CompletedTask;

        public static Task<T> GetGenericTask<T>() => Task.FromResult<T>(default(T));
    }

    /// <summary>
    /// Helper class for task rewriting tests.
    /// </summary>
    /// <remarks>
    /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
    /// </remarks>
    public class GenericTaskProvider<K,Z>
    {
        public class Nested<L>
        {
            public static Task GetTask() => Task.CompletedTask;

            public static Task<T> GetGenericMethodTask<T>() => Task.FromResult<T>(default(T));

            public static Task<Z> GetGenericTypeTask<T>() => Task.FromResult<Z>(default(Z));
        }
    }

    /// <summary>
    /// Helper class for value task rewriting tests.
    /// </summary>
    /// <remarks>
    /// We do not rewrite this class in purpose to test scenarios with partially rewritten code.
    /// </remarks>
    public class ValueTaskProvider
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
    public class GenericValueTaskProvider<K,Z>
    {
        public class Nested<L>
        {
            public static ValueTask GetTask() => ValueTask.CompletedTask;

            public static ValueTask<T> GetGenericMethodTask<T>() => ValueTask.FromResult<T>(default(T));

            public static ValueTask<Z> GetGenericTypeTask<T>() => ValueTask.FromResult<Z>(default(Z));
        }
    }
}
