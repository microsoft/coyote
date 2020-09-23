// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;

namespace Microsoft.Coyote.IO
{
    /// <summary>
    /// A logger is used to capture messages, warnings and errors.
    /// </summary>
    public interface ILogger : IDisposable
    {
        /// <summary>
        /// This property provides a TextWriter that implements ILogger which is handy if you
        /// have existing code that requires a TextWriter.
        /// </summary>
        public TextWriter TextWriter { get; }

        /// <summary>
        /// Writes an informational string to the log.
        /// </summary>
        /// <param name="value">The string to write.</param>
        public void Write(string value);

        /// <summary>
        /// Writes an informational string to the log.
        /// </summary>
        /// <param name="format">The string format to write.</param>
        /// <param name="args">The arguments needed to format the string.</param>
        public void Write(string format, params object[] args);

        /// <summary>
        /// Writes a string to the log.
        /// </summary>
        /// <param name="severity">The severity of the issue being logged.</param>
        /// <param name="value">The string to write.</param>
        public void Write(LogSeverity severity, string value);

        /// <summary>
        /// Writes a string to the log.
        /// </summary>
        /// <param name="severity">The severity of the issue being logged.</param>
        /// <param name="format">The string format to write.</param>
        /// <param name="args">The arguments needed to format the string.</param>
        public void Write(LogSeverity severity, string format, params object[] args);

        /// <summary>
        /// Writes an informational string to the log.
        /// </summary>
        /// <param name="value">The string to write.</param>
        public void WriteLine(string value);

        /// <summary>
        /// Writes an informational string to the log.
        /// </summary>
        /// <param name="format">The string format to write.</param>
        /// <param name="args">The arguments needed to format the string.</param>
        public void WriteLine(string format, params object[] args);

        /// <summary>
        /// Writes a string followed by a line terminator to the text string or stream.
        /// </summary>
        /// <param name="severity">The severity of the issue being logged.</param>
        /// <param name="value">The string to write.</param>
        public void WriteLine(LogSeverity severity, string value);

        /// <summary>
        /// Writes a string followed by a line terminator to the text string or stream.
        /// </summary>
        /// <param name="severity">The severity of the issue being logged.</param>
        /// <param name="format">The string format to write.</param>
        /// <param name="args">The arguments needed to format the string.</param>
        public void WriteLine(LogSeverity severity, string format, params object[] args);
    }
}
