// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class UncontrolledInvocationsTests : BaseBugFindingTest
    {
        public UncontrolledInvocationsTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledContinueWithTaskInvocation()
        {
            this.TestWithException<NotSupportedException>(() =>
            {
                var task = new Task(() => { });
                task.ContinueWith(_ => { }, null);
            },
            configuration: this.GetConfiguration().WithTestingIterations(1));
        }

#if !NETFRAMEWORK
        [Fact(Timeout = 5000)]
        public void TestUncontrolledValueTaskInvocation()
        {
            this.TestWithException<NotSupportedException>(async () =>
            {
                var task = default(ValueTask);
                await task;
            },
            configuration: this.GetConfiguration().WithTestingIterations(1));
        }
#endif

        [Fact(Timeout = 5000)]
        public void TestUncontrolledTimerInvocation()
        {
            this.TestWithException<NotSupportedException>(() =>
            {
                using var timer = new Timer(_ => Console.WriteLine("Hello!"), null, 1, 0);
            },
            configuration: this.GetConfiguration().WithTestingIterations(1));
        }
    }
}
