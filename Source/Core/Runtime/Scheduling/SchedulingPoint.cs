// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Runtime.CompilerServices;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Provides a set of static methods for declaring points in the execution where interleavings
    /// between operations should be explored during testing.
    /// </summary>
    /// <remarks>
    /// These methods are thread-safe and no-op unless the test engine is attached.
    /// </remarks>
    public static class SchedulingPoint
    {
        /// <summary>
        /// Explores a possible interleaving with another controlled operation.
        /// </summary>
        public static void Interleave()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    runtime.ScheduleNextOperation(current, SchedulingPointType.Interleave, isSuppressible: false);
                }
                else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                {
                    runtime.DelayOperation(current);
                }
            }
        }

        /// <summary>
        /// Attempts to yield execution to another controlled operation.
        /// </summary>
        /// <remarks>
        /// Invoking this method might lower the scheduling priority of the currently executing
        /// operation when certain exploration strategies are used.
        /// </remarks>
        public static void Yield()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    runtime.ScheduleNextOperation(current, SchedulingPointType.Yield, isSuppressible: false, isYielding: true);
                }
                else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                {
                    runtime.DelayOperation(current);
                }
            }
        }

#pragma warning disable CA1801 // Parameter not used
        /// <summary>
        /// Explores a possible interleaving due to a 'READ' operation on the specified shared state.
        /// </summary>
        /// <param name="state">The shared state that is being read represented as a string.</param>
        /// <param name="comparer">
        /// Checks if the read shared state is equal with another shared state that is being accessed concurrently.
        /// </param>
        public static void Read(string state, IEqualityComparer<string> comparer = default)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    runtime.ScheduleNextOperation(current, SchedulingPointType.Read, isSuppressible: false);
                }
                else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                {
                    runtime.DelayOperation(current);
                }
            }
        }

        /// <summary>
        /// Explores a possible interleaving due to a 'WRITE' operation on the specified shared state.
        /// </summary>
        /// <param name="state">The shared state that is being written represented as a string.</param>
        /// <param name="comparer">
        /// Checks if the written shared state is equal with another shared state that is being accessed concurrently.
        /// </param>
        public static void Write(string state, IEqualityComparer<string> comparer = default)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    runtime.ScheduleNextOperation(current, SchedulingPointType.Write, isSuppressible: false);
                }
                else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                {
                    runtime.DelayOperation(current);
                }
            }
        }

        /// <summary>
        /// Waits for a signal to a resource with the specified name.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static void Wait(string name)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                using (runtime.EnterSynchronizedSection())
                {
                    if (!runtime.SignalMap.TryGetValue(name, out var signal))
                    {
                        runtime.SignalMap.Add(name, 0);
                    }

                    runtime.SignalMap[name]--;
                    if (runtime.SignalMap[name] < 0)
                    {
                        runtime.LogWriter.LogDebug("[coyote::debug] Operation '{0}' is waiting a signal for '{1}'.",
                            current.Name, name);
                        runtime.SignalMap[name] = 0;
                        current.Status = OperationStatus.Suppressed;
                        runtime.OperationSignalAwaiters.Add(current.Id, name);
                        runtime.ScheduleNextOperation(current, SchedulingPointType.Default, isSuppressible: false);
                    }
                }
            }
        }

        /// <summary>
        /// Waits asynchronously for a signal to a resource with the specified name.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static SignalAwaitable WaitAsync(string name)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                return new SignalAwaitable(runtime, current, name);
            }

            return new SignalAwaitable(null, null, name);
        }

        /// <summary>
        /// Signals a resource with the specified name.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static void Signal(string name)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                using (runtime.EnterSynchronizedSection())
                {
                    foreach (var kvp in runtime.OperationSignalAwaiters.Where(kvp => kvp.Value == name).ToList())
                    {
                        var op = runtime.GetOperationWithId(kvp.Key);
                        runtime.LogWriter.LogDebug("[coyote::debug] Operation '{0}' is signaled for '{1}'.",
                            op.Name, name);
                        op.Status = OperationStatus.Enabled;
                        runtime.OperationSignalAwaiters.Remove(kvp.Key);
                    }

                    if (!runtime.SignalMap.TryGetValue(name, out var signal))
                    {
                        runtime.SignalMap.Add(name, 0);
                    }

                    runtime.SignalMap[name]++;
                    runtime.ScheduleNextOperation(current, SchedulingPointType.Default, isSuppressible: false);
                }
            }
        }

        /// <summary>
        /// Suppresses interleavings until <see cref="Resume"/> is invoked.
        /// </summary>
        /// <remarks>
        /// This method does not suppress interleavings that happen when an operation is waiting
        /// some other operation to complete, when an operation completes and the scheduler
        /// switches to a new operation, or interleavings from uncontrolled concurrency.
        /// </remarks>
        public static void Suppress()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                runtime.SuppressScheduling();
            }
        }

        /// <summary>
        /// Resumes interleavings that were suppressed by invoking <see cref="Suppress"/>.
        /// </summary>
        public static void Resume()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                runtime.ResumeScheduling();
            }
        }

        /// <summary>
        /// Sets a checkpoint in the execution path that is so far explored during the current
        /// test iteration. This will capture all controlled scheduling and nondeterministic
        /// decisions taken until the checkpoint and the testing engine will then try to replay
        /// the same decisions in subsequent iterations before performing any new exploration.
        /// </summary>
        /// <remarks>
        /// Only a single checkpoint can be set at a time, and invoking this method with an
        /// existing checkpoint will either extend it with new decisions, or overwrite it if
        /// the new checkpoint diverges or is empty.
        /// </remarks>
        public static void SetCheckpoint()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                runtime.CheckpointExecutionTrace();
            }
        }

        /// <summary>
        /// Returns true if the specified scheduling point is used-defined.
        /// </summary>
        /// <remarks>
        /// A user-defined scheduling point is one that can be explicitly created
        /// by invoking one of the <see cref="SchedulingPoint"/> methods.
        /// </remarks>
        internal static bool IsUserDefined(SchedulingPointType type) =>
            type is SchedulingPointType.Interleave ||
            type is SchedulingPointType.Yield ||
            type is SchedulingPointType.Read ||
            type is SchedulingPointType.Write;
    }
}
