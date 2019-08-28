// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Coyote.TestingServices
{
    /// <summary>
    /// Interface of a Coyote testing engine.
    /// </summary>
    public interface ITestingEngine
    {
        /// <summary>
        /// Data structure containing information
        /// gathered during testing.
        /// </summary>
        TestReport TestReport { get; }

        /// <summary>
        /// Runs the Coyote testing engine.
        /// </summary>
        /// <returns>ITestingEngine</returns>
        ITestingEngine Run();

        /// <summary>
        /// Stops the Coyote testing engine.
        /// </summary>
        void Stop();

        /// <summary>
        /// Tries to emit the testing traces, if any.
        /// </summary>
        /// <param name="directory">Directory name</param>
        /// <param name="file">File name</param>
        void TryEmitTraces(string directory, string file);

        /// <summary>
        /// Registers a callback to invoke at the end
        /// of each iteration. The callback takes as
        /// a parameter an integer representing the
        /// current iteration.
        /// </summary>
        /// <param name="callback">Callback</param>
        void RegisterPerIterationCallBack(Action<int> callback);

        /// <summary>
        /// Returns a report with the testing results.
        /// </summary>
        /// <returns>Report</returns>
        string Report();
    }
}
