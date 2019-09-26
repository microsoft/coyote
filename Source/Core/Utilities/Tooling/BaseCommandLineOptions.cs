// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.IO;

namespace Microsoft.Coyote.Tooling.Utilities
{
    /// <summary>
    /// Some common command line options shared by all Coyote tools.
    /// </summary>
    public abstract class BaseCommandLineOptions
    {
        /// <summary>
        /// The command line parser to use.
        /// </summary>
        protected CommandLineArgumentParser Parser;

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
        public BaseCommandLineOptions(string appName, string appDescription)
        {
            this.Parser = new CommandLineArgumentParser(appName, appDescription);
        }

        /// <summary>
        /// Add common options dealing with timeouts, logging and debugging.
        /// </summary>
        public void AddCommonOptions()
        {
            var group = this.Parser.GetOrCreateGroup("Basic", "Basic options");
            group.AddArgument("timeout", null, "Timeout in seconds (disabled by default)", typeof(uint));
            group.AddArgument("outdir", "o", "Dump output to directory x(absolute path or relative to current directory");
            group.AddArgument("verbose", "v", "Enable verbose log output during testing", typeof(bool));
            group.AddArgument("debug", "d", "Enable debugging", typeof(bool)).IsHidden = true;
        }

        /// <summary>
        /// Parses the command line options and returns a configuration.
        /// </summary>
        /// <returns>The Configuration object populated with the parsed command line options.</returns>
        public Configuration Parse(string[] args)
        {
            this.Configuration = Configuration.Create();
            try
            {
                var result = this.Parser.ParseArguments(args);

                foreach (var arg in result)
                {
                    this.HandledParsedArgument(arg);
                }

                this.UpdateConfiguration();
            }
            catch (Exception ex)
            {
                this.Parser.PrintHelp(Console.Out);
                Error.ReportAndExit(ex.Message);
            }

            return this.Configuration;
        }

        /// <summary>
        /// Parses the given option.
        /// </summary>
        /// <param name="option">Option</param>
        protected virtual void HandledParsedArgument(CommandLineArgument option)
        {
            switch (option.LongName)
            {
                case "outdir":
                    this.Configuration.OutputFilePath = (string)option.Value;
                    break;
                case "verbose":
                    this.Configuration.IsVerbose = true;
                    break;
                case "debug":
                    this.Configuration.EnableDebugging = true;
                    break;
                case "timeout":
                    this.Configuration.Timeout = (int)(uint)option.Value;
                    break;
                default:
                    throw new Exception(string.Format("Unhandled parsed argument: '{0}'", option.LongName));
            }
        }

        /// <summary>
        /// Updates the configuration depending on the user specified options (set useful defaults, etc).
        /// </summary>
        protected abstract void UpdateConfiguration();
    }
}
