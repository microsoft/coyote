// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Interface of a controlled operation builder.
    /// </summary>
    public interface IOperationBuilder
    {
        /// <summary>
        /// The name of the operation being built.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Optional id of the operation group that contains the operation being built.
        /// If multiple operations have the same group id, then the test engine can use
        /// this information to optimize exploration.
        /// </summary>
        public Guid? GroupId { get; }

        /// <summary>
        /// Optional callback that returns the hashed state of the operation being built.
        /// If provided, it can be used by the test engine to optimize exploration.
        /// </summary>
        public Func<int> HashedStateCallback { get; }
    }
}
