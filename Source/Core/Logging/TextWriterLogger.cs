// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;

namespace Microsoft.Coyote.Logging
{
    /// <summary>
    /// Logger that writes to the specified <see cref="System.IO.TextWriter"/>.
    /// </summary>
    public sealed class TextWriterLogger : ILogger
    {
        /// <summary>
        /// The user-provided <see cref="System.IO.TextWriter"/>.
        /// </summary>
        private readonly TextWriter Logger;

        /// <summary>
        /// The level of verbosity used during logging.
        /// </summary>
        private readonly VerbosityLevel VerbosityLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextWriterLogger"/> class.
        /// </summary>
        public TextWriterLogger(TextWriter logger, VerbosityLevel level)
        {
            this.Logger = logger;
            this.VerbosityLevel = level;
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
                this.Logger.Write(value);
            }
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, object arg0)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                this.Logger.Write(format, arg0);
            }
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, object arg0, object arg1)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                this.Logger.Write(format, arg0, arg1);
            }
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, object arg0, object arg1, object arg2)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                this.Logger.Write(format, arg0, arg1, arg2);
            }
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, params object[] args)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                this.Logger.Write(format, args);
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
                this.Logger.WriteLine(value);
            }
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, object arg0)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                this.Logger.WriteLine(format, arg0);
            }
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, object arg0, object arg1)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                this.Logger.WriteLine(format, arg0, arg1);
            }
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, object arg0, object arg1, object arg2)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                this.Logger.WriteLine(format, arg0, arg1, arg2);
            }
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, params object[] args)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                this.Logger.WriteLine(format, args);
            }
        }

        /// <summary>
        /// Releases any resources held by the logger.
        /// </summary>
        public void Dispose() => this.Logger.Dispose();
    }
}
