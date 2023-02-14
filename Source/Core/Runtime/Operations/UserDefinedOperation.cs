// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Represents a controlled operation that can be delayed during testing.
    /// </summary>
    internal class UserDefinedOperation : ControlledOperation
    {
        /// <summary>
        /// Optional id of the operation group that contains the operation being built.
        /// If multiple operations have the same group id, then the test engine can use
        /// this information to optimize exploration.
        /// </summary>
        internal Guid? GroupId { get; }

        /// <summary>
        /// Optional callback that returns the hashed state of the operation being built.
        /// If provided, it can be used by the test engine to optimize exploration.
        /// </summary>
        internal Func<ulong> HashedStateCallback { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserDefinedOperation"/> class.
        /// </summary>
        internal UserDefinedOperation(CoyoteRuntime runtime, IOperationBuilder builder, ulong operationId)
            : base(operationId, builder.Name, runtime)
        {
            this.HashedStateCallback = builder.HashedStateCallback;
        }

        /// <inheritdoc/>
        protected override ulong GetLatestHashedState(SchedulingPolicy policy) => this.HashedStateCallback?.Invoke() ?? 0;
    }
}
