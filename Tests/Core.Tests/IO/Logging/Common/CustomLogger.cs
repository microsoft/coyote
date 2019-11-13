// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using Microsoft.Coyote.IO;

namespace Microsoft.Coyote.Core.Tests.IO
{
    internal class CustomLogger : ILogger
    {
        private StringBuilder StringBuilder;

        public bool IsVerbose { get; set; } = false;

        public CustomLogger(bool isVerbose)
        {
            this.StringBuilder = new StringBuilder();
            this.IsVerbose = isVerbose;
        }

        public void Write(string value)
        {
            this.StringBuilder.Append(value);
        }

        public void Write(string format, object arg0)
        {
            this.StringBuilder.AppendFormat(format, arg0.ToString());
        }

        public void Write(string format, object arg0, object arg1)
        {
            this.StringBuilder.AppendFormat(format, arg0.ToString(), arg1.ToString());
        }

        public void Write(string format, object arg0, object arg1, object arg2)
        {
            this.StringBuilder.AppendFormat(format, arg0.ToString(), arg1.ToString(), arg2.ToString());
        }

        public void Write(string format, params object[] args)
        {
            this.StringBuilder.AppendFormat(format, args);
        }

        public void WriteLine(string value)
        {
            this.StringBuilder.AppendLine(value);
        }

        public void WriteLine(string format, object arg0)
        {
            this.StringBuilder.AppendFormat(format, arg0.ToString());
            this.StringBuilder.AppendLine();
        }

        public void WriteLine(string format, object arg0, object arg1)
        {
            this.StringBuilder.AppendFormat(format, arg0.ToString(), arg1.ToString());
            this.StringBuilder.AppendLine();
        }

        public void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            this.StringBuilder.AppendFormat(format, arg0.ToString(), arg1.ToString(), arg2.ToString());
            this.StringBuilder.AppendLine();
        }

        public void WriteLine(string format, params object[] args)
        {
            this.StringBuilder.AppendFormat(format, args);
            this.StringBuilder.AppendLine();
        }

        public override string ToString()
        {
            return this.StringBuilder.ToString();
        }

        public void Dispose()
        {
            this.StringBuilder.Clear();
            this.StringBuilder = null;
        }
    }
}
