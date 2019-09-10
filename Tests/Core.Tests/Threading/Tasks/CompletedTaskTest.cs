// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using Microsoft.Coyote.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests
{
    public class CompletedTaskTest : BaseTest
    {
        public CompletedTaskTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestCompletedTask()
        {
            ControlledTask task = ControlledTask.CompletedTask;
            Assert.True(task.IsCompleted);
        }

        [Fact(Timeout = 5000)]
        public void TestCanceledTask()
        {
            CancellationToken token = new CancellationToken(true);
            ControlledTask task = ControlledTask.FromCanceled(token);
            Assert.True(task.IsCanceled);
        }

        [Fact(Timeout = 5000)]
        public void TestCanceledTaskWithResult()
        {
            CancellationToken token = new CancellationToken(true);
            ControlledTask<int> task = ControlledTask.FromCanceled<int>(token);
            Assert.True(task.IsCanceled);
        }

        [Fact(Timeout = 5000)]
        public void TestFailedTask()
        {
            ControlledTask task = ControlledTask.FromException(new InvalidOperationException());
            Assert.True(task.IsFaulted);
            Assert.Equal(typeof(AggregateException), task.Exception.GetType());
            Assert.Equal(typeof(InvalidOperationException), task.Exception.InnerException.GetType());
        }

        [Fact(Timeout = 5000)]
        public void TestFailedTaskWithResult()
        {
            ControlledTask<int> task = ControlledTask.FromException<int>(new InvalidOperationException());
            Assert.True(task.IsFaulted);
            Assert.Equal(typeof(AggregateException), task.Exception.GetType());
            Assert.Equal(typeof(InvalidOperationException), task.Exception.InnerException.GetType());
        }
    }
}
