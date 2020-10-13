// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BinaryRewriting.Tests.Tasks
{
    public class UncontrolledTaskTests : BaseProductionTest
    {
        public UncontrolledTaskTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledReadAllBytesAsync()
        {
            this.TestWithError(async () =>
            {
                string path = Assembly.GetExecutingAssembly().Location;
                await File.ReadAllBytesAsync(path);
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            errorChecker: (e) =>
            {
                Assert.True(e.StartsWith("Method 'System.IO.File.ReadAllBytesAsync' returned an uncontrolled task"),
                    "Expected uncontrolled task from invoking the async method.");
            },
            replay: true);
        }
    }
}
