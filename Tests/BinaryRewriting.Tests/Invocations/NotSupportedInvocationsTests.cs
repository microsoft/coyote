// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BinaryRewriting.Tests.Invocations
{
    public class NotSupportedInvocationsTests : BaseRewritingTest
    {
        public NotSupportedInvocationsTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestNotSupportedContinueWithTaskInvocation()
        {
            this.TestWithException<NotSupportedException>(() =>
            {
                var task = new Task(() => { });
                task.ContinueWith(_ => { }, null);
            },
            configuration: GetConfiguration().WithTestingIterations(1));
        }

#if !NETFRAMEWORK
        [Fact(Timeout = 5000)]
        public void TestNotSupportedValueTaskInvocation()
        {
            this.TestWithException<NotSupportedException>(async () =>
            {
                var task = default(ValueTask);
                await task;
            },
            configuration: GetConfiguration().WithTestingIterations(1));
        }
#endif

        [Fact(Timeout = 5000)]
        public void TestNotSupportedTimerInvocation()
        {
            this.TestWithException<NotSupportedException>(() =>
            {
                using var timer = new Timer(_ => Console.WriteLine("Hello!"), null, 1, 0);
            },
            configuration: GetConfiguration().WithTestingIterations(1));
        }
    }
}
