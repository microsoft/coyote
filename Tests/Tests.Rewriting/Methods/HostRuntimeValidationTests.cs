// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET
using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Runtime;
using Mono.Cecil;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class HostRuntimeValidationTests : BaseRewritingTest
    {
        /// <summary>
        /// The assembly used as the source of every synthesized rewriting target. It is small and
        /// it targets the same framework as the running host.
        /// </summary>
        private const string RewritingSourceAssembly = "Microsoft.Coyote.Tests.Rewriting.Helpers.dll";

        public HostRuntimeValidationTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestMatchingTargetFrameworkMetadataIsAccepted()
        {
            string assemblyPath = typeof(HostRuntimeValidationTests).Assembly.Location;
            TargetRuntimeValidator.ValidateTestingTarget(assemblyPath);
            TargetRuntimeValidator.ValidateRewritingTarget(assemblyPath);
        }

        [Fact(Timeout = 5000)]
        public void TestMissingTargetFrameworkMetadataIsAccepted()
        {
            string assemblyPath = CreateAssemblyWithTargetFramework(null);
            try
            {
                TargetRuntimeValidator.ValidateTestingTarget(assemblyPath);
                TargetRuntimeValidator.ValidateRewritingTarget(assemblyPath);
            }
            finally
            {
                File.Delete(assemblyPath);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMalformedTargetFrameworkMetadataIsAccepted()
        {
            string assemblyPath = CreateAssemblyWithTargetFramework("not-a-framework");
            try
            {
                TargetRuntimeValidator.ValidateTestingTarget(assemblyPath);
                TargetRuntimeValidator.ValidateRewritingTarget(assemblyPath);
            }
            finally
            {
                File.Delete(assemblyPath);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestNonCoreTargetFrameworkMetadataIsAccepted()
        {
            string assemblyPath = CreateAssemblyWithTargetFramework(".NETStandard,Version=v2.0");
            try
            {
                TargetRuntimeValidator.ValidateTestingTarget(assemblyPath);
                TargetRuntimeValidator.ValidateRewritingTarget(assemblyPath);
            }
            finally
            {
                File.Delete(assemblyPath);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestNewerTargetFrameworkHasAnActionableDiagnostic()
        {
            int targetMajorVersion = Environment.Version.Major + 1;
            string targetFramework = $".NETCoreApp,Version=v{targetMajorVersion}.0";
            string assemblyPath = CreateAssemblyWithTargetFramework(targetFramework);
            try
            {
                var exception = Assert.Throws<InvalidOperationException>(() =>
                    TargetRuntimeValidator.ValidateTestingTarget(assemblyPath));

                Assert.Contains($"The Coyote host is running on .NET {Environment.Version.Major}.{Environment.Version.Minor}",
                    exception.Message);
                Assert.Contains($"requires {targetFramework}", exception.Message);
                Assert.Contains($"Run the net{targetMajorVersion}.0 Coyote host", exception.Message);
            }
            finally
            {
                File.Delete(assemblyPath);
            }
        }

        [Theory(Timeout = 5000)]
        [InlineData(1)]
        [InlineData(2)]
        public void TestOlderTargetFrameworkIsAcceptedWhenTesting(int majorVersionOffset)
        {
            // The .NET runtime rolls forward, so a newer host can load an older target assembly. For
            // example, the net10.0 host must still be able to test a net8.0 assembly.
            int targetMajorVersion = Environment.Version.Major - majorVersionOffset;
            string assemblyPath = CreateAssemblyWithTargetFramework($".NETCoreApp,Version=v{targetMajorVersion}.0");
            try
            {
                TargetRuntimeValidator.ValidateTestingTarget(assemblyPath);
            }
            finally
            {
                File.Delete(assemblyPath);
            }
        }

        [Theory(Timeout = 5000)]
        [InlineData(1)]
        [InlineData(2)]
        public void TestNewerTargetFrameworkIsRejectedWhenRewriting(int majorVersionOffset)
        {
            // Covers the net8.0 host rejecting a net10.0 target assembly.
            int targetMajorVersion = Environment.Version.Major + majorVersionOffset;
            string targetFramework = $".NETCoreApp,Version=v{targetMajorVersion}.0";
            string assemblyPath = CreateAssemblyWithTargetFramework(targetFramework);
            try
            {
                var exception = Assert.Throws<InvalidOperationException>(() =>
                    TargetRuntimeValidator.ValidateRewritingTarget(assemblyPath));

                Assert.Contains($"The Coyote host is running on .NET {Environment.Version.Major}.{Environment.Version.Minor}",
                    exception.Message);
                Assert.Contains($"targets {targetFramework}", exception.Message);
                Assert.Contains($"Run the net{targetMajorVersion}.0 Coyote host to rewrite this assembly",
                    exception.Message);
            }
            finally
            {
                File.Delete(assemblyPath);
            }
        }

        [Theory(Timeout = 5000)]
        [InlineData(1)]
        [InlineData(2)]
        public void TestOlderTargetFrameworkIsRejectedWhenRewriting(int majorVersionOffset)
        {
            // Covers the net10.0 host rejecting net9.0 and net8.0 target assemblies.
            int targetMajorVersion = Environment.Version.Major - majorVersionOffset;
            string targetFramework = $".NETCoreApp,Version=v{targetMajorVersion}.0";
            string assemblyPath = CreateAssemblyWithTargetFramework(targetFramework);
            try
            {
                var exception = Assert.Throws<InvalidOperationException>(() =>
                    TargetRuntimeValidator.ValidateRewritingTarget(assemblyPath));

                Assert.Contains($"The Coyote host is running on .NET {Environment.Version.Major}.{Environment.Version.Minor}",
                    exception.Message);
                Assert.Contains($"targets {targetFramework}", exception.Message);
                Assert.Contains(
                    $"would inject .NET {Environment.Version.Major}.{Environment.Version.Minor} runtime references",
                    exception.Message);
                Assert.Contains($"Run the net{targetMajorVersion}.0 Coyote host to rewrite this assembly",
                    exception.Message);
            }
            finally
            {
                File.Delete(assemblyPath);
            }
        }

        [Theory(Timeout = 10000)]
        [InlineData(1)]
        [InlineData(2)]
        public void TestRewritingOlderTargetFrameworkDoesNotModifyAssembly(int majorVersionOffset)
        {
            // Covers the net10.0 host refusing to rewrite net9.0 and net8.0 target assemblies. Before
            // this validation existed, rewriting reported success but injected .NET 10 references that
            // made the target unloadable on its own runtime.
            int targetMajorVersion = Environment.Version.Major - majorVersionOffset;
            this.AssertRewritingIsRejectedWithoutModifyingAssembly($".NETCoreApp,Version=v{targetMajorVersion}.0");
        }

        [Theory(Timeout = 10000)]
        [InlineData(1)]
        [InlineData(2)]
        public void TestRewritingNewerTargetFrameworkDoesNotModifyAssembly(int majorVersionOffset)
        {
            // Covers the net8.0 host refusing to rewrite a net10.0 target assembly.
            int targetMajorVersion = Environment.Version.Major + majorVersionOffset;
            this.AssertRewritingIsRejectedWithoutModifyingAssembly($".NETCoreApp,Version=v{targetMajorVersion}.0");
        }

        [Fact(Timeout = 10000)]
        public void TestRewritingSameTargetFrameworkSucceeds()
        {
            string directory = CreateScratchDirectory();
            try
            {
                string assemblyPath = CreateRewritingTarget(directory,
                    $".NETCoreApp,Version=v{Environment.Version.Major}.0");
                RunRewritingEngine(directory, assemblyPath);

                using AssemblyDefinition definition = ReadAssembly(assemblyPath);
                Assert.Contains(definition.CustomAttributes, attribute =>
                    attribute.AttributeType.FullName == typeof(RewritingSignatureAttribute).FullName);

                foreach (AssemblyNameReference reference in definition.MainModule.AssemblyReferences.Where(
                    candidate => candidate.Name is "System.Runtime" || candidate.Name is "System.Private.CoreLib"))
                {
                    Assert.Equal(Environment.Version.Major, reference.Version.Major);
                }
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        /// <summary>
        /// Asserts that rewriting an assembly declaring the specified target framework is rejected
        /// before the assembly, or any other output, is written.
        /// </summary>
        private void AssertRewritingIsRejectedWithoutModifyingAssembly(string targetFramework)
        {
            string directory = CreateScratchDirectory();
            try
            {
                string assemblyPath = CreateRewritingTarget(directory, targetFramework);
                byte[] hash = ComputeHash(assemblyPath);

                var exception = Assert.Throws<InvalidOperationException>(() =>
                    RunRewritingEngine(directory, assemblyPath));
                this.TestOutput.WriteLine(exception.Message);

                Assert.Contains($"targets {targetFramework}", exception.Message);
                Assert.Contains("Coyote host to rewrite this assembly", exception.Message);

                // The target assembly must be left untouched and no output must have been produced.
                Assert.Equal(hash, ComputeHash(assemblyPath));
                Assert.Equal(new[] { assemblyPath }, Directory.GetFiles(directory, "*", SearchOption.AllDirectories));
                Assert.Empty(Directory.GetDirectories(directory));

                using AssemblyDefinition definition = ReadAssembly(assemblyPath);
                Assert.DoesNotContain(definition.CustomAttributes, attribute =>
                    attribute.AttributeType.FullName == typeof(RewritingSignatureAttribute).FullName);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        /// <summary>
        /// Runs the rewriting engine over the specified assembly, replacing it in place.
        /// </summary>
        private static void RunRewritingEngine(string directory, string assemblyPath)
        {
            var options = RewritingOptions.Create();
            options.AssembliesDirectory = directory;
            options.OutputDirectory = directory;
            options.AssemblyPaths.Add(assemblyPath);

            var configuration = Microsoft.Coyote.Configuration.Create();
            using var logWriter = new LogWriter(configuration);
            RewritingEngine.Run(options, configuration, logWriter, new Profiler());
        }

        /// <summary>
        /// Creates a new empty directory that is local to the test binaries.
        /// </summary>
        private static string CreateScratchDirectory() => Directory.CreateDirectory(Path.Combine(
            Path.GetDirectoryName(typeof(HostRuntimeValidationTests).Assembly.Location),
            nameof(HostRuntimeValidationTests),
            Guid.NewGuid().ToString("N"))).FullName;

        /// <summary>
        /// Copies an assembly that can be rewritten to the specified directory, declaring the
        /// specified target framework and dropping any existing rewriting signature.
        /// </summary>
        private static string CreateRewritingTarget(string directory, string targetFramework)
        {
            string sourcePath = Path.Combine(
                Path.GetDirectoryName(typeof(HostRuntimeValidationTests).Assembly.Location),
                RewritingSourceAssembly);
            Assert.True(File.Exists(sourcePath), $"File not found: {sourcePath}");

            string outputPath = Path.Combine(directory, RewritingSourceAssembly);
            using (AssemblyDefinition definition = ReadAssembly(sourcePath))
            {
                RemoveCustomAttributes(definition, typeof(RewritingSignatureAttribute).FullName);
                SetTargetFramework(definition, targetFramework);
                definition.Write(outputPath);
            }

            return outputPath;
        }

        /// <summary>
        /// Creates a copy of the test assembly declaring the specified target framework.
        /// </summary>
        private static string CreateAssemblyWithTargetFramework(string targetFramework)
        {
            string sourcePath = typeof(HostRuntimeValidationTests).Assembly.Location;
            string outputPath = Path.Combine(
                Path.GetDirectoryName(sourcePath),
                $"{nameof(HostRuntimeValidationTests)}.{Guid.NewGuid():N}.dll");

            using (AssemblyDefinition definition = ReadAssembly(sourcePath))
            {
                SetTargetFramework(definition, targetFramework);
                definition.Write(outputPath);
            }

            return outputPath;
        }

        /// <summary>
        /// Reads the specified assembly without keeping the file on disk locked.
        /// </summary>
        private static AssemblyDefinition ReadAssembly(string assemblyPath) =>
            AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters { InMemory = true });

        /// <summary>
        /// Replaces the target framework metadata of the specified assembly, or removes
        /// it if no target framework is specified.
        /// </summary>
        private static void SetTargetFramework(AssemblyDefinition definition, string targetFramework)
        {
            RemoveCustomAttributes(definition, typeof(TargetFrameworkAttribute).FullName);
            if (targetFramework != null)
            {
                var constructor = definition.MainModule.ImportReference(
                    typeof(TargetFrameworkAttribute).GetConstructor(new[] { typeof(string) }));
                var replacement = new CustomAttribute(constructor);
                replacement.ConstructorArguments.Add(new CustomAttributeArgument(
                    definition.MainModule.TypeSystem.String, targetFramework));
                definition.CustomAttributes.Add(replacement);
            }
        }

        /// <summary>
        /// Removes all assembly-level custom attributes with the specified type name.
        /// </summary>
        private static void RemoveCustomAttributes(AssemblyDefinition definition, string attributeTypeName)
        {
            foreach (CustomAttribute attribute in definition.CustomAttributes.Where(
                candidate => candidate.AttributeType.FullName == attributeTypeName).ToArray())
            {
                definition.CustomAttributes.Remove(attribute);
            }
        }

        /// <summary>
        /// Computes the hash of the specified file.
        /// </summary>
        private static byte[] ComputeHash(string path)
        {
            using var stream = File.OpenRead(path);
            using var algorithm = SHA256.Create();
            return algorithm.ComputeHash(stream);
        }
    }
}
#endif
