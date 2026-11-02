// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET10_0_OR_GREATER
using System;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Tools.Tests
{
    public class LockScopeInterfaceTests : BaseToolsTest
    {
        public LockScopeInterfaceTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Theory(Timeout = 10000)]
        [InlineData(false, false, false)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, false)]
        [InlineData(false, false, true)]
        [InlineData(false, true, true)]
        [InlineData(true, false, true)]
        [InlineData(true, true, true)]
        public void TestExternalScopeInterfaceIsRejected(bool explicitImplementation, bool inheritedInterface, bool isValueType)
        {
            string directory = CreateScratchDirectory();
            try
            {
                string target = CreateTarget(directory, explicitImplementation, inheritedInterface, isValueType);
                AssertInterfaceContractLoads(directory);
                byte[] original = File.ReadAllBytes(target);
                var exception = Assert.Throws<InvalidOperationException>(() =>
                    Rewrite(directory, target, rewriteContract: false));

                Assert.Contains("Cannot rewrite type 'Regression.Provider'", exception.Message);
                Assert.Contains("Regression.IProvider::EnterScope", exception.Message);
                Assert.Contains("would change the interface contract", exception.Message);
                Assert.Contains("Include assembly 'ScopeContract' in rewriting", exception.Message);
                Assert.Equal(original, File.ReadAllBytes(target));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Theory(Timeout = 10000)]
        [InlineData(false, false, false)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, false)]
        [InlineData(false, false, true)]
        [InlineData(false, true, true)]
        [InlineData(true, false, true)]
        [InlineData(true, true, true)]
        public void TestRewrittenScopeInterfaceIsAccepted(bool explicitImplementation, bool inheritedInterface, bool isValueType)
        {
            string directory = CreateScratchDirectory();
            try
            {
                string target = CreateTarget(directory, explicitImplementation, inheritedInterface, isValueType);
                Rewrite(directory, target, rewriteContract: true);

                string scopeName = typeof(Rewriting.Types.Threading.Lock.Scope).FullName.Replace('+', '/');
                foreach (string file in new[] { "ScopeContract.dll", "ScopeImplementation.dll" })
                {
                    using var assembly = AssemblyDefinition.ReadAssembly(Path.Combine(directory, file));
                    var method = assembly.MainModule.Types.SelectMany(type => type.Methods).Single();
                    Assert.Equal(scopeName, method.ReturnType.FullName);
                    foreach (var methodOverride in method.Overrides)
                    {
                        Assert.Equal(scopeName, methodOverride.ReturnType.FullName);
                    }
                }

                AssertInterfaceContractLoads(directory);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        private static string CreateScratchDirectory() => Directory.CreateDirectory(Path.Combine(
            Path.GetDirectoryName(typeof(LockScopeInterfaceTests).Assembly.Location),
            nameof(LockScopeInterfaceTests), Guid.NewGuid().ToString("N"))).FullName;

        private static string CreateTarget(string directory, bool explicitImplementation,
            bool inheritedInterface, bool isValueType)
        {
            using var contract = AssemblyDefinition.CreateAssembly(
                new AssemblyNameDefinition("ScopeContract", new Version(1, 0)), "ScopeContract", ModuleKind.Dll);
            var contractModule = contract.MainModule;
            var provider = new TypeDefinition("Regression", "IProvider",
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            contractModule.Types.Add(provider);
            var declaration = new MethodDefinition("EnterScope",
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual |
                MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                contractModule.ImportReference(typeof(System.Threading.Lock.Scope)));
            provider.Methods.Add(declaration);
            TypeDefinition implementedInterface = provider;
            if (inheritedInterface)
            {
                implementedInterface = new TypeDefinition("Regression", "IDerivedProvider",
                    TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
                implementedInterface.Interfaces.Add(new InterfaceImplementation(provider));
                contractModule.Types.Add(implementedInterface);
            }

            contract.Write(Path.Combine(directory, "ScopeContract.dll"));

            using var target = AssemblyDefinition.CreateAssembly(
                new AssemblyNameDefinition("ScopeImplementation", new Version(1, 0)), "ScopeImplementation", ModuleKind.Dll);
            var module = target.MainModule;
            var implementation = new TypeDefinition("Regression", "Provider",
                TypeAttributes.Public | (isValueType ? TypeAttributes.Sealed | TypeAttributes.SequentialLayout : 0),
                isValueType ? module.ImportReference(typeof(ValueType)) : module.TypeSystem.Object);
            implementation.Interfaces.Add(new InterfaceImplementation(module.ImportReference(implementedInterface)));
            module.Types.Add(implementation);
            var method = new MethodDefinition(explicitImplementation ? "Regression.IProvider.EnterScope" : "EnterScope",
                (explicitImplementation ? MethodAttributes.Private : MethodAttributes.Public) |
                MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                module.ImportReference(typeof(System.Threading.Lock.Scope)));
            if (explicitImplementation)
            {
                method.Overrides.Add(module.ImportReference(declaration));
            }

            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Throw));
            implementation.Methods.Add(method);
            string path = Path.Combine(directory, "ScopeImplementation.dll");
            target.Write(path);
            return path;
        }

        private static void Rewrite(string directory, string target, bool rewriteContract)
        {
            var options = Rewriting.RewritingOptions.Create();
            options.AssembliesDirectory = directory;
            options.OutputDirectory = directory;
            options.AssemblyPaths.Add(target);
            if (rewriteContract)
            {
                options.AssemblyPaths.Add(Path.Combine(directory, "ScopeContract.dll"));
            }

            var configuration = Configuration.Create();
            using var logWriter = new LogWriter(configuration);
            Rewriting.RewritingEngine.Run(options, configuration, logWriter, new Profiler());
        }

        private static void AssertInterfaceContractLoads(string directory)
        {
            var context = new AssemblyLoadContext(Guid.NewGuid().ToString("N"), isCollectible: true);
            try
            {
                using var contractStream = File.OpenRead(Path.Combine(directory, "ScopeContract.dll"));
                using var targetStream = File.OpenRead(Path.Combine(directory, "ScopeImplementation.dll"));
                var contract = context.LoadFromStream(contractStream);
                var target = context.LoadFromStream(targetStream);
                var implementation = target.GetType("Regression.Provider", throwOnError: true);
                var map = implementation.GetInterfaceMap(contract.GetType("Regression.IProvider", throwOnError: true));
                Assert.Equal(Assert.Single(map.InterfaceMethods).ReturnType, Assert.Single(map.TargetMethods).ReturnType);
            }
            finally
            {
                context.Unload();
            }
        }
    }
}
#endif
