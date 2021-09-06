// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Interception
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Invoke(params Action[] actions)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.Invoke));
            SystemTasks.Parallel.Invoke(actions);
        }

        /// <summary>
        /// Executes each of the provided actions, possibly in parallel, unless the operation is cancelled by the user.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Invoke(ParallelOptions parallelOptions, params Action[] actions)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.Invoke));
            SystemTasks.Parallel.Invoke(parallelOptions, actions);
        }

        /// <summary>
        /// Executes a for loop in which iterations may run in parallel.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult For(int fromInclusive, int toExclusive, Action<int> body)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return For(fromInclusive, toExclusive, new ParallelOptions(), body);
            }

            return SystemTasks.Parallel.For(fromInclusive, toExclusive, body);
        }

        /// <summary>
        /// Executes a for loop in which iterations may run in parallel and loop options
        /// can be configured.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult For(int fromInclusive, int toExclusive, ParallelOptions parallelOptions, Action<int> body)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return SystemTasks.Parallel.For(fromInclusive, toExclusive, new ParallelOptions()
                {
                    CancellationToken = parallelOptions.CancellationToken,
                    MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                    TaskScheduler = CoyoteRuntime.Current.ControlledTaskScheduler
                }, body);
            }

            return SystemTasks.Parallel.For(fromInclusive, toExclusive, parallelOptions, body);
        }

        /// <summary>
        /// Executes a for loop in which iterations may run in parallel and the state of
        /// the loop can be monitored and manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult For(int fromInclusive, int toExclusive, Action<int, ParallelLoopState> body)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.For));
            return SystemTasks.Parallel.For(fromInclusive, toExclusive, body);
        }

        /// <summary>
        /// Executes a for loop in which iterations may run in parallel, loop options can
        /// be configured, and the state of the loop can be monitored and manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult For(int fromInclusive, int toExclusive, ParallelOptions parallelOptions,
            Action<int, ParallelLoopState> body)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.For));
            return SystemTasks.Parallel.For(fromInclusive, toExclusive, parallelOptions, body);
        }

        /// <summary>
        /// Executes a for loop with 64-bit indexes in which iterations may run in parallel.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult For(long fromInclusive, long toExclusive, Action<long> body)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.For));
            return SystemTasks.Parallel.For(fromInclusive, toExclusive, body);
        }

        /// <summary>
        /// Executes a for loop with 64-bit indexes in which iterations may run in parallel
        /// and loop options can be configured.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult For(long fromInclusive, long toExclusive, ParallelOptions parallelOptions, Action<long> body)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.For));
            return SystemTasks.Parallel.For(fromInclusive, toExclusive, parallelOptions, body);
        }

        /// <summary>
        /// Executes a for loop with 64-bit indexes in which iterations may run in parallel
        /// and the state of the loop can be monitored and manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult For(long fromInclusive, long toExclusive, Action<long, ParallelLoopState> body)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.For));
            return SystemTasks.Parallel.For(fromInclusive, toExclusive, body);
        }

        /// <summary>
        /// Executes a for loop with 64-bit indexes in which iterations may run in parallel, loop
        /// options can be configured, and the state of the loop can be monitored and manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult For(long fromInclusive, long toExclusive, ParallelOptions parallelOptions,
            Action<long, ParallelLoopState> body)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.For));
            return SystemTasks.Parallel.For(fromInclusive, toExclusive, parallelOptions, body);
        }

        /// <summary>
        /// Executes a for loop with thread-local data in which iterations may run in parallel,
        /// and the state of the loop can be monitored and manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult For<TLocal>(int fromInclusive, int toExclusive, Func<TLocal> localInit, Func<int, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.For));
            return SystemTasks.Parallel.For(fromInclusive, toExclusive, localInit, body, localFinally);
        }

        /// <summary>
        /// Executes a for loop with thread-local data in which iterations may run in parallel, loop
        /// options can be configured, and the state of the loop can be monitored and manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult For<TLocal>(int fromInclusive, int toExclusive, ParallelOptions parallelOptions, Func<TLocal> localInit, Func<int, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.For));
            return SystemTasks.Parallel.For(fromInclusive, toExclusive, parallelOptions, localInit, body, localFinally);
        }

        /// <summary>
        /// Executes a for loop with 64-bit indexes and thread-local data in which iterations
        /// may run in parallel, and the state of the loop can be monitored and manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult For<TLocal>(long fromInclusive, long toExclusive, Func<TLocal> localInit,
            Func<long, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.For));
            return SystemTasks.Parallel.For(fromInclusive, toExclusive, localInit, body, localFinally);
        }

        /// <summary>
        /// Executes a for loop with 64-bit indexes and thread-local data in which iterations
        /// may run in parallel, loop options can be configured, and the state of the loop
        /// can be monitored and manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult For<TLocal>(long fromInclusive, long toExclusive, ParallelOptions parallelOptions,
            Func<TLocal> localInit, Func<long, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.For));
            return SystemTasks.Parallel.For(fromInclusive, toExclusive, parallelOptions, localInit, body, localFinally);
        }

        /// <summary>
        /// Executes a foreach operation on a <see cref="System.Collections.IEnumerable"/>
        /// in which iterations may run in parallel.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, Action<TSource> body)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return ForEach(source, new ParallelOptions(), body);
            }

            return SystemTasks.Parallel.ForEach(source, body);
        }

        /// <summary>
        /// Executes a foreach operation on a <see cref="System.Collections.IEnumerable"/>
        /// in which iterations may run in parallel and loop options can be configured.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Action<TSource> body)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                return SystemTasks.Parallel.ForEach(source, new ParallelOptions()
                {
                    CancellationToken = parallelOptions.CancellationToken,
                    MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                    TaskScheduler = CoyoteRuntime.Current.ControlledTaskScheduler
                }, body);
            }

            return SystemTasks.Parallel.ForEach(source, parallelOptions, body);
        }

        /// <summary>
        /// Executes a foreach operation on a <see cref="Partitioner"/> in which iterations may run in parallel.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult ForEach<TSource>(Partitioner<TSource> source, Action<TSource> body)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.ForEach));
            return SystemTasks.Parallel.ForEach(source, body);
        }

        /// <summary>
        /// Executes a foreach operation on a <see cref="Partitioner"/> in which iterations may run
        /// in parallel and loop options can be configured.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult ForEach<TSource>(Partitioner<TSource> source, ParallelOptions parallelOptions, Action<TSource> body)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.ForEach));
            return SystemTasks.Parallel.ForEach(source, parallelOptions, body);
        }

        /// <summary>
        /// Executes a foreach operation on a <see cref="System.Collections.IEnumerable"/> in which iterations
        /// may run in parallel, and the state of the loop can be monitored and manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, Action<TSource, ParallelLoopState> body)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.ForEach));
            return SystemTasks.Parallel.ForEach(source, body);
        }

        /// <summary>
        /// Executes a foreach operation on a <see cref="System.Collections.IEnumerable"/> in which iterations
        /// may run in parallel, loop options can be configured, and the state of the loop can be monitored and
        /// manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions,
            Action<TSource, ParallelLoopState> body)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.ForEach));
            return SystemTasks.Parallel.ForEach(source, parallelOptions, body);
        }

        /// <summary>
        /// Executes a foreach operation on a <see cref="Partitioner"/> in which iterations may run in parallel,
        /// and the state of the loop can be monitored
        /// and manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult ForEach<TSource>(Partitioner<TSource> source, Action<TSource, ParallelLoopState> body)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.ForEach));
            return SystemTasks.Parallel.ForEach(source, body);
        }

        /// <summary>
        /// Executes a foreach operation on a <see cref="Partitioner"/> in which iterations may run in parallel,
        /// loop options can be configured, and the state of the loop can be monitored and manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult ForEach<TSource>(Partitioner<TSource> source, ParallelOptions parallelOptions,
            Action<TSource, ParallelLoopState> body)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.ForEach));
            return SystemTasks.Parallel.ForEach(source, parallelOptions, body);
        }

        /// <summary>
        /// Executes a foreach operation with 64-bit indexes on a <see cref="System.Collections.IEnumerable"/>
        /// in which iterations may run in parallel, and the state of the loop can be monitored and manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, Action<TSource, ParallelLoopState, long> body)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.ForEach));
            return SystemTasks.Parallel.ForEach(source, body);
        }

        /// <summary>
        /// Executes a foreach operation with 64-bit indexes on a <see cref="System.Collections.IEnumerable"/>
        /// in which iterations may run in parallel, loop options can be configured, and the state of the loop
        /// can be monitored and manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions,
            Action<TSource, ParallelLoopState, long> body)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.ForEach));
            return SystemTasks.Parallel.ForEach(source, parallelOptions, body);
        }

        /// <summary>
        /// Executes a foreach operation on a <see cref="OrderablePartitioner{TSource}"/> in which iterations
        /// may run in parallel and the state of the loop can be monitored and manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult ForEach<TSource>(OrderablePartitioner<TSource> source, Action<TSource, ParallelLoopState, long> body)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.ForEach));
            return SystemTasks.Parallel.ForEach(source, body);
        }

        /// <summary>
        /// Executes a foreach operation on a <see cref="OrderablePartitioner{TSource}"/>
        /// in which iterations may run in parallel, loop options can be configured, and
        /// the state of the loop can be monitored and manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult ForEach<TSource>(OrderablePartitioner<TSource> source, ParallelOptions parallelOptions,
            Action<TSource, ParallelLoopState, long> body)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.ForEach));
            return SystemTasks.Parallel.ForEach(source, parallelOptions, body);
        }

        /// <summary>
        /// Executes a foreach operation with thread-local data on a <see cref="System.Collections.IEnumerable"/>
        /// in which iterations may run in parallel, and the state of the loop can be monitored and manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source, Func<TLocal> localInit,
            Func<TSource, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.ForEach));
            return SystemTasks.Parallel.ForEach(source, localInit, body, localFinally);
        }

        /// <summary>
        /// Executes a foreach operation with thread-local data on a <see cref="System.Collections.IEnumerable"/>
        /// in which iterations may run in parallel, loop options can be configured, and the state of the loop
        /// can be monitored and manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source, ParallelOptions parallelOptions,
            Func<TLocal> localInit, Func<TSource, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.ForEach));
            return SystemTasks.Parallel.ForEach(source, parallelOptions, localInit, body, localFinally);
        }

        /// <summary>
        /// Executes a foreach operation with thread-local data on a <see cref="Partitioner"/> in which iterations may run in
        /// parallel and the state of the loop can be monitored and manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult ForEach<TSource, TLocal>(Partitioner<TSource> source, Func<TLocal> localInit,
            Func<TSource, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.ForEach));
            return SystemTasks.Parallel.ForEach(source, localInit, body, localFinally);
        }

        /// <summary>
        /// Executes a foreach operation with thread-local data on a <see cref="Partitioner"/> in which iterations may run in
        /// parallel, loop options can be configured, and the state of the loop can be monitored
        /// and manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult ForEach<TSource, TLocal>(Partitioner<TSource> source, ParallelOptions parallelOptions,
            Func<TLocal> localInit, Func<TSource, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.ForEach));
            return SystemTasks.Parallel.ForEach(source, parallelOptions, localInit, body, localFinally);
        }

        /// <summary>
        /// Executes a foreach operation with thread-local data on a <see cref="System.Collections.IEnumerable"/>
        /// in which iterations may run in parallel and the state of the loop can be monitored and manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source, Func<TLocal> localInit,
            Func<TSource, ParallelLoopState, long, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.ForEach));
            return SystemTasks.Parallel.ForEach(source, localInit, body, localFinally);
        }

        /// <summary>
        /// Executes a foreach operation with thread-local data and 64-bit indexes on a <see cref="System.Collections.IEnumerable"/>
        /// in which iterations may run in parallel, loop options can be configured, and the state of the loop can be monitored and
        /// manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source, ParallelOptions parallelOptions,
            Func<TLocal> localInit, Func<TSource, ParallelLoopState, long, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.ForEach));
            return SystemTasks.Parallel.ForEach(source, parallelOptions, localInit, body, localFinally);
        }

        /// <summary>
        /// Executes a foreach operation with thread-local data on a <see cref="OrderablePartitioner{TSource}"/>
        /// in which iterations may run in parallel, loop options can be configured, and the state of the loop
        /// can be monitored and manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult ForEach<TSource, TLocal>(OrderablePartitioner<TSource> source, Func<TLocal> localInit,
            Func<TSource, ParallelLoopState, long, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.ForEach));
            return SystemTasks.Parallel.ForEach(source, localInit, body, localFinally);
        }

        /// <summary>
        /// Executes a foreach operation with 64-bit indexes and with thread-local data on
        /// a <see cref="OrderablePartitioner{TSource}"/> in which iterations may run in
        /// parallel , loop options can be configured, and the state of the loop can be
        /// monitored and manipulated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParallelLoopResult ForEach<TSource, TLocal>(OrderablePartitioner<TSource> source, ParallelOptions parallelOptions,
            Func<TLocal> localInit, Func<TSource, ParallelLoopState, long, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            ExceptionProvider.ThrowNotSupportedInvocationException(nameof(SystemTasks.Parallel.ForEach));
            return SystemTasks.Parallel.ForEach(source, parallelOptions, localInit, body, localFinally);
        }
    }
}
