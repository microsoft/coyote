// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Microsoft.Coyote.IO;

namespace Microsoft.Coyote.Utilities
{
    /// <summary>
    /// Reports errors and warnings to the user.
    /// </summary>
    public sealed class ErrorReporter
    {
        /// <summary>
        /// Configuration.
        /// </summary>
        private readonly Configuration Configuration;

        /// <summary>
        /// The installed logger.
        /// </summary>
        internal ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorReporter"/> class.
        /// </summary>
        internal ErrorReporter(Configuration configuration, ILogger logger)
        {
            this.Configuration = configuration;
            this.Logger = logger ?? new ConsoleLogger();
        }

        /// <summary>
        /// Reports an error, followed by the current line terminator.
        /// </summary>
        public void WriteErrorLine(string value)
        {
            this.Write("Error: ", ConsoleColor.Red);
            this.Write(value, ConsoleColor.Yellow);
            this.Logger.WriteLine(string.Empty);
        }

        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        private void Write(string value, ConsoleColor color)
        {
            lock (Error.ColorLock)
            {
                ConsoleColor previousForegroundColor = default;
                if (this.Configuration.EnableColoredConsoleOutput)
                {
                    previousForegroundColor = Console.ForegroundColor;
                    Console.ForegroundColor = color;
                }

                try
                {
                    this.Logger.Write(value);
                }
                finally
                {
                    if (this.Configuration.EnableColoredConsoleOutput)
                    {
                        Console.ForegroundColor = previousForegroundColor;
                    }
                }
            }
        }
    }
}
