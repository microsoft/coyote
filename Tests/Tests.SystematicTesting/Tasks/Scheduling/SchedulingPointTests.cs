// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class SchedulingPointTests : BaseSystematicTest
    {
        public SchedulingPointTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private int x = 0;
        private int a = 0;
        private int b = 0;

        private async Task A1()
        {
            this.a = this.x + 1;
            SchedulingPoint.Interleave();
            this.x = this.a;
            await Task.CompletedTask;
        }

        private async Task B1()
        {
            this.b = this.x + 1;
            SchedulingPoint.Interleave();
            this.x = this.b;
            await Task.CompletedTask;
        }

        [Fact(Timeout = 5000)]
        public void TestDataRaceDetectionWithInterleave()
        {
            this.TestWithError(async r =>
            {
                this.x = 0;
                var t1 = Task.Run(this.A1);
                var t2 = Task.Run(this.B1);
                await Task.WhenAll(t1, t2);
                Specification.Assert(this.a > 1 || this.b > 1, string.Format("A: {0}, B: {1}", this.a, this.b));
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "A: 1, B: 1",
            replay: true);
        }

        private async Task A2()
        {
            this.a = this.x + 1;
            SchedulingPoint.Deprioritize();
            this.x = this.a;
            await Task.CompletedTask;
        }

        private async Task B2()
        {
            this.b = this.x + 1;
            SchedulingPoint.Deprioritize();
            this.x = this.b;
            await Task.CompletedTask;
        }

        [Fact(Timeout = 5000)]
        public void TestDataRaceDetectionWithDeprioritize()
        {
            this.TestWithError(async r =>
            {
                this.x = 0;
                var t1 = Task.Run(this.A2);
                var t2 = Task.Run(this.B2);
                await Task.WhenAll(t1, t2);
                Specification.Assert(this.a > 1 || this.b > 1, string.Format("A: {0}, B: {1}", this.a, this.b));
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "A: 1, B: 1",
            replay: true);
        }
    }
}
