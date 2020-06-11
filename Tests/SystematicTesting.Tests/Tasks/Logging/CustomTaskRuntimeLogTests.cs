// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class CustomTaskRuntimeLogTests : BaseTest
    {
        public CustomTaskRuntimeLogTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool SystematicTest => true;

        [Fact(Timeout = 5000)]
        public void TestCustomLogger()
        {
            StringWriter log = new StringWriter();

            var config = Configuration.Create().WithVerbosityEnabled().WithTestingIterations(3);
            TestingEngine engine = TestingEngine.Create(config, (ICoyoteRuntime runtime) =>
            {
                runtime.Logger.WriteLine("Hi mom!");
            });

            engine.SetLogger(log);
            engine.Run();

            var result = log.ToString();
            result = result.RemoveNonDeterministicValues();
            var expected = @"... Task 0 is using 'random' strategy (seed:4005173804).
..... Iteration #1
<TestLog> Running test.
Hi mom!
..... Iteration #2
<TestLog> Running test.
Hi mom!
..... Iteration #3
<TestLog> Running test.
Hi mom!
";
            expected = expected.RemoveNonDeterministicValues();

            Assert.Equal(expected, result);
        }
    }
}
