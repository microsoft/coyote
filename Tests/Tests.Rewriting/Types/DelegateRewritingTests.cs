// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class DelegateRewritingTests : BaseRewritingTest
    {
        public DelegateRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingAction()
        {
            _ = new Action<Task>(task => { });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingFunc()
        {
            _ = new Func<Task>(() => Task.CompletedTask);
        }
    }
}
