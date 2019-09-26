// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Tooling.Utilities;

namespace Microsoft.Coyote.Utilities
{
    public sealed class ReplayerCommandLineOptions : BaseCommandLineOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayerCommandLineOptions"/> class.
        /// </summary>
        public ReplayerCommandLineOptions()
            : base("CoyoteReplayer", "Replays a previously recorded bug trace found by CoyoteTester.")
        {
            var basic = this.Parser.GetOrCreateGroup("Basic", "Basic options");
            basic.AddArgument("test", "t", "Path to the Coyote program to test", required: true);
            basic.AddArgument("method", "m", "Suffix of the test method to execute");
            this.AddCommonOptions();

            var group1 = this.Parser.GetOrCreateGroup("group1", "Replaying options");
            group1.AddArgument("replay", "r", "Schedule file to replay", required: true);
            group1.AddArgument("break", "b", "Attach debugger and break at bug", typeof(bool));

            // hidden options (for debugging only).
            var hiddenGroup = this.Parser.GetOrCreateGroup("group4", "Hidden Options");
            hiddenGroup.IsHidden = true;
            hiddenGroup.AddArgument("runtime", null, "The path to the testing runtime to use");
            hiddenGroup.AddArgument("attach-debugger", null, "Attach debugger and break at bug", typeof(bool));
            hiddenGroup.AddArgument("cycle-detection", null, "Enable cycle detection", typeof(bool));
            hiddenGroup.AddArgument("custom-state-hashing", null, "Enable custom state hashing", typeof(bool));
        }

        /// <summary>
        /// Handles the given parsed command line option.
        /// </summary>
        protected override void HandledParsedArgument(CommandLineArgument option)
        {
            switch (option.LongName)
            {
                case "test":
                    this.Configuration.AssemblyToBeAnalyzed = (string)option.Value;
                    break;
                case "runtime":
                    this.Configuration.TestingRuntimeAssembly = (string)option.Value;
                    break;
                case "method":
                    this.Configuration.TestMethodName = (string)option.Value;
                    break;
                case "replay":
                    {
                        string filename = (string)option.Value;
                        string extension = System.IO.Path.GetExtension(filename);
                        if (!extension.Equals(".schedule"))
                        {
                            Error.ReportAndExit("Please give a valid schedule file " +
                                "'--replay:[x]', where [x] has extension '.schedule'.");
                        }

                        this.Configuration.ScheduleFile = filename;
                    }

                    break;
                case "break":
                case "attach-debugger":
                    this.Configuration.AttachDebugger = true;
                    break;
                case "cycle-detection":
                    this.Configuration.EnableCycleDetection = true;
                    break;
                case "custom-state-hashing":
                    this.Configuration.EnableUserDefinedStateHashing = true;
                    break;
                default:
                    base.HandledParsedArgument(option);
                    break;
            }
        }

        /// <summary>
        /// Updates the configuration depending on the user specified options.
        /// </summary>
        protected override void UpdateConfiguration()
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
    }
}
