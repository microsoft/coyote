// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.SystematicTesting;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Microsoft.Coyote.Rewriting
{
    internal class MSTestTransform : AssemblyTransform
    {
        /// <summary>
        /// The current module being transformed.
        /// </summary>
        private ModuleDefinition Module;

        /// <summary>
        /// Initializes a new instance of the <see cref="MSTestTransform"/> class.
        /// </summary>
        internal MSTestTransform(ConsoleLogger log)
            : base(log)
        {
        }

        /// <inheritdoc/>
        internal override void VisitModule(ModuleDefinition module)
        {
            this.Module = module;
        }

        /// <inheritdoc/>
        internal override void VisitMethod(MethodDefinition method)
        {
            if (method.IsAbstract)
            {
                return;
            }

            bool isTestMethod = false;
            if (method.CustomAttributes.Count > 0)
            {
                // Search for a method with a unit testing framework attribute.
                foreach (var attr in method.CustomAttributes)
                {
                    if (attr.AttributeType.FullName == "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute")
                    {
                        isTestMethod = true;
                        break;
                    }
                }
            }

            if (isTestMethod)
            {
                Debug.WriteLine($"............. [-] test method '{method.Name}'");

                MethodDefinition newMethod = this.CloneMethod(method);
                this.RewriteTestMethod(method, newMethod);

                method.DeclaringType.Methods.Add(newMethod);

                Debug.WriteLine($"............. [+] systematic test method '{method.Name}'");
                Debug.WriteLine($"............. [+] test method '{newMethod.Name}'");
            }
        }

        /// <summary>
        /// Clones the test method.
        /// </summary>
        internal MethodDefinition CloneMethod(MethodDefinition method)
        {
            string newName = $"{method.Name}_";
            while (method.DeclaringType.Methods.Any(m => m.Name == newName))
            {
                newName += "_";
            }

            MethodDefinition newMethod = new MethodDefinition(newName, method.Attributes, method.ReturnType);
            foreach (var variable in method.Body.Variables)
            {
                newMethod.Body.Variables.Add(variable);
            }

            foreach (var instruction in method.Body.Instructions)
            {
                newMethod.Body.Instructions.Add(instruction);
            }

            return newMethod;
        }

        /// <summary>
        /// Creates a new method for invoking the original test method from the testing engine.
        /// </summary>
        internal void RewriteTestMethod(MethodDefinition method, MethodDefinition testMethod)
        {
            bool isAsyncMethod = false;
            TypeReference asyncReturnType = null;
            // move any AsyncStateMachineAttributes.
            foreach (var attr in method.CustomAttributes.ToArray())
            {
                var typeName = attr.AttributeType.FullName;
                if (typeName == "System.Runtime.CompilerServices.AsyncStateMachineAttribute")
                {
                    isAsyncMethod = true;
                    asyncReturnType = method.ReturnType;
                    method.ReturnType = this.Module.ImportReference(typeof(void));
                }

                if (typeName == "System.Runtime.CompilerServices.AsyncStateMachineAttribute" ||
                    typeName == "System.Diagnostics.DebuggerStepThroughAttribute")
                {
                    method.CustomAttributes.Remove(attr);
                    testMethod.CustomAttributes.Add(attr);
                }
            }

            method.Body.Variables.Clear();
            method.Body.Instructions.Clear();

            TypeReference actionType;
            if (isAsyncMethod)
            {
                var funcType = this.Module.ImportReference(typeof(Func<>));
                actionType = ImportGenericTypeInstance(this.Module, funcType, asyncReturnType);
            }
            else
            {
                actionType = this.Module.ImportReference(typeof(Action));
            }

            var configurationType = this.Module.ImportReference(typeof(Configuration));
            var engineType = this.Module.ImportReference(typeof(TestingEngine));

            var resolvedActionType = actionType.Resolve(); // Func<>
            var resolvedConfigurationType = configurationType.Resolve();
            var resolvedEngineType = engineType.Resolve();

            MethodReference actionConstructor = this.Module.ImportReference(
                resolvedActionType.Methods.FirstOrDefault(m => m.IsConstructor));
            if (isAsyncMethod)
            {
                actionConstructor = ImportGenericMethodInstance(this.Module, actionConstructor, asyncReturnType);
            }

            MethodReference createConfigurationMethod = this.Module.ImportReference(
                resolvedConfigurationType.Methods.FirstOrDefault(m => m.Name is "Create"));
            MethodReference createEngineMethod = this.Module.ImportReference(
                FindMatchingMethod(resolvedEngineType, "Create", configurationType, actionType));

            MethodReference runEngineMethod = this.Module.ImportReference(
                resolvedEngineType.Methods.FirstOrDefault(m => m.Name is "Run"));

            // The emitted IL corresponds to a method body such as:
            //   Configuration configuration = Configuration.Create();
            //   TestingEngine engine = TestingEngine.Create(configuration, new Action(Test));
            //   engine.Run();
            var processor = method.Body.GetILProcessor();
            processor.Emit(OpCodes.Nop);
            processor.Emit(OpCodes.Call, createConfigurationMethod);
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldftn, testMethod);
            processor.Emit(OpCodes.Newobj, actionConstructor);
            processor.Emit(OpCodes.Call, createEngineMethod);
            processor.Emit(OpCodes.Callvirt, runEngineMethod);
            processor.Emit(OpCodes.Nop);
            processor.Emit(OpCodes.Ret);

            method.Body.OptimizeMacros();
        }
    }
}
