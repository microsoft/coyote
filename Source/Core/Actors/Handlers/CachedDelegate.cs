// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// An actor delegate that has been cached to optimize performance of invocations.
    /// </summary>
    internal class CachedDelegate
    {
        internal readonly MethodInfo MethodInfo;
        internal readonly Delegate Handler;

        internal CachedDelegate(MethodInfo methodInfo, Actor actor)
        {
            this.MethodInfo = methodInfo;

            // MethodInfo.Invoke catches the exception to wrap it in a TargetInvocationException.
            // This unwinds the stack before StateMachine.ExecuteAction's exception filter is invoked,
            // so call through the delegate instead (which is also much faster than Invoke).
            if (methodInfo.ReturnType == typeof(void))
            {
                this.Handler = Delegate.CreateDelegate(typeof(Action), actor, methodInfo);
            }
            else if (methodInfo.ReturnType == typeof(Task))
            {
                this.Handler = Delegate.CreateDelegate(typeof(Func<Task>), actor, methodInfo);
            }
            else
            {
                throw new InvalidOperationException($"'{actor.Id}' is trying to cache invalid delegate '{methodInfo.Name}'.");
            }
        }
    }
}
