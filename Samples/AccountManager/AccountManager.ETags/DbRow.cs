// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Samples.AccountManager.ETags
{
    public class DbRow
    {
        public string Value { get; set; }

        public Guid ETag { get; set; }
    }
}
