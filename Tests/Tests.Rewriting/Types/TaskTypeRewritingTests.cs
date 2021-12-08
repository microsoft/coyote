// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class TaskTypeRewritingTests : BaseRewritingTest
    {
        public TaskTypeRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestTaskWhenAllRewriting()
        {
            this.Test(() =>
            {
                Task.WhenAll(default(Task));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestGenericTaskWhenAllRewriting()
        {
            this.Test(() =>
            {
                Task.WhenAll(default(Task<int>));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestTaskWhenAnyRewriting()
        {
            this.Test(() =>
            {
                Task.WhenAny(default(Task));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestGenericTaskWhenAnyRewriting()
        {
            this.Test(() =>
            {
                Task.WhenAny(default(Task<int>));
            });
        }
    }
}
