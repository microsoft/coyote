// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Logging
{
    /// <summary>
    /// A logger is used to capture messages, warnings and errors.
    /// </summary>
    public interface ILogger : IDisposable
    {
        /// <summary>
        /// Writes an informational string to the log.
        /// </summary>
        /// <param name="value">The string to write.</param>
        public void Write(string value);

        /// <summary>
        /// Writes an informational string to the log.
        /// </summary>
        /// <param name="format">The string format to write.</param>
        /// <param name="arg0">The first object to format and write.</param>
        public void Write(string format, object arg0);

        /// <summary>
        /// Writes an informational string to the log.
        /// </summary>
        /// <param name="format">The string format to write.</param>
        /// <param name="arg0">The first object to format and write.</param>
        /// <param name="arg1">The second object to format and write.</param>
        public void Write(string format, object arg0, object arg1);

        /// <summary>
        /// Writes an informational string to the log.
        /// </summary>
        /// <param name="format">The string format to write.</param>
        /// <param name="arg0">The first object to format and write.</param>
        /// <param name="arg1">The second object to format and write.</param>
        /// <param name="arg2">The third object to format and write.</param>
        public void Write(string format, object arg0, object arg1, object arg2);

        /// <summary>
        /// Writes an informational string to the log.
        /// </summary>
        /// <param name="format">The string format to write.</param>
        /// <param name="args">The arguments needed to format the string.</param>
        public void Write(string format, params object[] args);

        /// <summary>
        /// Writes a string to the log with the specified verbosity level.
        /// </summary>
        /// <param name="severity">The severity of the message being logged.</param>
        /// <param name="value">The string to write.</param>
        public void Write(LogSeverity severity, string value);

        /// <summary>
        /// Writes an informational string to the log.
        /// </summary>
        /// <param name="severity">The severity of the message being logged.</param>
        /// <param name="format">The string format to write.</param>
        /// <param name="arg0">The first object to format and write.</param>
        public void Write(LogSeverity severity, string format, object arg0);

        /// <summary>
        /// Writes an informational string to the log.
        /// </summary>
        /// <param name="severity">The severity of the message being logged.</param>
        /// <param name="format">The string format to write.</param>
        /// <param name="arg0">The first object to format and write.</param>
        /// <param name="arg1">The second object to format and write.</param>
        public void Write(LogSeverity severity, string format, object arg0, object arg1);

        /// <summary>
        /// Writes an informational string to the log.
        /// </summary>
        /// <param name="severity">The severity of the message being logged.</param>
        /// <param name="format">The string format to write.</param>
        /// <param name="arg0">The first object to format and write.</param>
        /// <param name="arg1">The second object to format and write.</param>
        /// <param name="arg2">The third object to format and write.</param>
        public void Write(LogSeverity severity, string format, object arg0, object arg1, object arg2);

        /// <summary>
        /// Writes a string to the log with the specified verbosity level.
        /// </summary>
        /// <param name="severity">The severity of the message being logged.</param>
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
        /// <param name="arg0">The first object to format and write.</param>
        public void WriteLine(string format, object arg0);

        /// <summary>
        /// Writes an informational string to the log.
        /// </summary>
        /// <param name="format">The string format to write.</param>
        /// <param name="arg0">The first object to format and write.</param>
        /// <param name="arg1">The second object to format and write.</param>
        public void WriteLine(string format, object arg0, object arg1);

        /// <summary>
        /// Writes an informational string to the log.
        /// </summary>
        /// <param name="format">The string format to write.</param>
        /// <param name="arg0">The first object to format and write.</param>
        /// <param name="arg1">The second object to format and write.</param>
        /// <param name="arg2">The third object to format and write.</param>
        public void WriteLine(string format, object arg0, object arg1, object arg2);

        /// <summary>
        /// Writes an informational string to the log.
        /// </summary>
        /// <param name="format">The string format to write.</param>
        /// <param name="args">The arguments needed to format the string.</param>
        public void WriteLine(string format, params object[] args);

        /// <summary>
        /// Writes a string followed by a line terminator to the text string or stream
        /// with the specified verbosity level.
        /// </summary>
        /// <param name="severity">The severity of the message being logged.</param>
        /// <param name="value">The string to write.</param>
        public void WriteLine(LogSeverity severity, string value);

        /// <summary>
        /// Writes an informational string to the log.
        /// </summary>
        /// <param name="severity">The severity of the message being logged.</param>
        /// <param name="format">The string format to write.</param>
        /// <param name="arg0">The first object to format and write.</param>
        public void WriteLine(LogSeverity severity, string format, object arg0);

        /// <summary>
        /// Writes an informational string to the log.
        /// </summary>
        /// <param name="severity">The severity of the message being logged.</param>
        /// <param name="format">The string format to write.</param>
        /// <param name="arg0">The first object to format and write.</param>
        /// <param name="arg1">The second object to format and write.</param>
        public void WriteLine(LogSeverity severity, string format, object arg0, object arg1);

        /// <summary>
        /// Writes an informational string to the log.
        /// </summary>
        /// <param name="severity">The severity of the message being logged.</param>
        /// <param name="format">The string format to write.</param>
        /// <param name="arg0">The first object to format and write.</param>
        /// <param name="arg1">The second object to format and write.</param>
        /// <param name="arg2">The third object to format and write.</param>
        public void WriteLine(LogSeverity severity, string format, object arg0, object arg1, object arg2);

        /// <summary>
        /// Writes a string followed by a line terminator to the text string or stream
        /// with the specified verbosity level.
        /// </summary>
        /// <param name="severity">The severity of the message being logged.</param>
        /// <param name="format">The string format to write.</param>
        /// <param name="args">The arguments needed to format the string.</param>
        public void WriteLine(LogSeverity severity, string format, params object[] args);
    }
}
