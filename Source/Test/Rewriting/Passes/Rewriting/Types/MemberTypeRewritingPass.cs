// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.Logging;
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
        internal MemberTypeRewritingPass(RewritingOptions options, IEnumerable<AssemblyInfo> visitedAssemblies, LogWriter logWriter)
            : base(options, visitedAssemblies, logWriter)
        {
        }

        /// <inheritdoc/>
        protected internal override void VisitType(TypeDefinition type)
        {
            base.VisitType(type);
            this.RewriteGenericConstraints(type.GenericParameters);
            if (type.BaseType != null &&
                this.TryRewriteType(type.BaseType, out TypeReference newBaseType) &&
                this.TryResolve(newBaseType, out TypeDefinition _))
            {
                this.LogWriter.LogDebug("............. [-] base type '{0}'", type.BaseType);
                type.BaseType = newBaseType;
                this.LogWriter.LogDebug("............. [+] base type '{0}'", type.BaseType);
            }

            foreach (var implementedInterface in type.Interfaces)
            {
                if (this.TryRewriteType(implementedInterface.InterfaceType, out TypeReference newInterfaceType) &&
                    this.TryResolve(newInterfaceType, out TypeDefinition _))
                {
                    this.LogWriter.LogDebug("............. [-] interface '{0}'", implementedInterface.InterfaceType);
                    implementedInterface.InterfaceType = newInterfaceType;
                    this.LogWriter.LogDebug("............. [+] interface '{0}'", implementedInterface.InterfaceType);
                }
            }
        }

        /// <inheritdoc/>
        protected internal override void VisitField(FieldDefinition field)
        {
            if (this.TryRewriteType(field.FieldType, out TypeReference newFieldType) &&
                this.TryResolve(newFieldType, out TypeDefinition _))
            {
                this.LogWriter.LogDebug("............. [-] field '{0}'", field);
                field.FieldType = newFieldType;
                this.LogWriter.LogDebug("............. [+] field '{0}'", field);
            }
        }

        /// <inheritdoc/>
        protected internal override void VisitMethod(MethodDefinition method)
        {
            // TODO: guard against rewriting methods that have been inherited by non rewritten assemblies.
            this.RewriteMethodSignature(method);
            this.RewriteGenericConstraints(method.GenericParameters);
            foreach (var methodOverride in method.Overrides)
            {
                if (this.TryRewriteType(methodOverride.DeclaringType, out TypeReference newDeclaringType) &&
                    this.TryResolve(newDeclaringType, out TypeDefinition _))
                {
                    this.LogWriter.LogDebug("............. [-] override '{0}'", methodOverride);
                    methodOverride.DeclaringType = newDeclaringType;
                    this.LogWriter.LogDebug("............. [+] override '{0}'", methodOverride);
                }

                this.RewriteMethodSignature(methodOverride);
            }
        }

        /// <summary>
        /// Rewrites the return and parameter types of a method declaration.
        /// </summary>
        private void RewriteMethodSignature(MethodReference method)
        {
            // Try to rewrite the return type.
            if (this.TryRewriteType(method.ReturnType, out TypeReference newReturnType) &&
                this.TryResolve(newReturnType, out TypeDefinition _))
            {
                this.LogWriter.LogDebug("............. [-] return type '{0}'", method.ReturnType);
                method.ReturnType = newReturnType;
                this.LogWriter.LogDebug("............. [+] return type '{0}'", method.ReturnType);
            }

            if (method.HasParameters)
            {
                // Try to rewrite the parameter types.
                foreach (var parameter in method.Parameters)
                {
                    if (this.TryRewriteType(parameter.ParameterType, out TypeReference newParameterType) &&
                        this.TryResolve(newParameterType, out TypeDefinition _))
                    {
                        this.LogWriter.LogDebug("............. [-] parameter '{0} {1}'", parameter.ParameterType, parameter.Name);
                        parameter.ParameterType = newParameterType;
                        this.LogWriter.LogDebug("............. [+] parameter '{0} {1}'", parameter.ParameterType, parameter.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Rewrites constraints on type or method generic parameters.
        /// </summary>
        private void RewriteGenericConstraints(IEnumerable<GenericParameter> parameters)
        {
            foreach (var parameter in parameters)
            {
                foreach (var constraint in parameter.Constraints)
                {
                    if (this.TryRewriteType(constraint.ConstraintType, out TypeReference newConstraintType) &&
                        this.TryResolve(newConstraintType, out TypeDefinition _))
                    {
                        this.LogWriter.LogDebug("............. [-] constraint '{0}'", constraint.ConstraintType);
                        constraint.ConstraintType = newConstraintType;
                        this.LogWriter.LogDebug("............. [+] constraint '{0}'", constraint.ConstraintType);
                    }
                }
            }
        }
    }
}
