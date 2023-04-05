// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;

namespace Microsoft.Coyote.Logging
{
    /// <summary>
    /// Logger that writes text to the console.
    /// </summary>
    public sealed class ConsoleLogger : ILogger
    {
        /// <summary>
        /// The level of verbosity used during logging.
        /// </summary>
        private readonly VerbosityLevel VerbosityLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleLogger"/> class.
        /// </summary>
        public ConsoleLogger(VerbosityLevel level)
        {
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
                if (severity == LogSeverity.Warning || severity == LogSeverity.Error)
                {
                    using (new ConsoleColorManager(severity))
                    {
                        Console.Write(value);
                    }
                }
                else
                {
                    Console.Write(value);
                }
            }
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, object arg0)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                if (severity == LogSeverity.Warning || severity == LogSeverity.Error)
                {
                    using (new ConsoleColorManager(severity))
                    {
                        Console.Write(format, arg0);
                    }
                }
                else
                {
                    Console.Write(format, arg0);
                }
            }
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, object arg0, object arg1)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                if (severity == LogSeverity.Warning || severity == LogSeverity.Error)
                {
                    using (new ConsoleColorManager(severity))
                    {
                        Console.Write(format, arg0, arg1);
                    }
                }
                else
                {
                    Console.Write(format, arg0, arg1);
                }
            }
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, object arg0, object arg1, object arg2)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                if (severity == LogSeverity.Warning || severity == LogSeverity.Error)
                {
                    using (new ConsoleColorManager(severity))
                    {
                        Console.Write(format, arg0, arg1, arg2);
                    }
                }
                else
                {
                    Console.Write(format, arg0, arg1, arg2);
                }
            }
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, params object[] args)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                if (severity == LogSeverity.Warning || severity == LogSeverity.Error)
                {
                    using (new ConsoleColorManager(severity))
                    {
                        Console.Write(format, args);
                    }
                }
                else
                {
                    Console.Write(format, args);
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
                if (severity == LogSeverity.Warning || severity == LogSeverity.Error)
                {
                    using (new ConsoleColorManager(severity))
                    {
                        Console.WriteLine(value);
                    }
                }
                else
                {
                    Console.WriteLine(value);
                }
            }
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, object arg0)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                if (severity == LogSeverity.Warning || severity == LogSeverity.Error)
                {
                    using (new ConsoleColorManager(severity))
                    {
                        Console.WriteLine(format, arg0);
                    }
                }
                else
                {
                    Console.WriteLine(format, arg0);
                }
            }
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, object arg0, object arg1)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                if (severity == LogSeverity.Warning || severity == LogSeverity.Error)
                {
                    using (new ConsoleColorManager(severity))
                    {
                        Console.WriteLine(format, arg0, arg1);
                    }
                }
                else
                {
                    Console.WriteLine(format, arg0, arg1);
                }
            }
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, object arg0, object arg1, object arg2)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                if (severity == LogSeverity.Warning || severity == LogSeverity.Error)
                {
                    using (new ConsoleColorManager(severity))
                    {
                        Console.WriteLine(format, arg0, arg1, arg2);
                    }
                }
                else
                {
                    Console.WriteLine(format, arg0, arg1, arg2);
                }
            }
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, params object[] args)
        {
            if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
            {
                if (severity == LogSeverity.Warning || severity == LogSeverity.Error)
                {
                    using (new ConsoleColorManager(severity))
                    {
                        Console.WriteLine(format, args);
                    }
                }
                else
                {
                    Console.WriteLine(format, args);
                }
            }
        }

        /// <summary>
        /// Releases any resources held by the logger.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Sets and restores colors when logging messages to the console.
        /// </summary>
        private struct ConsoleColorManager : IDisposable
        {
            /// <summary>
            /// The original foreground color.
            /// </summary>
            private readonly ConsoleColor? OriginalForegroundColor;

            /// <summary>
            /// Serializes changes to the console color.
            /// </summary>
            private static readonly SemaphoreSlim Lock = new SemaphoreSlim(1, 1);

            /// <summary>
            /// Initializes a new instance of the <see cref="ConsoleColorManager"/> struct.
            /// </summary>
            internal ConsoleColorManager(LogSeverity severity)
            {
                var originalForegroundColor = Console.ForegroundColor;
                switch (severity)
                {
                    case LogSeverity.Warning:
                        Lock.Wait();
                        this.OriginalForegroundColor = originalForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LogSeverity.Error:
                        Lock.Wait();
                        this.OriginalForegroundColor = originalForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    default:
                        this.OriginalForegroundColor = null;
                        break;
                }
            }

            /// <summary>
            /// Restores the original console color.
            /// </summary>
            public void Dispose()
            {
                if (this.OriginalForegroundColor.HasValue)
                {
                    Console.ForegroundColor = this.OriginalForegroundColor.Value;
                    Lock.Release();
                }
            }
        }
    }
}
