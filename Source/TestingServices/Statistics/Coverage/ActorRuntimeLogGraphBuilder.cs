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
using Microsoft.Coyote.IO;
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
            public string ActorId;
            public string State;
            public string Event;
        }

        private readonly Dictionary<string, List<EventInfo>> Inbox = new Dictionary<string, List<EventInfo>>();
        private static readonly Dictionary<string, string> EventAliases = new Dictionary<string, string>();

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
        public ILogger Logger { get; set; }

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

        /// <summary>
        /// Invoked when the specified actor has been created.
        /// </summary>
        /// <param name="id">The id of the actor that has been created.</param>
        /// <param name="creator">The id of the creator, or null.</param>
        public void OnCreateActor(ActorId id, ActorId creator)
        {
            string resolvedId = this.ResolveActorId(id);
            this.Graph.GetOrCreateNode(resolvedId, resolvedId);
        }

        /// <summary>
        /// Invoked when the specified actor executes an action.
        /// </summary>
        /// <param name="id">The id of the actor executing the action.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public void OnExecuteAction(ActorId id, string stateName, string actionName)
        {
        }

        /// <summary>
        /// Invoked when the specified event is sent to a target actor.
        /// </summary>
        /// <param name="targetActorId">The id of the target actor.</param>
        /// <param name="senderId">The id of the actor that sent the event, if any.</param>
        /// <param name="senderStateName">The state name, if the sender actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">The event being sent.</param>
        /// <param name="opGroupId">The id used to identify the send operation.</param>
        /// <param name="isTargetHalted">Is the target actor halted.</param>
        public void OnSendEvent(ActorId targetActorId, ActorId senderId, string senderStateName, string eventName,
            Guid opGroupId, bool isTargetHalted)
        {
            this.AddEvent(targetActorId, senderId, senderStateName, eventName);
        }

        /// <summary>
        /// Invoked when the specified state machine raises an event.
        /// </summary>
        /// <param name="id">The id of the actor raising the event.</param>
        /// <param name="stateName">The name of the current state.</param>
        /// <param name="eventName">The name of the event being raised.</param>
        public void OnRaiseEvent(ActorId id, string stateName, string eventName)
        {
            // Raising event to self.
            this.AddEvent(id, id, stateName, eventName);
        }

        /// <summary>
        /// Invoked when the specified event is about to be enqueued to an actor.
        /// </summary>
        /// <param name="id">The id of the actor that the event is being enqueued to.</param>
        /// <param name="eventName">Name of the event.</param>
        public void OnEnqueueEvent(ActorId id, string eventName)
        {
        }

        /// <summary>
        /// Invoked when the specified event is dequeued by an actor.
        /// </summary>
        /// <param name="id">The id of the actor that the event is being dequeued by.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">Name of the event.</param>
        public void OnDequeueEvent(ActorId id, string stateName, string eventName)
        {
            string resolvedId = this.ResolveActorId(id);
            if (this.Inbox.TryGetValue(resolvedId, out List<EventInfo> inbox))
            {
                for (int i = inbox.Count - 1; i >= 0; i--)
                {
                    EventInfo e = inbox[i];
                    if (e.Event == eventName)
                    {
                        // Yay, found it so we can draw the complete link connecting the Sender state to this state!
                        var source = this.GetOrCreateChild(e.ActorId, e.State);
                        var target = this.GetOrCreateChild(resolvedId, GetLabel(id, stateName));
                        this.GetOrCreateEventLink(source, target, e);
                        inbox.RemoveAt(i);
                        this.Dequeued = e;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Invoked when the specified event is received by an actor.
        /// </summary>
        /// <param name="id">The id of the actor that received the event.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="wasBlocked">The actor was waiting for one or more specific events,
        /// and <paramref name="eventName"/> was one of them</param>
        public void OnReceiveEvent(ActorId id, string stateName, string eventName, bool wasBlocked)
        {
            string resolvedId = this.ResolveActorId(id);
            if (this.Inbox.TryGetValue(resolvedId, out List<EventInfo> inbox))
            {
                for (int i = inbox.Count - 1; i >= 0; i--)
                {
                    EventInfo e = inbox[i];
                    if (e.Event == eventName)
                    {
                        // Yay, found it so we can draw the complete link connecting the Sender state to this state!
                        var source = this.GetOrCreateChild(e.ActorId, e.State);
                        var target = this.GetOrCreateChild(resolvedId, GetLabel(id, stateName));
                        this.GetOrCreateEventLink(source, target, e);
                        inbox.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Invoked when the specified actor waits to receive an event of a specified type.
        /// </summary>
        /// <param name="id">The id of the actor that is entering the wait state.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventType">The type of the event being waited for.</param>
        public void OnWaitEvent(ActorId id, string stateName, Type eventType)
        {
        }

        /// <summary>
        /// Invoked when the specified actor waits to receive an event of one of the specified types.
        /// </summary>
        /// <param name="id">The id of the actor that is entering the wait state.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventTypes">The types of the events being waited for, if any.</param>
        public void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
        {
        }

        /// <summary>
        /// Invoked when the specified random result has been obtained.
        /// </summary>
        /// <param name="id">The id of the source actor, if any; otherwise, the runtime itself was the source.</param>
        /// <param name="result">The random result (may be bool or int).</param>
        public void OnRandom(ActorId id, object result)
        {
        }

        /// <summary>
        /// Invoked when the specified state machine enters or exits a state.
        /// </summary>
        /// <param name="id">The id of the actor entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited.</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        public void OnStateTransition(ActorId id, string stateName, bool isEntry)
        {
        }

        /// <summary>
        /// Invoked when the specified state machine transitions states via a 'goto'.
        /// </summary>
        /// <param name="id">The id of the actor.</param>
        /// <param name="currStateName">The name of the current state.</param>
        /// <param name="newStateName">The target state of the transition.</param>
        public void OnGotoState(ActorId id, string currStateName, string newStateName)
        {
            this.LinkTransition("goto", id, currStateName, newStateName, false);
        }

        /// <summary>
        /// Invoked when the specified state machine is being pushed to a state.
        /// </summary>
        /// <param name="id">The id of the actor being pushed to the state.</param>
        /// <param name="currStateName">The name of the current state.</param>
        /// <param name="newStateName">The target state of the transition.</param>
        public void OnPushState(ActorId id, string currStateName, string newStateName)
        {
            this.LinkTransition("push", id, currStateName, newStateName, true);
        }

        /// <summary>
        /// Invoked when the specified state machine has been popped from a state.
        /// </summary>
        /// <param name="id">The id of the actor that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state.</param>
        /// <param name="restoredStateName">The name of the state being re-entered, if any.</param>
        public void OnPopState(ActorId id, string currStateName, string restoredStateName)
        {
            if (!string.IsNullOrEmpty(currStateName))
            {
                this.LinkTransition("pop", id, currStateName, restoredStateName, true);
            }
        }

        /// <summary>
        /// Invoked when the specified actor has been halted.
        /// </summary>
        /// <param name="id">The id of the actor that has been halted.</param>
        /// <param name="inboxSize">Approximate size of the inbox.</param>
        public void OnHalt(ActorId id, int inboxSize)
        {
            string resolvedId = this.ResolveActorId(id);
            string stateName = this.HaltedState;
            if (string.IsNullOrEmpty(stateName))
            {
                stateName = "null";
            }

            // Transition to the Halt state.
            var source = this.GetOrCreateChild(resolvedId, GetLabel(id, stateName));
            var target = this.GetOrCreateChild(resolvedId, "Halt");
            this.Graph.GetOrCreateLink(source, target, "halt");
        }

        /// <summary>
        /// Invoked when the specified actor is idle (there is nothing to dequeue) and the default
        /// event handler is about to be executed.
        /// </summary>
        /// <param name="id">The id of the actor that the state will execute in.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        public void OnDefaultEventHandler(ActorId id, string stateName)
        {
        }

        /// <summary>
        /// Invoked when the specified actor handled a raised event.
        /// </summary>
        /// <param name="id">The id of the actor handling the event.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">The name of the event being handled.</param>
        public void OnHandleRaisedEvent(ActorId id, string stateName, string eventName)
        {
            // We used the inbox to store raised event, but it should be the first one handled since
            // raised events are highest priority.
            string resolvedId = this.ResolveActorId(id);
            if (this.Inbox.TryGetValue(resolvedId, out List<EventInfo> inbox))
            {
                for (int i = inbox.Count - 1; i >= 0; i--)
                {
                    EventInfo e = inbox[i];
                    if (e.Event == eventName)
                    {
                        // Yay, found it so we can draw the complete link connecting the Sender state to this state!
                        var source = this.GetOrCreateChild(e.ActorId, e.State);
                        var target = this.GetOrCreateChild(resolvedId, GetLabel(id, stateName));
                        this.GetOrCreateEventLink(source, target, e);
                        inbox.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Invoked when the specified event cannot be handled in the current state, its exit
        /// handler is executed and then the state is popped and any previous "current state"
        /// is reentered. This handler is called when that pop has been done.
        /// </summary>
        /// <param name="actorId">Id of the machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        public void OnPopUnhandledEvent(ActorId actorId, string currStateName, string eventName)
        {
            if (eventName == typeof(HaltEvent).FullName)
            {
                this.HaltedState = currStateName;
            }
        }

        /// <summary>
        /// Invoked when the specified actor throws an exception.
        /// </summary>
        /// <param name="id">The id of the actor that threw the exception.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        public void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
        {
        }

        /// <summary>
        /// Invoked when the specified OnException method is used to handle a thrown exception.
        /// </summary>
        /// <param name="id">The id of the actor that threw the exception.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        public void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
        {
        }

        /// <summary>
        /// Invoked when the specified actor timer has been created.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        public void OnCreateTimer(TimerInfo info)
        {
            // TODO: figure out how to graph timers when we have no "timer id" at this point...
        }

        /// <summary>
        /// Invoked when the specified actor timer has been stopped.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        public void OnStopTimer(TimerInfo info)
        {
        }

        /// <summary>
        /// Invoked when the specified monitor has been created.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor that has been created.</param>
        /// <param name="id">The id of the monitor that has been created.</param>
        public void OnCreateMonitor(string monitorTypeName, ActorId id)
        {
            string resolvedId = this.ResolveActorId(id);
            this.Graph.GetOrCreateNode(resolvedId, monitorTypeName);
        }

        /// <summary>
        /// Invoked when the specified monitor executes an action.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that is executing the action.</param>
        /// <param name="id">The id of the monitor that is executing the action</param>
        /// <param name="stateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public void OnMonitorExecuteAction(string monitorTypeName, ActorId id, string stateName, string actionName)
        {
            string resolvedId = this.ResolveActorId(id);
            // Monitors process actions immediately, so this state transition is a result of the only event in the inbox.
            if (this.Inbox.TryGetValue(resolvedId, out List<EventInfo> inbox) && inbox.Count > 0)
            {
                var e = inbox[inbox.Count - 1];
                inbox.RemoveAt(inbox.Count - 1);
                // draw the link connecting the Sender state to this state!
                var source = this.GetOrCreateChild(e.ActorId, e.State);
                var target = this.GetOrCreateChild(resolvedId, GetLabel(id, stateName));
                this.GetOrCreateEventLink(source, target, e);
            }
        }

        /// <summary>
        /// Invoked when the specified monitor is about to process an event.
        /// </summary>
        /// <param name="senderId">The sender of the event.</param>
        /// <param name="senderStateName">The name of the state the sender is in.</param>
        /// <param name="monitorTypeName">Name of type of the monitor that will process the event.</param>
        /// <param name="id">The id of the monitor that will process the event.</param>
        /// <param name="stateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        public void OnMonitorProcessEvent(ActorId senderId, string senderStateName, string monitorTypeName,
            ActorId id, string stateName, string eventName)
        {
            // If sender is null then it means we are dealing with a Monitor call from external code.
            this.AddEvent(id, senderId, senderStateName, eventName);
        }

        /// <summary>
        /// Invoked when the specified monitor raised an event.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor raising the event.</param>
        /// <param name="id">The id of the monitor raising the event.</param>
        /// <param name="stateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        public void OnMonitorRaiseEvent(string monitorTypeName, ActorId id, string stateName, string eventName)
        {
            // Raising event to self.
            this.AddEvent(id, id, stateName, eventName);
        }

        /// <summary>
        /// Invoked when the specified monitor enters or exits a state.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor entering or exiting the state</param>
        /// <param name="id">The id of the monitor entering or exiting the state</param>
        /// <param name="stateName">The name of the state being entered or exited; if <paramref name="isInHotState"/>
        /// is not null, then the temperature is appended to the statename in brackets, e.g. "stateName[hot]".</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        /// <param name="isInHotState">If true, the monitor is in a hot state; if false, the monitor is in a cold state;
        /// else no liveness state is available.</param>
        public void OnMonitorStateTransition(string monitorTypeName, ActorId id, string stateName,
            bool isEntry, bool? isInHotState)
        {
            if (isEntry)
            {
                string resolvedId = this.ResolveActorId(id);
                // Monitors process events immediately (and does not call OnDequeue), so this state transition is a result of the only event in the inbox.
                if (this.Inbox.TryGetValue(resolvedId, out List<EventInfo> inbox) && inbox.Count > 0)
                {
                    var e = inbox[inbox.Count - 1];
                    inbox.RemoveAt(inbox.Count - 1);
                    // draw the link connecting the Sender state to this state!
                    var source = this.GetOrCreateChild(e.ActorId, e.State);
                    var target = this.GetOrCreateChild(resolvedId, GetLabel(id, stateName));
                    this.GetOrCreateEventLink(source, target, e);
                }
            }
        }

        /// <summary>
        /// Invoked when the specified assertion failure has occurred.
        /// </summary>
        /// <param name="error">The text of the error.</param>
        public void OnAssertionFailure(string error)
        {
        }

        /// <summary>
        /// Invoked to describe the specified scheduling strategy.
        /// </summary>
        /// <param name="strategy">The scheduling strategy that was used.</param>
        /// <param name="description">More information about the scheduling strategy.</param>
        public void OnStrategyDescription(SchedulingStrategy strategy, string description)
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

        private string ResolveActorId(ActorId id)
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

        private void AddEvent(ActorId targetActorId, ActorId senderId, string senderStateName, string eventName)
        {
            string targetId = this.ResolveActorId(targetActorId);
            if (!this.Inbox.TryGetValue(targetId, out List<EventInfo> inbox))
            {
                inbox = new List<EventInfo>();
                this.Inbox[targetId] = inbox;
            }

            if (senderId == null)
            {
                senderStateName = "ExternalState";
            }

            string sender = this.ResolveActorId(senderId);
            inbox.Add(new EventInfo() { ActorId = sender, State = senderStateName, Event = eventName });
        }

        private void LinkTransition(string type, ActorId actorId, string currStateName, string newStateName, bool suffixLabel)
        {
            string id = this.ResolveActorId(actorId);
            var source = this.GetOrCreateChild(id, GetLabel(actorId, currStateName));
            var target = this.GetOrCreateChild(id, GetLabel(actorId, newStateName));

            string label = type;
            if (this.Dequeued != null)
            {
                var eventLabel = GetEventLabel(this.Dequeued.Event);
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
                link.AddAttribute("EventId", this.Dequeued.Event);
            }

            this.Dequeued = null;
        }

        private GraphNode GetOrCreateChild(string actorId, string stateName)
        {
            GraphNode parent = this.Graph.GetOrCreateNode(actorId);
            parent.AddAttribute("Group", "Expanded");
            GraphNode child = this.Graph.GetOrCreateNode(actorId + "." + stateName, stateName);
            this.Graph.GetOrCreateLink(parent, child, null, "Contains");
            return child;
        }

        private void GetOrCreateEventLink(GraphNode source, GraphNode target, EventInfo e)
        {
            string label = GetEventLabel(e.Event);
            GraphLink link = this.Graph.GetOrCreateLink(source, target, label);
            link.AddAttribute("EventId", e.Event);
        }

        private static string GetLabel(ActorId actorId, string fullyQualifiedName)
        {
            if (fullyQualifiedName.StartsWith(actorId.Type))
            {
                fullyQualifiedName = fullyQualifiedName.Substring(actorId.Type.Length + 1).Trim('+');
            }

            return fullyQualifiedName;
        }

        private static string GetEventLabel(string fullyQualifiedName)
        {
            if (EventAliases.TryGetValue(fullyQualifiedName, out string label))
            {
                return label;
            }

            int i = fullyQualifiedName.IndexOf('+');
            if (i > 0)
            {
                return fullyQualifiedName.Substring(i + 1);
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
                this.GetOrCreateNode(node.Id, node.Label, node.Category);
            }

            foreach (var link in other.InternalLinks.Values)
            {
                var source = this.GetOrCreateNode(link.Source.Id, link.Source.Label, link.Source.Category);
                var target = this.GetOrCreateNode(link.Target.Id, link.Target.Label, link.Target.Category);
                this.GetOrCreateLink(source, target, link.Label, link.Category);
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

        internal void WriteAttributes(TextWriter writer)
        {
            if (this.Attributes != null)
            {
                List<string> names = new List<string>(this.Attributes.Keys);
                names.Sort();
                foreach (string name in names)
                {
                    var value = this.Attributes[name];
                    writer.Write(" {0}='{1}'", name, value);
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
