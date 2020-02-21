// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Linq;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Runtime.Exploration;
using Microsoft.Coyote.TestingServices.Timers;

namespace Microsoft.Coyote.TestingServices.Coverage
{
    /// <summary>
    /// Implements the <see cref="IActorRuntimeLog"/> and builds a directed graph
    /// from the recorded events and state transitions.
    /// </summary>
    public class ActorRuntimeLogGraphBuilder : IActorRuntimeLog
    {
        private Graph CurrentGraph;
        private EventInfo Dequeued; // current dequeued event.
        private string HaltedState;

        private class EventInfo
        {
            public ActorId ActorId;
            public string State;
            public string Event;
        }

        private readonly Dictionary<string, List<EventInfo>> Inbox = new Dictionary<string, List<EventInfo>>();
        private static readonly Dictionary<string, string> EventAliases = new Dictionary<string, string>();
        private readonly HashSet<string> Namespaces = new HashSet<string>();
        private readonly Dictionary<ActorId, string> CurrentStates = new Dictionary<ActorId, string>();

        static ActorRuntimeLogGraphBuilder()
        {
            EventAliases[typeof(GotoStateEvent).FullName] = "goto";
            EventAliases[typeof(HaltEvent).FullName] = "halt";
            EventAliases[typeof(DefaultEvent).FullName] = "default";
            EventAliases[typeof(PushStateEvent).FullName] = "push";
            EventAliases[typeof(QuiescentEvent).FullName] = "quiescent";
            EventAliases[typeof(WildCardEvent).FullName] = "*";
            EventAliases[typeof(TimerElapsedEvent).FullName] = "timer_elapsed";
            EventAliases[typeof(TimerSetupEvent).FullName] = "timer_setup";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorRuntimeLogGraphBuilder"/> class.
        /// </summary>
        public ActorRuntimeLogGraphBuilder()
        {
            this.CurrentGraph = new Graph();
        }

        /// <summary>
        /// Set this boolean to true to get a collapsed graph showing only
        /// machine types, states and events.  This will not show machine "instances".
        /// </summary>
        public bool CollapseMachineInstances { get; set; }

        /// <summary>
        /// Get or set the underlying logging object.
        /// </summary>
        /// <remarks>
        /// See <see href="/coyote/learn/advanced/logging" >Logging</see> for more information.
        /// </remarks>
        public TextWriter Logger { get; set; }

        /// <summary>
        /// Get the Graph object built by this logger.
        /// </summary>
        public Graph Graph
        {
            get
            {
                if (this.CurrentGraph == null)
                {
                    this.CurrentGraph = new Graph();
                }

                return this.CurrentGraph;
            }
        }

        /// <inheritdoc/>
        public void OnCreateActor(ActorId id, ActorId creator)
        {
            var resolvedId = this.GetResolveActorId(id);
            this.Graph.GetOrCreateNode(resolvedId);
        }

        /// <inheritdoc/>
        public void OnExecuteAction(ActorId id, string stateName, string actionName)
        {
        }

        /// <inheritdoc/>
        public void OnSendEvent(ActorId targetActorId, ActorId senderId, string senderStateName, Event e,
            Guid opGroupId, bool isTargetHalted)
        {
            string eventName = e.GetType().FullName;
            this.AddEvent(targetActorId, senderId, senderStateName, eventName);
        }

        /// <inheritdoc/>
        public void OnRaiseEvent(ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            // Raising event to self.
            this.AddEvent(id, id, stateName, eventName);
        }

        /// <inheritdoc/>
        public void OnEnqueueEvent(ActorId id, Event e)
        {
        }

        /// <inheritdoc/>
        public void OnDequeueEvent(ActorId id, string stateName, Event e)
        {
            var resolvedId = this.GetResolveActorId(id);
            if (this.Inbox.TryGetValue(resolvedId, out List<EventInfo> inbox))
            {
                string eventName = e.GetType().FullName;
                for (int i = inbox.Count - 1; i >= 0; i--)
                {
                    EventInfo info = inbox[i];
                    if (info.Event == eventName)
                    {
                        // Yay, found it so we can draw the complete link connecting the Sender state to this state!
                        var source = this.GetOrCreateChild(info.ActorId, info.State);
                        var target = this.GetOrCreateChild(id, this.GetLabel(id, stateName));
                        this.GetOrCreateEventLink(source, target, info);
                        inbox.RemoveAt(i);
                        this.Dequeued = info;
                        break;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void OnReceiveEvent(ActorId id, string stateName, Event e, bool wasBlocked)
        {
            string resolvedId = this.GetResolveActorId(id);
            if (this.Inbox.TryGetValue(resolvedId, out List<EventInfo> inbox))
            {
                string eventName = e.GetType().FullName;
                for (int i = inbox.Count - 1; i >= 0; i--)
                {
                    EventInfo info = inbox[i];
                    if (info.Event == eventName)
                    {
                        // Yay, found it so we can draw the complete link connecting the Sender state to this state!
                        var source = this.GetOrCreateChild(info.ActorId, info.State);
                        var target = this.GetOrCreateChild(id, this.GetLabel(id, stateName));
                        this.GetOrCreateEventLink(source, target, info);
                        inbox.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void OnWaitEvent(ActorId id, string stateName, Type eventType)
        {
        }

        /// <inheritdoc/>
        public void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
        {
        }

        /// <inheritdoc/>
        public void OnRandom(ActorId id, object result)
        {
        }

        /// <inheritdoc/>
        public void OnStateTransition(ActorId id, string stateName, bool isEntry)
        {
        }

        /// <inheritdoc/>
        public void OnGotoState(ActorId id, string currStateName, string newStateName)
        {
            this.LinkTransition("goto", id, currStateName, newStateName, false);
        }

        /// <inheritdoc/>
        public void OnPushState(ActorId id, string currStateName, string newStateName)
        {
            this.LinkTransition("push", id, currStateName, newStateName, true);
        }

        /// <inheritdoc/>
        public void OnPopState(ActorId id, string currStateName, string restoredStateName)
        {
            if (!string.IsNullOrEmpty(currStateName))
            {
                this.LinkTransition("pop", id, currStateName, restoredStateName, true);
            }
        }

        /// <inheritdoc/>
        public void OnHalt(ActorId id, int inboxSize)
        {
            string stateName = this.HaltedState;
            if (string.IsNullOrEmpty(stateName))
            {
                stateName = "null";
            }

            // Transition to the Halt state.
            var source = this.GetOrCreateChild(id, this.GetLabel(id, stateName));
            var target = this.GetOrCreateChild(id, "Halt");
            this.Graph.GetOrCreateLink(source, target, "halt");
        }

        /// <inheritdoc/>
        public void OnDefaultEventHandler(ActorId id, string stateName)
        {
        }

        /// <inheritdoc/>
        public void OnHandleRaisedEvent(ActorId id, string stateName, Event e)
        {
            // We used the inbox to store raised event, but it should be the first one handled since
            // raised events are highest priority.
            string resolvedId = this.GetResolveActorId(id);
            if (this.Inbox.TryGetValue(resolvedId, out List<EventInfo> inbox))
            {
                string eventName = e.GetType().FullName;
                for (int i = inbox.Count - 1; i >= 0; i--)
                {
                    EventInfo info = inbox[i];
                    if (info.Event == eventName)
                    {
                        this.Dequeued = info;
                        break;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void OnPopStateUnhandledEvent(ActorId actorId, string currStateName, Event e)
        {
            if (e is HaltEvent)
            {
                this.HaltedState = currStateName;
            }
        }

        /// <inheritdoc/>
        public void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
        {
        }

        /// <inheritdoc/>
        public void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
        {
        }

        /// <inheritdoc/>
        public void OnCreateTimer(TimerInfo info)
        {
            // TODO: figure out how to graph timers when we have no "timer id" at this point...
        }

        /// <inheritdoc/>
        public void OnStopTimer(TimerInfo info)
        {
        }

        /// <inheritdoc/>
        public void OnCreateMonitor(string monitorTypeName, ActorId id)
        {
            string resolvedId = this.GetResolveActorId(id);
            this.Graph.GetOrCreateNode(resolvedId, monitorTypeName);
        }

        /// <inheritdoc/>
        public void OnMonitorExecuteAction(string monitorTypeName, ActorId id, string stateName, string actionName)
        {
            this.CurrentStates[id] = stateName;
            string resolvedId = this.GetResolveActorId(id);
            // Monitors process actions immediately, so this state transition is a result of the only event in the inbox.
            if (this.Inbox.TryGetValue(resolvedId, out List<EventInfo> inbox) && inbox.Count > 0)
            {
                var e = inbox[inbox.Count - 1];
                inbox.RemoveAt(inbox.Count - 1);
                // Draw the link connecting the Sender state to this state!
                var source = this.GetOrCreateChild(e.ActorId, e.State);
                var target = this.GetOrCreateChild(id, this.GetLabel(id, stateName));
                this.GetOrCreateEventLink(source, target, e);
            }
        }

        /// <inheritdoc/>
        public void OnMonitorProcessEvent(ActorId senderId, string senderStateName, string monitorTypeName,
            ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            // If sender is null then it means we are dealing with a Monitor call from external code.
            var info = this.AddEvent(id, senderId, senderStateName, eventName);

            // Draw the link connecting the Sender state to this state!
            var source = this.GetOrCreateChild(senderId, senderStateName);
            var shortStateName = this.GetLabel(id, stateName);
            var target = this.GetOrCreateChild(id, shortStateName);
            this.GetOrCreateEventLink(source, target, info);
            this.CurrentStates[id] = stateName;
        }

        /// <inheritdoc/>
        public void OnMonitorRaiseEvent(string monitorTypeName, ActorId id, string stateName, Event e)
        {
            // Raising event to self.
            string eventName = e.GetType().FullName;
            this.CurrentStates[id] = stateName;
            this.AddEvent(id, id, stateName, eventName);
        }

        /// <inheritdoc/>
        public void OnMonitorStateTransition(string monitorTypeName, ActorId id, string stateName,
            bool isEntry, bool? isInHotState)
        {
            if (isEntry)
            {
                string resolvedId = this.GetResolveActorId(id);
                // Monitors process events immediately (and does not call OnDequeue), so this state transition is a result of the only event in the inbox.
                if (this.Inbox.TryGetValue(resolvedId, out List<EventInfo> inbox) && inbox.Count > 0)
                {
                    var e = inbox[inbox.Count - 1];
                    inbox.RemoveAt(inbox.Count - 1);

                    // draw the link connecting the current state to this new state!
                    string currentState = this.CurrentStates[id];
                    var shortStateName = this.GetLabel(id, currentState);
                    var source = this.GetOrCreateChild(id, shortStateName);

                    shortStateName = this.GetLabel(id, stateName);
                    string suffix = string.Empty;
                    if (isInHotState.HasValue)
                    {
                        suffix = (isInHotState == true) ? "[hot]" : "[cold]";
                    }

                    string label = shortStateName + suffix;
                    var target = this.GetOrCreateChild(id, shortStateName, label);
                    target.Label = label;
                    this.GetOrCreateEventLink(source, target, e);
                }
            }
        }

        /// <inheritdoc/>
        public void OnAssertionFailure(string error)
        {
        }

        /// <inheritdoc/>
        public void OnStrategyDescription(SchedulingStrategy strategy, string description)
        {
        }

        /// <inheritdoc/>
        public void OnCompleted()
        {
        }

        /// <summary>
        /// Return current graph and reset for next iteration.
        /// </summary>
        /// <param name="reset">Set to true will reset the graph for the next iteration.</param>
        /// <returns>The graph.</returns>
        public Graph SnapshotGraph(bool reset)
        {
            Graph result = this.CurrentGraph;
            if (reset)
            {
                // start fresh.
                this.CurrentGraph = null;
            }

            return result;
        }

        private string GetResolveActorId(ActorId id)
        {
            if (id == null)
            {
                // The sender id can be null if an event is fired from non-actor code.
                return "ExternalCode";
            }

            if (this.CollapseMachineInstances)
            {
                return id.Type;
            }

            return id.Name;
        }

        private EventInfo AddEvent(ActorId targetActorId, ActorId senderId, string senderStateName, string eventName)
        {
            string targetId = this.GetResolveActorId(targetActorId);
            if (!this.Inbox.TryGetValue(targetId, out List<EventInfo> inbox))
            {
                inbox = new List<EventInfo>();
                this.Inbox[targetId] = inbox;
            }

            if (senderId == null)
            {
                senderStateName = "ExternalState";
            }

            var info = new EventInfo() { ActorId = senderId, State = senderStateName, Event = eventName };
            inbox.Add(info);
            return info;
        }

        private void LinkTransition(string type, ActorId actorId, string currStateName, string newStateName, bool suffixLabel)
        {
            var source = this.GetOrCreateChild(actorId, this.GetLabel(actorId, currStateName));
            var target = this.GetOrCreateChild(actorId, this.GetLabel(actorId, newStateName));

            string label = type;
            if (this.Dequeued != null)
            {
                var eventLabel = this.GetEventLabel(this.Dequeued.Event);
                if (suffixLabel)
                {
                    label = eventLabel + "(" + label + ")";
                }
                else
                {
                    label = eventLabel;
                }
            }

            GraphLink link = this.Graph.GetOrCreateLink(source, target, label);
            if (this.Dequeued != null)
            {
                if (link.AddListAttribute("EventIds", this.Dequeued.Event) > 1)
                {
                    link.Label = "*";
                }
            }

            this.Dequeued = null;
        }

        private GraphNode GetOrCreateChild(ActorId actorId, string stateName, string label = null)
        {
            this.AddNamespace(actorId);

            string id = this.GetResolveActorId(actorId);
            GraphNode parent = this.Graph.GetOrCreateNode(id);
            parent.AddAttribute("Group", "Expanded");
            if (string.IsNullOrEmpty(stateName))
            {
                stateName = this.GetLabel(actorId, null);
            }

            if (label == null)
            {
                label = stateName;
            }

            GraphNode child = this.Graph.GetOrCreateNode(id + "." + stateName, label);
            this.Graph.GetOrCreateLink(parent, child, null, "Contains");
            return child;
        }

        private void GetOrCreateEventLink(GraphNode source, GraphNode target, EventInfo e)
        {
            string label = this.GetEventLabel(e.Event);
            GraphLink link = this.Graph.GetOrCreateLink(source, target, label);
            if (link.AddListAttribute("EventIds", e.Event) > 1)
            {
                link.Label = "*";
            }
        }

        private void AddNamespace(ActorId actorId)
        {
            if (actorId != null && !this.Namespaces.Contains(actorId.Type))
            {
                string typeName = actorId.Type;
                int index = typeName.Length;
                do
                {
                    typeName = typeName.Substring(0, index);
                    this.Namespaces.Add(typeName);
                    index = typeName.LastIndexOfAny(Separators);
                }
                while (index > 0);
            }
        }

        private string GetLabel(ActorId actorId, string fullyQualifiedName)
        {
            this.AddNamespace(actorId);
            if (fullyQualifiedName == null)
            {
                // then this is probably an Actor, not a StateMachine.  For Actors we can invent a state
                // name equal to the short name of the class, this then looks like a Constructor which is fine.
                int pos = actorId.Type.LastIndexOf(".");
                if (pos > 0)
                {
                    return actorId.Type.Substring(pos + 1);
                }

                return actorId.Name;
            }

            if (fullyQualifiedName.StartsWith(actorId.Type))
            {
                fullyQualifiedName = fullyQualifiedName.Substring(actorId.Type.Length + 1).Trim('+');
            }

            return fullyQualifiedName;
        }

        private static readonly char[] Separators = new char[] { '.', '+' };

        private string GetEventLabel(string fullyQualifiedName)
        {
            if (EventAliases.TryGetValue(fullyQualifiedName, out string label))
            {
                return label;
            }

            int i = fullyQualifiedName.LastIndexOfAny(Separators);
            if (i > 0)
            {
                string ns = fullyQualifiedName.Substring(0, i);
                if (this.Namespaces.Contains(ns))
                {
                    return fullyQualifiedName.Substring(i + 1);
                }
            }

            return fullyQualifiedName;
        }
    }

    /// <summary>
    /// A directed graph made up of Nodes and Links.
    /// </summary>
    [DataContract]
    public class Graph
    {
        internal const string DgmlNamespace = "http://schemas.microsoft.com/vs/2009/dgml";

        [DataMember]
        private readonly Dictionary<string, GraphNode> InternalNodes = new Dictionary<string, GraphNode>();

        [DataMember]
        private readonly Dictionary<string, GraphLink> InternalLinks = new Dictionary<string, GraphLink>();

        /// <summary>
        /// Return the current list of nodes (in no particular order).
        /// </summary>
        public IEnumerable<GraphNode> Nodes
        {
            get { return this.InternalNodes.Values; }
        }

        /// <summary>
        /// Return the current list of links (in no particular order).
        /// </summary>
        public IEnumerable<GraphLink> Links
        {
            get { return this.InternalLinks.Values; }
        }

        /// <summary>
        /// Get existing node or create a new one with the given id and label.
        /// </summary>
        /// <returns>Returns the new node or the existing node if it was already defined.</returns>
        public GraphNode GetOrCreateNode(string id, string label = null, string category = null)
        {
            if (!this.InternalNodes.TryGetValue(id, out GraphNode node))
            {
                node = new GraphNode(id, label, category);
                this.InternalNodes.Add(id, node);
            }

            return node;
        }

        /// <summary>
        /// Get existing node or create a new one with the given id and label.
        /// </summary>
        /// <returns>Returns the new node or the existing node if it was already defined.</returns>
        private GraphNode GetOrCreateNode(GraphNode newNode)
        {
            if (!this.InternalNodes.ContainsKey(newNode.Id))
            {
                this.InternalNodes.Add(newNode.Id, newNode);
            }

            return newNode;
        }

        /// <summary>
        /// Get existing link or create a new one connecting the given source and target nodes.
        /// </summary>
        /// <returns>The new link or the existing link if it was already defined.</returns>
        public GraphLink GetOrCreateLink(GraphNode source, GraphNode target, string linkLabel = null, string category = null)
        {
            string key = source.Id + "->" + target.Id;
            if (!this.InternalLinks.TryGetValue(key, out GraphLink link))
            {
                link = new GraphLink(source, target, linkLabel, category);
                this.InternalLinks.Add(key, link);
            }

            return link;
        }

        /// <summary>
        /// Serialize the graph to a DGML string.
        /// </summary>
        public override string ToString()
        {
            using (var writer = new StringWriter())
            {
                this.WriteDgml(writer);
                return writer.ToString();
            }
        }

        internal void SaveDgml(string graphFilePath)
        {
            using (StreamWriter writer = new StreamWriter(graphFilePath, false, Encoding.UTF8))
            {
                this.WriteDgml(writer);
            }
        }

        /// <summary>
        /// Serialize the graph to DGML.
        /// </summary>
        public void WriteDgml(TextWriter writer)
        {
            writer.WriteLine("<DirectedGraph xmlns='{0}'>", DgmlNamespace);
            writer.WriteLine("  <Nodes>");

            List<string> nodes = new List<string>(this.InternalNodes.Keys);
            nodes.Sort();
            foreach (var id in nodes)
            {
                GraphNode node = this.InternalNodes[id];
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

            writer.WriteLine("  </Nodes>");
            writer.WriteLine("  <Links>");

            List<string> links = new List<string>(this.InternalLinks.Keys);
            links.Sort();
            foreach (var id in links)
            {
                GraphLink link = this.InternalLinks[id];
                writer.Write("    <Link Source='{0}' Target='{1}'", link.Source.Id, link.Target.Id);
                if (!string.IsNullOrEmpty(link.Label))
                {
                    writer.Write(" Label='{0}'", link.Label);
                }

                if (!string.IsNullOrEmpty(link.Category))
                {
                    writer.Write(" Category='{0}'", link.Category);
                }

                link.WriteAttributes(writer);
                writer.WriteLine("/>");
            }

            writer.WriteLine("  </Links>");
            writer.WriteLine("</DirectedGraph>");
        }

        /// <summary>
        /// Load a DGML file into a new Graph object.
        /// </summary>
        /// <param name="graphFilePath">Full path to the DGML file.</param>
        /// <returns>The loaded Graph object.</returns>
        public static Graph LoadDgml(string graphFilePath)
        {
            XDocument doc = XDocument.Load(graphFilePath);
            Graph result = new Graph();
            var ns = doc.Root.Name.Namespace;
            if (ns != DgmlNamespace)
            {
                throw new Exception(string.Format("File '{0}' does not contain the DGML namespace", graphFilePath));
            }

            foreach (var e in doc.Root.Element(ns + "Nodes").Elements(ns + "Node"))
            {
                var id = (string)e.Attribute("Id");
                var label = (string)e.Attribute("Label");
                var category = (string)e.Attribute("Category");

                GraphNode node = new GraphNode(id, label, category);
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
                var link = result.GetOrCreateLink(srcNode, targetNode, label, category);
                link.AddDgmlProperties(e);
            }

            return result;
        }

        /// <summary>
        /// Merge the given graph so that this graph becomes a superset of both graphs.
        /// </summary>
        /// <param name="other">The new graph to merge into this graph.</param>
        public void Merge(Graph other)
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
                var newLink = this.GetOrCreateLink(source, target, link.Label, link.Category);
                newLink.Merge(link);
            }
        }
    }

    /// <summary>
    /// A Node of a Graph.
    /// </summary>
    [DataContract]
    public class GraphObject
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
        /// Add an attribute to the node.
        /// </summary>
        public void AddAttribute(string name, string value)
        {
            if (this.Attributes == null)
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
            if (this.AttributeLists == null)
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
                List<string> names = new List<string>(this.Attributes.Keys);
                names.Sort();  // creates a more stable output file (can be handy for expected output during testing).
                foreach (string name in names)
                {
                    var value = this.Attributes[name];
                    writer.Write(" {0}='{1}'", name, value);
                }
            }

            if (this.AttributeLists != null)
            {
                List<string> names = new List<string>(this.AttributeLists.Keys);
                names.Sort();  // creates a more stable output file (can be handy for expected output during testing).
                foreach (string name in names)
                {
                    var value = this.AttributeLists[name];
                    writer.Write(" {0}='{1}'", name, string.Join(",", value));
                }
            }
        }

        internal void Merge(GraphObject other)
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
    /// A Node of a Graph.
    /// </summary>
    [DataContract]
    public class GraphNode : GraphObject
    {
        /// <summary>
        /// The unique Id of the Node within the Graph.
        /// </summary>
        [DataMember]
        public string Id { get; internal set; }

        /// <summary>
        /// An optional display label for the node (does not need to be unique).
        /// </summary>
        [DataMember]
        public string Label { get; internal set; }

        /// <summary>
        /// An optional category for the node
        /// </summary>
        [DataMember]
        public string Category { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphNode"/> class.
        /// </summary>
        public GraphNode(string id, string label, string category)
        {
            this.Id = id;
            this.Label = label;
            this.Category = category;
        }

        /// <summary>
        /// Add additional properties from XML element.
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
    /// A Link represents a directed graph connection between two Nodes.
    /// </summary>
    [DataContract]
    public class GraphLink : GraphObject
    {
        /// <summary>
        /// An optional display label for the link.
        /// </summary>
        [DataMember]
        public string Label { get; internal set; }

        /// <summary>
        /// An optional category for the link.
        /// The special category "Contains" is reserved for building groups.
        /// </summary>
        [DataMember]
        public string Category { get; internal set; }

        /// <summary>
        /// The source end of the link.
        /// </summary>
        [DataMember]
        public GraphNode Source { get; internal set; }

        /// <summary>
        /// The target end of the link.
        /// </summary>
        [DataMember]
        public GraphNode Target { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphLink"/> class.
        /// </summary>
        public GraphLink(GraphNode source, GraphNode target, string label, string category)
        {
            this.Source = source;
            this.Target = target;
            this.Label = label;
            this.Category = category;
        }

        /// <summary>
        /// Add additional properties from XML element.
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
