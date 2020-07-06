// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Xunit;
using Xunit.Abstractions;
using CoyoteTasks = Microsoft.Coyote.Tasks;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.SystematicTesting.Tests.Runtime
{
    public class EntryPointTests : BaseSystematicTest
    {
        public EntryPointTests(ITestOutputHelper output)
            : base(output)
        {
        }

#pragma warning disable xUnit1013 // Public method should be marked as test
        [Test]
        public static void VoidTest() => Assert.True(true);

        [Test]
        public static void VoidTestWithRuntime(ICoyoteRuntime runtime) => Assert.NotNull(runtime);

        [Test]
        public static void VoidTestWithActorRuntime(IActorRuntime runtime) => Assert.NotNull(runtime);

        [Test]
        public static SystemTasks.Task SystemTaskTest()
        {
            Assert.True(true);
            return SystemTasks.Task.CompletedTask;
        }

        [Test]
        public static async SystemTasks.Task AsyncSystemTaskTest()
        {
            Assert.True(true);
            await SystemTasks.Task.CompletedTask;
        }

        [Test]
        public static SystemTasks.Task SystemTaskTestWithRuntime(ICoyoteRuntime runtime)
        {
            Assert.NotNull(runtime);
            return SystemTasks.Task.CompletedTask;
        }

        [Test]
        public static async SystemTasks.Task AsyncSystemTaskTestWithRuntime(ICoyoteRuntime runtime)
        {
            Assert.NotNull(runtime);
            await SystemTasks.Task.CompletedTask;
        }

        [Test]
        public static SystemTasks.Task SystemTaskTestWithActorRuntime(IActorRuntime runtime)
        {
            Assert.NotNull(runtime);
            return SystemTasks.Task.CompletedTask;
        }

        [Test]
        public static async SystemTasks.Task AsyncSystemTaskTestWithActorRuntime(IActorRuntime runtime)
        {
            Assert.NotNull(runtime);
            await SystemTasks.Task.CompletedTask;
        }

        [Test]
        public static CoyoteTasks.Task CoyoteTaskTest()
        {
            Assert.True(true);
            return CoyoteTasks.Task.CompletedTask;
        }

        [Test]
        public static async CoyoteTasks.Task AsyncCoyoteTaskTest()
        {
            Assert.True(true);
            await CoyoteTasks.Task.CompletedTask;
        }

        [Test]
        public static CoyoteTasks.Task CoyoteTaskTestWithRuntime(ICoyoteRuntime runtime)
        {
            Assert.NotNull(runtime);
            return CoyoteTasks.Task.CompletedTask;
        }

        [Test]
        public static async CoyoteTasks.Task AsyncCoyoteTaskTestWithRuntime(ICoyoteRuntime runtime)
        {
            Assert.NotNull(runtime);
            await CoyoteTasks.Task.CompletedTask;
        }

        [Test]
        public static CoyoteTasks.Task CoyoteTaskTestWithActorRuntime(IActorRuntime runtime)
        {
            Assert.NotNull(runtime);
            return CoyoteTasks.Task.CompletedTask;
        }

        [Test]
        public static async CoyoteTasks.Task AsyncCoyoteTaskTestWithActorRuntime(IActorRuntime runtime)
        {
            Assert.NotNull(runtime);
            await CoyoteTasks.Task.CompletedTask;
        }
#pragma warning restore xUnit1013 // Public method should be marked as test

        [Fact(Timeout = 5000)]
        public void TestVoidEntryPoint() => CheckEntryPoint(nameof(VoidTest));

        [Fact(Timeout = 5000)]
        public void TestVoidEntryPointWithRuntime() => CheckEntryPoint(nameof(VoidTestWithRuntime));

        [Fact(Timeout = 5000)]
        public void TestVoidEntryPointWithActorRuntime() => CheckEntryPoint(nameof(VoidTestWithActorRuntime));

        [Fact(Timeout = 5000)]
        public void TestSystemTaskEntryPoint() => CheckEntryPoint(nameof(SystemTaskTest));

        [Fact(Timeout = 5000)]
        public void TestAsyncSystemTaskEntryPoint() => CheckEntryPoint(nameof(AsyncSystemTaskTest));

        [Fact(Timeout = 5000)]
        public void TestSystemTaskEntryPointWithRuntime() => CheckEntryPoint(nameof(SystemTaskTestWithRuntime));

        [Fact(Timeout = 5000)]
        public void TestAsyncSystemTaskEntryPointWithRuntime() => CheckEntryPoint(nameof(AsyncSystemTaskTestWithRuntime));

        [Fact(Timeout = 5000)]
        public void TestSystemTaskEntryPointWithActorRuntime() => CheckEntryPoint(nameof(SystemTaskTestWithActorRuntime));

        [Fact(Timeout = 5000)]
        public void TestAsyncSystemTaskEntryPointWithActorRuntime() => CheckEntryPoint(nameof(AsyncSystemTaskTestWithActorRuntime));

        [Fact(Timeout = 5000)]
        public void TestCoyoteTaskEntryPoint() => CheckEntryPoint(nameof(CoyoteTaskTest));

        [Fact(Timeout = 5000)]
        public void TestAsyncCoyoteTaskEntryPoint() => CheckEntryPoint(nameof(AsyncCoyoteTaskTest));

        [Fact(Timeout = 5000)]
        public void TestCoyoteTaskEntryPointWithRuntime() => CheckEntryPoint(nameof(CoyoteTaskTestWithRuntime));

        [Fact(Timeout = 5000)]
        public void TestAsyncCoyoteTaskEntryPointWithRuntime() => CheckEntryPoint(nameof(AsyncCoyoteTaskTestWithRuntime));

        [Fact(Timeout = 5000)]
        public void TestCoyoteTaskEntryPointWithActorRuntime() => CheckEntryPoint(nameof(CoyoteTaskTestWithActorRuntime));

        [Fact(Timeout = 5000)]
        public void TestAsyncCoyoteTaskEntryPointWithActorRuntime() => CheckEntryPoint(nameof(AsyncCoyoteTaskTestWithActorRuntime));

        private static void CheckEntryPoint(string name)
        {
            Configuration config = Configuration.Create();
            config.AssemblyToBeAnalyzed = Assembly.GetExecutingAssembly().Location;
            config.TestMethodName = name;
            var testMethodInfo = TestMethodInfo.Create(config);

            Assert.Equal(Assembly.GetExecutingAssembly(), testMethodInfo.Assembly);
            Assert.Equal($"{typeof(EntryPointTests).FullName}.{name}", testMethodInfo.Name);
        }
    }
}
