// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text;

namespace Microsoft.Coyote.Tests.Common.IO
{
    public class CustomLogger : TextWriter
    {
        private StringBuilder StringBuilder;

        public CustomLogger()
        {
            this.StringBuilder = new StringBuilder();
        }

        /// <summary>
        /// When overridden in a derived class, returns the character encoding in which the
        /// output is written.
        /// </summary>
        public override Encoding Encoding => Encoding.Unicode;

        public override void Write(string value)
        {
            if (this.StringBuilder != null)
            {
                this.StringBuilder.Append(value);
            }
        }

        public override void WriteLine(string value)
        {
            if (this.StringBuilder != null)
            {
                this.StringBuilder.AppendLine(value);
            }
        }

        public override string ToString()
        {
            if (this.StringBuilder != null)
            {
                return this.StringBuilder.ToString();
            }

            return string.Empty;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.StringBuilder.Clear();
                this.StringBuilder = null;
            }

            base.Dispose(disposing);
        }
    }
}
