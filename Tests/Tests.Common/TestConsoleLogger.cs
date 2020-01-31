// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Tests.Common
{
    /// <summary>
    /// Logger that writes to the console.
    /// </summary>
    public sealed class TestConsoleLogger : TextWriter, ITestOutputHelper
    {
        /// <summary>
        /// If true, then messages are logged. The default value is false.
        /// </summary>
        public bool IsVerbose { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestConsoleLogger"/> class.
        /// </summary>
        /// <param name="isVerbose">If true, then messages are logged.</param>
        public TestConsoleLogger(bool isVerbose)
        {
            this.IsVerbose = isVerbose;
        }

        /// <summary>
        /// When overridden in a derived class, returns the character encoding in which the
        /// output is written.
        /// </summary>
        public override Encoding Encoding => Console.OutputEncoding;

        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        /// <param name="value">Text</param>
        public override void Write(string value)
        {
            if (this.IsVerbose)
            {
                Console.Write(value);
            }
        }

        /// <summary>
        /// Writes the text representation of the specified argument.
        /// </summary>
        public override void Write(string format, object arg0)
        {
            if (this.IsVerbose)
            {
                Console.Write(format, arg0.ToString());
            }
        }

        /// <summary>
        /// Writes the text representation of the specified arguments.
        /// </summary>
        public override void Write(string format, object arg0, object arg1)
        {
            if (this.IsVerbose)
            {
                Console.Write(format, arg0.ToString(), arg1.ToString());
            }
        }

        /// <summary>
        /// Writes the text representation of the specified arguments.
        /// </summary>
        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            if (this.IsVerbose)
            {
                Console.Write(format, arg0.ToString(), arg1.ToString(), arg2.ToString());
            }
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public override void Write(string format, params object[] args)
        {
            if (this.IsVerbose)
            {
                Console.Write(format, args);
            }
        }

        /// <summary>
        /// Writes the specified string value, followed by the
        /// current line terminator.
        /// </summary>
        /// <param name="value">Text</param>
        public override void WriteLine(string value)
        {
            if (this.IsVerbose)
            {
                Console.WriteLine(value);
            }
        }

        /// <summary>
        /// Writes the text representation of the specified argument, followed by the
        /// current line terminator.
        /// </summary>
        public override void WriteLine(string format, object arg0)
        {
            if (this.IsVerbose)
            {
                Console.WriteLine(format, arg0.ToString());
            }
        }

        /// <summary>
        /// Writes the text representation of the specified arguments, followed by the
        /// current line terminator.
        /// </summary>
        public override void WriteLine(string format, object arg0, object arg1)
        {
            if (this.IsVerbose)
            {
                Console.WriteLine(format, arg0.ToString(), arg1.ToString());
            }
        }

        /// <summary>
        /// Writes the text representation of the specified arguments, followed by the
        /// current line terminator.
        /// </summary>
        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            if (this.IsVerbose)
            {
                Console.WriteLine(format, arg0.ToString(), arg1.ToString(), arg2.ToString());
            }
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects,
        /// followed by the current line terminator.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public override void WriteLine(string format, params object[] args)
        {
            if (this.IsVerbose)
            {
                Console.WriteLine(format, args);
            }
        }
    }
}
