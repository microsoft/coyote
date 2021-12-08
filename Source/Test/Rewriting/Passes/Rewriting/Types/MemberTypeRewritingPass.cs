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
                this.TryResolve(newFieldType, out TypeDefinition _))
            {
                Debug.WriteLine($"............. [-] field '{field}'");
                field.FieldType = newFieldType;
                Debug.WriteLine($"............. [+] field '{field}'");
            }
        }

        /// <inheritdoc/>
        protected internal override void VisitMethod(MethodDefinition method)
        {
            // TODO: guard against rewriting methods that have been inherited by non rewritten assemblies.
            // Try to rewrite the return type.
            if (this.TryRewriteType(method.ReturnType, out TypeReference newReturnType) &&
                this.TryResolve(newReturnType, out TypeDefinition _))
            {
                Debug.WriteLine($"............. [-] return type '{method.ReturnType}'");
                method.ReturnType = newReturnType;
                Debug.WriteLine($"............. [+] return type '{method.ReturnType}'");
            }

            if (method.HasParameters)
            {
                // Try to rewrite the parameter types.
                foreach (var parameter in method.Parameters)
                {
                    if (this.TryRewriteType(parameter.ParameterType, out TypeReference newParameterType) &&
                        this.TryResolve(newParameterType, out TypeDefinition _))
                    {
                        Debug.WriteLine($"............. [-] parameter '{parameter.ParameterType} {parameter.Name}'");
                        parameter.ParameterType = newParameterType;
                        Debug.WriteLine($"............. [+] parameter '{parameter.ParameterType} {parameter.Name}'");
                    }
                }
            }
        }
    }
}
