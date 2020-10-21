// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Unique actor id.
    /// </summary>
    [DataContract]
    [DebuggerStepThrough]
    public sealed class ActorId : IEquatable<ActorId>, IComparable<ActorId>
    {
        /// <summary>
        /// The runtime that executes the actor with this id.
        /// </summary>
        public IActorRuntime Runtime { get; private set; }

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
        /// The type of the actor associated with this id.
        /// </summary>
        [DataMember]
        public readonly string Type;

        /// <summary>
        /// Name used for logging.
        /// </summary>
        [DataMember]
        public readonly string Name;

        /// <summary>
        /// True if <see cref="NameValue"/> is used as the unique id, else false.
        /// </summary>
        public bool IsNameUsedForHashing => this.NameValue.Length > 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorId"/> class.
        /// </summary>
        internal ActorId(Type type, ulong value, string name, ActorExecutionContext context, bool useNameForHashing = false)
        {
            this.Runtime = context;
            this.Type = type.FullName;
            this.Value = value;

            if (useNameForHashing)
            {
                this.NameValue = name;
                this.Runtime.Assert(!string.IsNullOrEmpty(this.NameValue), "The actor name cannot be null when used as id.");
                this.Name = this.NameValue;
            }
            else
            {
                this.NameValue = string.Empty;
                this.Runtime.Assert(this.Value != ulong.MaxValue, "Detected actor id overflow.");
                this.Name = string.Format(CultureInfo.InvariantCulture, "{0}({1})",
                    string.IsNullOrEmpty(name) ? this.Type : name, this.Value.ToString());
            }
        }

        /// <summary>
        /// Bind the actor id.
        /// </summary>
        internal void Bind(ActorExecutionContext context)
        {
            this.Runtime = context;
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

                return this.IsNameUsedForHashing ? this.NameValue.Equals(id.NameValue) : this.Value == id.Value;
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

        /// <summary>
        /// Indicates whether the specified <see cref="ActorId"/> is equal
        /// to the current <see cref="ActorId"/>.
        /// </summary>
        bool IEquatable<ActorId>.Equals(ActorId other) => this.Equals(other);

        /// <summary>
        /// Compares the specified <see cref="ActorId"/> with the current
        /// <see cref="ActorId"/> for ordering or sorting purposes.
        /// </summary>
        int IComparable<ActorId>.CompareTo(ActorId other) => string.Compare(this.Name, other?.Name);
    }
}
