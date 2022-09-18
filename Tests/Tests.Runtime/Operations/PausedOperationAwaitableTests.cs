// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;
using CoyoteTypes = Microsoft.Coyote.Rewriting.Types;

namespace Microsoft.Coyote.Runtime.Tests
{
    public class PausedOperationAwaitableTests : BaseRuntimeTest
    {
        public PausedOperationAwaitableTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestPausedOperationAwaitablePersistsOperation()
        {
            this.RunSystematicTest(() =>
            {
                var tcs = new TaskCompletionSource<bool>();
                var task = CoyoteTypes.Threading.Tasks.Task.Run(async () =>
                {
                    var op = CoyoteRuntime.Current.GetExecutingOperation();
                    await CoyoteRuntime.Current.PauseOperationUntilAsync(() => tcs.Task.IsCompleted, false);
                    Specification.Assert(op == CoyoteRuntime.Current.GetExecutingOperation(),
                        "Operation of the continuation is not the same.");
                });

                CoyoteTypes.Threading.Tasks.TaskCompletionSource<bool>.SetResult(tcs, true);
                CoyoteTypes.Threading.Tasks.Task.Wait(task);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestPausedOperationAwaitableResumesAsynchronously()
        {
            this.RunSystematicTest(() =>
            {
                var tcs1 = new TaskCompletionSource<bool>();
                var tcs2 = new TaskCompletionSource<bool>();
                var task = CoyoteTypes.Threading.Tasks.Task.Run(async () =>
                {
                    var op = CoyoteRuntime.Current.GetExecutingOperation();
                    CoyoteTypes.Threading.Tasks.TaskCompletionSource<bool>.SetResult(tcs2, true);
                    await CoyoteRuntime.Current.PauseOperationUntilAsync(() => tcs1.Task.IsCompleted, true);
                    Specification.Assert(op != CoyoteRuntime.Current.GetExecutingOperation(),
                        "Operation of the continuation is the same.");
                });

                CoyoteRuntime.Current.PauseOperationUntil(default, () => tcs2.Task.IsCompleted);
                CoyoteTypes.Threading.Tasks.TaskCompletionSource<bool>.SetResult(tcs1, true);
                CoyoteTypes.Threading.Tasks.Task.Wait(task);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        private static async AsyncTask RecursiveAsync(TaskCompletionSource<bool> tcs, int depth, int maxDepth)
        {
            if (++depth < maxDepth)
            {
                await RecursiveAsync(tcs, depth, maxDepth);
            }

            if (depth == maxDepth)
            {
                await CoyoteRuntime.Current.PauseOperationUntilAsync(() => tcs.Task.IsCompleted, false);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestPausedOperationAwaitableRunsAsynchronously()
        {
            this.RunSystematicTest(() =>
            {
                var tcs1 = new TaskCompletionSource<bool>();
                var tcs2 = new TaskCompletionSource<bool>();
                var task = CoyoteTypes.Threading.Tasks.Task.Run(async () =>
                {
                    var op = CoyoteRuntime.Current.GetExecutingOperation();
                    var t = RecursiveAsync(tcs1, 0, 10);
                    CoyoteTypes.Threading.Tasks.TaskCompletionSource<bool>.SetResult(tcs2, true);
                    await t;
                    Specification.Assert(op == CoyoteRuntime.Current.GetExecutingOperation(),
                        "Operation of the continuation is not the same.");
                });

                CoyoteRuntime.Current.PauseOperationUntil(default, () => tcs2.Task.IsCompleted);
                CoyoteTypes.Threading.Tasks.TaskCompletionSource<bool>.SetResult(tcs1, true);
                CoyoteTypes.Threading.Tasks.Task.Wait(task);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }
    }
}
