// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.IO;

namespace Microsoft.Coyote.IO
{
    /// <summary>
    /// Thread safe logger that writes text in-memory.
    /// </summary>
    public sealed class InMemoryLogger : MachineLogger
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
        /// Initializes a new instance of the <see cref="InMemoryLogger"/> class.
        /// </summary>
        /// <param name="isVerbose">If true, then messages are logged.</param>
        public InMemoryLogger(bool isVerbose)
            : base(isVerbose)
        {
            this.Writer = new StringWriter();
            this.Lock = new object();
        }

        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        public override void Write(string value)
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
        public override void Write(string format, object arg0)
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
        public override void Write(string format, object arg0, object arg1)
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
        public override void Write(string format, object arg0, object arg1, object arg2)
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
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public override void Write(string format, params object[] args)
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
        public override void WriteLine(string value)
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
        public override void WriteLine(string format, object arg0)
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
        public override void WriteLine(string format, object arg0, object arg1)
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
        public override void WriteLine(string format, object arg0, object arg1, object arg2)
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
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public override void WriteLine(string format, params object[] args)
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
        public override void Dispose()
        {
            this.Writer.Dispose();
        }
    }
}
