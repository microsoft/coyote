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
            Task.WhenAll(default(Task));
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingGenericTaskWhenAll()
        {
            Task.WhenAll(default(Task<int>));
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWhenAny()
        {
            Task.WhenAny(default(Task));
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingGenericTaskWhenAny()
        {
            Task.WhenAny(default(Task<int>));
        }
    }
}
