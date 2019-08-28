// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.Coyote.TestingServices.StateCaching
{
    /// <summary>
    /// Class implementing a program state fingerprint.
    /// </summary>
    internal sealed class Fingerprint
    {
        /// <summary>
        /// The hash value of the fingerprint.
        /// </summary>
        private readonly int HashValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="Fingerprint"/> class.
        /// </summary>
        internal Fingerprint(int hash)
        {
            this.HashValue = hash;
        }

        /// <summary>
        /// Returns true if the fingerprint is equal to
        /// the given object.
        /// </summary>
        public override bool Equals(object obj)
        {
            var fingerprint = obj as Fingerprint;
            return fingerprint != null && this.HashValue == fingerprint.HashValue;
        }

        /// <summary>
        /// Returns the hashcode of the fingerprint.
        /// </summary>
        public override int GetHashCode() => this.HashValue;

        /// <summary>
        /// Returns a string representation of the fingerprint.
        /// </summary>
        public override string ToString() => $"fingerprint['{this.HashValue}']";
    }
}
