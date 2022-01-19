// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;

namespace Microsoft.Coyote
{
    /// <summary>
    /// The Coyote profiler.
    /// </summary>
    internal sealed class Profiler
    {
        private Stopwatch StopWatch = null;

        /// <summary>
        /// Starts measuring execution time.
        /// </summary>
        public void StartMeasuringExecutionTime()
        {
            this.StopWatch = new Stopwatch();
            this.StopWatch.Start();
        }

        /// <summary>
        /// Stops measuring execution time.
        /// </summary>
        public void StopMeasuringExecutionTime()
        {
            if (this.StopWatch != null)
            {
                this.StopWatch.Stop();
            }
        }

        /// <summary>
        /// Returns profiling results.
        /// </summary>
        public double Results() =>
            this.StopWatch != null ? this.StopWatch.Elapsed.TotalSeconds : 0;

        /// <summary>
        /// Returns profiling results.
        /// </summary>
        public double ResultsMs() =>
            this.StopWatch != null ? this.StopWatch.Elapsed.TotalMilliseconds : 0;
    }
}
