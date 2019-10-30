// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Base class for Actor model objects.
    /// This type is intended for runtime use only.
    /// See <see cref="StateMachine"/>.
    /// </summary>
    [DebuggerStepThrough]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class Actor
    {
        /// <summary>
        /// The runtime that executes this machine.
        /// </summary>
        internal CoyoteRuntime Runtime { get; private set; }

        /// <summary>
        /// The unique actor id.
        /// </summary>
        protected internal ActorId Id { get; private set; }

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
        internal void Initialize(CoyoteRuntime runtime, ActorId id)
        {
            this.Runtime = runtime;
            this.Id = id;
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
            if (obj is Actor m &&
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
