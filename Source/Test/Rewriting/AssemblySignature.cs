// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// Signature identifying the parameters used during binary rewriting of an assembly.
    /// </summary>
    [DataContract]
    internal class AssemblySignature
    {
        /// <summary>
        /// The full name of the assembly.
        /// </summary>
        [DataMember]
        internal readonly string FullName;

        /// <summary>
        /// The version of the binary rewriter.
        /// </summary>
        [DataMember]
        internal readonly string Version;

        /// <summary>
        /// The assembly direct dependencies.
        /// </summary>
        [DataMember]
        internal readonly IList<string> Dependencies;

        /// <summary>
        /// True if rewriting for memory locations is enabled, else false.
        /// </summary>
        [DataMember]
        internal readonly bool IsRewritingMemoryLocations;

        /// <summary>
        /// True if rewriting for concurrent collections is enabled, else false.
        /// </summary>
        [DataMember]
        internal readonly bool IsRewritingConcurrentCollections;

        /// <summary>
        /// True if rewriting for data race checking is enabled, else false.
        /// </summary>
        [DataMember]
        internal readonly bool IsDataRaceCheckingEnabled;

        /// <summary>
        /// True if rewriting dependent assemblies that are found in the same location is enabled, else false.
        /// </summary>
        [DataMember]
        internal readonly bool IsRewritingDependencies;

        /// <summary>
        /// True if rewriting of unit test methods is enabled, else false.
        /// </summary>
        [DataMember]
        internal readonly bool IsRewritingUnitTests;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblySignature"/> class.
        /// </summary>
        internal AssemblySignature(AssemblyInfo assembly, HashSet<AssemblyInfo> dependencies,
            Version rewriterVersion, RewritingOptions options)
        {
            this.FullName = assembly.FullName;
            this.Version = rewriterVersion.ToString();
            this.Dependencies = new List<string>(dependencies.Select(dependency => dependency.FullName));
            this.IsRewritingMemoryLocations = options.IsRewritingMemoryLocations;
            this.IsRewritingConcurrentCollections = options.IsRewritingConcurrentCollections;
            this.IsDataRaceCheckingEnabled = options.IsDataRaceCheckingEnabled;
            this.IsRewritingDependencies = options.IsRewritingDependencies;
            this.IsRewritingUnitTests = options.IsRewritingUnitTests;
        }

        /// <summary>
        /// Computes the hash of the signature.
        /// </summary>
        internal string ComputeHash()
        {
            using var stream = new MemoryStream();
            var serializer = new DataContractJsonSerializer(typeof(AssemblySignature));
            serializer.WriteObject(stream, this);
            var data = stream.GetBuffer();

            // Compute the SHA256 hash.
            using (SHA256 sha256Hash = SHA256.Create())
            {
                data = sha256Hash.ComputeHash(data);
            }

            // Format each byte of the hashed data as a hexadecimal string.
            var sb = new StringBuilder();
            foreach (var b in data)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}
