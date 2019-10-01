// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text;

namespace Microsoft.Coyote.IO
{
    /// <summary>
    /// Text writer that writes to the specified logger.
    /// </summary>
    internal sealed class LogWriter : TextWriter
    {
        /// <summary>
        /// The installed logger.
        /// </summary>
        private readonly ILogger Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogWriter"/> class.
        /// </summary>
        /// <param name="logger">ILogger</param>
        internal LogWriter(ILogger logger)
        {
            this.Logger = logger;
        }

        /// <summary>
        /// Writes the specified input to the runtime logger.
        /// </summary>
        public override void Write(bool value)
        {
            this.Logger.Write(value.ToString());
        }

        /// <summary>
        /// Writes the specified input to the runtime logger.
        /// </summary>
        public override void Write(char value)
        {
            this.Logger.Write(value.ToString());
        }

        /// <summary>
        /// Writes the specified input to the runtime logger.
        /// </summary>
        public override void Write(char[] buffer)
        {
            this.Logger.Write(new string(buffer));
        }

        /// <summary>
        /// Writes the specified input to the runtime logger.
        /// </summary>
        public override void Write(char[] buffer, int index, int count)
        {
            this.Logger.Write(new string(buffer, index, count));
        }

        /// <summary>
        /// Writes the specified input to the runtime logger.
        /// </summary>
        public override void Write(decimal value)
        {
            this.Logger.Write(value.ToString());
        }

        /// <summary>
        /// Writes the specified input to the runtime logger.
        /// </summary>
        public override void Write(double value)
        {
            this.Logger.Write(value.ToString());
        }

        /// <summary>
        /// Writes the specified input to the runtime logger.
        /// </summary>
        public override void Write(float value)
        {
            this.Logger.Write(value.ToString());
        }

        /// <summary>
        /// Writes the specified input to the runtime logger.
        /// </summary>
        public override void Write(int value)
        {
            this.Logger.Write(value.ToString());
        }

        /// <summary>
        /// Writes the specified input to the runtime logger.
        /// </summary>
        public override void Write(long value)
        {
            this.Logger.Write(value.ToString());
        }

        /// <summary>
        /// Writes the specified input to the runtime logger.
        /// </summary>
        public override void Write(object value)
        {
            this.Logger.Write(value.ToString());
        }

        /// <summary>
        /// Writes the specified input to the runtime logger.
        /// </summary>
        public override void Write(string format, object arg0)
        {
            this.Logger.Write(string.Format(format, arg0.ToString()));
        }

        /// <summary>
        /// Writes the specified input to the runtime logger.
        /// </summary>
        public override void Write(string format, object arg0, object arg1)
        {
            this.Logger.Write(string.Format(format, arg0.ToString(), arg1.ToString()));
        }

        /// <summary>
        /// Writes the specified input to the runtime logger.
        /// </summary>
        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            this.Logger.Write(string.Format(format, arg0.ToString(), arg1.ToString(), arg2.ToString()));
        }

        /// <summary>
        /// Writes the specified input to the runtime logger.
        /// </summary>
        public override void Write(string format, params object[] args)
        {
            this.Logger.Write(string.Format(format, args));
        }

        /// <summary>
        /// Writes the specified input to the runtime logger.
        /// </summary>
        public override void Write(string value)
        {
            this.Logger.Write(value);
        }

        /// <summary>
        /// Writes the specified input to the runtime logger.
        /// </summary>
        public override void Write(uint value)
        {
            this.Logger.Write(value.ToString());
        }

        /// <summary>
        /// Writes the specified input to the runtime logger.
        /// </summary>
        public override void Write(ulong value)
        {
            this.Logger.Write(value.ToString());
        }

        /// <summary>
        /// Writes a new line to the runtime logger,
        /// followed by the current line terminator.
        /// </summary>
        public override void WriteLine()
        {
            this.Logger.WriteLine(string.Empty);
        }

        /// <summary>
        /// Writes the specified input to the runtime logger,
        /// followed by the current line terminator.
        /// </summary>
        public override void WriteLine(bool value)
        {
            this.Logger.WriteLine(value.ToString());
        }

