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
using ScopeSignature = Microsoft.Coyote.Tools.Tests.LockScopeInterfaceTests.ScopeSignature;

namespace Microsoft.Coyote.Tools.Tests
{
    public class LockScopeBoundaryTests : BaseToolsTest
    {
        public LockScopeBoundaryTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Theory(Timeout = 10000)]
        [InlineData(ScopeSignature.Return, false)]
        [InlineData(ScopeSignature.Value, false)]
        [InlineData(ScopeSignature.Ref, false)]
        [InlineData(ScopeSignature.Out, false)]
        [InlineData(ScopeSignature.RefReturn, false)]
        [InlineData(ScopeSignature.Return, true)]
        [InlineData(ScopeSignature.Value, true)]
        [InlineData(ScopeSignature.Ref, true)]
        [InlineData(ScopeSignature.Out, true)]
        [InlineData(ScopeSignature.RefReturn, true)]
        public void TestScopeBaseOverrides(ScopeSignature signature, bool rewriteContract)
        {
            foreach (bool explicitOverride in new[] { false, true })
            {
                foreach (bool genericBase in new[] { false, true })
                {
                    foreach (bool concreteBase in new[] { false, true })
                    {
                        string directory = CreateScratchDirectory();
                        try
                        {
                            string target = CreateBaseOverride(directory, signature, explicitOverride, genericBase, concreteBase);
                            AssertTypesLoad(directory);
                            VerifyRewrite(directory, target, rewriteContract, "base", "ScopeContract");
                            if (rewriteContract)
                            {
                                AssertTypesLoad(directory);
                            }
                        }
                        finally
                        {
                            Directory.Delete(directory, true);
                        }
                    }
                }
            }
        }

