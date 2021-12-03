// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.IO;
using Mono.Cecil;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// A pass that rewrites types.
    /// </summary>
    internal sealed class MemberTypeRewritingPass : TypeRewritingPass
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemberTypeRewritingPass"/> class.
        /// </summary>
        internal MemberTypeRewritingPass(RewritingOptions options, IEnumerable<AssemblyInfo> visitedAssemblies, ILogger logger)
            : base(options, visitedAssemblies, logger)
        {
        }

        /// <inheritdoc/>
        protected internal override void VisitField(FieldDefinition field)
        {
            if (this.TryRewriteType(field.FieldType, out TypeReference newFieldType) &&
                this.TryResolve(newFieldType, out TypeDefinition newFieldDefinition) &&
                !IsStaticType(newFieldDefinition))
            {
                Debug.WriteLine($"............. [-] field '{field}'");
                field.FieldType = newFieldType;
                Debug.WriteLine($"............. [+] field '{field}'");
            }
        }

        /// <inheritdoc/>
        protected internal override void VisitMethod(MethodDefinition method)
        {
            // TODO: rewrite method parameters.

            // TODO: what if this is an override of an inherited virtual method? For example, what if there
            // is an external base class that is a Task like type that implements a virtual GetAwaiter() that
            // is overridden by this method?
            // I don't think we can really support task-like types, as their semantics can be arbitrary,
            // but good to understand what that entails and give warnings/errors perhaps: work item #4678.
            if (this.TryRewriteType(method.ReturnType, out TypeReference newReturnType) &&
                this.TryResolve(newReturnType, out TypeDefinition newReturnDefinition) &&
                !IsStaticType(newReturnDefinition))
            {
                Debug.WriteLine($"............. [-] return type '{method.ReturnType}'");
                method.ReturnType = newReturnType;
                Debug.WriteLine($"............. [+] return type '{method.ReturnType}'");
            }
        }
    }
}
