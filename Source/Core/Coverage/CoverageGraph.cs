// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.Coyote.Coverage
{
    /// <summary>
    /// A directed graph made up of <see cref="CoverageGraph.Node"/> and <see cref="CoverageGraph.Link"/> objects.
    /// </summary>
    [DataContract]
    public class CoverageGraph
    {
        internal const string DgmlNamespace = "http://schemas.microsoft.com/vs/2009/dgml";

        [DataMember]
        private readonly Dictionary<string, Node> InternalNodes = new Dictionary<string, Node>();

        [DataMember]
        private readonly Dictionary<string, Link> InternalLinks = new Dictionary<string, Link>();

        /// <summary>
        /// Last used index for simple link key "a->b".
        /// </summary>
        [DataMember]
        private readonly Dictionary<string, int> InternalNextLinkIndex = new Dictionary<string, int>();

        /// <summary>
        /// Maps an augmented link key to the index that has been allocated for that link id "a->b(goto)" => 0.
        /// </summary>
        [DataMember]
        private readonly Dictionary<string, int> InternalAllocatedLinkIndexes = new Dictionary<string, int>();

        [DataMember]
        private readonly Dictionary<string, string> InternalAllocatedLinkIds = new Dictionary<string, string>();

        /// <summary>
        /// Returns the current list of nodes (in no particular order).
        /// </summary>
        public IEnumerable<Node> Nodes
        {
            get { return this.InternalNodes.Values; }
        }

        /// <summary>
        /// Returns the current list of links (in no particular order).
        /// </summary>
        public IEnumerable<Link> Links
        {
            get
            {
                if (this.InternalLinks is null)
                {
                    return Array.Empty<Link>();
                }

                return this.InternalLinks.Values;
            }
        }

        /// <summary>
        /// Gets an existing node or null.
        /// </summary>
        /// <param name="id">The id of the node.</param>
        public Node GetNode(string id)
        {
            this.InternalNodes.TryGetValue(id, out Node node);
            return node;
        }

        /// <summary>
        /// Gets an existing node or create a new one with the given id and label.
        /// </summary>
        /// <returns>Returns the new node or the existing node if it was already defined.</returns>
        public Node GetOrCreateNode(string id, string label = null, string category = null)
        {
            if (!this.InternalNodes.TryGetValue(id, out Node node))
            {
                node = new Node(id, label, category);
                this.InternalNodes.Add(id, node);
            }

            return node;
        }

        /// <summary>
        /// Gets an existing node or create a new one with the given id and label.
        /// </summary>
        /// <returns>Returns the new node or the existing node if it was already defined.</returns>
        private Node GetOrCreateNode(Node newNode)
        {
            if (!this.InternalNodes.ContainsKey(newNode.Id))
            {
                this.InternalNodes.Add(newNode.Id, newNode);
            }

            return newNode;
        }

        /// <summary>
        /// Gets an existing link or create a new one connecting the given source and target nodes.
        /// </summary>
        /// <returns>The new link or the existing link if it was already defined.</returns>
        public Link GetOrCreateLink(Node source, Node target, int? index = null, string linkLabel = null, string category = null)
        {
            string key = source.Id + "->" + target.Id;
            if (index.HasValue)
            {
                key += string.Format("({0})", index.Value);
            }

            if (!this.InternalLinks.TryGetValue(key, out Link link))
            {
                link = new Link(source, target, linkLabel, category);
                if (index.HasValue)
                {
                    link.Index = index.Value;
                }

                this.InternalLinks.Add(key, link);
            }

            return link;
        }

        internal int GetUniqueLinkIndex(Node source, Node target, string id)
        {
            // The augmented key.
            string key = string.Format("{0}->{1}({2})", source.Id, target.Id, id);
            if (this.InternalAllocatedLinkIndexes.TryGetValue(key, out int index))
            {
                return index;
            }

            // Allocate a new index for the simple key.
            var simpleKey = string.Format("{0}->{1}", source.Id, target.Id);
            if (this.InternalNextLinkIndex.TryGetValue(simpleKey, out index))
            {
                index++;
            }

            this.InternalNextLinkIndex[simpleKey] = index;

            // Remember this index has been allocated for this link id.
            this.InternalAllocatedLinkIndexes[key] = index;

            // Remember the original id associated with this link index.
            key = string.Format("{0}->{1}({2})", source.Id, target.Id, index);
            this.InternalAllocatedLinkIds[key] = id;

            return index;
        }

        internal void SaveDgml(string graphFilePath, bool includeDefaultStyles)
        {
            using StreamWriter writer = new StreamWriter(graphFilePath, false, Encoding.UTF8);
            this.WriteDgml(writer, includeDefaultStyles);
        }

        /// <summary>
        /// Serializes the <see cref="CoverageGraph"/> to DGML format.
        /// </summary>
        public void WriteDgml(TextWriter writer, bool includeDefaultStyles)
        {
            writer.WriteLine("<DirectedGraph xmlns='{0}'>", DgmlNamespace);
            writer.WriteLine("  <Nodes>");

            if (this.InternalNodes != null)
            {
                List<string> nodes = new List<string>(this.InternalNodes.Keys);
                nodes.Sort(StringComparer.Ordinal);
                foreach (var id in nodes)
                {
                    Node node = this.InternalNodes[id];
                    writer.Write("    <Node Id='{0}'", node.Id);

                    if (!string.IsNullOrEmpty(node.Label))
                    {
                        writer.Write(" Label='{0}'", node.Label);
                    }

                    if (!string.IsNullOrEmpty(node.Category))
                    {
                        writer.Write(" Category='{0}'", node.Category);
                    }

                    node.WriteAttributes(writer);
                    writer.WriteLine("/>");
                }
            }

            writer.WriteLine("  </Nodes>");
            writer.WriteLine("  <Links>");

            if (this.InternalLinks != null)
            {
                List<string> links = new List<string>(this.InternalLinks.Keys);
                links.Sort(StringComparer.Ordinal);
                foreach (var id in links)
                {
                    Link link = this.InternalLinks[id];
                    writer.Write("    <Link Source='{0}' Target='{1}'", link.Source.Id, link.Target.Id);
                    if (!string.IsNullOrEmpty(link.Label))
                    {
                        writer.Write(" Label='{0}'", link.Label);
                    }

                    if (!string.IsNullOrEmpty(link.Category))
                    {
                        writer.Write(" Category='{0}'", link.Category);
                    }

                    if (link.Index.HasValue)
                    {
                        writer.Write(" Index='{0}'", link.Index.Value);
                    }

                    link.WriteAttributes(writer);
                    writer.WriteLine("/>");
                }
            }

            writer.WriteLine("  </Links>");
            if (includeDefaultStyles)
            {
                writer.WriteLine(
@"  <Styles>
    <Style TargetType=""Node"" GroupLabel=""Error"" ValueLabel=""True"">
      <Condition Expression=""HasCategory('Error')"" />
      <Setter Property=""Background"" Value=""#FFC15656"" />
    </Style>
    <Style TargetType=""Node"" GroupLabel=""Actor"" ValueLabel=""True"">
      <Condition Expression=""HasCategory('Actor')"" />
      <Setter Property=""Background"" Value=""#FF57AC56"" />
    </Style>
    <Style TargetType=""Node"" GroupLabel=""Monitor"" ValueLabel=""True"">
      <Condition Expression=""HasCategory('Monitor')"" />
      <Setter Property=""Background"" Value=""#FF558FDA"" />
    </Style>
    <Style TargetType=""Link"" GroupLabel=""halt"" ValueLabel=""True"">
      <Condition Expression=""HasCategory('halt')"" />
      <Setter Property=""Stroke"" Value=""#FFFF6C6C"" />
      <Setter Property=""StrokeDashArray"" Value=""4 2"" />
    </Style>
    <Style TargetType=""Link"" GroupLabel=""push"" ValueLabel=""True"">
      <Condition Expression=""HasCategory('push')"" />
      <Setter Property=""Stroke"" Value=""#FF7380F5"" />
      <Setter Property=""StrokeDashArray"" Value=""4 2"" />
    </Style>
    <Style TargetType=""Link"" GroupLabel=""pop"" ValueLabel=""True"">
      <Condition Expression=""HasCategory('pop')"" />
      <Setter Property=""Stroke"" Value=""#FF7380F5"" />
      <Setter Property=""StrokeDashArray"" Value=""4 2"" />
    </Style>
  </Styles>");
            }

            writer.WriteLine("</DirectedGraph>");
        }

        /// <summary>
        /// Loads a DGML formatted file into a new <see cref="CoverageGraph"/> object.
        /// </summary>
        /// <param name="graphFilePath">Full path to the DGML file.</param>
        /// <returns>The loaded <see cref="CoverageGraph"/> object.</returns>
        public static CoverageGraph LoadDgml(string graphFilePath)
        {
            XDocument doc = XDocument.Load(graphFilePath);
            CoverageGraph result = new CoverageGraph();
            var ns = doc.Root.Name.Namespace;
            if (ns != DgmlNamespace)
            {
                throw new InvalidOperationException(string.Format(
                    "File '{0}' does not contain the DGML namespace", graphFilePath));
            }

            foreach (var e in doc.Root.Element(ns + "Nodes").Elements(ns + "Node"))
            {
                var id = (string)e.Attribute("Id");
                var label = (string)e.Attribute("Label");
                var category = (string)e.Attribute("Category");

                Node node = new Node(id, label, category);
                node.AddDgmlProperties(e);
                result.GetOrCreateNode(node);
            }

            foreach (var e in doc.Root.Element(ns + "Links").Elements(ns + "Link"))
            {
                var srcId = (string)e.Attribute("Source");
                var targetId = (string)e.Attribute("Target");
                var label = (string)e.Attribute("Label");
                var category = (string)e.Attribute("Category");
                var srcNode = result.GetOrCreateNode(srcId);
                var targetNode = result.GetOrCreateNode(targetId);
                XAttribute indexAttr = e.Attribute("index");
                int? index = null;
                if (indexAttr != null)
                {
                    index = (int)indexAttr;
                }

                var link = result.GetOrCreateLink(srcNode, targetNode, index, label, category);
                link.AddDgmlProperties(e);
            }

            return result;
        }

        /// <summary>
        /// Merges the given <see cref="CoverageGraph"/> so that this <see cref="CoverageGraph"/> becomes a superset of both graphs.
        /// </summary>
        /// <param name="other">The new <see cref="CoverageGraph"/> to merge into this <see cref="CoverageGraph"/>.</param>
        public void Merge(CoverageGraph other)
        {
            foreach (var node in other.InternalNodes.Values)
            {
                var newNode = this.GetOrCreateNode(node.Id, node.Label, node.Category);
                newNode.Merge(node);
            }

            foreach (var link in other.InternalLinks.Values)
            {
                var source = this.GetOrCreateNode(link.Source.Id, link.Source.Label, link.Source.Category);
                var target = this.GetOrCreateNode(link.Target.Id, link.Target.Label, link.Target.Category);
                int? index = null;
                if (link.Index.HasValue)
                {
                    // ouch, link indexes cannot be compared across graph instances, we need to assign a new index here.
                    string key = string.Format("{0}->{1}({2})", source.Id, target.Id, link.Index.Value);
                    string linkId = other.InternalAllocatedLinkIds[key];
                    index = this.GetUniqueLinkIndex(source, target, linkId);
                }

                var newLink = this.GetOrCreateLink(source, target, index, link.Label, link.Category);
                newLink.Merge(link);
            }
        }

        /// <summary>
        /// Serialize the <see cref="CoverageGraph"/> to a DGML formatted string.
        /// </summary>
        public override string ToString()
        {
            using var writer = new StringWriter();
            this.WriteDgml(writer, false);
            return writer.ToString();
        }

        /// <summary>
        /// A <see cref="CoverageGraph"/> object.
        /// </summary>
        [DataContract]
        public class Object
        {
            /// <summary>
            /// Optional list of attributes for the node.
            /// </summary>
            [DataMember]
            public Dictionary<string, string> Attributes { get; internal set; }

            /// <summary>
            /// Optional list of attributes that have a multi-part value.
            /// </summary>
            [DataMember]
            public Dictionary<string, HashSet<string>> AttributeLists { get; internal set; }

            /// <summary>
            /// Adds an attribute to the node.
            /// </summary>
            public void AddAttribute(string name, string value)
            {
                if (this.Attributes is null)
                {
                    this.Attributes = new Dictionary<string, string>();
                }

                this.Attributes[name] = value;
            }

            /// <summary>
            /// Creates a compound attribute value containing a merged list of unique values.
            /// </summary>
            /// <param name="key">The attribute name.</param>
            /// <param name="value">The new value to add to the unique list.</param>
            public int AddListAttribute(string key, string value)
            {
                if (this.AttributeLists is null)
                {
                    this.AttributeLists = new Dictionary<string, HashSet<string>>();
                }

                if (!this.AttributeLists.TryGetValue(key, out HashSet<string> list))
                {
                    list = new HashSet<string>();
                    this.AttributeLists[key] = list;
                }

                list.Add(value);
                return list.Count;
            }

            internal void WriteAttributes(TextWriter writer)
            {
                if (this.Attributes != null)
                {
                    // Creates a more stable output file (can be handy for expected output during testing).
                    List<string> names = new List<string>(this.Attributes.Keys);
                    names.Sort(StringComparer.Ordinal);
                    foreach (string name in names)
                    {
                        var value = this.Attributes[name];
                        writer.Write(" {0}='{1}'", name, value);
                    }
                }

                if (this.AttributeLists != null)
                {
                    // Creates a more stable output file (can be handy for expected output during testing).
                    List<string> names = new List<string>(this.AttributeLists.Keys);
                    names.Sort(StringComparer.Ordinal);
                    foreach (string name in names)
                    {
                        var value = this.AttributeLists[name];
                        writer.Write(" {0}='{1}'", name, string.Join(",", value));
                    }
                }
            }

            internal void Merge(Object other)
            {
                if (other.Attributes != null)
                {
                    foreach (var key in other.Attributes.Keys)
                    {
                        this.AddAttribute(key, other.Attributes[key]);
                    }
                }

                if (other.AttributeLists != null)
                {
                    foreach (var key in other.AttributeLists.Keys)
                    {
                        foreach (var value in other.AttributeLists[key])
                        {
                            this.AddListAttribute(key, value);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// A node of a <see cref="CoverageGraph"/>.
        /// </summary>
        [DataContract]
        public class Node : Object
        {
            /// <summary>
            /// The unique id of the node within the <see cref="CoverageGraph"/>.
            /// </summary>
            [DataMember]
            public string Id { get; internal set; }

            /// <summary>
            /// An optional display label for the node (does not need to be unique).
            /// </summary>
            [DataMember]
            public string Label { get; internal set; }

            /// <summary>
            /// An optional category for the node.
            /// </summary>
            [DataMember]
            public string Category { get; internal set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Node"/> class.
            /// </summary>
            public Node(string id, string label, string category)
            {
                this.Id = id;
                this.Label = label;
                this.Category = category;
            }

            /// <summary>
            /// Adds additional properties from XML element.
            /// </summary>
            /// <param name="e">An XML element representing the graph node in DGML format.</param>
            public void AddDgmlProperties(XElement e)
            {
                foreach (XAttribute a in e.Attributes())
                {
                    switch (a.Name.LocalName)
                    {
                        case "Id":
                        case "Label":
                        case "Category":
                            break;
                        default:
                            this.AddAttribute(a.Name.LocalName, a.Value);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// A link represents a directed <see cref="CoverageGraph"/> connection between two <see cref="Node"/> objects.
        /// </summary>
        [DataContract]
        public class Link : Object
        {
            /// <summary>
            /// An optional display label for the link.
            /// </summary>
            [DataMember]
            public string Label { get; internal set; }

            /// <summary>
            /// An optional category for the link. The special category "Contains" is reserved for building groups.
            /// </summary>
            [DataMember]
            public string Category { get; internal set; }

            /// <summary>
            /// The source end of the link.
            /// </summary>
            [DataMember]
            public Node Source { get; internal set; }

            /// <summary>
            /// The target end of the link.
            /// </summary>
            [DataMember]
            public Node Target { get; internal set; }

            /// <summary>
            /// The optional link index.
            /// </summary>
            [DataMember]
            public int? Index { get; internal set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Link"/> class.
            /// </summary>
            public Link(Node source, Node target, string label, string category)
            {
                this.Source = source;
                this.Target = target;
                this.Label = label;
                this.Category = category;
            }

            /// <summary>
            /// Adds additional properties from XML element.
            /// </summary>
            /// <param name="e">An XML element representing the graph node in DGML format.</param>
            public void AddDgmlProperties(XElement e)
            {
                foreach (XAttribute a in e.Attributes())
                {
                    switch (a.Name.LocalName)
                    {
                        case "Source":
                        case "Target":
                        case "Label":
                        case "Category":
                            break;
                        default:
                            this.AddAttribute(a.Name.LocalName, a.Value);
                            break;
                    }
                }
            }
        }
    }
}
