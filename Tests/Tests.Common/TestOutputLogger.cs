// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Tests.Common
{
    /// <summary>
    /// Logger that writes to the test output.
    /// </summary>
    public sealed class TestOutputLogger : TextWriter
    {
        /// <summary>
        /// Underlying test output.
        /// </summary>
        private readonly ITestOutputHelper TestOutput;

        /// <summary>
        /// False means don't write anything.
        /// </summary>
        public bool IsVerbose { get; set; }

        /// <summary>
        /// When overridden in a derived class, returns the character encoding in which the
        /// output is written.
        /// </summary>
        public override Encoding Encoding => Encoding.Unicode;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestOutputLogger"/> class.
        /// </summary>
        /// <param name="output">The test output helper.</param>
        /// <param name="isVerbose">If true, then messages are logged. The default value is false.</param>
        public TestOutputLogger(ITestOutputHelper output, bool isVerbose = false)
        {
            this.TestOutput = output;
            this.IsVerbose = isVerbose;
        }

        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        public override void Write(string value)
        {
            if (this.IsVerbose)
            {
                this.TestOutput.WriteLine(value);
            }
        }

        /// <summary>
        /// Returns the logged text as a string.
        /// </summary>
        public override string ToString()
        {
            return this.TestOutput.ToString();
        }
    }
}
