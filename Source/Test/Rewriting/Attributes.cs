// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// Attribute that defines the version of Coyote used for rewriting an assembly.
    /// </summary>
    /// <remarks>
    /// If this attribute is applied to an assembly manifest, it denotes that the
    /// assembly has been rewritten.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class CoyoteVersionAttribute : Attribute
    {
        /// <summary>
        /// The version of Coyote used for the rewriting.
        /// </summary>
        public readonly string Version;

        /// <summary>
        /// Unique identifier applied to all dependent rewritten assemblies.
        /// </summary>
        public readonly string Identifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoyoteVersionAttribute"/> class.
        /// </summary>
        public CoyoteVersionAttribute(string version, string identifier)
        {
            this.Version = version;
            this.Identifier = identifier;
        }
    }
}
