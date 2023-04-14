// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests.Configuration
{
    public class ConfigurationTests : BaseRewritingTest
    {
        public ConfigurationTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestJsonConfigurationDifferentOutputDirectory()
        {
            string configDirectory = GetJsonConfigurationDirectory("Configurations");
            string configPath = Path.Combine(configDirectory, "test1.coyote.json");
            Assert.True(File.Exists(configPath));

            var options = RewritingOptions.ParseFromJSON(configPath);
            Assert.NotNull(options);
            options = options.Sanitize(Assembly.GetExecutingAssembly());

            Assert.Equal(Path.Combine(configDirectory, "Input"), options.AssembliesDirectory);
            Assert.Equal(Path.Combine(configDirectory, "Input", "Output"), options.OutputDirectory);
            Assert.False(options.IsReplacingAssemblies());

            Assert.Single(options.AssemblyPaths);
            Assert.Equal(Path.Combine(options.AssembliesDirectory, "Test.dll"), options.AssemblyPaths.First());

            // Ensure these options defaults to true.
            Assert.True(options.IsRewritingMemoryLocations);
            Assert.True(options.IsRewritingConcurrentCollections);
        }

        [Fact(Timeout = 5000)]
        public void TestJsonConfigurationReplacingBinaries()
        {
            string configDirectory = GetJsonConfigurationDirectory("Configurations");
            string configPath = Path.Combine(configDirectory, "test2.coyote.json");
            Assert.True(File.Exists(configPath), "File not found: " + configPath);

            var options = RewritingOptions.ParseFromJSON(configPath);
            Assert.NotNull(options);
            options = options.Sanitize(Assembly.GetExecutingAssembly());

            this.TestOutput.WriteLine("expected: " + configDirectory);
            this.TestOutput.WriteLine("actual: " + options.AssembliesDirectory);

            Assert.Equal(configDirectory, options.AssembliesDirectory);
            Assert.Equal(configDirectory, options.OutputDirectory);
            Assert.True(options.IsReplacingAssemblies());

            Assert.Equal(2, options.AssemblyPaths.Count());
            Assert.Equal(Path.Combine(options.AssembliesDirectory, "Test1.dll"),
                options.AssemblyPaths.First());

            // Ensure these options can be set to false.
            Assert.False(options.IsRewritingMemoryLocations);
            Assert.False(options.IsRewritingConcurrentCollections);
        }

        private static string GetJsonConfigurationDirectory(string subDirectory = null)
        {
            string binaryDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string configDirectory = subDirectory is null ? binaryDirectory : Path.Combine(binaryDirectory, subDirectory);
            Assert.True(Directory.Exists(configDirectory), "Directory not found: " + configDirectory);
            return configDirectory;
        }
    }
}
