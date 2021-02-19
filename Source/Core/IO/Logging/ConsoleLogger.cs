// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;

namespace Microsoft.Coyote.IO
{
    /// <summary>
    /// Logger that writes text to the console.
    /// </summary>
    /// <remarks>
    /// See <see href="/coyote/advanced-topics/actors/logging" >Logging</see> for more information.
    /// </remarks>
    public sealed class ConsoleLogger : TextWriter, ILogger
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleLogger"/> class.
        /// </summary>
        public ConsoleLogger()
        {
        }

        /// <inheritdoc/>
        public TextWriter TextWriter => this;

        /// <summary>
        /// When overridden in a derived class, returns the character encoding in which the
        /// output is written.
        /// </summary>
        public override Encoding Encoding => Console.OutputEncoding;

        /// <summary>
        /// The level of detail to report.
        /// </summary>
        public LogSeverity LogLevel { get; set; }

        /// <inheritdoc/>
        public override void Write(string value)
        {
            this.Write(LogSeverity.Informational, value);
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string value)
        {
            switch (severity)
            {
                case LogSeverity.Informational:
                    if (this.LogLevel <= LogSeverity.Informational)
                    {
                        Console.Write(value);
                    }

                    break;
                case LogSeverity.Warning:
                    if (this.LogLevel <= LogSeverity.Warning)
                    {
                        Error.Write(Console.Out, ConsoleColor.Yellow, value);
                    }

                    break;
                case LogSeverity.Error:
                    if (this.LogLevel <= LogSeverity.Error)
                    {
                        Error.Write(Console.Out, ConsoleColor.Red, value);
                    }

                    break;

                case LogSeverity.Important:
                    Console.Write(value);
                    break;

                default:
                    break;
            }
        }

        /// <inheritdoc/>
        public void Write(LogSeverity severity, string format, params object[] args)
        {
            string value = string.Format(format, args);
            this.Write(severity, value);
        }

        /// <inheritdoc/>
        public override void WriteLine(string value)
        {
            this.WriteLine(LogSeverity.Informational, value);
        }

        /// <inheritdoc/>
        public override void WriteLine(string format, params object[] args)
        {
            string value = string.Format(format, args);
            this.WriteLine(LogSeverity.Informational, value);
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string value)
        {
            switch (severity)
            {
                case LogSeverity.Informational:
                    if (this.LogLevel <= LogSeverity.Informational)
                    {
                        Console.WriteLine(value);
                    }

                    break;
                case LogSeverity.Warning:
                    if (this.LogLevel <= LogSeverity.Warning)
                    {
                        Error.Write(Console.Out, ConsoleColor.Yellow, value);
                        Console.WriteLine();
                    }

                    break;
                case LogSeverity.Error:
                    if (this.LogLevel <= LogSeverity.Error)
                    {
                        Error.Write(Console.Out, ConsoleColor.Red, value);
                        Console.WriteLine();
                    }

                    break;

                case LogSeverity.Important:
                    Console.WriteLine(value);
                    break;

                default:
                    break;
            }
        }

        /// <inheritdoc/>
        public void WriteLine(LogSeverity severity, string format, params object[] args)
        {
            string value = string.Format(format, args);
            this.WriteLine(severity, value);
        }
    }
}
