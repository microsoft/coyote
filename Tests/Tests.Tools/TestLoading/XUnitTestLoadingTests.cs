// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.Coyote.SystematicTesting.Frameworks.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Tools.Tests
{
    public class XUnitTestLoadingTests : BaseToolsTest
    {
        public XUnitTestLoadingTests(ITestOutputHelper output)
            : base(output)
        {
        }

#pragma warning disable CA1822 // Mark members as static
#pragma warning disable xUnit1013 // Public method should be marked as test
        [Test]
        public void VoidTest() => Assert.True(true);

        [Test]
        public Task TaskTest()
        {
            Assert.True(true);
            return Task.CompletedTask;
        }
#pragma warning restore xUnit1013 // Public method should be marked as test
#pragma warning restore CA1822 // Mark members as static

        [Fact(Timeout = 5000)]
        public void TestVoidEntryPoint() => this.CheckTestMethod(nameof(this.VoidTest));

        [Fact(Timeout = 5000)]
        public void TestTaskEntryPoint() => this.CheckTestMethod(nameof(this.TaskTest));

        private void CheckTestMethod(string name)
        {
            Configuration config = this.GetConfiguration();
            config.AssemblyToBeAnalyzed = Assembly.GetExecutingAssembly().Location;
            config.TestMethodName = name;
            var logWriter = new LogWriter(config);
            logWriter.SetLogger(new TestOutputLogger(this.TestOutput));
            using var testMethodInfo = TestMethodInfo.Create(config, logWriter);
            Assert.Equal(Assembly.GetExecutingAssembly(), testMethodInfo.Assembly);
            Assert.Equal($"{typeof(XUnitTestLoadingTests).FullName}.{name}", testMethodInfo.Name);
            if (testMethodInfo.Method is Action action)
            {
                action();
            }
            else if (testMethodInfo.Method is Func<Task> function)
            {
                function();
            }
            else
            {
                Assert.True(false, "Unexpected test method type.");
            }
        }
    }
}
