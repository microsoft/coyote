// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Tests.Common.Actors
{
    public class OperationCounter : Operation<bool>
    {
        public int ExpectedCount;

        public OperationCounter(int expected)
        {
            this.ExpectedCount = expected;
        }

        public override void SetResult(bool result)
        {
            var count = Interlocked.Decrement(ref this.ExpectedCount);
            if (count == 0)
            {
                base.SetResult(result);
            }
        }
    }
}
