// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;

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
        public static void Start(ulong operationId, ManualResetEventSlim handshakeSync)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None)
            {
                var op = runtime.GetOperationWithId(operationId);
                if (op is null)
                {
                    throw new InvalidOperationException($"Operation with id '{operationId}' does not exist.");
                }

                runtime.StartOperation(op, handshakeSync);
            }
        }

        /// <summary>
        /// Waits for the operation with the specified id to start executing.
        /// </summary>
        private static void WaitOperationStart(ulong operationId, ManualResetEventSlim handshakeSync)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None)
            {
                var op = runtime.GetOperationWithId(operationId);
                if (op is null)
                {
                    throw new InvalidOperationException($"Operation with id '{operationId}' does not exist.");
                }

                runtime.WaitOperationStart(op, handshakeSync);
            }
        }

        /// <summary>
        /// Pauses the currently executing operation until the specified condition gets satisfied.
        /// </summary>
        public static void PauseUntil(Func<bool> condition)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                runtime.PauseOperationUntil(condition);
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
                    runtime.PauseOperationUntil(() => op.Status == OperationStatus.Completed);
                }
            }
        }

        /// <summary>
        /// Schedules the next enabled operation, which can include the currently executing operation.
        /// </summary>
        /// <remarks>
        /// An enabled operation is one that is not blocked nor completed.
        /// </remarks>
        public static void ScheduleNext() => SchedulingPoint.Default();

        /// <summary>
        /// Completes the currently executing operation.
        /// </summary>
        public static void Complete()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None)
            {
                ControlledOperation current = runtime.GetExecutingOperation();
                if (current != null)
                {
                    runtime.CompleteOperation(current);
                }
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
    }
}
