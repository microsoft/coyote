// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using RewritingConfiguration = Microsoft.Coyote.Rewriting.Configuration;

namespace Microsoft.Coyote.BinaryRewriting.Tests.Tasks
{
    public class ConfigurationTests : BaseProductionTest
    {
        public ConfigurationTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestJsonConfigurationReplacingBinaries()
        {
            string configDirectory = this.GetJsonConfigurationDirectory();
            string configPath = Path.Combine(configDirectory, "BinaryRewritingTests.coyote.json");
            Assert.True(File.Exists(configPath));

            var config = RewritingConfiguration.ParseFromJSON(configPath);
            Assert.NotNull(config);
            config.PlatformVersion = GetPlatformVersion();

            Assert.Equal(configDirectory, config.AssembliesDirectory);
            Assert.Equal(Path.Combine(configDirectory), config.OutputDirectory);
            Assert.True(config.IsReplacingAssemblies);

            Assert.Single(config.AssemblyPaths);
            Assert.Equal(Path.Combine(config.AssembliesDirectory, "Microsoft.Coyote.BinaryRewriting.Tests.dll"), config.AssemblyPaths.First());
        }

        [Fact(Timeout = 5000)]
        public void TestJsonConfigurationDifferentOutputDirectory()
        {
            string configDirectory = this.GetJsonConfigurationDirectory("Configuration");
            string configPath = Path.Combine(configDirectory, "Test.coyote.json");
            Assert.True(File.Exists(configPath));

            var config = RewritingConfiguration.ParseFromJSON(configPath);
            Assert.NotNull(config);
            config.PlatformVersion = GetPlatformVersion();

            Assert.Equal(Path.Combine(configDirectory, "Input"), config.AssembliesDirectory);
            Assert.Equal(Path.Combine(configDirectory, "Input", "Output"), config.OutputDirectory);
            Assert.False(config.IsReplacingAssemblies);

            Assert.Single(config.AssemblyPaths);
            Assert.Equal(Path.Combine(config.AssembliesDirectory, "Test.dll"), config.AssemblyPaths.First());
        }

        private string GetJsonConfigurationDirectory(string subDirectory = null)
        {
            string binaryDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string configDirectory = subDirectory is null ? binaryDirectory : Path.Combine(binaryDirectory, subDirectory);
            Assert.True(Directory.Exists(configDirectory));
            return configDirectory;
        }

        /// <summary>
        /// Returns the .NET platform version this assembly was compiled for.
        /// </summary>
        private static string GetPlatformVersion()
        {
#if NETSTANDARD2_1
            return "netstandard2.1";
#elif NETSTANDARD2_0
            return "netstandard2.0";
#elif NETSTANDARD
            return "netstandard";
#elif NETCOREAPP3_1
            return "netcoreapp3.1";
#elif NETCOREAPP
            return "netcoreapp";
#elif NET48
            return "net48";
#elif NET47
            return "net47";
#elif NETFRAMEWORK
            return "net";
#endif
        }
    }
}
