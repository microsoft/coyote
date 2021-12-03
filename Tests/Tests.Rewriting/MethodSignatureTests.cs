// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class MethodSignatureTests : BaseRewritingTest
    {
        public MethodSignatureTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private static TaskAwaiter GetTaskAwaiter(TaskAwaiter taskAwaiter) => taskAwaiter;
        private static TaskAwaiter<T> GetGenericTaskAwaiter<T>(TaskAwaiter<T> taskAwaiter) => taskAwaiter;

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskAwaiterInMethodSignature()
        {
            this.Test(() =>
            {
                GetTaskAwaiter(default(TaskAwaiter));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingGenericTaskAwaiterInMethodSignature()
        {
            this.Test(() =>
            {
                GetGenericTaskAwaiter<int>(default(TaskAwaiter<int>));
            });
        }
    }
}
