// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Coyote.IO
{
    /// <summary>
    /// Interface of the runtime logger.
    /// </summary>
    public interface ILogger : IDisposable
    {
        /// <summary>
        /// If true, then messages are logged.
        /// </summary>
        bool IsVerbose { get; set; }

        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        void Write(string value);

        /// <summary>
        /// Writes the text representation of the specified argument.
        /// </summary>
        void Write(string format, object arg0);

        /// <summary>
        /// Writes the text representation of the specified arguments.
        /// </summary>
        void Write(string format, object arg0, object arg1);

        /// <summary>
        /// Writes the text representation of the specified arguments.
        /// </summary>
        void Write(string format, object arg0, object arg1, object arg2);

        /// <summary>
        /// Writes the text representation of the specified array of objects.
        /// </summary>
        void Write(string format, params object[] args);

        /// <summary>
        /// Writes the specified string value, followed by the
        /// current line terminator.
        /// </summary>
        void WriteLine(string value);

        /// <summary>
        /// Writes the text representation of the specified argument, followed by the
        /// current line terminator.
        /// </summary>
        void WriteLine(string format, object arg0);

        /// <summary>
        /// Writes the text representation of the specified arguments, followed by the
        /// current line terminator.
        /// </summary>
        void WriteLine(string format, object arg0, object arg1);

        /// <summary>
        /// Writes the text representation of the specified arguments, followed by the
        /// current line terminator.
        /// </summary>
        void WriteLine(string format, object arg0, object arg1, object arg2);

        /// <summary>
        /// Writes the text representation of the specified array of objects,
        /// followed by the current line terminator.
        /// </summary>
        void WriteLine(string format, params object[] args);
    }
}
