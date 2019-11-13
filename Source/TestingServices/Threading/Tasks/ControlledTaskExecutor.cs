// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.TestingServices.Runtime;
using Microsoft.Coyote.Threading.Tasks;

namespace Microsoft.Coyote.TestingServices.Threading.Tasks
{
    /// <summary>
    /// Provides the capability to control and execute a <see cref="ControlledTask"/> during testing.
    /// </summary>
    internal abstract class ControlledTaskExecutor
    {
        /// <summary>
        /// The testing runtime that executes this task.
        /// </summary>
        internal SystematicTestingRuntime Runtime { get; private set; }

        /// <summary>
        /// An actor id that can uniquely identify this task.
        /// </summary>
        protected internal ActorId Id { get; private set; }

        /// <summary>
        /// The id of the task that provides access to the completed work.
        /// </summary>
        internal abstract int AwaiterTaskId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledTaskExecutor"/> class.
        /// </summary>
        [DebuggerStepThrough]
        internal ControlledTaskExecutor(SystematicTestingRuntime runtime)
        {
            this.Runtime = runtime;
            this.Id = new ActorId(this.GetType(), "ControlledTask", runtime);
        }

        /// <summary>
        /// Executes the work asynchronously.
        /// </summary>
        internal abstract Task ExecuteAsync();

        /// <summary>
        /// Tries to complete the work with the specified exception.
        /// </summary>
        internal abstract void TryCompleteWithException(Exception exception);

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is ControlledTaskExecutor m &&
                this.GetType() == m.GetType())
            {
                return this.Id.Value == m.Id.Value;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            return this.Id.Value.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current executor.
        /// </summary>
        public override string ToString()
        {
            return this.Id.Name;
        }
    }
}
