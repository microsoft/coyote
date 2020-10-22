// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Coyote.Rewriting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Configuration
{
    public class ConfigurationTests : BaseSystematicTest
    {
        public ConfigurationTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestJsonConfigurationReplacingBinaries()
        {
            string configDirectory = this.GetJsonConfigurationDirectory();
            string configPath = Path.Combine(configDirectory, "rewrite.coyote.json");
            Assert.True(File.Exists(configPath), "File not found: " + configPath);

            var options = RewritingOptions.ParseFromJSON(configPath);
            Assert.NotNull(options);
            options.PlatformVersion = GetPlatformVersion();

            Assert.Equal(configDirectory, options.AssembliesDirectory);
            Assert.Equal(Path.Combine(configDirectory), options.OutputDirectory);
            Assert.True(options.IsReplacingAssemblies);

            Assert.Equal(2, options.AssemblyPaths.Count());
            Assert.Equal(Path.Combine(options.AssembliesDirectory, "Microsoft.Coyote.Tests.SystematicTesting.dll"),
                options.AssemblyPaths.First());
        }

        [Fact(Timeout = 5000)]
        public void TestJsonConfigurationDifferentOutputDirectory()
        {
            string configDirectory = this.GetJsonConfigurationDirectory("Rewriting");
            string configPath = Path.Combine(configDirectory, "test.coyote.json");
            Assert.True(File.Exists(configPath));

            var options = RewritingOptions.ParseFromJSON(configPath);
            Assert.NotNull(options);
            options.PlatformVersion = GetPlatformVersion();

            Assert.Equal(Path.Combine(configDirectory, "Input"), options.AssembliesDirectory);
            Assert.Equal(Path.Combine(configDirectory, "Input", "Output"), options.OutputDirectory);
            Assert.False(options.IsReplacingAssemblies);

            Assert.Single(options.AssemblyPaths);
            Assert.Equal(Path.Combine(options.AssembliesDirectory, "Test.dll"), options.AssemblyPaths.First());
        }

        private string GetJsonConfigurationDirectory(string subDirectory = null)
        {
            string binaryDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string configDirectory = subDirectory is null ? binaryDirectory : Path.Combine(binaryDirectory, subDirectory);
            Assert.True(Directory.Exists(configDirectory), "Directory not found: " + configDirectory);
            return configDirectory;
        }

        /// <summary>
        /// Returns the .NET platform version this assembly was compiled for.
        /// </summary>
        private static string GetPlatformVersion()
        {
#if NET5_0
            return "net5.0";
#elif NET48
            return "net48";
#elif NET47
            return "net47";
#elif NETSTANDARD2_1
            return "netstandard2.1";
#elif NETSTANDARD2_0
            return "netstandard2.0";
#elif NETSTANDARD
            return "netstandard";
#elif NETCOREAPP3_1
            return "netcoreapp3.1";
#elif NETCOREAPP
            return "netcoreapp";
#elif NETFRAMEWORK
            return "net";
#endif
        }
    }
}
