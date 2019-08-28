// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Text.RegularExpressions;
using Microsoft.Coyote.IO;

namespace Microsoft.Coyote.Utilities
{
    /// <summary>
    /// The Coyote base command line options.
    /// </summary>
    public abstract class BaseCommandLineOptions
    {
        /// <summary>
        /// Configuration.
        /// </summary>
        protected Configuration Configuration;

        /// <summary>
        /// Command line options.
        /// </summary>
        protected string[] Options;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCommandLineOptions"/> class.
        /// </summary>
        /// <param name="args">Array of arguments</param>
        public BaseCommandLineOptions(string[] args)
        {
            this.Configuration = Configuration.Create();
            this.Options = args;
        }

        /// <summary>
        /// Parses the command line options and returns a configuration.
        /// </summary>
        /// <returns>Configuration</returns>
        public Configuration Parse()
        {
            for (int idx = 0; idx < this.Options.Length; idx++)
            {
                this.ParseOption(this.Options[idx]);
            }

            this.CheckForParsingErrors();
            this.UpdateConfiguration();
            return this.Configuration;
        }

        /// <summary>
        /// Parses the given option.
        /// </summary>
        /// <param name="option">Option</param>
        protected virtual void ParseOption(string option)
        {
            if (IsMatch(option, @"^[\/|-]?$"))
            {
                this.ShowHelp();
                Environment.Exit(0);
            }
            else if (IsMatch(option, @"^[\/|-]o:") && option.Length > 3)
            {
                this.Configuration.OutputFilePath = option.Substring(3);
            }
            else if (IsMatch(option, @"^[\/|-]v$"))
            {
                this.Configuration.IsVerbose = true;
            }
            else if (IsMatch(option, @"^[\/|-]v:") && option.Length > 3)
            {
                if (!int.TryParse(option.Substring(3), out int i) && i > 0 && i <= 3)
                {
                    Error.ReportAndExit("This option is deprecated; please use '-v'.");
                }

                this.Configuration.IsVerbose = i > 0;
            }
            else if (IsMatch(option, @"^[\/|-]debug$"))
            {
                this.Configuration.EnableDebugging = true;
                Debug.IsEnabled = true;
            }
            else if (IsMatch(option, @"^[\/|-]warnings-on$"))
            {
                this.Configuration.ShowWarnings = true;
            }
            else if (IsMatch(option, @"^[\/|-]timeout:") && option.Length > 9)
            {
                if (!int.TryParse(option.Substring(9), out int i) &&
                    i > 0)
                {
                    Error.ReportAndExit("Please give a valid timeout '-timeout:[x]', where [x] > 0 seconds.");
                }

                this.Configuration.Timeout = i;
            }
            else
            {
                this.ShowHelp();
                Error.ReportAndExit("cannot recognise command line option '" + option + "'.");
            }
        }

        /// <summary>
        /// Checks for parsing errors.
        /// </summary>
        protected abstract void CheckForParsingErrors();

        /// <summary>
        /// Updates the configuration depending on the
        /// user specified options.
        /// </summary>
        protected abstract void UpdateConfiguration();

        /// <summary>
        /// Shows help.
        /// </summary>
        protected abstract void ShowHelp();

        /// <summary>
        /// Checks if the given input is a matches the specified pattern.
        /// </summary>
        /// <param name="input">The input to match.</param>
        /// <param name="pattern">The pattern to match.</param>
        /// <returns>True if the input matches the pattern.</returns>
        protected static bool IsMatch(string input, string pattern)
        {
            return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase);
        }
    }
}
