// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using System.Threading;

using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Machines
{
    /// <summary>
    /// Unique actor id.
    /// </summary>
    [DataContract]
    [DebuggerStepThrough]
    public sealed class ActorId : IEquatable<ActorId>, IComparable<ActorId>
    {
        /// <summary>
        /// The runtime that executes the machine with this id.
        /// </summary>
        public IMachineRuntime Runtime { get; private set; }

        /// <summary>
        /// Unique id, when <see cref="NameValue"/> is empty.
        /// </summary>
        [DataMember]
        public readonly ulong Value;

        /// <summary>
        /// Unique id, when non-empty.
        /// </summary>
        [DataMember]
        public readonly string NameValue;

        /// <summary>
        /// Type of the machine with this id.
        /// </summary>
        [DataMember]
        public readonly string Type;

        /// <summary>
        /// Name of the machine used for logging.
        /// </summary>
        [DataMember]
        public readonly string Name;

        /// <summary>
        /// Generation of the runtime that created this actor id.
        /// </summary>
        [DataMember]
        public readonly ulong Generation;

        /// <summary>
        /// Endpoint.
        /// </summary>
        [DataMember]
        public readonly string Endpoint;

        /// <summary>
        /// True if <see cref="NameValue"/> is used as the unique id, else false.
        /// </summary>
        public bool IsNameUsedForHashing => this.NameValue.Length > 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorId"/> class.
        /// </summary>
        internal ActorId(Type type, string machineName, CoyoteRuntime runtime, bool useNameForHashing = false)
        {
            this.Runtime = runtime;
            this.Endpoint = string.Empty;

            if (useNameForHashing)
            {
                this.Value = 0;
                this.NameValue = machineName;
                this.Runtime.Assert(!string.IsNullOrEmpty(this.NameValue), "Input machine name cannot be null when used as id.");
            }
            else
            {
                // Atomically increments and safely wraps into an unsigned long.
                this.Value = (ulong)Interlocked.Increment(ref runtime.ActorIdCounter) - 1;
                this.NameValue = string.Empty;

                // Checks for overflow.
                this.Runtime.Assert(this.Value != ulong.MaxValue, "Detected actor id overflow.");
            }

            this.Generation = runtime.Configuration.RuntimeGeneration;

            this.Type = type.FullName;
            if (this.IsNameUsedForHashing)
            {
                this.Name = this.NameValue;
            }
            else
            {
                this.Name = string.Format(CultureInfo.InvariantCulture, "{0}({1})",
                    string.IsNullOrEmpty(machineName) ? this.Type : machineName, this.Value.ToString());
            }
        }

        /// <summary>
        /// Bind the actor id.
        /// </summary>
        internal void Bind(CoyoteRuntime runtime)
        {
            this.Runtime = runtime;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is ActorId id)
            {
                // Use same machanism for hashing.
                if (this.IsNameUsedForHashing != id.IsNameUsedForHashing)
                {
                    return false;
                }

                return this.IsNameUsedForHashing ?
                    this.NameValue.Equals(id.NameValue) && this.Generation == id.Generation :
                    this.Value == id.Value && this.Generation == id.Generation;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = (hash * 23) + (this.IsNameUsedForHashing ? this.NameValue.GetHashCode() : this.Value.GetHashCode());
            hash = (hash * 23) + this.Generation.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a string that represents the current actor id.
        /// </summary>
        public override string ToString() => this.Name;

        /// <summary>
        /// Indicates whether the specified <see cref="ActorId"/> is equal
        /// to the current <see cref="ActorId"/>.
        /// </summary>
        public bool Equals(ActorId other) => this.Equals((object)other);

        /// <summary>
        /// Compares the specified <see cref="ActorId"/> with the current
        /// <see cref="ActorId"/> for ordering or sorting purposes.
        /// </summary>
        public int CompareTo(ActorId other) => string.Compare(this.Name, other?.Name);

        bool IEquatable<ActorId>.Equals(ActorId other) => this.Equals(other);

        int IComparable<ActorId>.CompareTo(ActorId other) => string.Compare(this.Name, other?.Name);
    }
}
