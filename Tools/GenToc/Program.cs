// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace GenToc
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                PrintUsage();
                return 1;
            }

            string original = args[0];
            string newtoc = args[1];

            if (!File.Exists(original))
            {
                Console.WriteLine("Missing mkdocs.yml file: " + original);
                return 2;
            }

            if (!File.Exists(newtoc))
            {
                Console.WriteLine("Missing generated toc: " + newtoc);
                return 3;
            }

            string dir = Path.GetDirectoryName(newtoc);
            FixXmlDocs(dir);

            return MergeToc(original, newtoc);
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: GenToc mkdocs.yml  new_toc.yml");
            Console.WriteLine("Merges the newly generated toc information into your mkdocs.yml nav section.");
            Console.WriteLine("The new_toc is in the format produced by 'xmldocmd.exe' and the");
            Console.WriteLine("mkdocs.yml nav section is in the format required by our website.");
            Console.WriteLine("Please provide full paths to each .yml file.");
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
                // Examine the new toc
                var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
                toc = deserializer.Deserialize<Toc>(reader);
            }

            // the mkdocs format is not ammenable to object serialization unfortunately...
            var root = (YamlMappingNode)mkdocs.Documents[0].RootNode;

            // Examine the stream
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

            // save the updated mkdocs
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
                    // leaf
                    var node = new YamlMappingNode();
                    node.Add(link.name, GetHref(link));
                    parent.Children.Add(node);
                    count++;
                }
                else
                {
                    // another sequence.
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

        private static void FixXmlDocs(string dir)
        {
            // the xmldocmd.exe tool is putting ".md.md" on some links, this fixes that bug.
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
