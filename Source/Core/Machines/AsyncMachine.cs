// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Machines
{
    /// <summary>
    /// Implements a machine that can execute asynchronously.
    /// This type is intended for runtime use only.
    /// </summary>
    [DebuggerStepThrough]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class AsyncMachine
    {
        /// <summary>
        /// The runtime that executes this machine.
        /// </summary>
        internal CoyoteRuntime Runtime { get; private set; }

        /// <summary>
        /// The unique machine id.
        /// </summary>
        protected internal MachineId Id { get; private set; }

        /// <summary>
        /// Id used to identify subsequent operations performed by this machine.
        /// </summary>
        protected internal abstract Guid OperationGroupId { get; set; }

        /// <summary>
        /// The logger installed to the Coyote runtime.
        /// </summary>
        protected ILogger Logger => this.Runtime.Logger;

        /// <summary>
        /// Initializes this machine.
        /// </summary>
        internal void Initialize(CoyoteRuntime runtime, MachineId mid)
        {
            this.Runtime = runtime;
            this.Id = mid;
        }

        /// <summary>
        /// Returns the cached state of this machine.
        /// </summary>
        internal virtual int GetCachedState() => 0;

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is AsyncMachine m &&
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
        /// Returns a string that represents the current machine.
        /// </summary>
        public override string ToString()
        {
            return this.Id.Name;
        }
    }
}
