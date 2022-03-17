// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System;

namespace ImageGallery.Logging
{
    /// <summary>
    /// Simple logger that writes text to the console.
    /// </summary>
    internal sealed class MockLogger : ILogger<ApplicationLogs>
    {
        private static readonly object ColorLock = new object();

        public void WriteErrorLine(string value)
        {
            lock (ColorLock)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{GetRequestId()}{value}");
                }
                finally
                {
                    Console.ResetColor();
                }
            }
        }

        public void WriteWarningLine(string value)
        {
            lock (ColorLock)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"{GetRequestId()}{value}");
                }
                finally
                {
                    Console.ResetColor();
                }
            }
        }

        public void WriteInformationLine(string value)
        {
            lock (ColorLock)
            {
                Console.WriteLine($"{GetRequestId()}{value}");
            }
        }

        private static string GetRequestId()
        {
            var requestId = RequestId.Get();
            return string.IsNullOrEmpty(requestId) ? string.Empty : $"[{requestId}] ";
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var msg = formatter(state, exception);
            switch (logLevel)
            {
                case LogLevel.Warning:
                    WriteWarningLine(msg);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    WriteErrorLine(msg);
                    break;
                default:
                    WriteInformationLine(msg);
                    break;
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }
}