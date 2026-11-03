// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
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
#if NET10_0_OR_GREATER
            this.ValidateLockScopeInterfaces(type, new HashSet<TypeDefinition>());
            this.ValidateLockScopeBaseMethods(type);
#endif
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

            foreach (var property in type.Properties)
            {
                if (this.TryRewriteType(property.PropertyType, out TypeReference newPropertyType) &&
                    this.TryResolve(newPropertyType, out TypeDefinition _))
                {
                    this.LogWriter.LogDebug("............. [-] property '{0}'", property);
                    property.PropertyType = newPropertyType;
                    this.LogWriter.LogDebug("............. [+] property '{0}'", property);
                }

                this.RewriteParameters(property.Parameters);
            }

            foreach (var eventDefinition in type.Events)
            {
                if (this.TryRewriteType(eventDefinition.EventType, out TypeReference newEventType) &&
                    this.TryResolve(newEventType, out TypeDefinition _))
                {
                    this.LogWriter.LogDebug("............. [-] event '{0}'", eventDefinition);
                    eventDefinition.EventType = newEventType;
                    this.LogWriter.LogDebug("............. [+] event '{0}'", eventDefinition);
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

#if NET10_0_OR_GREATER
        /// <summary>
        /// Rejects native scope contracts that cannot follow the rewritten implementation signature.
        /// </summary>
        private void ValidateLockScopeInterfaces(TypeDefinition type, HashSet<TypeDefinition> visited)
        {
            if (!visited.Add(type))
            {
                return;
            }

            foreach (var implementedInterface in type.Interfaces)
            {
                if (!this.TryResolve(implementedInterface.InterfaceType, out TypeDefinition contract))
                {
                    continue;
                }

                if (this.IsExternalLockScopeContract(contract))
                {
                    foreach (var method in contract.Methods)
                    {
                        if (ContainsLockScope(method.ReturnType) ||
                            method.Parameters.Any(parameter => ContainsLockScope(parameter.ParameterType)))
                        {
                            throw new InvalidOperationException(
                                $"Cannot rewrite type '{this.TypeDef.FullName}' because it implements " +
                                $"'{method.FullName}' from an unrewritten interface. Rewriting Lock.Scope " +
                                "would change the interface contract. Include assembly " +
                                $"'{contract.Module.Assembly.Name.Name}' in rewriting, and ensure the " +
                                "contract type is not marked with SkipRewriting.");
                        }
                    }
                }

                this.ValidateLockScopeInterfaces(contract, visited);
            }
        }

        /// <summary>
        /// Rejects overrides whose native base signature cannot follow the rewritten signature.
        /// </summary>
        private void ValidateLockScopeBaseMethods(TypeDefinition type)
        {
            foreach (var method in type.Methods)
            {
                if (!ContainsLockScope(method.ReturnType) &&
                    !method.Parameters.Any(parameter => ContainsLockScope(parameter.ParameterType)))
                {
                    continue;
                }

                foreach (var methodOverride in method.Overrides)
                {
                    this.ValidateLockScopeBaseMethod(methodOverride);
                }

                if (!method.IsVirtual || method.IsNewSlot)
                {
                    continue;
                }

                var arguments = new Dictionary<GenericParameter, TypeReference>();
                TypeReference baseType = type.BaseType;
                while (baseType != null)
                {
                    TypeDefinition contract = baseType.Resolve();
                    if (baseType is GenericInstanceType instance)
                    {
                        for (int i = 0; i < instance.GenericArguments.Count; i++)
                        {
                            arguments[contract.GenericParameters[i]] = instance.GenericArguments[i];
                        }
                    }

                    var match = contract.Methods.FirstOrDefault(candidate =>
                        candidate.IsVirtual && !candidate.IsPrivate && candidate.Name == method.Name &&
                        candidate.GenericParameters.Count == method.GenericParameters.Count &&
                        candidate.Parameters.Count == method.Parameters.Count &&
                        this.GetContractTypeName(candidate.ReturnType, arguments) ==
                            this.GetContractTypeName(method.ReturnType, arguments) &&
                        candidate.Parameters.Select(parameter => this.GetContractTypeName(parameter.ParameterType, arguments))
                            .SequenceEqual(method.Parameters.Select(parameter =>
                                this.GetContractTypeName(parameter.ParameterType, arguments))));
                    if (match != null)
                    {
                        this.ValidateLockScopeBaseMethod(match);
                        if (match.IsNewSlot)
                        {
                            break;
                        }
                    }

                    baseType = contract.BaseType;
                }
            }
        }

        private void ValidateLockScopeBaseMethod(MethodReference method)
        {
            TypeDefinition declaringType = method.DeclaringType.Resolve();
            if (this.IsExternalLockScopeContract(declaringType) &&
                (ContainsLockScope(method.ReturnType) ||
                method.Parameters.Any(parameter => ContainsLockScope(parameter.ParameterType))))
            {
                throw new InvalidOperationException(
                    $"Cannot rewrite type '{this.TypeDef.FullName}' because it overrides " +
                    $"'{method.FullName}' from an unrewritten base type. Rewriting Lock.Scope " +
                    "would change the base method contract. Include assembly " +
                    $"'{declaringType.Module.Assembly.Name.Name}' in rewriting, and ensure the " +
                    "contract type is not marked with SkipRewriting.");
            }
        }

        /// <summary>
        /// Compares inherited signatures using their instantiated type arguments and rewritten scopes.
        /// </summary>
        private string GetContractTypeName(TypeReference type, Dictionary<GenericParameter, TypeReference> arguments)
        {
            if (type is GenericParameter parameter)
            {
                return arguments.TryGetValue(parameter, out TypeReference argument) ?
                    this.GetContractTypeName(argument, arguments) : $"{parameter.Type}:{parameter.Position}";
            }

            if (type is GenericInstanceType instance)
            {
                return instance.ElementType.FullName + "<" + string.Join(",",
                    instance.GenericArguments.Select(argument => this.GetContractTypeName(argument, arguments))) + ">";
            }

            if (type is TypeSpecification specification)
            {
                return specification.FullName.Replace(specification.ElementType.FullName,
                    this.GetContractTypeName(specification.ElementType, arguments));
            }

            return this.RewriteType(type, Options.None).FullName;
        }
#endif

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

            this.RewriteParameters(method.Parameters);
        }

        /// <summary>
        /// Rewrites method or indexer parameter types.
        /// </summary>
        private void RewriteParameters(IEnumerable<ParameterDefinition> parameters)
        {
            foreach (var parameter in parameters)
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
