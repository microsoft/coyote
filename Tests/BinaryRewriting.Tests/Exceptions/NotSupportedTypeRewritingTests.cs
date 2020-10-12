// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BinaryRewriting.Tests.Tasks
{
    public class NotSupportedTypeRewritingTests : BaseProductionTest
    {
        public NotSupportedTypeRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestNotSupportedTaskTypeInvokingContinueWith()
        {
            this.TestWithException<NotSupportedException>(() =>
            {
                var task = new Task(() => { });
                task.ContinueWith(_ => { }, null);
            },
            configuration: GetConfiguration().WithTestingIterations(1));
        }

        [Fact(Timeout = 5000)]
        public void TestNotSupportedValueTaskType()
        {
            this.TestWithException<NotSupportedException>(async () =>
            {
                var task = default(ValueTask);
                await task;
            },
            configuration: GetConfiguration().WithTestingIterations(1));
        }

        [Fact(Timeout = 5000)]
        public void TestNotSupportedTimerType()
        {
            this.TestWithException<NotSupportedException>(() =>
            {
                using var timer = new Timer(_ => Console.WriteLine("Hello!"), null, 1, 0);
            },
            configuration: GetConfiguration().WithTestingIterations(1));
        }
    }
}
