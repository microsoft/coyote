// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET10_0_OR_GREATER
using System;
using System.Threading;

namespace Microsoft.Coyote.Tests.Common.Threading
{
    public sealed class LockScopeProvider
    {
        public static Lock.Scope Enter(Lock gate, Action onEnter)
        {
            onEnter();
            return gate.EnterScope();
        }

#pragma warning disable CA1822 // Exercise an unrewritten instance call.
        public Lock.Scope EnterInstance(Lock gate, Action onEnter) => Enter(gate, onEnter);
#pragma warning restore CA1822
    }
}
#endif
