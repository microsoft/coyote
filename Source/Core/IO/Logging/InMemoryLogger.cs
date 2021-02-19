// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;

namespace Microsoft.Coyote.IO
{
    /// <summary>
    /// Thread safe logger that writes text to an in-memory buffer.
    /// The buffered text can be extracted using the ToString() method.
    /// </summary>
    /// <remarks>
    /// See <see href="/coyote/advanced-topics/actors/logging" >Logging</see> for more information.
    /// </remarks>
    public sealed class InMemoryLogger : TextWriter, ILogger
    {
        /// <summary>
        /// Underlying string builder.
        /// </summary>
        private readonly StringBuilder Builder;

        /// <summary>
        /// Serializes access to the string writer.
        /// </summary>
        private readonly object Lock;

        /// <summary>
        /// Optional logger provided by the user to delegate logging to.
        /// </summary>
        internal ILogger UserLogger { get; set; }

        /// <inheritdoc/>
        public TextWriter TextWriter => this;

        /// <summary>
        /// When overridden in a derived class, returns the character encoding in which the
        /// output is written.
        /// </summary>
        public override Encoding Encoding => Encoding.Unicode;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryLogger"/> class.
        /// </summary>
        public InMemoryLogger()
        {
            this.Builder = new StringBuilder();
            this.Lock = new object();
        }

        /// <inheritdoc/>
        public override void Write(string value)
        {
            this.Write(LogSeverity.Informational, value);
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string value)
        {
            try
            {
                lock (this.Lock)
                {
                    this.Builder.Append(value);
                    if (this.UserLogger != null)
                    {
                        this.UserLogger.Write(severity, value);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // The writer was disposed.
            }
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, params object[] args)
        {
            string msg = string.Format(format, args);
            this.Write(severity, msg);
        }

        /// <inheritdoc/>
        public override void WriteLine(string value)
        {
            this.WriteLine(LogSeverity.Informational, value);
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string value)
        {
            try
            {
                lock (this.Lock)
                {
                    this.Builder.AppendLine(value);
                    if (this.UserLogger != null)
                    {
                        this.UserLogger.WriteLine(severity, value);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // The writer was disposed.
            }
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, params object[] args)
        {
            string msg = string.Format(format, args);
            this.WriteLine(severity, msg);
        }

        /// <summary>
        /// Returns the logged text as a string.
        /// </summary>
        public override string ToString()
        {
            lock (this.Lock)
            {
                return this.Builder.ToString();
            }
        }
    }
}
