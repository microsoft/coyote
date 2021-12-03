// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Coyote.Runtime;

using SystemParallel = System.Threading.Tasks.Parallel;
using SystemParallelLoopResult = System.Threading.Tasks.ParallelLoopResult;
using SystemParallelLoopState = System.Threading.Tasks.ParallelLoopState;
using SystemParallelOptions = System.Threading.Tasks.ParallelOptions;

namespace Microsoft.Coyote.Rewriting.Types.Threading.Tasks
{
    /// <summary>
    /// Provides methods for creating tasks that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class Parallel
    {
        // TODO: support cancelations.

        /// <summary>
        /// We pick a fixed max degree of parallelism to make sure the schedules are reproducible
        /// across machines with different processor count.
        /// </summary>
        /// <remarks>
        /// TODO: make this configurable.
        /// </remarks>
        private const int MaxDegreeOfParallelism = 4;

        /// <summary>
        /// Executes each of the provided actions, possibly in parallel.
        /// </summary>
        public static void Invoke(params Action[] actions)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.Invoke));
            SystemParallel.Invoke(actions);
        }

        /// <summary>
        /// Executes each of the provided actions, possibly in parallel, unless the operation is cancelled by the user.
        /// </summary>
        public static void Invoke(SystemParallelOptions parallelOptions, params Action[] actions)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.Invoke));
            SystemParallel.Invoke(parallelOptions, actions);
        }

        /// <summary>
        /// Executes a for loop in which iterations may run in parallel.
        /// </summary>
        public static SystemParallelLoopResult For(int fromInclusive, int toExclusive, Action<int> body)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return For(fromInclusive, toExclusive, new SystemParallelOptions(), body);
            }

            return SystemParallel.For(fromInclusive, toExclusive, body);
        }

        /// <summary>
        /// Executes a for loop in which iterations may run in parallel and loop options
        /// can be configured.
        /// </summary>
        public static SystemParallelLoopResult For(int fromInclusive, int toExclusive,
            SystemParallelOptions parallelOptions, Action<int> body)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return SystemParallel.For(fromInclusive, toExclusive, new SystemParallelOptions()
                {
                    CancellationToken = parallelOptions.CancellationToken,
                    MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                    TaskScheduler = CoyoteRuntime.Current.ControlledTaskScheduler
                }, body);
            }

            return SystemParallel.For(fromInclusive, toExclusive, parallelOptions, body);
        }

        /// <summary>
        /// Executes a for loop in which iterations may run in parallel and the state of
        /// the loop can be monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult For(int fromInclusive, int toExclusive,
            Action<int, SystemParallelLoopState> body)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.For));
            return SystemParallel.For(fromInclusive, toExclusive, body);
        }

        /// <summary>
        /// Executes a for loop in which iterations may run in parallel, loop options can
        /// be configured, and the state of the loop can be monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult For(int fromInclusive, int toExclusive,
            SystemParallelOptions parallelOptions, Action<int, SystemParallelLoopState> body)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.For));
            return SystemParallel.For(fromInclusive, toExclusive, parallelOptions, body);
        }

        /// <summary>
        /// Executes a for loop with 64-bit indexes in which iterations may run in parallel.
        /// </summary>
        public static SystemParallelLoopResult For(long fromInclusive, long toExclusive, Action<long> body)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.For));
            return SystemParallel.For(fromInclusive, toExclusive, body);
        }

        /// <summary>
        /// Executes a for loop with 64-bit indexes in which iterations may run in parallel
        /// and loop options can be configured.
        /// </summary>
        public static SystemParallelLoopResult For(long fromInclusive, long toExclusive,
            SystemParallelOptions parallelOptions, Action<long> body)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.For));
            return SystemParallel.For(fromInclusive, toExclusive, parallelOptions, body);
        }

        /// <summary>
        /// Executes a for loop with 64-bit indexes in which iterations may run in parallel
        /// and the state of the loop can be monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult For(long fromInclusive, long toExclusive,
            Action<long, SystemParallelLoopState> body)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.For));
            return SystemParallel.For(fromInclusive, toExclusive, body);
        }

        /// <summary>
        /// Executes a for loop with 64-bit indexes in which iterations may run in parallel, loop
        /// options can be configured, and the state of the loop can be monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult For(long fromInclusive, long toExclusive,
            SystemParallelOptions parallelOptions, Action<long, SystemParallelLoopState> body)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.For));
            return SystemParallel.For(fromInclusive, toExclusive, parallelOptions, body);
        }

        /// <summary>
        /// Executes a for loop with thread-local data in which iterations may run in parallel,
        /// and the state of the loop can be monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult For<TLocal>(int fromInclusive, int toExclusive, Func<TLocal> localInit,
            Func<int, SystemParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.For));
            return SystemParallel.For(fromInclusive, toExclusive, localInit, body, localFinally);
        }

        /// <summary>
        /// Executes a for loop with thread-local data in which iterations may run in parallel, loop
        /// options can be configured, and the state of the loop can be monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult For<TLocal>(int fromInclusive, int toExclusive,
            SystemParallelOptions parallelOptions, Func<TLocal> localInit,
            Func<int, SystemParallelLoopState, TLocal, TLocal> body,
            Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.For));
            return SystemParallel.For(fromInclusive, toExclusive, parallelOptions, localInit, body, localFinally);
        }

        /// <summary>
        /// Executes a for loop with 64-bit indexes and thread-local data in which iterations
        /// may run in parallel, and the state of the loop can be monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult For<TLocal>(long fromInclusive, long toExclusive,
            Func<TLocal> localInit, Func<long, SystemParallelLoopState, TLocal, TLocal> body,
            Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.For));
            return SystemParallel.For(fromInclusive, toExclusive, localInit, body, localFinally);
        }

        /// <summary>
        /// Executes a for loop with 64-bit indexes and thread-local data in which iterations
        /// may run in parallel, loop options can be configured, and the state of the loop
        /// can be monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult For<TLocal>(long fromInclusive, long toExclusive,
            SystemParallelOptions parallelOptions, Func<TLocal> localInit,
            Func<long, SystemParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.For));
            return SystemParallel.For(fromInclusive, toExclusive, parallelOptions, localInit, body, localFinally);
        }

        /// <summary>
        /// Executes a foreach operation on a <see cref="System.Collections.IEnumerable"/>
        /// in which iterations may run in parallel.
        /// </summary>
        public static SystemParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, Action<TSource> body)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return ForEach(source, new SystemParallelOptions(), body);
            }

            return SystemParallel.ForEach(source, body);
        }

        /// <summary>
        /// Executes a foreach operation on a <see cref="System.Collections.IEnumerable"/>
        /// in which iterations may run in parallel and loop options can be configured.
        /// </summary>
        public static SystemParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source,
            SystemParallelOptions parallelOptions, Action<TSource> body)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return SystemParallel.ForEach(source, new SystemParallelOptions()
                {
                    CancellationToken = parallelOptions.CancellationToken,
                    MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                    TaskScheduler = CoyoteRuntime.Current.ControlledTaskScheduler
                }, body);
            }

            return SystemParallel.ForEach(source, parallelOptions, body);
        }

        /// <summary>
        /// Executes a foreach operation on a <see cref="Partitioner"/> in which iterations may run in parallel.
        /// </summary>
        public static SystemParallelLoopResult ForEach<TSource>(Partitioner<TSource> source, Action<TSource> body)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.ForEach));
            return SystemParallel.ForEach(source, body);
        }

        /// <summary>
        /// Executes a foreach operation on a <see cref="Partitioner"/> in which iterations may run
        /// in parallel and loop options can be configured.
        /// </summary>
        public static SystemParallelLoopResult ForEach<TSource>(Partitioner<TSource> source,
            SystemParallelOptions parallelOptions, Action<TSource> body)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.ForEach));
            return SystemParallel.ForEach(source, parallelOptions, body);
        }

        /// <summary>
        /// Executes a foreach operation on a <see cref="System.Collections.IEnumerable"/> in which iterations
        /// may run in parallel, and the state of the loop can be monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source,
            Action<TSource, SystemParallelLoopState> body)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.ForEach));
            return SystemParallel.ForEach(source, body);
        }

        /// <summary>
        /// Executes a foreach operation on a <see cref="System.Collections.IEnumerable"/> in which iterations
        /// may run in parallel, loop options can be configured, and the state of the loop can be monitored and
        /// manipulated.
        /// </summary>
        public static SystemParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source,
            SystemParallelOptions parallelOptions, Action<TSource, SystemParallelLoopState> body)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.ForEach));
            return SystemParallel.ForEach(source, parallelOptions, body);
        }

        /// <summary>
        /// Executes a foreach operation on a <see cref="Partitioner"/> in which iterations may run in parallel,
        /// and the state of the loop can be monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult ForEach<TSource>(Partitioner<TSource> source,
            Action<TSource, SystemParallelLoopState> body)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.ForEach));
            return SystemParallel.ForEach(source, body);
        }

        /// <summary>
        /// Executes a foreach operation on a <see cref="Partitioner"/> in which iterations may run in parallel,
        /// loop options can be configured, and the state of the loop can be monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult ForEach<TSource>(Partitioner<TSource> source,
            SystemParallelOptions parallelOptions, Action<TSource, SystemParallelLoopState> body)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.ForEach));
            return SystemParallel.ForEach(source, parallelOptions, body);
        }

        /// <summary>
        /// Executes a foreach operation with 64-bit indexes on a <see cref="System.Collections.IEnumerable"/>
        /// in which iterations may run in parallel, and the state of the loop can be monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source,
            Action<TSource, SystemParallelLoopState, long> body)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.ForEach));
            return SystemParallel.ForEach(source, body);
        }

        /// <summary>
        /// Executes a foreach operation with 64-bit indexes on a <see cref="System.Collections.IEnumerable"/>
        /// in which iterations may run in parallel, loop options can be configured, and the state of the loop
        /// can be monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source,
            SystemParallelOptions parallelOptions, Action<TSource, SystemParallelLoopState, long> body)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.ForEach));
            return SystemParallel.ForEach(source, parallelOptions, body);
        }

        /// <summary>
        /// Executes a foreach operation on a <see cref="OrderablePartitioner{TSource}"/> in which iterations
        /// may run in parallel and the state of the loop can be monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult ForEach<TSource>(OrderablePartitioner<TSource> source,
            Action<TSource, SystemParallelLoopState, long> body)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.ForEach));
            return SystemParallel.ForEach(source, body);
        }

        /// <summary>
        /// Executes a foreach operation on a <see cref="OrderablePartitioner{TSource}"/>
        /// in which iterations may run in parallel, loop options can be configured, and
        /// the state of the loop can be monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult ForEach<TSource>(OrderablePartitioner<TSource> source,
            SystemParallelOptions parallelOptions, Action<TSource, SystemParallelLoopState, long> body)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.ForEach));
            return SystemParallel.ForEach(source, parallelOptions, body);
        }

        /// <summary>
        /// Executes a foreach operation with thread-local data on a <see cref="System.Collections.IEnumerable"/>
        /// in which iterations may run in parallel, and the state of the loop can be monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source, Func<TLocal> localInit,
            Func<TSource, SystemParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.ForEach));
            return SystemParallel.ForEach(source, localInit, body, localFinally);
        }

        /// <summary>
        /// Executes a foreach operation with thread-local data on a <see cref="System.Collections.IEnumerable"/>
        /// in which iterations may run in parallel, loop options can be configured, and the state of the loop
        /// can be monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source,
            SystemParallelOptions parallelOptions, Func<TLocal> localInit,
            Func<TSource, SystemParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.ForEach));
            return SystemParallel.ForEach(source, parallelOptions, localInit, body, localFinally);
        }

        /// <summary>
        /// Executes a foreach operation with thread-local data on a <see cref="Partitioner"/> in which
        /// iterations may run in parallel and the state of the loop can be monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult ForEach<TSource, TLocal>(Partitioner<TSource> source,
            Func<TLocal> localInit, Func<TSource, SystemParallelLoopState, TLocal, TLocal> body,
            Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.ForEach));
            return SystemParallel.ForEach(source, localInit, body, localFinally);
        }

        /// <summary>
        /// Executes a foreach operation with thread-local data on a <see cref="Partitioner"/> in which
        /// iterations may run in parallel, loop options can be configured, and the state of the loop
        /// can be monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult ForEach<TSource, TLocal>(Partitioner<TSource> source,
            SystemParallelOptions parallelOptions, Func<TLocal> localInit,
            Func<TSource, SystemParallelLoopState, TLocal, TLocal> body,
            Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.ForEach));
            return SystemParallel.ForEach(source, parallelOptions, localInit, body, localFinally);
        }

        /// <summary>
        /// Executes a foreach operation with thread-local data on a <see cref="System.Collections.IEnumerable"/>
        /// in which iterations may run in parallel and the state of the loop can be monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source,
            Func<TLocal> localInit, Func<TSource, SystemParallelLoopState, long, TLocal, TLocal> body,
            Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.ForEach));
            return SystemParallel.ForEach(source, localInit, body, localFinally);
        }

        /// <summary>
        /// Executes a foreach operation with thread-local data and 64-bit indexes on a
        /// <see cref="System.Collections.IEnumerable"/> in which iterations may run in
        /// parallel, loop options can be configured, and the state of the loop can be
        /// monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source,
            SystemParallelOptions parallelOptions, Func<TLocal> localInit,
            Func<TSource, SystemParallelLoopState, long, TLocal, TLocal> body,
            Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.ForEach));
            return SystemParallel.ForEach(source, parallelOptions, localInit, body, localFinally);
        }

        /// <summary>
        /// Executes a foreach operation with thread-local data on a <see cref="OrderablePartitioner{TSource}"/>
        /// in which iterations may run in parallel, loop options can be configured, and the state of the loop
        /// can be monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult ForEach<TSource, TLocal>(OrderablePartitioner<TSource> source,
            Func<TLocal> localInit, Func<TSource, SystemParallelLoopState, long, TLocal, TLocal> body,
            Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.ForEach));
            return SystemParallel.ForEach(source, localInit, body, localFinally);
        }

        /// <summary>
        /// Executes a foreach operation with 64-bit indexes and with thread-local data on
        /// a <see cref="OrderablePartitioner{TSource}"/> in which iterations may run in
        /// parallel , loop options can be configured, and the state of the loop can be
        /// monitored and manipulated.
        /// </summary>
        public static SystemParallelLoopResult ForEach<TSource, TLocal>(OrderablePartitioner<TSource> source,
            SystemParallelOptions parallelOptions, Func<TLocal> localInit,
            Func<TSource, SystemParallelLoopState, long, TLocal, TLocal> body,
            Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowUncontrolledInvocationException(nameof(SystemParallel.ForEach));
            return SystemParallel.ForEach(source, parallelOptions, localInit, body, localFinally);
        }
    }
}
