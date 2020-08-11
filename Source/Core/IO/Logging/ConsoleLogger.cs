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
    /// See <see href="/coyote/learn/core/logging" >Logging</see> for more information.
    /// </remarks>
    public sealed class ConsoleLogger : TextWriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleLogger"/> class.
        /// </summary>
        public ConsoleLogger()
        {
        }

        /// <summary>
        /// When overridden in a derived class, returns the character encoding in which the
        /// output is written.
        /// </summary>
        public override Encoding Encoding => Console.OutputEncoding;

        /// <summary>
        /// Writes the specified Unicode character value to the standard output stream.
        /// </summary>
        /// <param name="value">The Unicode character.</param>
        public override void Write(char value)
        {
            Console.Write(value);
        }

        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        public override void Write(string value)
        {
            Console.Write(value);
        }

        /// <summary>
        /// Writes a string followed by a line terminator to the text string or stream.
        /// </summary>
        /// <param name="value">The string to write.</param>
        public override void WriteLine(string value)
        {
            Console.WriteLine(value);
        }

        /// <summary>
        /// Reports an error, followed by the current line terminator.
        /// </summary>
        /// <param name="value">The string to write.</param>
#pragma warning disable CA1822 // Mark members as static
        public void WriteErrorLine(string value)
#pragma warning restore CA1822 // Mark members as static
        {
            Error.Write(Console.Out, ConsoleColor.Red, value);
            Console.WriteLine();
        }

        /// <summary>
        /// Reports an warning, followed by the current line terminator.
        /// </summary>
        /// <param name="value">The string to write.</param>
#pragma warning disable CA1822 // Mark members as static
        public void WriteWarningLine(string value)
#pragma warning restore CA1822 // Mark members as static
        {
            Error.Write(Console.Out, ConsoleColor.Yellow, value);
            Console.WriteLine();
        }
    }
}
