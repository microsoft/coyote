// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Logging
{
    /// <summary>
    /// Responsible for logging runtime messages using the installed <see cref="ILogger"/>,
    /// as well as optionally writing all observed messages to memory.
    /// </summary>
    internal class LogWriter : ILogger
    {
        /// <summary>
        /// The configuration used by the runtime.
        /// </summary>
        protected readonly Configuration Configuration;

        /// <summary>
        /// Used to log messages.
        /// </summary>
        internal ILogger Logger { get; private set; }

        /// <summary>
        /// Synchronizes access to the log writer.
        /// </summary>
        protected readonly object Lock;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogWriter"/> class.
        /// </summary>
        internal LogWriter(Configuration configuration, bool isConsoleLoggingEnabled = false)
        {
            this.Configuration = configuration;
            this.Lock = new object();
            if (this.Configuration.IsConsoleLoggingEnabled || isConsoleLoggingEnabled)
            {
                this.Logger = new ConsoleLogger(this.Configuration.VerbosityLevel);
            }
            else
            {
                this.Logger = new NullLogger();
            }
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
        public virtual void Write(LogSeverity severity, string value)
        {
            if (this.IsVerbose(severity))
            {
                try
                {
                    this.Logger.Write(severity, value);
                }
                catch (ObjectDisposedException)
                {
                    // The writer was disposed.
                }
            }
        }

        /// <inheritdoc/>
        public virtual void Write(LogSeverity severity, string format, object arg0)
        {
            if (this.IsVerbose(severity))
            {
                try
                {
                    this.Logger.Write(severity, format, arg0);
                }
                catch (ObjectDisposedException)
                {
                    // The writer was disposed.
                }
            }
        }

        /// <inheritdoc/>
        public virtual void Write(LogSeverity severity, string format, object arg0, object arg1)
        {
            if (this.IsVerbose(severity))
            {
                try
                {
                    this.Logger.Write(severity, format, arg0, arg1);
                }
                catch (ObjectDisposedException)
                {
                    // The writer was disposed.
                }
            }
        }

        /// <inheritdoc/>
        public virtual void Write(LogSeverity severity, string format, object arg0, object arg1, object arg2)
        {
            if (this.IsVerbose(severity))
            {
                try
                {
                    this.Logger.Write(severity, format, arg0, arg1, arg2);
                }
                catch (ObjectDisposedException)
                {
                    // The writer was disposed.
                }
            }
        }

        /// <inheritdoc/>
        public virtual void Write(LogSeverity severity, string format, params object[] args)
        {
            if (this.IsVerbose(severity))
            {
                try
                {
                    this.Logger.Write(severity, format, args);
                }
                catch (ObjectDisposedException)
                {
                    // The writer was disposed.
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
        public virtual void WriteLine(LogSeverity severity, string value)
        {
            if (this.IsVerbose(severity))
            {
                try
                {
                    this.Logger.WriteLine(severity, value);
                }
                catch (ObjectDisposedException)
                {
                    // The writer was disposed.
                }
            }
        }

        /// <inheritdoc/>
        public virtual void WriteLine(LogSeverity severity, string format, object arg0)
        {
            if (this.IsVerbose(severity))
            {
                try
                {
                    this.Logger.WriteLine(severity, format, arg0);
                }
                catch (ObjectDisposedException)
                {
                    // The writer was disposed.
                }
            }
        }

        /// <inheritdoc/>
        public virtual void WriteLine(LogSeverity severity, string format, object arg0, object arg1)
        {
            if (this.IsVerbose(severity))
            {
                try
                {
                    this.Logger.WriteLine(severity, format, arg0, arg1);
                }
                catch (ObjectDisposedException)
                {
                    // The writer was disposed.
                }
            }
        }

        /// <inheritdoc/>
        public virtual void WriteLine(LogSeverity severity, string format, object arg0, object arg1, object arg2)
        {
            if (this.IsVerbose(severity))
            {
                try
                {
                    this.Logger.WriteLine(severity, format, arg0, arg1, arg2);
                }
                catch (ObjectDisposedException)
                {
                    // The writer was disposed.
                }
            }
        }

        /// <inheritdoc/>
        public virtual void WriteLine(LogSeverity severity, string format, params object[] args)
        {
            if (this.IsVerbose(severity))
            {
                try
                {
                    this.Logger.WriteLine(severity, format, args);
                }
                catch (ObjectDisposedException)
                {
                    // The writer was disposed.
                }
            }
        }

        /// <summary>
        /// Logs the specified debug message.
        /// </summary>
        internal void LogDebug(string message) => this.WriteLine(LogSeverity.Debug, message);

        /// <summary>
        /// Logs the specified debug message.
        /// </summary>
        internal void LogDebug(string format, object arg0) =>
            this.WriteLine(LogSeverity.Debug, format, arg0);

        /// <summary>
        /// Logs the specified debug message.
        /// </summary>
        internal void LogDebug(string format, object arg0, object arg1) =>
            this.WriteLine(LogSeverity.Debug, format, arg0, arg1);

        /// <summary>
        /// Logs the specified debug message.
        /// </summary>
        internal void LogDebug(string format, object arg0, object arg1, object arg2) =>
            this.WriteLine(LogSeverity.Debug, format, arg0, arg1, arg2);

        /// <summary>
        /// Logs the specified debug message.
        /// </summary>
        internal void LogDebug(string format, params object[] args) =>
            this.WriteLine(LogSeverity.Debug, format, args);

        /// <summary>
        /// Logs the debug message produced by the specified function.
        /// </summary>
        internal void LogDebug(Func<string> messageProducer)
        {
            if (IsDebugVerbosityEnabled(this.Configuration.VerbosityLevel))
            {
                this.LogDebug(messageProducer());
            }
        }

        /// <summary>
        /// Logs the specified informational message.
        /// </summary>
        internal void LogInfo(string message) => this.WriteLine(LogSeverity.Info, message);

        /// <summary>
        /// Logs the specified informational message.
        /// </summary>
        internal void LogInfo(string format, object arg0) =>
            this.WriteLine(LogSeverity.Info, format, arg0);

        /// <summary>
        /// Logs the specified informational message.
        /// </summary>
        internal void LogInfo(string format, object arg0, object arg1) =>
            this.WriteLine(LogSeverity.Info, format, arg0, arg1);

        /// <summary>
        /// Logs the specified informational message.
        /// </summary>
        internal void LogInfo(string format, object arg0, object arg1, object arg2) =>
            this.WriteLine(LogSeverity.Info, format, arg0, arg1, arg2);

        /// <summary>
        /// Logs the specified informational message.
        /// </summary>
        internal void LogInfo(string format, params object[] args) =>
            this.WriteLine(LogSeverity.Info, format, args);

        /// <summary>
        /// Logs the specified warning message.
        /// </summary>
        internal void LogWarning(string message) => this.WriteLine(LogSeverity.Warning, message);

        /// <summary>
        /// Logs the specified warning message.
        /// </summary>
        internal void LogWarning(string format, object arg0) =>
            this.WriteLine(LogSeverity.Warning, format, arg0);

        /// <summary>
        /// Logs the specified warning message.
        /// </summary>
        internal void LogWarning(string format, object arg0, object arg1) =>
            this.WriteLine(LogSeverity.Warning, format, arg0, arg1);

        /// <summary>
        /// Logs the specified warning message.
        /// </summary>
        internal void LogWarning(string format, object arg0, object arg1, object arg2) =>
            this.WriteLine(LogSeverity.Warning, format, arg0, arg1, arg2);

        /// <summary>
        /// Logs the specified warning message.
        /// </summary>
        internal void LogWarning(string format, params object[] args) =>
            this.WriteLine(LogSeverity.Warning, format, args);

        /// <summary>
        /// Logs the specified error message.
        /// </summary>
        internal void LogError(string message) => this.WriteLine(LogSeverity.Error, message);

        /// <summary>
        /// Logs the specified error message.
        /// </summary>
        internal void LogError(string format, object arg0) =>
            this.WriteLine(LogSeverity.Error, format, arg0);

        /// <summary>
        /// Logs the specified error message.
        /// </summary>
        internal void LogError(string format, object arg0, object arg1) =>
            this.WriteLine(LogSeverity.Error, format, arg0, arg1);

        /// <summary>
        /// Logs the specified error message.
        /// </summary>
        internal void LogError(string format, object arg0, object arg1, object arg2) =>
            this.WriteLine(LogSeverity.Error, format, arg0, arg1, arg2);

        /// <summary>
        /// Logs the specified error message.
        /// </summary>
        internal void LogError(string format, params object[] args) =>
            this.WriteLine(LogSeverity.Error, format, args);

        /// <summary>
        /// Logs the specified important message.
        /// </summary>
        internal void LogImportant(string message) => this.WriteLine(LogSeverity.Important, message);

        /// <summary>
        /// Logs the specified important message.
        /// </summary>
        internal void LogImportant(string format, object arg0) =>
            this.WriteLine(LogSeverity.Important, format, arg0);

        /// <summary>
        /// Logs the specified important message.
        /// </summary>
        internal void LogImportant(string format, object arg0, object arg1) =>
            this.WriteLine(LogSeverity.Important, format, arg0, arg1);

        /// <summary>
        /// Logs the specified important message.
        /// </summary>
        internal void LogImportant(string format, object arg0, object arg1, object arg2) =>
            this.WriteLine(LogSeverity.Important, format, arg0, arg1, arg2);

        /// <summary>
        /// Logs the specified important message.
        /// </summary>
        internal void LogImportant(string format, params object[] args) =>
            this.WriteLine(LogSeverity.Important, format, args);

        /// <summary>
        /// Use this method to override the default <see cref="ILogger"/>.
        /// </summary>
        internal void SetLogger(ILogger logger)
        {
            lock (this.Lock)
            {
                if (logger is null)
                {
                    throw new InvalidOperationException("Cannot set a null logger.");
                }
                else if (this.Configuration.IsConsoleLoggingEnabled)
                {
                    throw new InvalidOperationException($"Cannot set custom logger '{logger.GetType().FullName}' when console logging is enabled.");
                }

                if (this.IsRuntimeLogger())
                {
                    // Only dispose a logger created by the runtime.
                    this.Logger.Dispose();
                }

                this.Logger = logger;
            }
        }

        /// <summary>
        /// Checks if the specified log severity can be logged.
        /// </summary>
        internal bool IsVerbose(LogSeverity severity) => IsVerbose(severity, this.Configuration.VerbosityLevel);

        /// <summary>
        /// Checks if the specified log severity can be logged.
        /// </summary>
        internal static bool IsVerbose(LogSeverity severity, VerbosityLevel level) => level switch
            {
                VerbosityLevel.None => severity >= LogSeverity.Important,
                VerbosityLevel.Error => severity >= LogSeverity.Error,
                VerbosityLevel.Warning => severity >= LogSeverity.Warning,
                VerbosityLevel.Info => severity >= LogSeverity.Info,
                VerbosityLevel.Debug => severity >= LogSeverity.Debug,
                VerbosityLevel.Exhaustive => true,
                _ => false
            };

        /// <summary>
        /// Checks if debug log severity is enabled.
        /// </summary>
        private static bool IsDebugVerbosityEnabled(VerbosityLevel level) =>
            level is VerbosityLevel.Debug || level is VerbosityLevel.Exhaustive;

        /// <summary>
        /// Checks if the installed <see cref="ILogger"/> is a runtime logger.
        /// </summary>
        internal bool IsRuntimeLogger()
        {
            lock (this.Lock)
            {
                return this.Logger is ConsoleLogger || this.Logger is NullLogger;
            }
        }

        /// <summary>
        /// Releases any resources held by the log writer.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this.Lock)
                {
                    if (this.IsRuntimeLogger())
                    {
                        // Only dispose a logger created by the runtime.
                        this.Logger.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Releases any resources held by the log writer.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
