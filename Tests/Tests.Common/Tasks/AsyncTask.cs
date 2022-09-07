// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Runtime.CompilerServices;
using SystemCompiler = System.Runtime.CompilerServices;

namespace Microsoft.Coyote.Tests.Common.Tasks
{
    /// <summary>
    /// A controlled task like type.
    /// </summary>
    [SystemCompiler.AsyncMethodBuilder(typeof(AsyncTask.MethodBuilder))]
    public readonly struct AsyncTask
    {
        private readonly TaskAwaiter Awaiter;

        private AsyncTask(ref AsyncTaskMethodBuilder builder) =>
            this.Awaiter = new TaskAwaiter(builder.Task);

        public TaskAwaiter GetAwaiter() => this.Awaiter;

        public struct MethodBuilder
        {
            private AsyncTaskMethodBuilder Builder;

            public AsyncTask Task => new AsyncTask(ref this.Builder);

            private MethodBuilder(ref AsyncTaskMethodBuilder builder) =>
                this.Builder = builder;

            public static MethodBuilder Create()
            {
                var builder = AsyncTaskMethodBuilder.Create();
                return new MethodBuilder(ref builder);
            }

            public void Start<TStateMachine>(ref TStateMachine stateMachine)
                where TStateMachine : SystemCompiler.IAsyncStateMachine =>
                this.Builder.Start(ref stateMachine);

            public void SetStateMachine(SystemCompiler.IAsyncStateMachine stateMachine) =>
                this.Builder.SetStateMachine(stateMachine);

            public void SetResult() => this.Builder.SetResult();

            public void SetException(Exception exception) => this.Builder.SetException(exception);

            public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : SystemCompiler.INotifyCompletion
                where TStateMachine : SystemCompiler.IAsyncStateMachine =>
                this.Builder.AwaitOnCompleted(ref awaiter, ref stateMachine);

            public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : SystemCompiler.ICriticalNotifyCompletion
                where TStateMachine : SystemCompiler.IAsyncStateMachine =>
                this.Builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }
    }
}
