// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Specifications
{
    /// <summary>
    /// Provides static methods that are useful for writing specifications
    /// and interacting with the systematic testing engine.
    /// </summary>
    /// <remarks>
    /// See <see href="/coyote/concepts/specifications">Specifications Overview</see>
    /// for more information.
    /// </remarks>
    public static class Specification
    {
        /// <summary>
        /// Checks if the predicate holds, and if not, throws an exception.
        /// </summary>
        public static void Assert(bool predicate, string s, object arg0) =>
            CoyoteRuntime.Current.Assert(predicate, s, arg0);

        /// <summary>
        /// Checks if the predicate holds, and if not, throws an exception.
        /// </summary>
        public static void Assert(bool predicate, string s, object arg0, object arg1) =>
            CoyoteRuntime.Current.Assert(predicate, s, arg0, arg1);

        /// <summary>
        /// Checks if the predicate holds, and if not, throws an exception.
        /// </summary>
        public static void Assert(bool predicate, string s, object arg0, object arg1, object arg2) =>
            CoyoteRuntime.Current.Assert(predicate, s, arg0, arg1, arg2);

        /// <summary>
        /// Checks if the predicate holds, and if not, throws an exception.
        /// </summary>
        public static void Assert(bool predicate, string s, params object[] args) =>
            CoyoteRuntime.Current.Assert(predicate, s, args);

        /// <summary>
        /// Creates a monitor that checks if the specified task eventually completes its execution successfully,
        /// and if not, fails with a liveness property violation.
        /// </summary>
        /// <param name="task">The task to monitor.</param>
        /// <remarks>
        /// The liveness property is only checked during systematic testing.
        /// </remarks>
        public static void IsEventuallyCompletedSuccessfully(Task task) =>
            CoyoteRuntime.Current.MonitorTaskCompletion(task);

        /// <summary>
        /// Registers a new safety or liveness monitor.
        /// </summary>
        /// <typeparam name="T">Type of the monitor.</typeparam>
        public static void RegisterMonitor<T>()
            where T : Monitor =>
            CoyoteRuntime.Current.RegisterMonitor<T>();

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor.</typeparam>
        /// <param name="e">Event to send to the monitor.</param>
        public static void Monitor<T>(Monitor.Event e)
            where T : Monitor =>
            CoyoteRuntime.Current.Monitor<T>(e);

        /// <summary>
        /// Registers a new state hashing function that contributes to computing
        /// a representation of the program state in each scheduling step.
        /// </summary>
        /// <param name="func">The state hashing function.</param>
        /// <remarks>
        /// If you register more than one state hashing function per iteration, the
        /// runtime will aggregate the hashes computed from each function.
        /// </remarks>
        public static void RegisterStateHashingFunction(Func<ulong> func) =>
            CoyoteRuntime.Current.RegisterStateHashingFunction(func);
    }
}
