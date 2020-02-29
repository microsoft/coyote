// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;

namespace Microsoft.Coyote.IO
{
    /// <summary>
    /// Thread safe logger that writes text in-memory.
    /// </summary>
    public sealed class InMemoryLogger : ILogger
    {
        /// <summary>
        /// Underlying string writer.
        /// </summary>
        private readonly StringWriter Writer;

        /// <summary>
        /// Serializes access to the string writer.
        /// </summary>
        private readonly object Lock;

        /// <summary>
        /// If true, then messages are logged. The default value is true.
        /// </summary>
        public bool IsVerbose { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryLogger"/> class.
        /// </summary>
        public InMemoryLogger()
        {
            this.Writer = new StringWriter();
            this.Lock = new object();
            this.IsVerbose = true;
        }

        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        public void Write(string value)
        {
            if (this.IsVerbose)
            {
                try
                {
                    lock (this.Lock)
                    {
                        this.Writer.Write(value);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // The writer was disposed.
                }
            }
        }

        /// <summary>
        /// Writes the text representation of the specified argument.
        /// </summary>
        public void Write(string format, object arg0)
        {
            if (this.IsVerbose)
            {
                try
                {
                    lock (this.Lock)
                    {
                        this.Writer.Write(format, arg0.ToString());
                    }
                }
                catch (ObjectDisposedException)
                {
                    // The writer was disposed.
                }
            }
        }

        /// <summary>
        /// Writes the text representation of the specified arguments.
        /// </summary>
        public void Write(string format, object arg0, object arg1)
        {
            if (this.IsVerbose)
            {
                try
                {
                    lock (this.Lock)
                    {
                        this.Writer.Write(format, arg0.ToString(), arg1.ToString());
                    }
                }
                catch (ObjectDisposedException)
                {
                    // The writer was disposed.
                }
            }
        }

        /// <summary>
        /// Writes the text representation of the specified arguments.
        /// </summary>
        public void Write(string format, object arg0, object arg1, object arg2)
        {
            if (this.IsVerbose)
            {
                try
                {
                    lock (this.Lock)
                    {
                        this.Writer.Write(format, arg0.ToString(), arg1.ToString(), arg2.ToString());
                    }
                }
                catch (ObjectDisposedException)
                {
                    // The writer was disposed.
                }
            }
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects.
        /// </summary>
        public void Write(string format, params object[] args)
        {
            if (this.IsVerbose)
            {
                try
                {
                    lock (this.Lock)
                    {
                        this.Writer.Write(format, args);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // The writer was disposed.
                }
            }
        }

        /// <summary>
        /// Writes the specified string value, followed by the
        /// current line terminator.
        /// </summary>
        public void WriteLine(string value)
        {
            if (this.IsVerbose)
            {
                try
                {
                    lock (this.Lock)
                    {
                        this.Writer.WriteLine(value);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // The writer was disposed.
                }
            }
        }

        /// <summary>
        /// Writes the text representation of the specified argument, followed by the
        /// current line terminator.
        /// </summary>
        public void WriteLine(string format, object arg0)
        {
            if (this.IsVerbose)
            {
                try
                {
                    lock (this.Lock)
                    {
                        this.Writer.WriteLine(format, arg0.ToString());
                    }
                }
                catch (ObjectDisposedException)
                {
                    // The writer was disposed.
                }
            }
        }

        /// <summary>
        /// Writes the text representation of the specified arguments, followed by the
        /// current line terminator.
        /// </summary>
        public void WriteLine(string format, object arg0, object arg1)
        {
            if (this.IsVerbose)
            {
                try
                {
                    lock (this.Lock)
                    {
                        this.Writer.WriteLine(format, arg0.ToString(), arg1.ToString());
                    }
                }
                catch (ObjectDisposedException)
                {
                    // The writer was disposed.
                }
            }
        }

        /// <summary>
        /// Writes the text representation of the specified arguments, followed by the
        /// current line terminator.
        /// </summary>
        public void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            if (this.IsVerbose)
            {
                try
                {
                    lock (this.Lock)
                    {
                        this.Writer.WriteLine(format, arg0.ToString(), arg1.ToString(), arg2.ToString());
                    }
                }
                catch (ObjectDisposedException)
                {
                    // The writer was disposed.
                }
            }
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects,
        /// followed by the current line terminator.
        /// </summary>
        public void WriteLine(string format, params object[] args)
        {
            if (this.IsVerbose)
            {
                try
                {
                    lock (this.Lock)
                    {
                        this.Writer.WriteLine(format, args);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // The writer was disposed.
                }
            }
        }

        /// <summary>
        /// Returns the logged text as a string.
        /// </summary>
        public override string ToString()
        {
            lock (this.Lock)
            {
                return this.Writer.ToString();
            }
        }

        /// <summary>
        /// Disposes the logger.
        /// </summary>
        public void Dispose()
        {
            this.Writer.Dispose();
        }
    }
}
