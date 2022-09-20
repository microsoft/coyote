// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;

namespace Microsoft.Coyote.Logging
{
    /// <summary>
    /// Responsible for logging runtime messages using the installed <see cref="ILogger"/>,
    /// as well as writing all observed messages to memory.
    /// </summary>
    internal sealed class MemoryLogWriter : LogWriter
    {
        /// <summary>
        /// Buffer where all observed messages are written.
        /// </summary>
        private readonly StringBuilder Builder;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryLogWriter"/> class.
        /// </summary>
        internal MemoryLogWriter(Configuration configuration, bool isConsoleLoggingEnabled = false)
            : base(configuration, isConsoleLoggingEnabled)
        {
            this.Builder = new StringBuilder();
        }

        /// <inheritdoc/>
        public override void Write(LogSeverity severity, string value)
        {
            if (this.IsObservable(severity))
            {
                lock (this.Lock)
                {
                    this.Builder.Append(value);
                }
            }

            base.Write(severity, value);
        }

        /// <inheritdoc/>
        public override void Write(LogSeverity severity, string format, object arg0)
        {
            if (this.IsObservable(severity))
            {
                lock (this.Lock)
                {
                    this.Builder.AppendFormat(format, arg0);
                }
            }

            base.Write(severity, format, arg0);
        }

        /// <inheritdoc/>
        public override void Write(LogSeverity severity, string format, object arg0, object arg1)
        {
            if (this.IsObservable(severity))
            {
                lock (this.Lock)
                {
                    this.Builder.AppendFormat(format, arg0, arg1);
                }
            }

            base.Write(severity, format, arg0, arg1);
        }

        /// <inheritdoc/>
        public override void Write(LogSeverity severity, string format, object arg0, object arg1, object arg2)
        {
            if (this.IsObservable(severity))
            {
                lock (this.Lock)
                {
                    this.Builder.AppendFormat(format, arg0, arg1, arg2);
                }
            }

            base.Write(severity, format, arg0, arg1, arg2);
        }

        /// <inheritdoc/>
        public override void Write(LogSeverity severity, string format, params object[] args)
        {
            if (this.IsObservable(severity))
            {
                lock (this.Lock)
                {
                    this.Builder.AppendFormat(format, args);
                }
            }

            base.Write(severity, format, args);
        }

        /// <inheritdoc/>
        public override void WriteLine(LogSeverity severity, string value)
        {
            if (this.IsObservable(severity))
            {
                lock (this.Lock)
                {
                    this.Builder.AppendLine(value);
                }
            }

            base.WriteLine(severity, value);
        }

        /// <inheritdoc/>
        public override void WriteLine(LogSeverity severity, string format, object arg0)
        {
            if (this.IsObservable(severity))
            {
                lock (this.Lock)
                {
                    this.Builder.AppendFormat(format, arg0);
                    this.Builder.AppendLine();
                }
            }

            base.WriteLine(severity, format, arg0);
        }

        /// <inheritdoc/>
        public override void WriteLine(LogSeverity severity, string format, object arg0, object arg1)
        {
            if (this.IsObservable(severity))
            {
                lock (this.Lock)
                {
                    this.Builder.AppendFormat(format, arg0, arg1);
                    this.Builder.AppendLine();
                }
            }

            base.WriteLine(severity, format, arg0, arg1);
        }

        /// <inheritdoc/>
        public override void WriteLine(LogSeverity severity, string format, object arg0, object arg1, object arg2)
        {
            if (this.IsObservable(severity))
            {
                lock (this.Lock)
                {
                    this.Builder.AppendFormat(format, arg0, arg1, arg2);
                    this.Builder.AppendLine();
                }
            }

            base.WriteLine(severity, format, arg0, arg1, arg2);
        }

        /// <inheritdoc/>
        public override void WriteLine(LogSeverity severity, string format, params object[] args)
        {
            if (this.IsObservable(severity))
            {
                lock (this.Lock)
                {
                    this.Builder.AppendFormat(format, args);
                    this.Builder.AppendLine();
                }
            }

            base.WriteLine(severity, format, args);
        }

        /// <summary>
        /// Returns any observed messages that have been written in memory.
        /// </summary>
        internal string GetObservedMessages()
        {
            lock (this.Lock)
            {
                return this.Builder.ToString();
            }
        }

        /// <summary>
        /// Clears any observed messages that have been written in memory.
        /// </summary>
        internal void ClearObservedMessages()
        {
            lock (this.Lock)
            {
                this.Builder.Clear();
            }
        }

        /// <summary>
        /// Checks if the specified log severity can be observed.
        /// </summary>
        private bool IsObservable(LogSeverity severity) => severity >= LogSeverity.Info || this.IsVerbose(severity);

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this.Lock)
                {
                    this.Builder.Clear();
                }
            }
        }
    }
}
