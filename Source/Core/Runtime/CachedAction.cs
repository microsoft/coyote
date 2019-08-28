// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Runtime
{
    internal class CachedAction
    {
        internal readonly MethodInfo MethodInfo;
        private readonly Action Action;
        private readonly Func<Task> TaskFunc;

        internal bool IsAsync => this.TaskFunc != null;

        internal CachedAction(MethodInfo methodInfo, Machine machine)
        {
            this.MethodInfo = methodInfo;

            // MethodInfo.Invoke catches the exception to wrap it in a TargetInvocationException.
            // This unwinds the stack before Machine.ExecuteAction's exception filter is invoked,
            // so call through the delegate instead (which is also much faster than Invoke).
            if (methodInfo.ReturnType == typeof(void))
            {
                this.Action = (Action)Delegate.CreateDelegate(typeof(Action), machine, methodInfo);
            }
            else
            {
                this.TaskFunc = (Func<Task>)Delegate.CreateDelegate(typeof(Func<Task>), machine, methodInfo);
            }
        }

        internal void Execute()
        {
            this.Action();
        }

        internal async Task ExecuteAsync()
        {
            await this.TaskFunc();
        }
    }
}
