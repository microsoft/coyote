// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Logging;

namespace Microsoft.Coyote.Samples.Common
{
    internal class LogWriter
    {
        private readonly ILogger Log;
        private readonly bool Echo;

        private LogWriter(ILogger log, bool echo)
        {
            this.Log = log;
            this.Echo = echo;
        }

        public static LogWriter Instance;

        public static void Initialize()
        {
            Instance = new LogWriter(null, true);
        }

        public static void Initialize(ILogger log)
        {
            Instance = new LogWriter(log, false);
        }

        public void WriteLine(string format, params object[] args)
        {
            this.Log?.WriteLine(format, args);
            if (this.Echo)
            {
                Console.WriteLine(format, args);
            }
        }

        public void WriteWarning(string format, params object[] args)
        {
            var msg = string.Format(format, args);
            this.Log?.WriteLine(LogSeverity.Warning, msg);
            if (this.Echo)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(msg);
                }
                finally
                {
                    Console.ResetColor();
                }
            }
        }

        internal void WriteError(string format, params object[] args)
        {
            var msg = string.Format(format, args);
            this.Log?.WriteLine(LogSeverity.Error, msg);
            if (this.Echo)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(msg);
                }
                finally
                {
                    Console.ResetColor();
                }
            }
        }
    }
}
