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
            this.TestWithError(() =>
            {
                var task = new Task(() => { });
                task.ContinueWith(_ => { }, null);
            },
            errorChecker: (e) =>
            {
                var expectedMethodName = GetFullyQualifiedMethodName(typeof(Task), nameof(Task.ContinueWith));
                Assert.StartsWith($"Invoking '{expectedMethodName}' is not intercepted", e);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledThreadYieldInvocation()
        {
            this.TestWithError(() =>
            {
                Thread.Yield();
            },
            errorChecker: (e) =>
            {
                var expectedMethodName = GetFullyQualifiedMethodName(typeof(Thread), nameof(Thread.Yield));
                Assert.StartsWith($"Invoking '{expectedMethodName}' is not intercepted", e);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledTimerInvocation()
        {
            this.TestWithError(() =>
            {
                using var timer = new Timer(_ => Console.WriteLine("Hello!"), null, 1, 0);
            },
            errorChecker: (e) =>
            {
                var expectedMethodName = GetFullyQualifiedMethodName(typeof(Timer), ".ctor");
                Assert.StartsWith($"Invoking '{expectedMethodName}' is not intercepted", e);
            });
        }
    }
}
