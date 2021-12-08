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
        public void TestTaskWhenAllRewriting()
        {
            Task.WhenAll(default(Task));
        }

        [Fact(Timeout = 5000)]
        public void TestGenericTaskWhenAllRewriting()
        {
            Task.WhenAll(default(Task<int>));
        }

        [Fact(Timeout = 5000)]
        public void TestTaskWhenAnyRewriting()
        {
            Task.WhenAny(default(Task));
        }

        [Fact(Timeout = 5000)]
        public void TestGenericTaskWhenAnyRewriting()
        {
            Task.WhenAny(default(Task<int>));
        }
    }
}
