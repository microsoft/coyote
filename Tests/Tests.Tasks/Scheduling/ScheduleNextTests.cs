// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Tasks.Tests.Scheduling
{
    public class ScheduleNextTests : BaseTaskTest
    {
        public ScheduleNextTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private int x = 0;
        private int a = 0;
        private int b = 0;

        public async Task A()
        {
            this.a = this.x + 1;
            Task.ExploreContextSwitch();
            this.x = this.a;
            await Task.CompletedTask;
        }

        public async Task B()
        {
            this.b = this.x + 1;
            Task.ExploreContextSwitch();
            this.x = this.b;
            await Task.CompletedTask;
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
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "A: 1, B: 1",
            replay: true);
        }
    }
}
