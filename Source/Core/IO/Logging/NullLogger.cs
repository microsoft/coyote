// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text;

namespace Microsoft.Coyote.IO
{
    /// <inheritdoc/>
    internal class NullLogger : TextWriter, ILogger
    {
        /// <inheritdoc/>
        public TextWriter TextWriter => this;

        /// <inheritdoc/>
        public override void Write(string value)
        {
        }

        /// <summary>
        /// When overridden in a derived class, returns the character encoding in which the
        /// output is written.
        /// </summary>
        public override Encoding Encoding => Encoding.Unicode;

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string value)
        {
        }

        /// <inheritdoc/>
        public override void Write(string format, params object[] args)
        {
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, params object[] args)
        {
        }

        /// <inheritdoc/>
        public override void WriteLine(string value)
        {
        }

        /// <inheritdoc/>
        public override void WriteLine(string format, params object[] args)
        {
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string value)
        {
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, params object[] args)
        {
        }
    }
}
