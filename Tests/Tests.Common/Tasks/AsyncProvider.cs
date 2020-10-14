// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Microsoft.Coyote.Tests.Common.Tasks
{
    public static class AsyncProvider
    {
        public static Task DelayAsync() => Task.Delay(100);
    }
}
