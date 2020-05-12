// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Random;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.IO;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Production.Tests.Tasks
{
    public class CustomTaskRuntimeLogTests : BaseProductionTest
    {
        public CustomTaskRuntimeLogTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestCustomLogger()
        {
            CustomLogger logger = new CustomLogger();
            Configuration config = Configuration.Create().WithVerbosityEnabled();

            ICoyoteRuntime runtime = RuntimeFactory.Create(config);
            runtime.SetLogger(logger);

            Generator generator = Generator.Create();

            Task task = Task.Run(() =>
            {
                int result = generator.NextInteger(10);
                logger.WriteLine($"Task '{Task.CurrentId}' completed with result '{result}'.");
            });

            await task;

            string expected = @"<RandomLog> Task '' nondeterministically chose ''. Task '' completed with result ''.";
            string actual = RemoveNonDeterministicValuesFromReport(logger.ToString());
            Assert.Equal(expected, actual);

            logger.Dispose();
        }
    }
}
