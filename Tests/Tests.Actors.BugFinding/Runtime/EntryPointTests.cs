// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests.Runtime
{
    public class EntryPointTests : BaseActorBugFindingTest
    {
        public EntryPointTests(ITestOutputHelper output)
            : base(output)
        {
        }

#pragma warning disable xUnit1013 // Public method should be marked as test
        [Test]
        public static void VoidTest() => Assert.True(true);

        [Test]
        public static void VoidTestWithNoRuntime() => Assert.True(true);

        [Test]
        public static void VoidTestWithRuntime(ICoyoteRuntime runtime) => Assert.NotNull(runtime);

        [Test]
        public static void VoidTestWithActorRuntime(IActorRuntime runtime) => Assert.NotNull(runtime);

        [Test]
        public static Task TaskTestWithNoRuntime()
        {
            Assert.True(true);
            return Task.CompletedTask;
        }

        [Test]
        public static async Task AsyncTaskTestWithNoRuntime()
        {
            Assert.True(true);
            await Task.CompletedTask;
        }

        [Test]
        public static Task TaskTestWithRuntime(ICoyoteRuntime runtime)
        {
            Assert.NotNull(runtime);
            return Task.CompletedTask;
        }

        [Test]
        public static async Task AsyncTaskTestWithRuntime(ICoyoteRuntime runtime)
        {
            Assert.NotNull(runtime);
            await Task.CompletedTask;
        }

        [Test]
        public static Task TaskTestWithActorRuntime(IActorRuntime runtime)
        {
            Assert.NotNull(runtime);
            return Task.CompletedTask;
        }

        [Test]
        public static async Task AsyncTaskTestWithActorRuntime(IActorRuntime runtime)
        {
            Assert.NotNull(runtime);
            await Task.CompletedTask;
        }

        public static class Foo
        {
            [Test]
            public static void VoidTest() => Assert.True(true);
        }

        public static class Bar
        {
            [Test]
            public static void VoidTest() => Assert.True(true);
        }
#pragma warning restore xUnit1013 // Public method should be marked as test

        [Fact(Timeout = 5000)]
        public void TestVoidEntryPoint() => this.CheckTestMethod(nameof(VoidTestWithNoRuntime));

        [Fact(Timeout = 5000)]
        public void TestVoidEntryPointWithRuntime() => this.CheckTestMethod(nameof(VoidTestWithRuntime));

        [Fact(Timeout = 5000)]
        public void TestVoidEntryPointWithActorRuntime() => this.CheckTestMethod(nameof(VoidTestWithActorRuntime));

        [Fact(Timeout = 5000)]
        public void TestTaskEntryPoint() => this.CheckTestMethod(nameof(TaskTestWithNoRuntime));

        [Fact(Timeout = 5000)]
        public void TestTaskEntryPointWithRuntime() => this.CheckTestMethod(nameof(TaskTestWithRuntime));

        [Fact(Timeout = 5000)]
        public void TestTaskEntryPointWithActorRuntime() => this.CheckTestMethod(nameof(TaskTestWithActorRuntime));

        [Fact(Timeout = 5000)]
        public void TestAsyncTaskEntryPoint() => this.CheckTestMethod(nameof(AsyncTaskTestWithNoRuntime));

        [Fact(Timeout = 5000)]
        public void TestAsyncTaskEntryPointWithRuntime() => this.CheckTestMethod(nameof(AsyncTaskTestWithRuntime));

        [Fact(Timeout = 5000)]
        public void TestAsyncTaskEntryPointWithActorRuntime() => this.CheckTestMethod(nameof(AsyncTaskTestWithActorRuntime));

        [Fact(Timeout = 5000)]
        public void TestUnspecifiedEntryPoint()
        {
            string name = string.Empty;
            var exception = Assert.Throws<InvalidOperationException>(() => this.CheckTestMethod(name));

            string possibleNames = GetPossibleTestNames();
            string expected = $"System.InvalidOperationException: Found '12' test methods declared with the " +
                $"'{typeof(TestAttribute).FullName}' attribute. Provide --method (-m) flag to qualify the test " +
                $"method that you want to run. {possibleNames}   at";
            string actual = exception.ToString();

            Assert.StartsWith(expected, actual);
        }

        [Fact(Timeout = 5000)]
        public void TestNotExistingEntryPoint()
        {
            string name = "NotExistingEntryPoint";
            var exception = Assert.Throws<InvalidOperationException>(() => this.CheckTestMethod(name));

            string possibleNames = GetPossibleTestNames();
            string expected = "System.InvalidOperationException: Cannot detect a Coyote test method name " +
                $"containing {name}. {possibleNames}   at";
            string actual = exception.ToString();

            Assert.StartsWith(expected, actual);
        }

        [Fact(Timeout = 5000)]
        public void TestAmbiguousEntryPoint()
        {
            string name = "VoidTest";
            var exception = Assert.Throws<InvalidOperationException>(() => this.CheckTestMethod(name));

            string possibleNames = GetPossibleTestNames(name);
            string expected = $"System.InvalidOperationException: The method name '{name}' is ambiguous. " +
                $"Please specify the full test method name. {possibleNames}   at";
            string actual = exception.ToString();

            Assert.StartsWith(expected, actual);
        }

        private void CheckTestMethod(string name)
        {
            Configuration config = this.GetConfiguration();
            config.AssemblyToBeAnalyzed = Assembly.GetExecutingAssembly().Location;
            config.TestMethodName = name;
            using var testMethodInfo = TestMethodInfo.Create(config);
            Assert.Equal(Assembly.GetExecutingAssembly(), testMethodInfo.Assembly);
            Assert.Equal($"{typeof(EntryPointTests).FullName}.{name}", testMethodInfo.Name);
        }

        private static string GetPossibleTestNames(string ambiguousName = null)
        {
            var testNames = new List<(string qualifier, string name)>()
            {
                (typeof(EntryPointTests).FullName, nameof(VoidTest)),
                (typeof(EntryPointTests).FullName, nameof(VoidTestWithNoRuntime)),
                (typeof(EntryPointTests).FullName, nameof(VoidTestWithRuntime)),
                (typeof(EntryPointTests).FullName, nameof(VoidTestWithActorRuntime)),
                (typeof(EntryPointTests).FullName, nameof(TaskTestWithNoRuntime)),
                (typeof(EntryPointTests).FullName, nameof(AsyncTaskTestWithNoRuntime)),
                (typeof(EntryPointTests).FullName, nameof(TaskTestWithRuntime)),
                (typeof(EntryPointTests).FullName, nameof(AsyncTaskTestWithRuntime)),
                (typeof(EntryPointTests).FullName, nameof(TaskTestWithActorRuntime)),
                (typeof(EntryPointTests).FullName, nameof(AsyncTaskTestWithActorRuntime)),
                (typeof(Foo).FullName, nameof(Foo.VoidTest)),
                (typeof(Bar).FullName, nameof(Bar.VoidTest))
            };

            string result = $"Possible methods are:{Environment.NewLine}";
            foreach (var testName in testNames)
            {
                if (ambiguousName is null || testName.name.Equals(ambiguousName) ||
                    $"{testName.qualifier}.{testName.name}".Equals(ambiguousName))
                {
                    result += $"  {testName.qualifier}.{testName.name}{Environment.NewLine}";
                }
            }

            return result;
        }
    }
}
