// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Runtime.CompilerServices;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Provides a set of static methods for instrumenting concurrency primitives
    /// that can then be controlled during testing.
    /// </summary>
    /// <remarks>
    /// These methods are thread-safe and no-op unless the test engine is attached.
    /// </remarks>
    public static class Operation
    {
        /// <summary>
        /// Returns the next available unique operation id, or null if the test engine is detached.
        /// </summary>
        public static ulong? GetNextId()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None)
            {
                return runtime.GetNextOperationId();
            }

            return null;
        }

        /// <summary>
        /// Creates a new controlled operation and returns its unique id, or null
        /// if the test engine is detached.
        /// </summary>
        public static ulong? CreateNext()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None)
            {
                var op = runtime.CreateControlledOperation();
                return op.Id;
            }

            return null;
        }

        /// <summary>
        /// Creates a new controlled operation from the specified builder and returns its
        /// unique id, or null if the test engine is detached.
        /// </summary>
        public static ulong? CreateFrom(IOperationBuilder builder)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None)
            {
                var op = runtime.CreateUserDefinedOperation(builder);
                return op.Id;
            }

            return null;
        }

        /// <summary>
        /// Starts executing the operation with the specified id.
        /// </summary>
        public static void Start(ulong operationId)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None)
            {
                var op = runtime.GetOperationWithId(operationId);
                if (op is null)
                {
                    throw new InvalidOperationException($"Operation with id '{operationId}' does not exist.");
                }

                runtime.StartOperation(op);
            }
        }

        /// <summary>
        /// Pauses the currently executing operation until the specified condition gets resolved.
        /// </summary>
        public static void PauseUntil(Func<bool> condition)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                runtime.PauseOperationUntil(default, condition);
            }
        }

        /// <summary>
        /// Pauses the currently executing operation until the operation with the specified id completes.
        /// </summary>
        public static void PauseUntilCompleted(ulong operationId)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                var op = runtime.GetOperationWithId(operationId);
                if (op != null)
                {
                    runtime.PauseOperationUntil(default, () => op.Status == OperationStatus.Completed);
                }
            }
        }

        /// <summary>
        /// Asynchronously pauses the currently executing operation until the operation with the specified id completes.
        /// If <paramref name="resumeAsynchronously"/> is set to true, then after the asynchronous pause, a new operation
        /// will be created to execute the continuation.
        /// </summary>
        public static PausedOperationAwaitable PauseUntilAsync(Func<bool> condition, bool resumeAsynchronously = false)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                return runtime.PauseOperationUntilAsync(condition, resumeAsynchronously);
            }

            return new PausedOperationAwaitable(runtime, null, condition, resumeAsynchronously);
        }

        /// <summary>
        /// Asynchronously pauses the currently executing operation until the operation with the specified id completes.
        /// If <paramref name="resumeAsynchronously"/> is set to true, then after the asynchronous pause, a new operation
        /// will be created to execute the continuation.
        /// </summary>
        public static PausedOperationAwaitable PauseUntilCompletedAsync(ulong operationId, bool resumeAsynchronously = false)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                var op = runtime.GetOperationWithId(operationId);
                if (op is null)
                {
                    throw new InvalidOperationException($"Operation with id '{operationId}' does not exist.");
                }

                return runtime.PauseOperationUntilAsync(() => op.Status == OperationStatus.Completed, resumeAsynchronously);
            }

            return new PausedOperationAwaitable(runtime, null, () => true, resumeAsynchronously);
        }

        /// <summary>
        /// Schedules the next enabled operation, which can include the currently executing operation.
        /// </summary>
        /// <remarks>
        /// An enabled operation is one that is not blocked nor completed.
        /// </remarks>
        public static void ScheduleNext()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    runtime.ScheduleNextOperation(current, SchedulingPointType.Default);
                }
                else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                {
                    runtime.DelayOperation(current);
                }
            }
        }

        /// <summary>
        /// Completes the currently executing operation.
        /// </summary>
        public static void Complete()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                runtime.CompleteOperation(current);
            }
        }

        /// <summary>
        /// Tries to reset the the operation with the specified id so that it can be reused.
        /// This is only allowed if the operation is already completed.
        /// </summary>
        public static bool TryReset(ulong operationId)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None)
            {
                var op = runtime.GetOperationWithId(operationId);
                if (op is null)
                {
                    throw new InvalidOperationException($"Operation with id '{operationId}' does not exist.");
                }

                return runtime.TryResetOperation(op);
            }

            return false;
        }

        /// <summary>
        /// Registers the method invoked by the currently executing operation.
        /// </summary>
        public static void RegisterCallSite(string method)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                current.LastCallSite = method;
            }
        }
    }
}
