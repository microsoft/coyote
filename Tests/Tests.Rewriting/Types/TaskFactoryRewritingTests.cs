// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class TaskFactoryRewritingTests : BaseRewritingTest
    {
#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler
        public TaskFactoryRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestTaskFactoryStartNewRewriting()
        {
            Task.Factory.StartNew(() => { });
        }
#pragma warning restore CA2008 // Do not create tasks without passing a TaskScheduler
    }
}
