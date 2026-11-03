// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime.CompilerServices;
using Xunit;
using Xunit.Abstractions;
using CoyoteCompiler = Microsoft.Coyote.Rewriting.Types.Runtime.CompilerServices;
using SystemCompiler = System.Runtime.CompilerServices;

#pragma warning disable CA1825 // MemberData attribute arguments cannot use Array.Empty.

namespace Microsoft.Coyote.Runtime.Tests
{
    public class AsyncMethodBuilderLifetimeTests : BaseRuntimeTest
    {
        public AsyncMethodBuilderLifetimeTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public enum BuilderType
        {
            Task,
            GenericTask,
            ValueTask,
            GenericValueTask
        }

        public enum CompletionMode
        {
            Deferred,
            DuringRegistration,
            DuringControlCheck
        }

        public static IEnumerable<object[]> LifetimeCases()
        {
            foreach (BuilderType builder in Enum.GetValues(typeof(BuilderType)))
            {
                foreach (bool useUnsafeRegistration in new[] { false, true })
                {
                    foreach (bool startWithControlledAwait in new[] { false, true })
                    {
                        foreach (CompletionMode completion in Enum.GetValues(typeof(CompletionMode)))
                        {
                            foreach (bool fuzzing in new[] { false, true })
                            {
                                yield return new object[]
                                {
                                    builder, useUnsafeRegistration, startWithControlledAwait, completion, fuzzing
                                };
                            }
                        }
                    }
                }
            }
        }

        public static IEnumerable<object[]> BuilderCases()
        {
            foreach (BuilderType builder in Enum.GetValues(typeof(BuilderType)))
            {
                foreach (bool useUnsafeRegistration in new[] { false, true })
                {
                    yield return new object[] { builder, useUnsafeRegistration };
                }
            }
        }

        private sealed class AwaitRegistration
        {
            internal Action Continuation;
            internal CompletionMode Completion;
            internal Exception Error;
            internal TaskCompletionSource<bool> Dependency;
            internal CoyoteRuntime Runtime;
            internal bool CompleteDependencyDuringRegistration;
            internal int ControlChecks;
            internal int Completions;

            internal void Resume()
            {
                Action continuation = this.Continuation;
                this.Continuation = null;
                continuation();
            }
        }

        private readonly struct ParkingAwaiter : SystemCompiler.ICriticalNotifyCompletion
        {
            private readonly AwaitRegistration Registration;

            internal ParkingAwaiter(AwaitRegistration registration) => this.Registration = registration;

            public void OnCompleted(Action continuation) => this.Registration.Continuation = continuation;

            public void UnsafeOnCompleted(Action continuation) => this.OnCompleted(continuation);
        }

        private readonly struct CompletingAwaiter : SystemCompiler.ICriticalNotifyCompletion, IControllableAwaiter
        {
            private readonly AwaitRegistration Registration;

            internal CompletingAwaiter(AwaitRegistration registration) => this.Registration = registration;

            bool IControllableAwaiter.IsControlled
            {
                get
                {
                    this.Registration.ControlChecks++;
                    if (this.Registration.Completion is CompletionMode.DuringControlCheck)
                    {
                        this.Registration.Resume();
                    }

                    return this.Registration.Dependency is null ||
                        !this.Registration.Runtime.IsTaskUncontrolled(this.Registration.Dependency.Task);
                }
            }

            public void OnCompleted(Action continuation)
            {
                this.Registration.Continuation = continuation;
                if (this.Registration.CompleteDependencyDuringRegistration)
                {
                    this.Registration.Dependency.SetResult(true);
                }

                if (this.Registration.Completion is CompletionMode.DuringRegistration)
                {
                    this.Registration.Resume();
                }
            }

            public void UnsafeOnCompleted(Action continuation) => this.OnCompleted(continuation);
        }

        private struct StateMachine : SystemCompiler.IAsyncStateMachine
        {
            internal BuilderType Kind;
            internal AwaitRegistration Registration;
            internal bool UseUnsafeRegistration;
            internal bool StartWithControlledAwait;
            private CoyoteCompiler.AsyncTaskMethodBuilder TaskBuilder;
            private CoyoteCompiler.AsyncTaskMethodBuilder<int> GenericTaskBuilder;
            private CoyoteCompiler.AsyncValueTaskMethodBuilder ValueTaskBuilder;
            private CoyoteCompiler.AsyncValueTaskMethodBuilder<int> GenericValueTaskBuilder;
            private CompletingAwaiter Awaiter;
            private int State;