        [Theory(Timeout = 10000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestNativeBaseWithoutOverrideRemainsSupported(bool hideBaseMethod)
        {
            string directory = CreateScratchDirectory();
            try
            {
                string target = CreateBaseOverride(directory, ScopeSignature.Return, explicitOverride: false,
                    genericBase: false, concreteBase: true, hideBaseMethod, omitOverride: !hideBaseMethod);
                AssertTypesLoad(directory);
                var options = CreateOptions(directory, target, rewriteContract: false);
                var configuration = Configuration.Create();
                using var logWriter = new LogWriter(configuration);
                Rewriting.RewritingEngine.Run(options, configuration, logWriter, new Profiler());
                AssertTypesLoad(directory);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Theory(Timeout = 10000)]
        [InlineData(ScopeSignature.Value, false)]
        [InlineData(ScopeSignature.Ref, false)]
        [InlineData(ScopeSignature.Out, false)]
        [InlineData(ScopeSignature.Value, true)]
        [InlineData(ScopeSignature.Ref, true)]
        [InlineData(ScopeSignature.Out, true)]
        public void TestScopeParameterCalls(ScopeSignature signature, bool rewriteContract)
        {
            foreach (bool constructor in new[] { false, true })
            {
                string directory = CreateScratchDirectory();
                try
                {
                    string target = CreateInvocation(directory, signature, constructor, delegateBinding: false, virtualBinding: false);
                    AssertTypesLoad(directory);
                    VerifyRewrite(directory, target, rewriteContract, "parameter", "ScopeContract");
                    if (rewriteContract)
                    {
                        AssertTypesLoad(directory);
                    }
                }
                finally
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Theory(Timeout = 10000)]
        [InlineData(ScopeSignature.Return, false)]
        [InlineData(ScopeSignature.Value, false)]
        [InlineData(ScopeSignature.Ref, false)]
        [InlineData(ScopeSignature.Out, false)]
        [InlineData(ScopeSignature.Return, true)]
        [InlineData(ScopeSignature.Value, true)]
        [InlineData(ScopeSignature.Ref, true)]
        [InlineData(ScopeSignature.Out, true)]
        [InlineData(ScopeSignature.RefReturn, false)]
        [InlineData(ScopeSignature.RefReturn, true)]
        public void TestScopeDelegateBindings(ScopeSignature signature, bool rewriteContract)
        {
            foreach (bool virtualBinding in new[] { false, true })
            {
                string directory = CreateScratchDirectory();
                try
                {
                    string target = CreateInvocation(directory, signature, constructor: false, delegateBinding: true, virtualBinding);
                    AssertTypesLoad(directory);
                    VerifyRewrite(directory, target, rewriteContract, "delegate", "ScopeContract");
                    if (rewriteContract)
                    {
                        AssertTypesLoad(directory);
                    }
                }
                finally
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Theory(Timeout = 10000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestScopeRefReturnCalls(bool rewriteContract)
        {
            string directory = CreateScratchDirectory();
            try
            {
                string target = CreateInvocation(directory, ScopeSignature.RefReturn,
                    constructor: false, delegateBinding: false, virtualBinding: false);
                AssertTypesLoad(directory);
                VerifyRewrite(directory, target, rewriteContract, "return", "ScopeContract");
                if (rewriteContract)
                {
                    AssertTypesLoad(directory);
                }
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Theory(Timeout = 10000)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void TestUnrewrittenProviderTypes(bool skippedProvider, bool delegateBinding)
        {
            string directory = CreateScratchDirectory();
            try
            {
                string target = CreateInvocation(directory, delegateBinding ? ScopeSignature.Return : ScopeSignature.Value,
                    constructor: false, delegateBinding, virtualBinding: false, skippedProvider,
                    systemNamespace: !skippedProvider);
                AssertTypesLoad(directory);
                var options = CreateOptions(directory, target, rewriteContract: skippedProvider);
                var configuration = Configuration.Create();
                using var logWriter = new LogWriter(configuration);
                byte[] original = File.ReadAllBytes(target);
                var exception = Assert.Throws<InvalidOperationException>(() =>
                    Rewriting.RewritingEngine.Run(options, configuration, logWriter, new Profiler()));
                Assert.Contains("Regression.Caller::Run", exception.Message);
                Assert.Contains("Include assembly 'ScopeContract' in rewriting", exception.Message);
                if (skippedProvider)
                {
                    Assert.Contains("SkipRewriting", exception.Message);
                }

                Assert.Equal(original, File.ReadAllBytes(target));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Theory(Timeout = 10000)]
        [InlineData(Code.Ldfld, false)]
        [InlineData(Code.Ldflda, false)]
        [InlineData(Code.Stfld, false)]
        [InlineData(Code.Ldfld, true)]
        [InlineData(Code.Ldflda, true)]
        [InlineData(Code.Stfld, true)]
        public void TestScopeFieldAccess(Code access, bool rewriteContract)
        {
            string directory = CreateScratchDirectory();
            try
            {
                using var contract = CreateAssembly("ScopeContract");
                var holder = AddType(contract.MainModule, "ScopeHolder",
                    TypeAttributes.Sealed | TypeAttributes.SequentialLayout,
                    contract.MainModule.ImportReference(typeof(ValueType)));
                holder.CustomAttributes.Add(new CustomAttribute(contract.MainModule.ImportReference(
                    typeof(System.Runtime.CompilerServices.IsByRefLikeAttribute).GetConstructor(Type.EmptyTypes))));
                var field = new FieldDefinition("Scope", FieldAttributes.Public,
                    contract.MainModule.ImportReference(typeof(System.Threading.Lock.Scope)));
                holder.Fields.Add(field);
                contract.Write(Path.Combine(directory, "ScopeContract.dll"));

                using var target = CreateAssembly("ScopeImplementation");
                var module = target.MainModule;
                var caller = AddType(module, "Caller", TypeAttributes.Abstract | TypeAttributes.Sealed, module.TypeSystem.Object);
                var call = new MethodDefinition("Run", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
                caller.Methods.Add(call);
                call.Parameters.Add(new ParameterDefinition(new ByReferenceType(module.ImportReference(holder))));
                call.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                if (access is Code.Stfld)
                {
                    call.Parameters.Add(new ParameterDefinition(module.ImportReference(field.FieldType)));
                    call.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
                }
                else
                {
                    call.ReturnType = access is Code.Ldflda ?
                        new ByReferenceType(module.ImportReference(field.FieldType)) : module.ImportReference(field.FieldType);
                }

                call.Body.Instructions.Add(Instruction.Create(access is Code.Ldfld ? OpCodes.Ldfld :
                    access is Code.Ldflda ? OpCodes.Ldflda : OpCodes.Stfld, module.ImportReference(field)));
                call.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                string path = Path.Combine(directory, "ScopeImplementation.dll");
                target.Write(path);
                AssertTypesLoad(directory);
                VerifyRewrite(directory, path, rewriteContract, "field", "ScopeContract");
                if (rewriteContract)
                {
                    AssertTypesLoad(directory);
                }
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        private static string CreateScratchDirectory() => Directory.CreateDirectory(Path.Combine(
            Path.GetDirectoryName(typeof(LockScopeBoundaryTests).Assembly.Location),
            nameof(LockScopeBoundaryTests), Guid.NewGuid().ToString("N"))).FullName;

        private static AssemblyDefinition CreateAssembly(string name) => AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition(name, new Version(1, 0)), name, ModuleKind.Dll);

        private static TypeDefinition AddType(ModuleDefinition module, string name, TypeAttributes attributes,
            TypeReference baseType)
        {
            var type = new TypeDefinition("Regression", name, TypeAttributes.Public | attributes, baseType);
            module.Types.Add(type);
            return type;
        }

        private static MethodDefinition AddMethod(TypeDefinition type, string name, MethodAttributes attributes,
            ScopeSignature signature)
        {
            var method = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.HideBySig | attributes,
                type.Module.TypeSystem.Void);
            type.Methods.Add(method);
            TypeReference scope = type.Module.ImportReference(typeof(System.Threading.Lock.Scope));
            if (signature is ScopeSignature.Return or ScopeSignature.RefReturn)
            {
                method.ReturnType = signature is ScopeSignature.RefReturn ? new ByReferenceType(scope) : scope;
            }
            else
            {
                method.Parameters.Add(new ParameterDefinition("scope",
                    signature is ScopeSignature.Out ? ParameterAttributes.Out : ParameterAttributes.None,
                    signature is ScopeSignature.Value ? scope : new ByReferenceType(scope)));
            }

            if (!method.IsAbstract)
            {
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Throw));
            }

            return method;
        }

        private static string CreateBaseOverride(string directory, ScopeSignature signature,
            bool explicitOverride, bool genericBase, bool concreteBase, bool hideBaseMethod = false, bool omitOverride = false)
        {
            using var contract = CreateAssembly("ScopeContract");
            var contractModule = contract.MainModule;
            var baseType = AddType(contractModule, genericBase ? "ProviderBase`1" : "ProviderBase",
                TypeAttributes.Abstract, contractModule.TypeSystem.Object);
            var declaration = AddMethod(baseType, signature is ScopeSignature.Return ? "EnterScope" : "Consume",
                (concreteBase ? 0 : MethodAttributes.Abstract) | MethodAttributes.Virtual | MethodAttributes.NewSlot, signature);
            if (genericBase)
            {
                var parameter = new GenericParameter("T", baseType);
                baseType.GenericParameters.Add(parameter);
                declaration.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, parameter));
            }

            contract.Write(Path.Combine(directory, "ScopeContract.dll"));
            using var target = CreateAssembly("ScopeImplementation");
            var module = target.MainModule;
            TypeReference importedBase = module.ImportReference(baseType);
            if (genericBase)
            {
                var instance = new GenericInstanceType(importedBase);
                instance.GenericArguments.Add(module.TypeSystem.String);
                importedBase = instance;
            }

            var intermediate = AddType(module, "Intermediate", TypeAttributes.Abstract, importedBase);
            var implementation = AddType(module, "Provider", 0, intermediate);
            var method = AddMethod(implementation, declaration.Name,
                MethodAttributes.Virtual | (explicitOverride || hideBaseMethod ? MethodAttributes.NewSlot : 0), signature);
            if (genericBase)
            {
                method.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, module.TypeSystem.String));
            }

            if (explicitOverride)
            {
                var methodOverride = module.ImportReference(declaration);
                methodOverride.DeclaringType = importedBase;
                method.Overrides.Add(methodOverride);
            }

            if (omitOverride)
            {
                implementation.Methods.Remove(method);
            }

            string path = Path.Combine(directory, "ScopeImplementation.dll");
            target.Write(path);
            return path;
        }

        private static string CreateInvocation(string directory, ScopeSignature signature,
            bool constructor, bool delegateBinding, bool virtualBinding,
            bool skippedProvider = false, bool systemNamespace = false)
        {
            using var contract = CreateAssembly("ScopeContract");
            var contractModule = contract.MainModule;
            var provider = AddType(contractModule, "NativeProvider", 0, contractModule.TypeSystem.Object);
            if (skippedProvider)
            {
                provider.CustomAttributes.Add(new CustomAttribute(contractModule.ImportReference(
                    typeof(Rewriting.SkipRewritingAttribute).GetConstructor(new[] { typeof(string) }))));
                provider.CustomAttributes.Last().ConstructorArguments.Add(
                    new CustomAttributeArgument(contractModule.TypeSystem.String, "Native boundary regression."));
            }

            if (systemNamespace)
            {
                provider.Namespace = "System.Regression";
            }

            var native = AddMethod(provider, constructor ? ".ctor" : "Invoke",
                constructor ? MethodAttributes.SpecialName | MethodAttributes.RTSpecialName :
                virtualBinding ? MethodAttributes.Virtual | MethodAttributes.NewSlot : MethodAttributes.Static, signature);
            contract.Write(Path.Combine(directory, "ScopeContract.dll"));

            using var target = CreateAssembly("ScopeImplementation");
            var module = target.MainModule;
            var caller = AddType(module, "Caller", TypeAttributes.Abstract | TypeAttributes.Sealed, module.TypeSystem.Object);
            var call = new MethodDefinition("Run", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
            caller.Methods.Add(call);
            var external = module.ImportReference(native);
            if (delegateBinding)
            {
                var delegateType = AddType(module, "ScopeDelegate", TypeAttributes.Sealed,
                    module.ImportReference(typeof(MulticastDelegate)));
                var invoke = AddMethod(delegateType, "Invoke", MethodAttributes.Virtual | MethodAttributes.NewSlot, signature);
                invoke.ImplAttributes = MethodImplAttributes.Runtime;
                invoke.Body = null;
                var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName |
                    MethodAttributes.RTSpecialName, module.TypeSystem.Void) { ImplAttributes = MethodImplAttributes.Runtime };
                ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
                ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
                delegateType.Methods.Add(ctor);
                if (virtualBinding)
                {
                    call.Parameters.Add(new ParameterDefinition("provider", ParameterAttributes.None, module.ImportReference(provider)));
                    call.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                    call.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
                }
                else
                {
                    call.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
                }

                call.Body.Instructions.Add(Instruction.Create(virtualBinding ? OpCodes.Ldvirtftn : OpCodes.Ldftn, external));
                call.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, ctor));
                call.ReturnType = delegateType;
            }
            else
            {
                foreach (var parameter in native.Parameters)
                {
                    call.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes,
                        module.ImportReference(parameter.ParameterType)));
                    call.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg, call.Parameters.Last()));
                }

                call.Body.Instructions.Add(Instruction.Create(constructor ? OpCodes.Newobj : OpCodes.Call, external));
                call.ReturnType = constructor ? module.ImportReference(provider) : module.ImportReference(native.ReturnType);
            }

