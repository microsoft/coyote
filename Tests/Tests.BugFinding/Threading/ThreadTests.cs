// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class ThreadTests : BaseBugFindingTest
    {
        public ThreadTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private static void SimpleThreadTest(Action action)
        {
            var t = new Thread(new ThreadStart(action));
            t.Start();
            t.Join();
        }

        [Fact(Timeout = 5000)]
        public void TestThreadStartJoin()
        {
            this.Test(async () =>
            {
                var tcs = new TaskCompletionSource<int>();
                SimpleThreadTest(() =>
                {
                    tcs.SetResult(1);
                });

                int result = await tcs.Task;
                Specification.Assert(result is 1, "Found unexpected value {0}.", result);
            },
            configuration: GetConfiguration().WithTestingIterations(1));
        }
    }
}
