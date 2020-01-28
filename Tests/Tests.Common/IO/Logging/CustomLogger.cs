// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;
using Microsoft.Coyote.IO;

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
            this.StringBuilder.Append(value);
        }

        public override void WriteLine(string value)
        {
            this.StringBuilder.AppendLine(value);
        }

        public override string ToString()
        {
            return this.StringBuilder.ToString();
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
