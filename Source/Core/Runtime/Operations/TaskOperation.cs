// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Represents a task operation that can be controlled during systematic testing.
    /// </summary>
    internal class TaskOperation : ControlledOperation
    {
        /// <summary>
        /// Set of tasks that this operation is waiting to join. All tasks
        /// in the set must complete before this operation can resume.
        /// </summary>
        internal readonly HashSet<Task> JoinDependencies;

        /// <summary>
        /// The value until the operation may complete.
        /// </summary>
        internal int Timeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskOperation"/> class.
        /// </summary>
        internal TaskOperation(ulong operationId, string name, uint delay)
            : base(operationId, name)
        {
            this.JoinDependencies = new HashSet<Task>();
            this.Timeout = delay > int.MaxValue ? int.MaxValue : (int)delay;
        }
    }
}
