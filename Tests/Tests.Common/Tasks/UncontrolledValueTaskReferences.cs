// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET
using System.Threading.Tasks;

namespace Microsoft.Coyote.Tests.Common.Tasks
{
    public class UncontrolledValueTaskReferences<T>
    {
        private ValueTask Task;
        private ValueTask<T> GenericTask;

        private static class Holder<TResult>
        {
            internal static ValueTask<TResult> Task;
        }

        public ref ValueTask GetTask() => ref this.Task;

        public ref ValueTask<T> GetGenericTask() => ref this.GenericTask;

        public ref readonly ValueTask<T> GetReadOnlyTask() => ref this.GenericTask;

        public ref ValueTask<TResult> GetMethodTask<TResult>() => ref Holder<TResult>.Task;

        public ValueTask GetTaskValue() => this.Task;

        public ValueTask<T> GetGenericTaskValue() => this.GenericTask;

        public void SetGenericTask(ValueTask<T> task) => this.GenericTask = task;

        public void SetPendingTasks()
        {
            this.Task = new ValueTask(new TaskCompletionSource<bool>().Task);
            this.GenericTask = new ValueTask<T>(new TaskCompletionSource<T>().Task);
        }
    }
}
#endif
