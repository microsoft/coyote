// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Maintains information about a method to be tested.
    /// </summary>
    internal sealed class TestMethodInfo
    {
        /// <summary>
        /// The assembly that contains the test method.
        /// </summary>
        internal readonly Assembly Assembly;

        /// <summary>
        /// The method to be tested.
        /// </summary>
        internal readonly Delegate Method;

        /// <summary>
        /// The name of the test method.
        /// </summary>
        internal readonly string Name;

        /// <summary>
        /// The test initialization method.
        /// </summary>
        private readonly MethodInfo InitMethod;

        /// <summary>
        /// The test dispose method.
        /// </summary>
        private readonly MethodInfo DisposeMethod;

        /// <summary>
        /// The test dispose method per iteration.
        /// </summary>
        private readonly MethodInfo IterationDisposeMethod;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestMethodInfo"/> class.
        /// </summary>
        private TestMethodInfo(Assembly assembly, Delegate method, string name, MethodInfo initMethod,
            MethodInfo disposeMethod, MethodInfo iterationDisposeMethod)
        {
            this.Assembly = assembly;
            this.Method = method;
            this.Name = name;
            this.InitMethod = initMethod;
            this.DisposeMethod = disposeMethod;
            this.IterationDisposeMethod = iterationDisposeMethod;
        }

        /// <summary>
        /// Creates a <see cref="TestMethodInfo"/> instance from the specified delegate.
        /// </summary>
        internal static TestMethodInfo Create(Delegate method) =>
            new TestMethodInfo(method.GetMethodInfo().Module.Assembly, method, null, null, null, null);

        /// <summary>
        /// Creates a <see cref="TestMethodInfo"/> instance from assembly specified in the configuration.
        /// </summary>
        internal static TestMethodInfo Create(Configuration configuration)
        {
            Assembly assembly = Assembly.LoadFrom(configuration.AssemblyToBeAnalyzed);

            var (testMethod, testName) = GetTestMethod(assembly, configuration.TestMethodName);
            var initMethod = GetTestSetupMethod(assembly, typeof(TestInitAttribute));
            var disposeMethod = GetTestSetupMethod(assembly, typeof(TestDisposeAttribute));
            var iterationDisposeMethod = GetTestSetupMethod(assembly, typeof(TestIterationDisposeAttribute));

            return new TestMethodInfo(assembly, testMethod, testName, initMethod, disposeMethod, iterationDisposeMethod);
        }

        /// <summary>
        /// Invokes the user-specified initialization method for all iterations executing this test.
        /// </summary>
        internal void InitializeAllIterations() => this.InitMethod?.Invoke(null, Array.Empty<object>());

        /// <summary>
        /// Invokes the user-specified disposal method for the iteration currently executing this test.
        /// </summary>
        internal void DisposeCurrentIteration() => this.IterationDisposeMethod?.Invoke(null, null);

        /// <summary>
        /// Invokes the user-specified disposal method for all iterations executing this test.
        /// </summary>
        internal void DisposeAllIterations() => this.DisposeMethod?.Invoke(null, Array.Empty<object>());

        /// <summary>
        /// Returns the test method with the specified name. A test method must
        /// be annotated with the <see cref="TestAttribute"/> attribute.
        /// </summary>
        private static (Delegate testMethod, string testName) GetTestMethod(Assembly assembly, string methodName)
        {
            BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod;
            List<MethodInfo> testMethods = FindTestMethodsWithAttribute(typeof(TestAttribute), flags, assembly);

            if (testMethods.Count > 0)
            {
                List<MethodInfo> filteredTestMethods = null;
                string error = null;

                if (!string.IsNullOrEmpty(methodName))
                {
                    // Filter by test method name.
                    filteredTestMethods = testMethods.FindAll(mi => mi.Name.Equals(methodName) ||
                        string.Format("{0}.{1}", mi.DeclaringType.FullName, mi.Name).Equals(methodName));
                    if (filteredTestMethods.Count > 1)
                    {
                        error = $"The method name '{methodName}' is ambiguous. Please specify the full test method name.";
                    }
                    else if (filteredTestMethods.Count is 0)
                    {
                        error = $"Cannot detect a Coyote test method name containing {methodName}.";
                    }
                }
                else if (testMethods.Count > 1)
                {
                    error = $"Found '{testMethods.Count}' test methods declared with the '{typeof(TestAttribute).FullName}' " +
                        $"attribute. Provide --method (-m) flag to qualify the test method that you want to run.";
                }

                if (!string.IsNullOrEmpty(error))
                {
                    error += " Possible methods are:" + Environment.NewLine;

                    var possibleMethods = filteredTestMethods?.Count > 1 ? filteredTestMethods : testMethods;
                    for (int idx = 0; idx < possibleMethods.Count; idx++)
                    {
                        var mi = possibleMethods[idx];
                        error += string.Format("  {0}.{1}", mi.DeclaringType.FullName, mi.Name);
                        if (idx < possibleMethods.Count - 1)
                        {
                            error += Environment.NewLine;
                        }
                    }

                    throw new InvalidOperationException(error);
                }

                if (!string.IsNullOrEmpty(methodName))
                {
                    testMethods = filteredTestMethods;
                }
            }
            else if (testMethods.Count is 0)
            {
                // see if user forgot to make it static!
                flags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                testMethods = FindTestMethodsWithAttribute(typeof(TestAttribute), flags, assembly);
                if (testMethods.Count > 0)
                {
                    ReportBadTestMethods(testMethods, "test");
                }
                else
                {
                    throw new InvalidOperationException($"Cannot detect a public static Coyote test method declared with the " +
                        $"'[{typeof(TestAttribute).FullName}]' attribute.");
                }
            }

            MethodInfo testMethod = testMethods[0];
            ParameterInfo[] testParams = testMethod.GetParameters();

            bool hasVoidReturnType = testMethod.ReturnType == typeof(void);
            bool hasTaskReturnType = typeof(Task).IsAssignableFrom(testMethod.ReturnType);

            bool hasNoInputParameters = testParams.Length is 0;
            bool hasActorInputParameters = testParams.Length is 1 && testParams[0].ParameterType == typeof(IActorRuntime);
            bool hasTaskInputParameters = testParams.Length is 1 && testParams[0].ParameterType == typeof(ICoyoteRuntime);

            if (!((hasVoidReturnType || hasTaskReturnType) &&
                (hasNoInputParameters || hasActorInputParameters || hasTaskInputParameters) &&
                !testMethod.IsAbstract && !testMethod.IsVirtual && !testMethod.IsConstructor &&
                !testMethod.ContainsGenericParameters && testMethod.IsPublic && testMethod.IsStatic))
            {
                throw new InvalidOperationException("Incorrect test method declaration. Please " +
                    $"make sure your [{typeof(TestAttribute).FullName}] methods have:\n\n" +
                    $"Parameters:\n" +
                    $"  ()\n" +
                    $"  (ICoyoteRuntime runtime)\n" +
                    $"  (IActorRuntime runtime)\n\n" +
                    $"Return type:\n" +
                    $"  void\n" +
                    $"  {typeof(Task).FullName}\n" +
                    $"  {typeof(Task).FullName}<T>\n" +
                    $"  async {typeof(Task).FullName}\n" +
                    $"  async {typeof(Task).FullName}<T>\n");
            }

            Delegate test;
            if (hasTaskReturnType)
            {
                if (hasActorInputParameters)
                {
                    test = testMethod.CreateDelegate(typeof(Func<IActorRuntime, Task>));
                }
                else if (hasTaskInputParameters)
                {
                    test = testMethod.CreateDelegate(typeof(Func<ICoyoteRuntime, Task>));
                }
                else
                {
                    test = testMethod.CreateDelegate(typeof(Func<Task>));
                }
            }
            else
            {
                if (hasActorInputParameters)
                {
                    test = testMethod.CreateDelegate(typeof(Action<IActorRuntime>));
                }
                else if (hasTaskInputParameters)
                {
                    test = testMethod.CreateDelegate(typeof(Action<ICoyoteRuntime>));
                }
                else
                {
                    test = testMethod.CreateDelegate(typeof(Action));
                }
            }

            return (test, $"{testMethod.DeclaringType}.{testMethod.Name}");
        }

        private static void ReportBadTestMethods(List<MethodInfo> testMethods, string caption)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var method in testMethods)
            {
                string wrong = null;
                if (!method.IsStatic)
                {
                    wrong = "not static";
                }

                if (!method.IsPublic)
                {
                    wrong = string.IsNullOrEmpty(wrong) ? "not public" : wrong + " and not public";
                }

                sb.AppendLine($"Ignoring method '{method.DeclaringType.Name + "." + method.Name}' because it is {wrong}");
            }

            throw new InvalidOperationException($"The following Coyote {caption} methods cannot be used :\n{sb}");
        }

        /// <summary>
        /// Returns the test method with the specified attribute.
        /// Returns null if no such method is found.
        /// </summary>
        private static MethodInfo GetTestSetupMethod(Assembly assembly, Type attribute)
        {
            BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod;
            List<MethodInfo> testMethods = FindTestMethodsWithAttribute(attribute, flags, assembly);

            if (testMethods.Count is 0)
            {
                flags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                testMethods = FindTestMethodsWithAttribute(attribute, flags, assembly);
                if (testMethods.Count > 0)
                {
                    ReportBadTestMethods(testMethods, "test setup");
                }

                return null;
            }
            else if (testMethods.Count > 1)
            {
                throw new InvalidOperationException("Only one test method can be declared with the attribute " +
                    $"'{attribute.FullName}'. '{testMethods.Count}' test methods were found instead.");
            }

            string error = null;
            if (testMethods[0].ReturnType != typeof(void))
            {
                error = "The test method return type is not void.";
            }
            else if (testMethods[0].IsGenericMethod)
            {
                error = "The test method is generic.";
            }
            else if (testMethods[0].ContainsGenericParameters)
            {
                error = "The test method inherits generic parameters which is not supported.";
            }
            else if (testMethods[0].IsAbstract)
            {
                error = "The test method is abstract.";
            }
            else if (testMethods[0].IsVirtual)
            {
                error = "The test method is virtual.";
            }
            else if (testMethods[0].IsConstructor)
            {
                error = "The test method is a constructor.";
            }
            else if (testMethods[0].GetParameters().Length != 0)
            {
                error = "The test method has unexpected parameters.";
            }

            if (error != null)
            {
                throw new InvalidOperationException(error + " Please " +
                    "declare it as follows:\n" +
                    $"  [{attribute.FullName}] public static void " +
                    $"{testMethods[0].Name}() {{ ... }}");
            }

            return testMethods[0];
        }

        /// <summary>
        /// Finds the test methods with the specified attribute in the given assembly.
        /// Returns an empty list if no such methods are found.
        /// </summary>
        private static List<MethodInfo> FindTestMethodsWithAttribute(Type attribute, BindingFlags bindingFlags, Assembly assembly)
        {
            List<MethodInfo> testMethods = null;

            try
            {
                testMethods = assembly.GetTypes().SelectMany(t => t.GetMethods(bindingFlags)).
                    Where(m => m.GetCustomAttributes(attribute, false).Any()).ToList();
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var le in ex.LoaderExceptions)
                {
                    Debug.WriteLine(le.Message);
                }

                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }

            return testMethods;
        }
    }
}
