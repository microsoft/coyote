// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Samples.AccountManager.ETags
{
    public class Account
    {
        public string Name { get; set; }

        public string Payload { get; set; }

        public int Version { get; set; }
    }
}
