// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;

namespace Microsoft.Coyote.Tests.Common
{
    /// <summary>
    /// Intercepts all console output.
    /// </summary>
    internal struct ConsoleOutputInterceptor : IDisposable
    {
        /// <summary>
        /// The original console output writer.
        /// </summary>
        private readonly TextWriter ConsoleOutput;

        /// <summary>
        /// Used to intercepts console output.
        /// </summary>
        private readonly StreamWriter Writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleOutputInterceptor"/> struct.
        /// </summary>
        internal ConsoleOutputInterceptor(MemoryStream stream)
        {
            this.ConsoleOutput = Console.Out;
            this.Writer = new StreamWriter(stream)
            {
                AutoFlush = true,
            };

            Console.SetOut(this.Writer);
        }

        /// <summary>
        /// Restores the original console color.
        /// </summary>
        public void Dispose()
        {
            Console.SetOut(this.ConsoleOutput);
        }
    }
}
