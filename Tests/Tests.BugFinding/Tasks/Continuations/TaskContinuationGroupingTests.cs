// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class TaskContinuationGroupingTests : BaseBugFindingTest
    {
        public TaskContinuationGroupingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestTaskContinuationGroupingWithYield()
        {
            this.Test(async () =>
            {
                OperationGroup originalGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                await Task.Yield();
                OperationGroup newGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                Specification.Assert(newGroup == originalGroup,
                    $"The new '{newGroup}' and original '{originalGroup}' groups differ.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestTaskContinuationGroupingWithTaskRun()
        {
            this.Test(async () =>
            {
                OperationGroup originalGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                OperationGroup taskGroup = null;
                Task task = Task.Run(() =>
                {
                    taskGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                });

                bool isAwaitCompleted = task.IsCompleted;
                await task;

                OperationGroup newGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                if (isAwaitCompleted)
                {
                    Specification.Assert(newGroup == originalGroup,
                        $"The new '{newGroup}' and original '{originalGroup}' groups differ.");
                }
                else
                {
                    Specification.Assert(newGroup == taskGroup,
                        $"The new '{newGroup}' and task '{taskGroup}' groups differ.");
                }
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestTaskContinuationGroupingWithAsyncTaskRun()
        {
            this.Test(async () =>
            {
                OperationGroup originalGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                OperationGroup taskGroup = null;
                Task task = Task.Run(async () =>
                {
                    OperationGroup originalTaskGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                    await Task.CompletedTask;
                    taskGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                    Specification.Assert(taskGroup == originalTaskGroup,
                        $"The task '{taskGroup}' and original task '{originalTaskGroup}' groups differ.");
                });

                bool isAwaitCompleted = task.IsCompleted;
                await task;

                OperationGroup newGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                if (isAwaitCompleted)
                {
                    Specification.Assert(newGroup == originalGroup,
                        $"The new '{newGroup}' and original '{originalGroup}' groups differ.");
                }
                else
                {
                    Specification.Assert(newGroup == taskGroup,
                        $"The new '{newGroup}' and task '{taskGroup}' groups differ.");
                }
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestTaskContinuationGroupingWithYieldedTaskRun()
        {
            this.Test(async () =>
            {
                OperationGroup originalGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                OperationGroup taskGroup = null;
                Task task = Task.Run(async () =>
                {
                    OperationGroup originalTaskGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                    await Task.Yield();
                    taskGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                    Specification.Assert(taskGroup == originalTaskGroup,
                        $"The task '{taskGroup}' and original task '{originalTaskGroup}' groups differ.");
                });

                bool isAwaitCompleted = task.IsCompleted;
                await task;

                OperationGroup newGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                if (isAwaitCompleted)
                {
                    Specification.Assert(newGroup == originalGroup,
                        $"The new '{newGroup}' and original '{originalGroup}' groups differ.");
                }
                else
                {
                    Specification.Assert(newGroup == taskGroup,
                        $"The new '{newGroup}' and task '{taskGroup}' groups differ.");
                }
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestTaskContinuationGroupingWithNestedAsyncTaskRun()
        {
            this.Test(async () =>
            {
                OperationGroup originalGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                OperationGroup taskGroup = null;
                Task task = Task.Run(async () =>
                {
                    OperationGroup originalTaskGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                    OperationGroup innerTaskGroup = null;
                    Task innerTask = Task.Run(async () =>
                    {
                        OperationGroup originalInnerTaskGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                        await Task.CompletedTask;
                        innerTaskGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                        Specification.Assert(innerTaskGroup == originalInnerTaskGroup,
                            $"The inner task '{innerTaskGroup}' and original inner task '{originalInnerTaskGroup}' groups differ.");
                    });

                    bool isInnerAwaitCompleted = innerTask.IsCompleted;
                    await innerTask;

                    taskGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                    if (isInnerAwaitCompleted)
                    {
                        Specification.Assert(taskGroup == originalTaskGroup,
                            $"The task '{taskGroup}' and original task '{originalTaskGroup}' groups differ.");
                    }
                    else
                    {
                        Specification.Assert(taskGroup == innerTaskGroup,
                            $"The task '{taskGroup}' and inner task '{innerTaskGroup}' groups differ.");
                    }
                });

                bool isAwaitCompleted = task.IsCompleted;
                await task;

                OperationGroup newGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                if (isAwaitCompleted)
                {
                    Specification.Assert(newGroup == originalGroup,
                        $"The new '{newGroup}' and original '{originalGroup}' groups differ.");
                }
                else
                {
                    Specification.Assert(newGroup == taskGroup,
                        $"The new '{newGroup}' and task '{taskGroup}' groups differ.");
                }
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestTaskContinuationGroupingWithNestedYieldedTaskRun()
        {
            this.Test(async () =>
            {
                OperationGroup originalGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                OperationGroup taskGroup = null;
                Task task = Task.Run(async () =>
                {
                    OperationGroup originalTaskGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                    OperationGroup innerTaskGroup = null;
                    Task innerTask = Task.Run(async () =>
                    {
                        OperationGroup originalInnerTaskGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                        await Task.Yield();
                        innerTaskGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                        Specification.Assert(innerTaskGroup == originalInnerTaskGroup,
                            $"The inner task '{innerTaskGroup}' and original inner task '{originalInnerTaskGroup}' groups differ.");
                    });

                    bool isInnerAwaitCompleted = innerTask.IsCompleted;
                    await innerTask;

                    taskGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                    if (isInnerAwaitCompleted)
                    {
                        Specification.Assert(taskGroup == originalTaskGroup,
                            $"The task '{taskGroup}' and original task '{originalTaskGroup}' groups differ.");
                    }
                    else
                    {
                        Specification.Assert(taskGroup == innerTaskGroup,
                            $"The task '{taskGroup}' and inner task '{innerTaskGroup}' groups differ.");
                    }
                });

                bool isAwaitCompleted = task.IsCompleted;
                await task;

                OperationGroup newGroup = CoyoteRuntime.Current.GetExecutingOperation().Group;
                if (isAwaitCompleted)
                {
                    Specification.Assert(newGroup == originalGroup,
                        $"The new '{newGroup}' and original '{originalGroup}' groups differ.");
                }
                else
                {
                    Specification.Assert(newGroup == taskGroup,
                        $"The new '{newGroup}' and task '{taskGroup}' groups differ.");
                }
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }
    }
}
