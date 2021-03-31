// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Tasks.BugFinding.Tests.Scheduling
{
    public class YieldTests : BaseTaskTest
    {
        public YieldTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private int x = 0;
        private int a = 0;
        private int b = 0;

        public async Task A()
        {
            this.a = this.x + 1;
            await Task.Yield();
            this.x = this.a;
        }

        public async Task B()
        {
            this.b = this.x + 1;
            await Task.Yield();
            this.x = this.b;
        }

        [Fact(Timeout = 5000)]
        public void TestDataRaceDetection()
        {
            this.TestWithError((r) =>
            {
                this.x = 0;
                var t1 = Task.Run(this.A);
                var t2 = Task.Run(this.B);
                Task.WaitAll(t1, t2);
                Specification.Assert(this.a > 1 || this.b > 1, string.Format("A: {0}, B: {1}", this.a, this.b));
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "A: 1, B: 1",
            replay: true);
        }
    }
}
