// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.IO
{
    /// <summary>
    /// Logger that disposes all written text.
    /// </summary>
    internal sealed class NulLogger : ILogger
    {
        /// <summary>
        /// If true, then messages are logged. This logger ignores
        /// this value and always disposes any written text.
        /// </summary>
        public bool IsVerbose { get; set; } = false;

        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        public void Write(string value)
        {
        }

        /// <summary>
        /// Writes the text representation of the specified argument.
        /// </summary>
        public void Write(string format, object arg0)
        {
        }

        /// <summary>
        /// Writes the text representation of the specified arguments.
        /// </summary>
        public void Write(string format, object arg0, object arg1)
        {
        }

        /// <summary>
        /// Writes the text representation of the specified arguments.
        /// </summary>
        public void Write(string format, object arg0, object arg1, object arg2)
        {
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects.
        /// </summary>
        public void Write(string format, params object[] args)
        {
        }

        /// <summary>
        /// Writes the specified string value, followed by the
        /// current line terminator.
        /// </summary>
        public void WriteLine(string value)
        {
        }

        /// <summary>
        /// Writes the text representation of the specified argument, followed by the
        /// current line terminator.
        /// </summary>
        public void WriteLine(string format, object arg0)
        {
        }

        /// <summary>
        /// Writes the text representation of the specified arguments, followed by the
        /// current line terminator.
        /// </summary>
        public void WriteLine(string format, object arg0, object arg1)
        {
        }

        /// <summary>
        /// Writes the text representation of the specified arguments, followed by the
        /// current line terminator.
        /// </summary>
        public void WriteLine(string format, object arg0, object arg1, object arg2)
        {
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects,
        /// followed by the current line terminator.
        /// </summary>
        public void WriteLine(string format, params object[] args)
        {
        }

        /// <summary>
        /// Disposes the logger.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
