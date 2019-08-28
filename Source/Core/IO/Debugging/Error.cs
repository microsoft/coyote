// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.Coyote.IO
{
    /// <summary>
    /// Static class implementing error reporting methods.
    /// </summary>
    internal static class Error
    {
        /// <summary>
        /// Reports a generic error to the user.
        /// </summary>
        public static void Report(string format, params object[] args)
        {
            string message = string.Format(CultureInfo.InvariantCulture, format, args);
            Write(ConsoleColor.Red, "Error: ");
            Write(ConsoleColor.Yellow, message);
            Console.Error.WriteLine(string.Empty);
        }

        /// <summary>
        /// Reports a generic error to the user and exits.
        /// </summary>
        public static void ReportAndExit(string value)
        {
            Write(ConsoleColor.Red, "Error: ");
            Write(ConsoleColor.Yellow, value);
            Console.Error.WriteLine(string.Empty);
            Environment.Exit(1);
        }

        /// <summary>
        /// Reports a generic error to the user and exits.
        /// </summary>
        public static void ReportAndExit(string format, params object[] args)
        {
            string message = string.Format(CultureInfo.InvariantCulture, format, args);
            Write(ConsoleColor.Red, "Error: ");
            Write(ConsoleColor.Yellow, message);
            Console.Error.WriteLine(string.Empty);
            Environment.Exit(1);
        }

        /// <summary>
        ///  Writes the specified string value to the output stream.
        /// </summary>
        private static void Write(ConsoleColor color, string value)
        {
            var previousForegroundColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Error.Write(value);
            Console.ForegroundColor = previousForegroundColor;
        }
    }
}
