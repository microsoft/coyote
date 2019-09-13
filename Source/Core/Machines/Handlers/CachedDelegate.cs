// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Coyote.Threading.Tasks;

namespace Microsoft.Coyote.Machines
{
    /// <summary>
    /// A machine delegate that has been cached for performance optimization.
    /// </summary>
    internal class CachedDelegate
    {
        internal readonly MethodInfo MethodInfo;
        internal readonly Delegate Handler;

        internal CachedDelegate(MethodInfo methodInfo, Machine machine)
        {
            this.MethodInfo = methodInfo;

            // MethodInfo.Invoke catches the exception to wrap it in a TargetInvocationException.
            // This unwinds the stack before Machine.ExecuteAction's exception filter is invoked,
            // so call through the delegate instead (which is also much faster than Invoke).
            if (methodInfo.ReturnType == typeof(void))
            {
                this.Handler = Delegate.CreateDelegate(typeof(Action), machine, methodInfo);
            }
            else if (methodInfo.ReturnType == typeof(ControlledTask))
            {
                this.Handler = Delegate.CreateDelegate(typeof(Func<ControlledTask>), machine, methodInfo);
            }
            else if (methodInfo.ReturnType == typeof(Task))
            {
                this.Handler = Delegate.CreateDelegate(typeof(Func<Task>), machine, methodInfo);
            }
            else
            {
                throw new InvalidOperationException($"Machine '{machine.Id}' is trying to cache invalid delegate '{methodInfo.Name}'.");
            }
        }
    }
}
