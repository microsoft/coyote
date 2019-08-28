// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.Coyote.IO;

namespace Microsoft.Coyote.Utilities
{
    public sealed class ReplayerCommandLineOptions : BaseCommandLineOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayerCommandLineOptions"/> class.
        /// </summary>
        public ReplayerCommandLineOptions(string[] args)
            : base(args)
        {
        }

        /// <summary>
        /// Parses the given option.
        /// </summary>
        protected override void ParseOption(string option)
        {
            if (IsMatch(option, @"^[\/|-]test:") && option.Length > 6)
            {
                this.Configuration.AssemblyToBeAnalyzed = option.Substring(6);
            }
            else if (IsMatch(option, @"^[\/|-]runtime:") && option.Length > 9)
            {
                this.Configuration.TestingRuntimeAssembly = option.Substring(9);
            }
            else if (IsMatch(option, @"^[\/|-]method:") && option.Length > 8)
            {
                this.Configuration.TestMethodName = option.Substring(8);
            }
            else if (IsMatch(option, @"^[\/|-]replay:") && option.Length > 8)
            {
                string extension = System.IO.Path.GetExtension(option.Substring(8));
                if (!extension.Equals(".schedule"))
                {
                    Error.ReportAndExit("Please give a valid schedule file " +
                        "'-replay:[x]', where [x] has extension '.schedule'.");
                }

                this.Configuration.ScheduleFile = option.Substring(8);
            }
            else if (IsMatch(option, @"^[\/|-]break$"))
            {
                this.Configuration.AttachDebugger = true;
            }
            else if (IsMatch(option, @"^[\/|-]attach-debugger$"))
            {
                this.Configuration.AttachDebugger = true;
            }
            else if (IsMatch(option, @"^[\/|-]cycle-detection$"))
            {
                this.Configuration.EnableCycleDetection = true;
            }
            else if (IsMatch(option, @"^[\/|-]custom-state-hashing$"))
            {
                this.Configuration.EnableUserDefinedStateHashing = true;
            }
            else
            {
                base.ParseOption(option);
            }
        }

        /// <summary>
        /// Checks for parsing errors.
        /// </summary>
        protected override void CheckForParsingErrors()
        {
            if (string.IsNullOrEmpty(this.Configuration.AssemblyToBeAnalyzed))
            {
                Error.ReportAndExit("Please give a valid path to a Coyote " +
                    "program's dll using '-test:[x]'.");
            }

            if (string.IsNullOrEmpty(this.Configuration.ScheduleFile))
            {
                Error.ReportAndExit("Please give a valid path to a Coyote schedule " +
                    "file using '-replay:[x]', where [x] has extension '.schedule'.");
            }
        }

        /// <summary>
        /// Updates the configuration depending on the user specified options.
        /// </summary>
        protected override void UpdateConfiguration()
        {
        }

        /// <summary>
        /// Shows help.
        /// </summary>
        protected override void ShowHelp()
        {
            string help = "\n";

            help += " --------------";
            help += "\n Basic options:";
            help += "\n --------------";
            help += "\n  -?\t\t Show this help menu";
            help += "\n  -test:[x]\t Path to the Coyote program to test";
            help += "\n  -timeout:[x]\t Timeout (default is no timeout)";
            help += "\n  -v:[x]\t Enable verbose mode (values from '1' to '3')";

            help += "\n\n ------------------";
            help += "\n Replaying options:";
            help += "\n ------------------";
            help += "\n  -replay:[x]\t Schedule to replay";
            help += "\n  -break:[x]\t Attach debugger and break at bug";

            help += "\n";

            Console.WriteLine(help);
        }
    }
}
