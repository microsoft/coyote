// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Runtime.CompilerServices;
using Xunit;
using Xunit.Abstractions;
using CoyoteTask = Microsoft.Coyote.Rewriting.Types.Threading.Tasks.Task;

namespace Microsoft.Coyote.Runtime.Tests
{
    public class AsyncConditionAwaiterStateMachineTests : BaseRuntimeTest
    {
        public AsyncConditionAwaiterStateMachineTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestAsyncConditionPreservesDependencyControlStatus(bool isConditionControlled)
        {
            this.RunSystematicTest(() =>
            {
                CoyoteRuntime runtime = CoyoteRuntime.Current;
                ControlledOperation waiting = null;
                bool complete = false;
                Task waiter = AsyncConditionAwaiterStateMachine.RunAsync(runtime, () =>
                {
                    ControlledOperation current = runtime.GetExecutingOperation();
                    if (!current.IsRoot)
                    {
                        waiting ??= current;
                    }

                    return complete;
                },
                isConditionControlled: isConditionControlled);
                Task completer = CoyoteTask.Run(() =>
                {
                    runtime.PauseOperationUntil(default, () => waiting?.IsPaused is true);
                    Assert.Equal(!isConditionControlled, waiting.IsDependencyUncontrolled);
                    complete = true;
                });

                CoyoteTask.WaitAll(new[] { waiter, completer });
            },
            configuration: this.GetConfiguration().WithTestingIterations(20));
        }
    }
}
