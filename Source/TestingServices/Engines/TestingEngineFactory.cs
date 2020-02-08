// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Reflection;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Tasks;

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
        public static ITestingEngine CreateBugFindingEngine(Configuration configuration, Action<IActorRuntime> test) =>
            BugFindingEngine.Create(configuration, test);

        /// <summary>
        /// Creates a new bug-finding engine.
        /// </summary>
        public static ITestingEngine CreateBugFindingEngine(Configuration configuration, Func<ControlledTask> test) =>
            BugFindingEngine.Create(configuration, test);

        /// <summary>
        /// Creates a new bug-finding engine.
        /// </summary>
        public static ITestingEngine CreateBugFindingEngine(Configuration configuration, Func<IActorRuntime, ControlledTask> test) =>
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
        public static ITestingEngine CreateReplayEngine(Configuration configuration, Action<IActorRuntime> test) =>
            ReplayEngine.Create(configuration, test);

        /// <summary>
        /// Creates a new replay engine.
        /// </summary>
        public static ITestingEngine CreateReplayEngine(Configuration configuration, Func<ControlledTask> test) =>
            ReplayEngine.Create(configuration, test);

        /// <summary>
        /// Creates a new replay engine.
        /// </summary>
        public static ITestingEngine CreateReplayEngine(Configuration configuration, Func<IActorRuntime, ControlledTask> test) =>
            ReplayEngine.Create(configuration, test);
    }
}
