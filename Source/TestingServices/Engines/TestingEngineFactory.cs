// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Coyote.TestingServices
{
    /// <summary>
    /// The testing engine factory.
    /// </summary>
    public static class TestingEngineFactory
    {
        /// <summary>
        /// Creates a new bug-finding engine.
        /// </summary>
        public static ITestingEngine CreateBugFindingEngine(Configuration configuration) =>
            BugFindingEngine.Create(configuration);

        /// <summary>
        /// Creates a new bug-finding engine.
        /// </summary>
        public static ITestingEngine CreateBugFindingEngine(Configuration configuration, Assembly assembly) =>
            BugFindingEngine.Create(configuration, assembly);

        /// <summary>
        /// Creates a new bug-finding engine.
        /// </summary>
        public static ITestingEngine CreateBugFindingEngine(Configuration configuration, Action test) =>
            BugFindingEngine.Create(configuration, test);

        /// <summary>
        /// Creates a new bug-finding engine.
        /// </summary>
        public static ITestingEngine CreateBugFindingEngine(Configuration configuration, Action<IMachineRuntime> test) =>
            BugFindingEngine.Create(configuration, test);

        /// <summary>
        /// Creates a new bug-finding engine.
        /// </summary>
        public static ITestingEngine CreateBugFindingEngine(Configuration configuration, Func<Task> test) =>
            BugFindingEngine.Create(configuration, test);

        /// <summary>
        /// Creates a new bug-finding engine.
        /// </summary>
        public static ITestingEngine CreateBugFindingEngine(Configuration configuration, Func<IMachineRuntime, Task> test) =>
            BugFindingEngine.Create(configuration, test);

        /// <summary>
        /// Creates a new replay engine.
        /// </summary>
        public static ITestingEngine CreateReplayEngine(Configuration configuration) =>
            ReplayEngine.Create(configuration);

        /// <summary>
        /// Creates a new replay engine.
        /// </summary>
        public static ITestingEngine CreateReplayEngine(Configuration configuration, Assembly assembly) =>
            ReplayEngine.Create(configuration, assembly);

        /// <summary>
        /// Creates a new replay engine.
        /// </summary>
        public static ITestingEngine CreateReplayEngine(Configuration configuration, Action test) =>
            ReplayEngine.Create(configuration, test);

        /// <summary>
        /// Creates a new replay engine.
        /// </summary>
        public static ITestingEngine CreateReplayEngine(Configuration configuration, Action<IMachineRuntime> test) =>
            ReplayEngine.Create(configuration, test);

        /// <summary>
        /// Creates a new replay engine.
        /// </summary>
        public static ITestingEngine CreateReplayEngine(Configuration configuration, Func<Task> test) =>
            ReplayEngine.Create(configuration, test);

        /// <summary>
        /// Creates a new replay engine.
        /// </summary>
        public static ITestingEngine CreateReplayEngine(Configuration configuration, Func<IMachineRuntime, Task> test) =>
            ReplayEngine.Create(configuration, test);
    }
}
