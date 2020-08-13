// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// Attribute for checking if an assembly has been rewritten by Coyote. If this attribute
    /// is applied to an assembly, it denotes that the assembly has been rewritten.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class IsAssemblyRewrittenAttribute : Attribute
    {
        /// <summary>
        /// The version of Coyote used for the rewritting.
        /// </summary>
        public readonly string Version;

        /// <summary>
        /// Initializes a new instance of the <see cref="IsAssemblyRewrittenAttribute"/> class.
        /// </summary>
        public IsAssemblyRewrittenAttribute(string version)
        {
            this.Version = version;
        }
    }
}
