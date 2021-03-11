// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Options for executing an operation.
    /// </summary>
    [Flags]
    internal enum OperationExecutionOptions
    {
        None = 0b_0000,
        FailOnException = 0b_0001,
        YieldAtStart = 0b_0010
    }
}
