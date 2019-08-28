// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Reflection;

namespace Microsoft.Coyote.TestingServices
{
    /// <summary>
    /// The Coyote testing engine factory.
    /// </summary>
    public static class TestingEngineFactory
    {
        /// <summary>
        /// Creates a new bug-finding engine.
        /// </summary>
        public static ITestingEngine CreateBugFindingEngine(Configuration configuration)
        {
            return BugFindingEngine.Create(configuration);
        }

        /// <summary>
        /// Creates a new bug-finding engine.
        /// </summary>
        public static ITestingEngine CreateBugFindingEngine(
            Configuration configuration, Assembly assembly)
        {
            return BugFindingEngine.Create(configuration, assembly);
        }

        /// <summary>
        /// Creates a new bug-finding engine.
        /// </summary>
        public static ITestingEngine CreateBugFindingEngine(
            Configuration configuration, Action<ICoyoteRuntime> action)
        {
            return BugFindingEngine.Create(configuration, action);
        }

        /// <summary>
        /// Creates a new replay engine.
        /// </summary>
        public static ITestingEngine CreateReplayEngine(Configuration configuration)
        {
            return ReplayEngine.Create(configuration);
        }

        /// <summary>
        /// Creates a new replay engine.
        /// </summary>
        public static ITestingEngine CreateReplayEngine(Configuration configuration, Assembly assembly)
        {
            return ReplayEngine.Create(configuration, assembly);
        }

        /// <summary>
        /// Creates a new replay engine.
        /// </summary>
        public static ITestingEngine CreateReplayEngine(Configuration configuration, Action<ICoyoteRuntime> action)
        {
            return ReplayEngine.Create(configuration, action);
        }
    }
}
