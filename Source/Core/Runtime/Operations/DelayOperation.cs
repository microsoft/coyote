// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Represents a controlled operation that can be delayed during testing.
    /// </summary>
    internal class DelayOperation : ControlledOperation
    {
        /// <summary>
        /// The value until the operation may resume executing.
        /// </summary>
        internal int Delay;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayOperation"/> class.
        /// </summary>
        internal DelayOperation(ulong operationId, string name, uint delay, CoyoteRuntime runtime)
            : base(operationId, name, runtime)
        {
            this.Delay = delay > int.MaxValue ? int.MaxValue : (int)delay;
        }
    }
}
