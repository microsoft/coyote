// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Visualization
{
    /// <summary>
    /// The execution trace visualization format to use during testing.
    /// </summary>
    internal enum TraceFormat
    {
        DGML = 0,
        GraphViz
    }

    /// <summary>
    /// Extension methods for the <see cref="TraceFormat"/>.
    /// </summary>
    internal static class TraceFormatExtensions
    {
        /// <summary>
        /// Returns the <see cref="TraceFormat"/> from the specified string.
        /// </summary>
        internal static TraceFormat FromString(string format) => format switch
            {
                "dgml" => TraceFormat.DGML,
                "graphviz" => TraceFormat.GraphViz,
                _ => throw new ArgumentOutOfRangeException($"The trace format '{format}' is not expected.")
            };

        /// <summary>
        /// Returns the file extension to be used for the specified <see cref="TraceFormat"/>.
        /// </summary>
        internal static string GetFileExtension(TraceFormat format) => format switch
            {
                TraceFormat.DGML => "dgml",
                TraceFormat.GraphViz => "gv",
                _ => throw new ArgumentOutOfRangeException($"The trace format '{format}' is not expected.")
            };
    }
}