            call.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            string path = Path.Combine(directory, "ScopeImplementation.dll");
            target.Write(path);
            return path;
        }

        private static void VerifyRewrite(string directory, string target, bool rewriteContract, string boundary,
            string assemblyName)
        {
            var options = CreateOptions(directory, target, rewriteContract);
            var configuration = Configuration.Create();
            using var logWriter = new LogWriter(configuration);
            if (rewriteContract)
            {
                Rewriting.RewritingEngine.Run(options, configuration, logWriter, new Profiler());
            }
            else
            {
                byte[] original = File.ReadAllBytes(target);
                var exception = Assert.Throws<InvalidOperationException>(() =>
                    Rewriting.RewritingEngine.Run(options, configuration, logWriter, new Profiler()));
                Assert.Contains("Cannot rewrite", exception.Message);
                Assert.Contains(boundary, exception.Message);
                Assert.Contains("Include assembly '" + assemblyName + "' in rewriting", exception.Message);
                Assert.Equal(original, File.ReadAllBytes(target));
            }
        }

        private static Rewriting.RewritingOptions CreateOptions(string directory, string target, bool rewriteContract)
        {
            var options = Rewriting.RewritingOptions.Create();
            options.AssembliesDirectory = directory;
            options.OutputDirectory = directory;
            options.AssemblyPaths.Add(target);
            if (rewriteContract)
            {
                options.AssemblyPaths.Add(Path.Combine(directory, "ScopeContract.dll"));
            }

            return options;
        }

        private static void AssertTypesLoad(string directory)
        {
            var context = new AssemblyLoadContext(Guid.NewGuid().ToString("N"), isCollectible: true);
            try
            {
                using var contractStream = File.OpenRead(Path.Combine(directory, "ScopeContract.dll"));
                using var targetStream = File.OpenRead(Path.Combine(directory, "ScopeImplementation.dll"));
                var contract = context.LoadFromStream(contractStream);
                var target = context.LoadFromStream(targetStream);
                Assert.NotEmpty(target.GetTypes());
                var call = target.GetType("Regression.Caller")?.GetMethod("Run");
                if (call != null)
                {
                    System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(call.MethodHandle);
                    if (typeof(Delegate).IsAssignableFrom(call.ReturnType))
                    {
                        object[] arguments = call.GetParameters().Length is 0 ? Array.Empty<object>() :
                            new[]
                            {
                                System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(
                                    contract.GetType("Regression.NativeProvider", throwOnError: true))
                            };
                        Assert.IsAssignableFrom<Delegate>(call.Invoke(null, arguments));
                    }
                }
            }
            finally
            {
                context.Unload();
            }
        }
    }
}
#endif