            internal void Initialize(bool useDefaultBuilder = false)
            {
                switch (this.Kind)
                {
                    case BuilderType.Task:
                        this.TaskBuilder = useDefaultBuilder ? default : CoyoteCompiler.AsyncTaskMethodBuilder.Create();
                        break;
                    case BuilderType.GenericTask:
                        this.GenericTaskBuilder = useDefaultBuilder ? default : CoyoteCompiler.AsyncTaskMethodBuilder<int>.Create();
                        break;
                    case BuilderType.ValueTask:
                        this.ValueTaskBuilder = useDefaultBuilder ? default : CoyoteCompiler.AsyncValueTaskMethodBuilder.Create();
                        break;
                    case BuilderType.GenericValueTask:
                        this.GenericValueTaskBuilder = useDefaultBuilder ? default : CoyoteCompiler.AsyncValueTaskMethodBuilder<int>.Create();
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }

            internal void Start()
            {
                switch (this.Kind)
                {
                    case BuilderType.Task:
                        this.TaskBuilder.Start(ref this);
                        break;
                    case BuilderType.GenericTask:
                        this.GenericTaskBuilder.Start(ref this);
                        break;
                    case BuilderType.ValueTask:
                        this.ValueTaskBuilder.Start(ref this);
                        break;
                    case BuilderType.GenericValueTask:
                        this.GenericValueTaskBuilder.Start(ref this);
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }

            internal Task GetTask()
            {
                switch (this.Kind)
                {
                    case BuilderType.Task:
                        return this.TaskBuilder.Task;
                    case BuilderType.GenericTask:
                        return this.GenericTaskBuilder.Task;
                    case BuilderType.ValueTask:
                        var valueTask = this.ValueTaskBuilder.Task;
                        if (ValueTaskAwaiter.TryGetTask(ref valueTask, out Task task))
                        {
                            return task;
                        }

                        throw new InvalidOperationException("The suspended ValueTask must have a backing task.");
                    case BuilderType.GenericValueTask:
                        var genericValueTask = this.GenericValueTaskBuilder.Task;
                        if (ValueTaskAwaiter.TryGetTask(ref genericValueTask, out Task<int> genericTask))
                        {
                            return genericTask;
                        }

                        throw new InvalidOperationException("The suspended ValueTask must have a backing task.");
                    default:
                        throw new InvalidOperationException();
                }
            }

            private void Await<TAwaiter>(ref TAwaiter awaiter)
                where TAwaiter : SystemCompiler.ICriticalNotifyCompletion
            {
                switch (this.Kind)
                {
                    case BuilderType.Task:
                        if (this.UseUnsafeRegistration)
                        {
                            this.TaskBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
                        }
                        else
                        {
                            this.TaskBuilder.AwaitOnCompleted(ref awaiter, ref this);
                        }

                        break;
                    case BuilderType.GenericTask:
                        if (this.UseUnsafeRegistration)
                        {
                            this.GenericTaskBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
                        }
                        else
                        {
                            this.GenericTaskBuilder.AwaitOnCompleted(ref awaiter, ref this);
                        }

                        break;
                    case BuilderType.ValueTask:
                        if (this.UseUnsafeRegistration)
                        {
                            this.ValueTaskBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
                        }
                        else
                        {
                            this.ValueTaskBuilder.AwaitOnCompleted(ref awaiter, ref this);
                        }

                        break;
                    case BuilderType.GenericValueTask:
                        if (this.UseUnsafeRegistration)
                        {
                            this.GenericValueTaskBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
                        }
                        else
                        {
                            this.GenericValueTaskBuilder.AwaitOnCompleted(ref awaiter, ref this);
                        }

                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }

            public void MoveNext()
            {
                // Native completion clears the boxed struct, so keep the probe in a local.
                AwaitRegistration registration = this.Registration;
                if (this.State is 0 && !this.StartWithControlledAwait)
                {
                    this.State = 1;
                    var awaiter = new ParkingAwaiter(registration);
                    this.Await(ref awaiter);
                }
                else if (this.State < 2)
                {
                    this.State = 2;
                    this.Awaiter = new CompletingAwaiter(registration);
                    this.Await(ref this.Awaiter);
                }
                else
                {
                    switch (this.Kind)
                    {
                        case BuilderType.Task:
                            if (registration.Error is null)
                            {
                                this.TaskBuilder.SetResult();
                            }
                            else
                            {
                                this.TaskBuilder.SetException(registration.Error);
                            }

                            break;
                        case BuilderType.GenericTask:
                            if (registration.Error is null)
                            {
                                this.GenericTaskBuilder.SetResult(42);
                            }
                            else
                            {
                                this.GenericTaskBuilder.SetException(registration.Error);
                            }

                            break;
                        case BuilderType.ValueTask:
                            if (registration.Error is null)
                            {
                                this.ValueTaskBuilder.SetResult();
                            }
                            else
                            {
                                this.ValueTaskBuilder.SetException(registration.Error);
                            }

                            break;
                        case BuilderType.GenericValueTask:
                            if (registration.Error is null)
                            {
                                this.GenericValueTaskBuilder.SetResult(42);
                            }
                            else
                            {
                                this.GenericValueTaskBuilder.SetException(registration.Error);
                            }

                            break;
                        default:
                            throw new InvalidOperationException();
                    }

                    registration.Completions++;
                }
            }

            public void SetStateMachine(SystemCompiler.IAsyncStateMachine stateMachine)
            {
                switch (this.Kind)
                {
                    case BuilderType.Task:
                        this.TaskBuilder.SetStateMachine(stateMachine);
                        break;
                    case BuilderType.GenericTask:
                        this.GenericTaskBuilder.SetStateMachine(stateMachine);
                        break;
                    case BuilderType.ValueTask:
                        this.ValueTaskBuilder.SetStateMachine(stateMachine);
                        break;
                    case BuilderType.GenericValueTask:
                        this.GenericValueTaskBuilder.SetStateMachine(stateMachine);
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        private static ConcurrentDictionary<Task, ControlledOperation> GetRegisteredTasks(CoyoteRuntime runtime)
        {
            FieldInfo field = typeof(CoyoteRuntime).GetField("ControlledTasks", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            return Assert.IsType<ConcurrentDictionary<Task, ControlledOperation>>(field.GetValue(runtime));
        }

        [Theory(Timeout = 5000)]
        [MemberData(nameof(LifetimeCases))]
        public void TestTaskIdentitySurvivesStateMachineCompletion(BuilderType builder, bool useUnsafeRegistration,
            bool startWithControlledAwait, CompletionMode completion, bool fuzzing)
        {
            this.RunSystematicTest(() =>
            {
                var tasks = GetRegisteredTasks(CoyoteRuntime.Current);
                int registeredTaskCount = tasks.Count;
                var registration = new AwaitRegistration { Completion = completion };
                var stateMachine = new StateMachine
                {
                    Kind = builder,
                    Registration = registration,
                    UseUnsafeRegistration = useUnsafeRegistration,
                    StartWithControlledAwait = startWithControlledAwait
                };
                stateMachine.Initialize();
                stateMachine.Start();
                Task task = stateMachine.GetTask();
                if (!startWithControlledAwait)
                {
                    Assert.False(task.IsCompleted);
                    Assert.False(tasks.ContainsKey(task));
                    registration.Resume();
                }

                Assert.Equal(1, registration.ControlChecks);
                Assert.True(tasks.TryGetValue(task, out ControlledOperation operation),
                    "The original returned task was not registered.");
                Assert.Null(operation);
                Assert.Equal(registeredTaskCount + 1, tasks.Count);
                Assert.Same(task, stateMachine.GetTask());
                if (completion is CompletionMode.Deferred)
                {
                    Assert.False(task.IsCompleted);
                    registration.Resume();
                }

                Assert.Equal(TaskStatus.RanToCompletion, task.Status);
                if (task is Task<int> genericTask)
                {
                    Assert.Equal(42, genericTask.Result);
                }

                Assert.Equal(1, registration.Completions);
            },
            configuration: this.GetConfiguration().WithTestingIterations(1).WithSystematicFuzzingEnabled(fuzzing));
        }

        [Theory(Timeout = 5000)]
        [MemberData(nameof(BuilderCases))]
        public void TestControlStatusIsCheckedAfterRegistration(BuilderType builder, bool useUnsafeRegistration)
        {
            this.RunSystematicTest(() =>
            {
                var tasks = GetRegisteredTasks(CoyoteRuntime.Current);
                foreach (bool completesDuringRegistration in new[] { false, true })
                {
                    var registration = new AwaitRegistration
                    {
                        Runtime = CoyoteRuntime.Current,
                        Dependency = new TaskCompletionSource<bool>(),
                        CompleteDependencyDuringRegistration = completesDuringRegistration
                    };
                    var stateMachine = new StateMachine
                    {
                        Kind = builder,
                        Registration = registration,
                        UseUnsafeRegistration = useUnsafeRegistration,
                        StartWithControlledAwait = true
                    };
                    int count = tasks.Count;
                    stateMachine.Initialize();
                    stateMachine.Start();
                    Task task = stateMachine.GetTask();
                    Assert.False(task.IsCompleted);
                    Assert.Equal(completesDuringRegistration, tasks.ContainsKey(task));
                    Assert.Equal(count + (completesDuringRegistration ? 1 : 0), tasks.Count);
                    Assert.Equal(1, registration.ControlChecks);
                    registration.Resume();
                    Assert.Equal(TaskStatus.RanToCompletion, task.Status);
                }
            },
            configuration: this.GetConfiguration().WithTestingIterations(1));
        }

        [Theory(Timeout = 5000)]
        [MemberData(nameof(BuilderCases))]
        public void TestExceptionAndCancellationPreserveTaskIdentity(BuilderType builder, bool useUnsafeRegistration)
        {
            this.RunSystematicTest(() =>
            {
                var tasks = GetRegisteredTasks(CoyoteRuntime.Current);
                using var cancellation = new CancellationTokenSource();
                cancellation.Cancel();
                foreach (Exception error in new Exception[]
                {
                    new InvalidOperationException("Expected builder failure."),
                    new OperationCanceledException(cancellation.Token)
                })
                {
                    var registration = new AwaitRegistration
                    {
                        Completion = CompletionMode.DuringControlCheck,
                        Error = error
                    };
                    var stateMachine = new StateMachine
                    {
                        Kind = builder,
                        Registration = registration,
                        UseUnsafeRegistration = useUnsafeRegistration
                    };
                    int count = tasks.Count;
                    stateMachine.Initialize();
                    stateMachine.Start();
                    Task task = stateMachine.GetTask();
                    registration.Resume();
                    Assert.True(tasks.ContainsKey(task));
                    Assert.Equal(count + 1, tasks.Count);
                    Assert.Same(task, stateMachine.GetTask());
                    Assert.Equal(1, registration.Completions);
                    if (error is OperationCanceledException)
                    {
                        Assert.True(task.IsCanceled);
                        var exception = Assert.ThrowsAny<OperationCanceledException>(() => task.GetAwaiter().GetResult());
                        Assert.Equal(cancellation.Token, exception.CancellationToken);
                    }
                    else
                    {
                        Assert.True(task.IsFaulted);
                        Assert.Same(error, Assert.Single(task.Exception.InnerExceptions));
                    }
                }
            },
            configuration: this.GetConfiguration().WithTestingIterations(1));
        }

        [Theory(Timeout = 5000)]
        [MemberData(nameof(BuilderCases))]
        public void TestDefaultBuilderDoesNotRegisterTasks(BuilderType builder, bool useUnsafeRegistration)
        {
            this.RunSystematicTest(() =>
            {
                var tasks = GetRegisteredTasks(CoyoteRuntime.Current);
                int count = tasks.Count;
                var registration = new AwaitRegistration();
                var stateMachine = new StateMachine
                {
                    Kind = builder,
                    Registration = registration,
                    UseUnsafeRegistration = useUnsafeRegistration,
                    StartWithControlledAwait = true
                };
                stateMachine.Initialize(useDefaultBuilder: true);
                stateMachine.Start();
                Task task = stateMachine.GetTask();
                Assert.False(task.IsCompleted);
                Assert.False(tasks.ContainsKey(task));
                Assert.Equal(count, tasks.Count);
                Assert.Equal(0, registration.ControlChecks);
                registration.Resume();
                Assert.Equal(TaskStatus.RanToCompletion, task.Status);
            },
            configuration: this.GetConfiguration().WithTestingIterations(1));
        }

        [Theory(Timeout = 5000)]
        [MemberData(nameof(BuilderCases))]
        public void TestBuilderOutsideControlledContext(BuilderType builder, bool useUnsafeRegistration)
        {
            var registration = new AwaitRegistration();
            var stateMachine = new StateMachine
            {
                Kind = builder,
                Registration = registration,
                UseUnsafeRegistration = useUnsafeRegistration,
                StartWithControlledAwait = true
            };
            stateMachine.Initialize();
            stateMachine.Start();
            Task task = stateMachine.GetTask();
            Assert.False(task.IsCompleted);
            Assert.Equal(0, registration.ControlChecks);
            registration.Resume();
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
            Assert.Equal(1, registration.Completions);
        }
    }
}
