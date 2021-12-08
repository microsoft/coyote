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
        public void TestRewritingTaskFactoryStartNew()
        {
            Task.Factory.StartNew(() => { });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingGenericTaskFactoryStartNew()
        {
            Task<int>.Factory.StartNew(() => 1);
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingNestedGenericTaskFactoryStartNew()
        {
            Task<Task<int>>.Factory.StartNew(() => Task.FromResult(1));
        }
#pragma warning restore CA2008 // Do not create tasks without passing a TaskScheduler
    }
}
