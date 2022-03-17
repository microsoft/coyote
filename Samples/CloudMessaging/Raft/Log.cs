// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Samples.CloudMessaging
{
    public class Log
    {
        public readonly int Term;
        public readonly string Command;

        public Log(int term, string command)
        {
            this.Term = term;
            this.Command = command;
        }
    }
}
