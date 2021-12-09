// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class TaskRewritingTests : BaseRewritingTest
    {
        public TaskRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWhenAll()
        {
            Task.WhenAll(Task.CompletedTask);
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingGenericTaskWhenAll()
        {
            Task.WhenAll(Task.FromResult(1));
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWhenAny()
        {
            Task.WhenAny(Task.CompletedTask);
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingGenericTaskWhenAny()
        {
            Task.WhenAny(Task.FromResult(1));
        }
    }
}
