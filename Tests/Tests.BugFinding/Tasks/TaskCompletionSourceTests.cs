// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class TaskCompletionSourceTests : BaseBugFindingTest
    {
        public TaskCompletionSourceTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestSetResult()
        {
            this.TestWithError(async () =>
            {
                var tcs = new TaskCompletionSource();
                tcs.SetResult();
                await tcs.Task;
                Specification.Assert(tcs.Task.Status is TaskStatus.RanToCompletion,
                    "Found unexpected status {0}.", tcs.Task.Status);
                Specification.Assert(false, "Reached test assertion.");
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestTrySetResult()
        {
            this.TestWithError(async () =>
            {
                var tcs = new TaskCompletionSource();
                tcs.SetResult();
                bool check = tcs.TrySetResult();
                await tcs.Task;
                Specification.Assert(!check, "Cannot set result again.");
                Specification.Assert(tcs.Task.Status is TaskStatus.RanToCompletion,
                    "Found unexpected status {0}.", tcs.Task.Status);
                Specification.Assert(false, "Reached test assertion.");
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAsynchronousSetResult()
        {
            this.Test(async () =>
            {
                var tcs = new TaskCompletionSource();
                var task1 = Task.Run(async () =>
                {
                    await tcs.Task;
                });

                var task2 = Task.Run(() =>
                {
                    tcs.SetResult();
                });

                await task1;
                Specification.Assert(tcs.Task.Status is TaskStatus.RanToCompletion,
                    "Found unexpected status {0}.", tcs.Task.Status);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAsynchronousSetResultTask()
        {
            this.Test(async () =>
            {
                var tcs = new TaskCompletionSource();
                var task1 = Task.Run(async () =>
                {
                    await tcs.Task;
                });

                var task2 = Task.Run(() =>
                {
                    tcs.SetResult();
                });

                await task1;
                Specification.Assert(tcs.Task.Status is TaskStatus.RanToCompletion,
                    "Found unexpected status {0}.", tcs.Task.Status);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAsynchronousSetResultWithTwoAwaiters()
        {
            this.Test(async () =>
            {
                var tcs = new TaskCompletionSource();
                var task1 = Task.Run(async () =>
                {
                    await tcs.Task;
                });

                var task2 = Task.Run(async () =>
                {
                    await tcs.Task;
                });

                tcs.SetResult();
                await Task.WhenAll(task1, task2);
                Specification.Assert(tcs.Task.Status is TaskStatus.RanToCompletion,
                    "Found unexpected status {0}.", tcs.Task.Status);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestSetCanceled()
        {
            this.TestWithError(async () =>
            {
                var tcs = new TaskCompletionSource();
                tcs.SetCanceled();

                Exception exception = null;
                try
                {
                    await tcs.Task;
                }
                catch (Exception ex) when (!(ex is ThreadInterruptedException))
                {
                    exception = ex;
                }

                Specification.Assert(exception is OperationCanceledException,
                    "Threw unexpected exception {0}.", exception.GetType());
                Specification.Assert(tcs.Task.Status is TaskStatus.Canceled,
                    "Found unexpected status {0}.", tcs.Task.Status);
                Specification.Assert(false, "Reached test assertion.");
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestTrySetCanceled()
        {
            this.TestWithError(async () =>
            {
                var tcs = new TaskCompletionSource();
                tcs.SetCanceled();
                bool check = tcs.TrySetCanceled();

                Exception exception = null;
                try
                {
                    await tcs.Task;
                }
                catch (Exception ex) when (!(ex is ThreadInterruptedException))
                {
                    exception = ex;
                }

                Specification.Assert(!check, "Cannot set result again.");
                Specification.Assert(exception is OperationCanceledException,
                    "Threw unexpected exception {0}.", exception.GetType());
                Specification.Assert(tcs.Task.Status is TaskStatus.Canceled,
                    "Found unexpected status {0}.", tcs.Task.Status);
                Specification.Assert(false, "Reached test assertion.");
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAsynchronousSetCanceled()
        {
            this.Test(async () =>
            {
                var tcs = new TaskCompletionSource();
                var task = Task.Run(() =>
                {
                    tcs.SetCanceled();
                });

                Exception exception = null;
                try
                {
                    await tcs.Task;
                }
                catch (Exception ex) when (!(ex is ThreadInterruptedException))
                {
                    exception = ex;
                }

                Specification.Assert(exception is OperationCanceledException,
                    "Threw unexpected exception {0}.", exception.GetType());
                Specification.Assert(tcs.Task.Status is TaskStatus.Canceled,
                    "Found unexpected status {0}.", tcs.Task.Status);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestSetException()
        {
            this.TestWithError(async () =>
            {
                var tcs = new TaskCompletionSource();
                tcs.SetException(new InvalidOperationException());

                Exception exception = null;
                try
                {
                    await tcs.Task;
                }
                catch (Exception ex) when (!(ex is ThreadInterruptedException))
                {
                    exception = ex;
                }

                Specification.Assert(exception is InvalidOperationException,
                    "Threw unexpected exception {0}.", exception.GetType());
                Specification.Assert(tcs.Task.Status is TaskStatus.Faulted,
                    "Found unexpected status {0}.", tcs.Task.Status);
                Specification.Assert(false, "Reached test assertion.");
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestTrySetException()
        {
            this.TestWithError(async () =>
            {
                var tcs = new TaskCompletionSource();
                tcs.SetException(new InvalidOperationException());
                bool check = tcs.TrySetException(new NotImplementedException());

                Exception exception = null;
                try
                {
                    await tcs.Task;
                }
                catch (Exception ex) when (!(ex is ThreadInterruptedException))
                {
                    exception = ex;
                }

                Specification.Assert(!check, "Cannot set result again.");
                Specification.Assert(exception is InvalidOperationException,
                    "Threw unexpected exception {0}.", exception.GetType());
                Specification.Assert(tcs.Task.Status is TaskStatus.Faulted,
                    "Found unexpected status {0}.", tcs.Task.Status);
                Specification.Assert(false, "Reached test assertion.");
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAsynchronousSetException()
        {
            this.Test(async () =>
            {
                var tcs = new TaskCompletionSource();
                var task = Task.Run(() =>
                {
                    tcs.SetException(new InvalidOperationException());
                });

                Exception exception = null;
                try
                {
                    await tcs.Task;
                }
                catch (Exception ex) when (!(ex is ThreadInterruptedException))
                {
                    exception = ex;
                }

                Specification.Assert(exception is InvalidOperationException,
                    "Threw unexpected exception {0}.", exception.GetType());
                Specification.Assert(tcs.Task.Status is TaskStatus.Faulted,
                    "Found unexpected status {0}.", tcs.Task.Status);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestInvalidSetResult()
        {
            this.TestWithError(() =>
            {
                var tcs = new TaskCompletionSource();
                tcs.SetResult();

                Exception exception = null;
                try
                {
                    tcs.SetResult();
                }
                catch (Exception ex) when (!(ex is ThreadInterruptedException))
                {
                    exception = ex;
                }

                Specification.Assert(exception is InvalidOperationException,
                    "Threw unexpected exception {0}.", exception.GetType());
                Specification.Assert(false, "Reached test assertion.");
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestInvalidSetCanceled()
        {
            this.TestWithError(() =>
            {
                var tcs = new TaskCompletionSource();
                tcs.SetResult();

                Exception exception = null;
                try
                {
                    tcs.SetCanceled();
                }
                catch (Exception ex) when (!(ex is ThreadInterruptedException))
                {
                    exception = ex;
                }

                Specification.Assert(exception is InvalidOperationException,
                    "Threw unexpected exception {0}.", exception.GetType());
                Specification.Assert(false, "Reached test assertion.");
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestInvalidSetException()
        {
            this.TestWithError(() =>
            {
                var tcs = new TaskCompletionSource();
                tcs.SetResult();

                Exception exception = null;
                try
                {
                    tcs.SetException(new InvalidOperationException());
                }
                catch (Exception ex) when (!(ex is ThreadInterruptedException))
                {
                    exception = ex;
                }

                Specification.Assert(exception is InvalidOperationException,
                    "Threw unexpected exception {0}.", exception.GetType());
                Specification.Assert(false, "Reached test assertion.");
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestIsCompleted()
        {
            this.TestWithError(async () =>
            {
                var tcs = new TaskCompletionSource();
                var task = tcs.Task;
                tcs.SetResult();
                Specification.Assert(tcs.Task.IsCompleted, "Task is not completed.");
                await task;
                Specification.Assert(task.IsCompleted, "Task is not completed.");
                Specification.Assert(false, "Reached test assertion.");
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }
    }
}
#endif
