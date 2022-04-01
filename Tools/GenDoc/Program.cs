// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using XmlDocMarkdown.Core;
using YamlDotNet.RepresentationModel;

namespace Microsoft.Coyote.GenDoc
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var rootCommand = new RootCommand("The Coyote documentation generator.");
            var generateCommand = CreateGenerateCommand();
            var mergeCommand = CreateMergeCommand();

            rootCommand.AddCommand(generateCommand);
            rootCommand.AddCommand(mergeCommand);
            rootCommand.TreatUnmatchedTokensAsErrors = true;

            return rootCommand.Invoke(args);
        }

        /// <summary>
        /// Creates the generate command.
        /// </summary>
        private static Command CreateGenerateCommand()
        {
            var pathArg = new Argument<string>("path", $"Path to the assembly (*.dll) to generate documentation.")
            {
                HelpName = "PATH"
            };

            var outputOption = new Option<string>(
                name: "-o",
                description: "The output path.")
            {
                ArgumentHelpName = "OUTPUT"
            };

            var namespaceOption = new Option<string>(
                name: "--namespace",
                description: "The root namespace of the input assembly.")
            {
                ArgumentHelpName = "NAMESPACE"
            };

            // Add validators.
            pathArg.AddValidator(result => ValidateArgumentValueIsExpectedFile(result, ".dll"));

            // Build command.
            var command = new Command("gen", "Generate the documentation.");
            command.AddArgument(pathArg);
            command.AddOption(outputOption);
            command.AddOption(namespaceOption);
            command.TreatUnmatchedTokensAsErrors = true;

            command.SetHandler((string assembly, string output, string name) =>
            {
                var settings = new XmlDocMarkdownSettings()
                {
                    GenerateToc = true,
                    TocPrefix = "ref",
                    RootNamespace = name,
                    VisibilityLevel = XmlDocVisibilityLevel.Protected,
                    SkipUnbrowsable = true,
                    NamespacePages = true
                };

                XmlDocMarkdownGenerator.Generate(assembly, output, settings);
            }, pathArg, outputOption, namespaceOption);

            return command;
        }

        /// <summary>
        /// Creates the merge command.
        /// </summary>
        private static Command CreateMergeCommand()
        {
            var sourceTocArg = new Argument<string>("src", $"Path to the source mkdocs.yml file.")
            {
                HelpName = "SRC_TOC"
            };

            var destinationTocArg = new Argument<string>("dst", $"Path to the destination toc.yml file.")
            {
                HelpName = "DST_TOC"
            };

            // Add validators.
            sourceTocArg.AddValidator(result => ValidateArgumentValueIsExpectedFile(result, ".yml"));
            destinationTocArg.AddValidator(result => ValidateArgumentValueIsExpectedFile(result, ".yml"));

            // Build command.
            var command = new Command("merge", "Merges the ToC information into the mkdocs.yml nav section.");
            command.AddArgument(sourceTocArg);
            command.AddArgument(destinationTocArg);
            command.TreatUnmatchedTokensAsErrors = true;

            command.SetHandler((string src, string dst) =>
            {
                FixXmlDocs(Path.GetDirectoryName(dst));
                int result = MergeToc(src, dst);
                Environment.ExitCode = result;
            }, sourceTocArg, destinationTocArg);

            return command;
        }

        private static void FixXmlDocs(string dir)
        {
            // The xmldocmd.exe tool is putting ".md.md" on some links, this fixes that bug.
            foreach (var file in Directory.GetFiles(dir, "*.md"))
            {
                FixXmlDoc(file);
            }

            foreach (var child in Directory.GetDirectories(dir))
            {
                FixXmlDocs(child);
            }
        }

        private static void FixXmlDoc(string filename)
        {
            string text = File.ReadAllText(filename);
            string correct = text.Replace(".md.md", ".md");
            if (correct != text)
            {
                Console.WriteLine("Fixing " + filename);
                File.WriteAllText(filename, correct);
            }
        }

        private static int MergeToc(string mkdocsPath, string newTocPath)
        {
            var mkdocs = new YamlStream();

            using (var reader = new StreamReader(mkdocsPath))
            {
                mkdocs.Load(reader);
            }

            Toc toc = null;
            using (var reader = new StreamReader(newTocPath))
            {
                // Examine the new toc.
                var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
                toc = deserializer.Deserialize<Toc>(reader);
            }

            // The mkdocs format is not amenable to object serialization.
            var root = (YamlMappingNode)mkdocs.Documents[0].RootNode;

            // Examine the stream.
            var items = (YamlSequenceNode)root.Children[new YamlScalarNode("nav")];

            var apitoc = items.Where(it => it is YamlMappingNode ym && ym.Children.Count > 0 &&
                                    ym.Children[0].Key is YamlScalarNode s && s.Value == "API documentation").FirstOrDefault() as YamlMappingNode;
            if (apitoc == null)
            {
                apitoc = new YamlMappingNode();
                items.Add(apitoc);
            }

            apitoc.Children.Clear();
            var s = new YamlSequenceNode();
            apitoc.Add(new YamlScalarNode("API documentation"), s);
            int count = AddLinks(s, toc.toc);

            // Save the updated mkdocs.
            using (var writer = new StreamWriter(mkdocsPath))
            {
                mkdocs.Save(writer, false);
            }

            Console.WriteLine("Added {0} api documentation links to the nav: section in {1}", count, mkdocsPath);

            return 0;
        }

        private static string GetHref(Link link)
        {
            var href = link.link;
            if (!href.EndsWith(".md"))
            {
                href += ".md";
            }

            return href;
        }

        private static int AddLinks(YamlSequenceNode parent, Link[] links)
        {
            int count = 0;
            foreach (var link in links)
            {
                if (link.subfolderitems == null || link.subfolderitems.Length == 0)
                {
                    var node = new YamlMappingNode();
                    node.Add(link.name, GetHref(link));
                    parent.Children.Add(node);
                    count++;
                }
                else
                {
                    var node = new YamlMappingNode();
                    var s = new YamlSequenceNode();
                    var href = GetHref(link);

                    node.Add(link.name, s);
                    parent.Children.Add(node);
                    var overview = new YamlMappingNode();
                    if (href.EndsWith("Assembly.md"))
                    {
                        overview.Add("Assembly Overview", href);
                    }
                    else if (href.EndsWith("Namespace.md"))
                    {
                        overview.Add("Namespace Overview", href);
                    }
                    else if (href.EndsWith("Type.md"))
                    {
                        overview.Add("Type Overview", href);
                    }
                    else
                    {
                        overview.Add("Overview", href);
                    }

                    s.Add(overview);
                    count += AddLinks(s, link.subfolderitems) + 1;
                }
            }

            return count;
        }

        /// <summary>
        /// Validates that the specified argument result is found and has an expected file extension.
        /// </summary>
        private static void ValidateArgumentValueIsExpectedFile(ArgumentResult result, params string[] extensions)
        {
            string fileName = result.GetValueOrDefault<string>();
            string foundExtension = Path.GetExtension(fileName);
            if (!extensions.Any(extension => extension == foundExtension))
            {
                if (extensions.Length is 1)
                {
                    result.ErrorMessage = $"File '{fileName}' does not have the expected '{extensions[0]}' extension.";
                }
                else
                {
                    result.ErrorMessage = $"File '{fileName}' does not have one of the expected extensions: " +
                        $"{string.Join(", ", extensions)}.";
                }
            }
            else if (!File.Exists(fileName))
            {
                result.ErrorMessage = $"File '{fileName}' does not exist.";
            }
        }

#pragma warning disable SA1300 // Element should begin with upper-case letter

        public class Toc
        {
            public Link[] toc { get; set; }
        }

        public class Link
        {
            public string name { get; set; }
            public string link { get; set; }
            public Link[] subfolderitems { get; set; }
        }
#pragma warning restore SA1300 // Element should begin with upper-case letter
    }
}
