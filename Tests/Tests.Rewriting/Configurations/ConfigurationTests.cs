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
            options = options.Sanitize();

            Assert.Equal(Path.Combine(configDirectory, "Input"), options.AssembliesDirectory);
            Assert.Equal(Path.Combine(configDirectory, "Input", "Output"), options.OutputDirectory);
            Assert.False(options.IsReplacingAssemblies());

            Assert.Single(options.AssemblyPaths);
            Assert.Equal(Path.Combine(options.AssembliesDirectory, "Test.dll"), options.AssemblyPaths.First());

            // Ensure this option defaults to true.
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
            this.X();
            this.TestOutput.WriteLine("x: " + options.AssembliesDirectory);
            options = options.Sanitize();

            this.TestOutput.WriteLine("expected: " + configDirectory);
            this.TestOutput.WriteLine("actual: " + options.AssembliesDirectory);

            Assert.Equal(configDirectory, options.AssembliesDirectory);
            Assert.Equal(configDirectory, options.OutputDirectory);
            Assert.True(options.IsReplacingAssemblies());

            Assert.Equal(2, options.AssemblyPaths.Count());
            Assert.Equal(Path.Combine(options.AssembliesDirectory, "Test1.dll"),
                options.AssemblyPaths.First());

            // Ensure this option can be set to false.
            Assert.False(options.IsRewritingConcurrentCollections);
        }

        private string X()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var targetFramework = assembly.GetCustomAttributes(typeof(System.Runtime.Versioning.TargetFrameworkAttribute), false)
                .SingleOrDefault() as System.Runtime.Versioning.TargetFrameworkAttribute;
            var tokens = targetFramework?.FrameworkName.Split(new string[] { ",Version=" }, System.StringSplitOptions.None);

            var resolvedFramework = "$(TargetFramework)";
            if (tokens != null && tokens.Length is 2)
            {
                this.TestOutput.WriteLine("TargetFramework: " + tokens[0]);
                if (tokens[0] == ".NETCoreApp")
                {
                    resolvedFramework = tokens[1] is "v6.0" ? "net6.0" :
                        tokens[1] is "v5.0" ? "net5.0" :
                        tokens[1] is "v3.1" ? "netcoreapp3.1" :
                        resolvedFramework;
                }
                else if (tokens[0] == ".NETStandard")
                {
                    resolvedFramework = tokens[1] is "v2.0" ? "netstandard2.0" : resolvedFramework;
                }
                else if (tokens[0] == ".NETFramework")
                {
                    resolvedFramework = tokens[1] is "v4.6.2" ? "net462" : resolvedFramework;
                }
            }

            return resolvedFramework;
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
