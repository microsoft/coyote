// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote.IO;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Tests.Common
{
    /// <summary>
    /// Logger that writes to the test output.
    /// </summary>
    public sealed class TestOutputLogger : MachineLogger
    {
        /// <summary>
        /// Underlying test output.
        /// </summary>
        private readonly ITestOutputHelper TestOutput;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestOutputLogger"/> class.
        /// </summary>
        /// <param name="output">The test output helper.</param>
        /// <param name="isVerbose">If true, then messages are logged. The default value is false.</param>
        public TestOutputLogger(ITestOutputHelper output, bool isVerbose = false)
            : base(isVerbose)
        {
            this.TestOutput = output;
        }

        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        /// <param name="value">Text</param>
        public override void Write(string value)
        {
            if (this.IsVerbose)
            {
                this.TestOutput.WriteLine(value);
            }
        }

        /// <summary>
        /// Writes the text representation of the specified argument.
        /// </summary>
        public override void Write(string format, object arg0)
        {
            if (this.IsVerbose)
            {
                this.TestOutput.WriteLine(format, arg0.ToString());
            }
        }

        /// <summary>
        /// Writes the text representation of the specified arguments.
        /// </summary>
        public override void Write(string format, object arg0, object arg1)
        {
            if (this.IsVerbose)
            {
                this.TestOutput.WriteLine(format, arg0.ToString(), arg1.ToString());
            }
        }

        /// <summary>
        /// Writes the text representation of the specified arguments.
        /// </summary>
        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            if (this.IsVerbose)
            {
                this.TestOutput.WriteLine(format, arg0.ToString(), arg1.ToString(), arg2.ToString());
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
                this.TestOutput.WriteLine(format, args);
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
                this.TestOutput.WriteLine(value);
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
                this.TestOutput.WriteLine(format, arg0.ToString());
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
                this.TestOutput.WriteLine(format, arg0.ToString(), arg1.ToString());
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
                this.TestOutput.WriteLine(format, arg0.ToString(), arg1.ToString(), arg2.ToString());
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
                this.TestOutput.WriteLine(format, args);
            }
        }

        /// <summary>
        /// Returns the logged text as a string.
        /// </summary>
        public override string ToString()
        {
            return this.TestOutput.ToString();
        }

        /// <summary>
        /// Disposes the logger.
        /// </summary>
        public override void Dispose()
        {
        }
    }
}
