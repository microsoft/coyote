// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Runtime.Versioning;
using Mono.Cecil;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Validates that the .NET runtime of the running Coyote host is compatible with the runtime
    /// targeted by an assembly that Coyote is about to test or rewrite.
    /// </summary>
    internal static class TargetRuntimeValidator
    {
        /// <summary>
        /// The full name of the attribute declaring the target framework of an assembly.
        /// </summary>
        private const string TargetFrameworkAttributeName = "System.Runtime.Versioning.TargetFrameworkAttribute";

        /// <summary>
        /// The framework identifier used by assemblies that target .NET (Core).
        /// </summary>
        private const string CoreFrameworkIdentifier = ".NETCoreApp";

        /// <summary>
        /// The .NET version of the running Coyote host, or null if the host is not running on .NET.
        /// </summary>
        private static readonly Version HostVersion = GetHostVersion();

        /// <summary>
        /// Validates that the running Coyote host can load the specified assembly for testing.
        /// </summary>
        /// <remarks>
        /// A host can load an assembly that targets an older .NET version, because the runtime rolls
        /// forward, but it cannot load an assembly that targets a newer .NET version.
        /// </remarks>
        internal static void ValidateTestingTarget(string assemblyPath)
        {
            using (AssemblyDefinition definition = AssemblyDefinition.ReadAssembly(assemblyPath))
            {
                ValidateTestingTarget(assemblyPath, definition);
            }
        }

        /// <summary>
        /// Validates that the running Coyote host can load the specified assembly for testing.
        /// </summary>
        internal static void ValidateTestingTarget(string assemblyPath, AssemblyDefinition definition)
        {
            FrameworkName framework = GetCoreTargetFramework(definition);
            if (framework is null)
            {
                return;
            }

            if (framework.Version > HostVersion)
            {
                throw new InvalidOperationException(
                    $"The Coyote host is running on .NET {HostVersion}, but test assembly " +
                    $"'{assemblyPath}' requires {framework.Identifier},Version=v{framework.Version}. " +
                    $"Run the net{framework.Version.Major}.0 Coyote host for this assembly.");
            }
        }

        /// <summary>
        /// Validates that the running Coyote host can rewrite the specified assembly.
        /// </summary>
        /// <remarks>
        /// Rewriting resolves the replacement types of the running Coyote host, so the rewritten
        /// assembly ends up referencing the runtime of that host. This is only safe when the host
        /// and the assembly target the same .NET major version.
        /// </remarks>
        internal static void ValidateRewritingTarget(string assemblyPath)
        {
            using (AssemblyDefinition definition = AssemblyDefinition.ReadAssembly(assemblyPath))
            {
                ValidateRewritingTarget(assemblyPath, definition);
            }
        }

        /// <summary>
        /// Validates that the running Coyote host can rewrite the specified assembly.
        /// </summary>
        internal static void ValidateRewritingTarget(string assemblyPath, AssemblyDefinition definition)
        {
            FrameworkName framework = GetCoreTargetFramework(definition);
            if (framework is null)
            {
                return;
            }

            if (framework.Version > HostVersion)
            {
                throw new InvalidOperationException(
                    $"The Coyote host is running on .NET {HostVersion}, but assembly '{assemblyPath}' " +
                    $"targets {framework.Identifier},Version=v{framework.Version}, which this host cannot " +
                    $"load. Run the net{framework.Version.Major}.0 Coyote host to rewrite this assembly.");
            }
            else if (framework.Version.Major < HostVersion.Major)
            {
                throw new InvalidOperationException(
                    $"The Coyote host is running on .NET {HostVersion}, but assembly '{assemblyPath}' " +
                    $"targets {framework.Identifier},Version=v{framework.Version}. Rewriting it with this " +
                    $"host would inject .NET {HostVersion} runtime references and the rewritten assembly " +
                    $"would fail to load on .NET {framework.Version}. Run the net{framework.Version.Major}.0 " +
                    "Coyote host to rewrite this assembly.");
            }
        }

        /// <summary>
        /// Returns the .NET target framework of the specified assembly, or null if the assembly does
        /// not declare a parsable .NET target framework, or if the host runtime is unknown.
        /// </summary>
        private static FrameworkName GetCoreTargetFramework(AssemblyDefinition definition)
        {
            if (HostVersion is null)
            {
                return null;
            }

            CustomAttribute attribute = definition.CustomAttributes.FirstOrDefault(
                candidate => candidate.AttributeType.FullName == TargetFrameworkAttributeName);
            if (attribute is null || attribute.ConstructorArguments.Count != 1 ||
                !(attribute.ConstructorArguments[0].Value is string frameworkName))
            {
                return null;
            }

            FrameworkName framework;
            try
            {
                framework = new FrameworkName(frameworkName);
            }
            catch (ArgumentException)
            {
                return null;
            }

            return framework.Identifier == CoreFrameworkIdentifier ? framework : null;
        }

        /// <summary>
        /// Returns the .NET version of the running Coyote host, or null if the host is not running on .NET.
        /// </summary>
        private static Version GetHostVersion()
        {
#if NET
            return new Version(Environment.Version.Major, Environment.Version.Minor);
#else
            return null;
#endif
        }
    }
}
