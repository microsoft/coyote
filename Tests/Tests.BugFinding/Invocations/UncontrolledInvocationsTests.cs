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
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledThreadYieldInvocation()
        {
            this.TestWithException<NotSupportedException>(() =>
            {
                Thread.Yield();
            });
        }

#if !NETFRAMEWORK
        [Fact(Timeout = 5000)]
        public void TestUncontrolledValueTaskInvocation()
        {
            this.TestWithException<NotSupportedException>(async () =>
            {
                var task = default(ValueTask);
                await task;
            });
        }
#endif

        [Fact(Timeout = 5000)]
        public void TestUncontrolledTimerInvocation()
        {
            this.TestWithException<NotSupportedException>(() =>
            {
                using var timer = new Timer(_ => Console.WriteLine("Hello!"), null, 1, 0);
            });
        }
    }
}
