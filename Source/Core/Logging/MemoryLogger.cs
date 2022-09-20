// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;

namespace Microsoft.Coyote.Logging
{
    /// <summary>
    /// Logger that writes all messages to memory.
    /// </summary>
    /// <remarks>
    /// This class is thread-safe.
    /// </remarks>
    public sealed class MemoryLogger : ILogger
    {
        /// <summary>
        /// The underlying string builder.
        /// </summary>
        private readonly StringBuilder Builder;

        /// <summary>
        /// The level of verbosity used during logging.
        /// </summary>
        private readonly VerbosityLevel VerbosityLevel;

        /// <summary>
        /// Synchronizes access to the logger.
        /// </summary>
        private readonly object Lock;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryLogger"/> class.
        /// </summary>
        public MemoryLogger(VerbosityLevel level)
        {
            this.Builder = new StringBuilder();
            this.VerbosityLevel = level;
            this.Lock = new object();
        }

        /// <inheritdoc/>
        public void Write(string value) => this.Write(LogSeverity.Info, value);

        /// <inheritdoc/>
        public void Write(string format, object arg0) => this.Write(LogSeverity.Info, format, arg0);

        /// <inheritdoc/>
        public void Write(string format, object arg0, object arg1) =>
            this.Write(LogSeverity.Info, format, arg0, arg1);

        /// <inheritdoc/>
        public void Write(string format, object arg0, object arg1, object arg2) =>
            this.Write(LogSeverity.Info, format, arg0, arg1, arg2);

        /// <inheritdoc/>
        public void Write(string format, params object[] args) =>
            this.Write(LogSeverity.Info, string.Format(format, args));

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string value)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                lock (this.Lock)
                {
                    this.Builder.Append(value);
                }
            }
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, object arg0)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                lock (this.Lock)
                {
                    this.Builder.AppendFormat(format, arg0);
                }
            }
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, object arg0, object arg1)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                lock (this.Lock)
                {
                    this.Builder.AppendFormat(format, arg0, arg1);
                }
            }
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, object arg0, object arg1, object arg2)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                lock (this.Lock)
                {
                    this.Builder.AppendFormat(format, arg0, arg1, arg2);
                }
            }
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, params object[] args)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                lock (this.Lock)
                {
                    this.Builder.AppendFormat(format, args);
                }
            }
        }

        /// <inheritdoc/>
        public void WriteLine(string value) => this.WriteLine(LogSeverity.Info, value);

        /// <inheritdoc/>
        public void WriteLine(string format, object arg0) => this.WriteLine(LogSeverity.Info, format, arg0);

        /// <inheritdoc/>
        public void WriteLine(string format, object arg0, object arg1) =>
            this.WriteLine(LogSeverity.Info, format, arg0, arg1);

        /// <inheritdoc/>
        public void WriteLine(string format, object arg0, object arg1, object arg2) =>
            this.WriteLine(LogSeverity.Info, format, arg0, arg1, arg2);

        /// <inheritdoc/>
        public void WriteLine(string format, params object[] args) =>
            this.WriteLine(LogSeverity.Info, string.Format(format, args));

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string value)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                lock (this.Lock)
                {
                    this.Builder.AppendLine(value);
                }
            }
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, object arg0)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                lock (this.Lock)
                {
                    this.Builder.AppendFormat(format, arg0);
                    this.Builder.AppendLine();
                }
            }
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, object arg0, object arg1)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                lock (this.Lock)
                {
                    this.Builder.AppendFormat(format, arg0, arg1);
                    this.Builder.AppendLine();
                }
            }
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, object arg0, object arg1, object arg2)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                lock (this.Lock)
                {
                    this.Builder.AppendFormat(format, arg0, arg1, arg2);
                    this.Builder.AppendLine();
                }
            }
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, params object[] args)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                lock (this.Lock)
                {
                    this.Builder.AppendFormat(format, args);
                    this.Builder.AppendLine();
                }
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            lock (this.Lock)
            {
                return this.Builder.ToString();
            }
        }

        /// <summary>
        /// Releases any resources held by the logger.
        /// </summary>
        public void Dispose()
        {
            lock (this.Lock)
            {
                this.Builder.Clear();
            }
        }
    }
}
