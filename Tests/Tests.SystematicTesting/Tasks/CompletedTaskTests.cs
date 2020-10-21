// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class CompletedTaskTests : BaseSystematicTest
    {
        public CompletedTaskTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestCompletedTask()
        {
            Task task = Task.CompletedTask;
            Assert.True(task.IsCompleted);
        }

        [Fact(Timeout = 5000)]
        public void TestCanceledTask()
        {
            CancellationToken token = new CancellationToken(true);
            Task task = Task.FromCanceled(token);
            Assert.True(task.IsCanceled);
        }

        [Fact(Timeout = 5000)]
        public void TestCanceledTaskWithResult()
        {
            CancellationToken token = new CancellationToken(true);
            Task<int> task = Task.FromCanceled<int>(token);
            Assert.True(task.IsCanceled);
        }

        [Fact(Timeout = 5000)]
        public void TestFailedTask()
        {
            Task task = Task.FromException(new InvalidOperationException());
            Assert.True(task.IsFaulted);
            Assert.Equal(typeof(AggregateException), task.Exception.GetType());
            Assert.Equal(typeof(InvalidOperationException), task.Exception.InnerException.GetType());
        }

        [Fact(Timeout = 5000)]
        public void TestFailedTaskWithResult()
        {
            Task<int> task = Task.FromException<int>(new InvalidOperationException());
            Assert.True(task.IsFaulted);
            Assert.Equal(typeof(AggregateException), task.Exception.GetType());
            Assert.Equal(typeof(InvalidOperationException), task.Exception.InnerException.GetType());
        }
    }
}
