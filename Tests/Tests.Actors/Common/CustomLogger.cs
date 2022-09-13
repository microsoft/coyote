// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text;
using Microsoft.Coyote.IO;

namespace Microsoft.Coyote.Actors.Tests
{
    public class CustomLogger : TextWriter, ILogger
    {
        private StringBuilder StringBuilder;

        public CustomLogger()
        {
            this.StringBuilder = new StringBuilder();
        }

        /// <inheritdoc/>
        public TextWriter TextWriter => this;

        /// <inheritdoc/>
        public override Encoding Encoding => Encoding.Unicode;

        /// <inheritdoc/>
        public override string ToString()
        {
            if (this.StringBuilder != null)
            {
                return this.StringBuilder.ToString();
            }

            return string.Empty;
        }

        /// <inheritdoc/>
        public override void Write(string value)
        {
            this.Write(LogSeverity.Informational, value);
        }

        /// <inheritdoc/>
        public override void Write(string format, params object[] args)
        {
            this.Write(LogSeverity.Informational, string.Format(format, args));
        }

        /// <inheritdoc/>
        public void Write(VerbosityLevel level, string value)
        {
            if (this.StringBuilder != null)
            {
                this.StringBuilder.Append(value);
            }
        }

        /// <inheritdoc/>
        public void Write(VerbosityLevel level, string format, params object[] args)
        {
            if (this.StringBuilder != null)
            {
                this.StringBuilder.Append(string.Format(format, args));
            }
        }

        /// <inheritdoc/>
        public override void WriteLine(string value)
        {
            this.WriteLine(LogSeverity.Informational, value);
        }

        /// <inheritdoc/>
        public override void WriteLine(string format, params object[] args)
        {
            this.WriteLine(LogSeverity.Informational, string.Format(format, args));
        }

        /// <inheritdoc/>
        public void WriteLine(VerbosityLevel level, string value)
        {
            if (this.StringBuilder != null)
            {
                this.StringBuilder.AppendLine(value);
            }
        }

        /// <inheritdoc/>
        public void WriteLine(VerbosityLevel level, string format, params object[] args)
        {
            if (this.StringBuilder != null)
            {
                this.StringBuilder.AppendLine(string.Format(format, args));
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.StringBuilder.Clear();
                this.StringBuilder = null;
            }
        }
    }
}
