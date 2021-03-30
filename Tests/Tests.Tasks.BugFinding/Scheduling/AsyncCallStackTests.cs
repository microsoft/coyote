// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Tasks.BugFinding.Tests
{
    public class AsyncCallStackTests : BaseTaskTest
    {
        public AsyncCallStackTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestExpectedAsyncCallStackSize()
        {
            this.Test(async () =>
            {
                int frameCount = 0;
                for (int i = 0; i < 100; i++)
                {
                    int? taskId = Task.CurrentId;
                    await Task.Delay(1);
                    var st = new StackTrace();
                    if (i is 0)
                    {
                        frameCount = st.FrameCount;
                    }

                    Specification.Assert(st.FrameCount < frameCount + 5, $"Call stack size of {st.FrameCount} in iteration {i}.");
                }
            },
            configuration: GetConfiguration());
        }
    }
}