        /// <summary>
        /// Writes the specified input to the runtime logger,
        /// followed by the current line terminator.
        /// </summary>
        public override void WriteLine(char value)
        {
            this.Logger.WriteLine(value.ToString());
        }

        /// <summary>
        /// Writes the specified input to the runtime logger,
        /// followed by the current line terminator.
        /// </summary>
        public override void WriteLine(char[] buffer)
        {
            this.Logger.WriteLine(new string(buffer));
        }

        /// <summary>
        /// Writes the specified input to the runtime logger,
        /// followed by the current line terminator.
        /// </summary>
        public override void WriteLine(char[] buffer, int index, int count)
        {
            this.Logger.WriteLine(new string(buffer, index, count));
        }

        /// <summary>
        /// Writes the specified input to the runtime logger,
        /// followed by the current line terminator.
        /// </summary>
        public override void WriteLine(decimal value)
        {
            this.Logger.WriteLine(value.ToString());
        }

        /// <summary>
        /// Writes the specified input to the runtime logger,
        /// followed by the current line terminator.
        /// </summary>
        public override void WriteLine(double value)
        {
            this.Logger.WriteLine(value.ToString());
        }

        /// <summary>
        /// Writes the specified input to the runtime logger,
        /// followed by the current line terminator.
        /// </summary>
        public override void WriteLine(float value)
        {
            this.Logger.WriteLine(value.ToString());
        }

        /// <summary>
        /// Writes the specified input to the runtime logger,
        /// followed by the current line terminator.
        /// </summary>
        public override void WriteLine(int value)
        {
            this.Logger.WriteLine(value.ToString());
        }

        /// <summary>
        /// Writes the specified input to the runtime logger,
        /// followed by the current line terminator.
        /// </summary>
        public override void WriteLine(long value)
        {
            this.Logger.WriteLine(value.ToString());
        }

        /// <summary>
        /// Writes the specified input to the runtime logger,
        /// followed by the current line terminator.
        /// </summary>
        public override void WriteLine(object value)
        {
            this.Logger.WriteLine(value.ToString());
        }

        /// <summary>
        /// Writes the specified input to the runtime logger,
        /// followed by the current line terminator.
        /// </summary>
        public override void WriteLine(string format, object arg0)
        {
            this.Logger.WriteLine(string.Format(format, arg0.ToString()));
        }

        /// <summary>
        /// Writes the specified input to the runtime logger,
        /// followed by the current line terminator.
        /// </summary>
        public override void WriteLine(string format, object arg0, object arg1)
        {
            this.Logger.WriteLine(string.Format(format, arg0.ToString(), arg1.ToString()));
        }

        /// <summary>
        /// Writes the specified input to the runtime logger,
        /// followed by the current line terminator.
        /// </summary>
        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            this.Logger.WriteLine(string.Format(format, arg0.ToString(), arg1.ToString(), arg2.ToString()));
        }

        /// <summary>
        /// Writes the specified input to the runtime logger,
        /// followed by the current line terminator.
        /// </summary>
        public override void WriteLine(string format, params object[] args)
        {
            this.Logger.Write(string.Format(format, args));
        }

        /// <summary>
        /// Writes the specified input to the runtime logger,
        /// followed by the current line terminator.
        /// </summary>
        public override void WriteLine(string value)
        {
            this.Logger.WriteLine(value);
        }

        /// <summary>
        /// Writes the specified input to the runtime logger,
        /// followed by the current line terminator.
        /// </summary>
        public override void WriteLine(uint value)
        {
            this.Logger.WriteLine(value.ToString());
        }

        /// <summary>
        /// Writes the specified input to the runtime logger,
        /// followed by the current line terminator.
        /// </summary>
        public override void WriteLine(ulong value)
        {
            this.Logger.WriteLine(value.ToString());
        }

        /// <summary>
        /// The character encoding in which the output is written.
        /// </summary>
        public override Encoding Encoding => Encoding.ASCII;

        /// <summary>
        /// Returns the logged text as a string.
        /// </summary>
        public override string ToString()
        {
            return this.Logger.ToString();
        }
    }
}
