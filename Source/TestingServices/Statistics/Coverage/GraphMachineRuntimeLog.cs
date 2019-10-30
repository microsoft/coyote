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

namespace Microsoft.Coyote.TestingServices.Coverage
{
    /// <summary>
    /// Implements the IMachineRuntimeLog and builds a directed graph from the recorded
    /// events and state transitions.
    /// </summary>
    public class GraphMachineRuntimeLog : IMachineRuntimeLog
    {
        private Graph CurrentGraph;
        private EventInfo dequeued; // current dequeued event.

        private class EventInfo
        {
            public string ActorId;
            public string State;
            public string Event;
        }

        private readonly Dictionary<string, List<EventInfo>> InBox = new Dictionary<string, List<EventInfo>>();
        private readonly Dictionary<string, string> CurrentStates = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphMachineRuntimeLog"/> class.
        /// </summary>
        public GraphMachineRuntimeLog()
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
        /// Allows you to chain log writers.
        /// </summary>
        public IMachineRuntimeLog Next { get; set; }

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
        /// Called when an event is about to be enqueued to a machine.
        /// </summary>
        /// <param name="actorId">Id of the machine that the event is being enqueued to.</param>
        /// <param name="eventName">Name of the event.</param>
        public void OnEnqueue(ActorId actorId, string eventName)
        {
            this.Next?.OnEnqueue(actorId, eventName);
        }

        /// <summary>
        /// Called when an event is dequeued by a machine.
        /// </summary>
        /// <param name="actorId">Id of the machine that the event is being dequeued by.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">Name of the event.</param>
        public void OnDequeue(ActorId actorId, string currStateName, string eventName)
        {
            this.Next?.OnDequeue(actorId, currStateName, eventName);

            string id = this.GetActorId(actorId);
            if (this.InBox.TryGetValue(id, out List<EventInfo> inbox))
            {
                for (int i = inbox.Count - 1; i >= 0; i--)
                {
                    EventInfo e = inbox[i];
                    if (e.Event == eventName)
                    {
                        // yay, found it so we can draw the complete link connecting the Sender state to this state!
                        var source = this.GetOrCreateChild(e.ActorId, e.State);
                        var target = this.GetOrCreateChild(id, GetLabel(actorId, currStateName));
                        this.GetOrCreateEventLink(source, target, e);
                        inbox.RemoveAt(i);
                        this.dequeued = e;
                        break;
                    }
                }
            }
        }

        private void GetOrCreateEventLink(GraphNode source, GraphNode target, EventInfo e)
        {
            string label = GetEventLabel(e.Event);
            GraphLink link = this.Graph.GetOrCreateLink(source, target, label);
            link.AddAttribute("EventId", e.Event);
        }

        private string GetActorId(ActorId id)
        {
            if (this.CollapseMachineInstances)
            {
                return id.Type;
            }

            return id.Name;
        }

        /// <summary>
        /// Called when the default event handler for a state is about to be executed.
        /// </summary>
        /// <param name="actorId">Id of the machine that the state will execute in.</param>
        /// <param name="currStateName">Name of the current state of the machine.</param>
        public void OnDefault(ActorId actorId, string currStateName)
        {
            this.Next?.OnDefault(actorId, currStateName);
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
            if (fullyQualifiedName == "Microsoft.Coyote.Machines.GotoStateEvent")
            {
                return "goto";
            }

            int i = fullyQualifiedName.IndexOf('+');
            if (i > 0)
            {
                return fullyQualifiedName.Substring(i + 1);
            }

            return fullyQualifiedName;
        }

        /// <summary>
        /// Called when a machine transitions states via a 'goto'.
        /// </summary>
        /// <param name="actorId">Id of the machine.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="newStateName">The target state of goto.</param>
        public void OnGoto(ActorId actorId, string currStateName, string newStateName)
        {
            this.Next?.OnGoto(actorId, currStateName, newStateName);
            this.LinkTransition("goto", actorId, currStateName, newStateName, false);
        }

        private void LinkTransition(string type, ActorId actorId, string currStateName, string newStateName, bool suffixLabel)
        {
            string id = this.GetActorId(actorId);
            this.CurrentStates[id] = newStateName;
            var source = this.GetOrCreateChild(id, GetLabel(actorId, currStateName));
            var target = this.GetOrCreateChild(id, GetLabel(actorId, newStateName));

            string label = type;
            if (this.dequeued != null)
            {
                var eventLabel = GetEventLabel(this.dequeued.Event);
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
            if (this.dequeued != null)
            {
                link.AddAttribute("EventId", this.dequeued.Event);
            }

            this.dequeued = null;
        }

        /// <summary>
        /// Called when a machine is being pushed to a state.
        /// </summary>
        /// <param name="actorId">Id of the machine being pushed to the state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="newStateName">The state the machine is pushed to.</param>
        public void OnPush(ActorId actorId, string currStateName, string newStateName)
        {
            this.Next?.OnPush(actorId, currStateName, newStateName);

            this.LinkTransition("push", actorId, currStateName, newStateName, true);
        }

        /// <summary>
        /// Called when a machine has been popped from a state.
        /// </summary>
        /// <param name="actorId">Id of the machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="restoredStateName">The name of the state being re-entered, if any</param>
        public void OnPop(ActorId actorId, string currStateName, string restoredStateName)
        {
            this.Next?.OnPop(actorId, currStateName, restoredStateName);

            if (!string.IsNullOrEmpty(currStateName))
            {
                this.LinkTransition("pop", actorId, currStateName, restoredStateName, true);
            }
            else
            {
                string id = this.GetActorId(actorId);
                this.CurrentStates[id] = restoredStateName;
            }
        }

        /// <summary>
        /// When an event cannot be handled in the current state, its exit handler is executed and then the state is
        /// popped and any previous "current state" is reentered. This handler is called when that pop has been done.
        /// </summary>
        /// <param name="actorId">Id of the machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        public void OnPopUnhandledEvent(ActorId actorId, string currStateName, string eventName)
        {
            this.Next?.OnPopUnhandledEvent(actorId, currStateName, eventName);
        }

        /// <summary>
        /// Called when an event is received by a machine.
        /// </summary>
        /// <param name="actorId">Id of the machine that received the event.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="wasBlocked">The machine was waiting for one or more specific events,
        ///     and <paramref name="eventName"/> was one of them</param>
        public void OnReceive(ActorId actorId, string currStateName, string eventName, bool wasBlocked)
        {
            this.Next?.OnReceive(actorId, currStateName, eventName, wasBlocked);

            string id = this.GetActorId(actorId);
            if (this.InBox.TryGetValue(id, out List<EventInfo> inbox))
            {
                for (int i = inbox.Count - 1; i >= 0; i--)
                {
                    EventInfo e = inbox[i];
                    if (e.Event == eventName)
                    {
                        // yay, found it so we can draw the complete link connecting the Sender state to this state!
                        var source = this.GetOrCreateChild(e.ActorId, e.State);
                        var target = this.GetOrCreateChild(id, GetLabel(actorId, currStateName));
                        this.GetOrCreateEventLink(source, target, e);
                        inbox.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Called when a machine waits to receive an event of a specified type.
        /// </summary>
        /// <param name="actorId">Id of the machine that is entering the wait state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventType">The type of the event being waited for.</param>
        public void OnWait(ActorId actorId, string currStateName, Type eventType)
        {
            this.Next?.OnWait(actorId, currStateName, eventType);
        }

        /// <summary>
        /// Called when a machine waits to receive an event of one of the specified types.
        /// </summary>
        /// <param name="actorId">Id of the machine that is entering the wait state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventTypes">The types of the events being waited for, if any.</param>
        public void OnWait(ActorId actorId, string currStateName, params Type[] eventTypes)
        {
            this.Next?.OnWait(actorId, currStateName, eventTypes);
        }

        /// <summary>
        /// Called when an event is sent to a target machine.
        /// </summary>
        /// <param name="targetActorId">Id of the target machine.</param>
        /// <param name="senderId">The id of the machine that sent the event, if any.</param>
        /// <param name="senderStateName">The name of the current state of the sender machine, if any.</param>
        /// <param name="eventName">The event being sent.</param>
        /// <param name="opGroupId">Id used to identify the send operation.</param>
        /// <param name="isTargetHalted">Is the target machine halted.</param>
        public void OnSend(ActorId targetActorId, ActorId senderId, string senderStateName, string eventName, Guid opGroupId, bool isTargetHalted)
        {
            this.Next?.OnSend(targetActorId, senderId, senderStateName, eventName, opGroupId, isTargetHalted);

            string targetId = this.GetActorId(targetActorId);
            if (!this.InBox.TryGetValue(targetId, out List<EventInfo> inbox))
            {
                inbox = new List<EventInfo>();
                this.InBox[targetId] = inbox;
            }

            if (senderId == null)
            {
                // senderId can be null if an event is fired from code.
                senderId = this.GetExternalActorId(targetActorId.Runtime);
                senderStateName = "ExternalState";
            }

            inbox.Add(new EventInfo() { ActorId = this.GetActorId(senderId), State = senderStateName, Event = eventName });
        }

        /// <summary>
        /// Called when a machine has been created.
        /// </summary>
        /// <param name="actorId">The id of the machine that has been created.</param>
        /// <param name="creator">Id of the creator machine, or null.</param>
        public void OnCreateMachine(ActorId actorId, ActorId creator)
        {
            this.Next?.OnCreateMachine(actorId, creator);

            string id = this.GetActorId(actorId);
            this.Graph.GetOrCreateNode(id, id);
        }

        /// <summary>
        /// Called when a monitor has been created.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor that has been created.</param>
        /// <param name="monitorId">The id of the monitor that has been created.</param>
        public void OnCreateMonitor(string monitorTypeName, ActorId monitorId)
        {
            this.Next?.OnCreateMonitor(monitorTypeName, monitorId);

            string id = this.GetActorId(monitorId);
            this.Graph.GetOrCreateNode(id, monitorTypeName);
        }

        /// <summary>
        /// Called when a machine timer has been created.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        public void OnCreateTimer(TimerInfo info)
        {
            this.Next?.OnCreateTimer(info);

            // todo: figure out how to graph timers when we have no "timer id" at this point...
        }

        /// <summary>
        /// Called when a machine timer has been stopped.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        public void OnStopTimer(TimerInfo info)
        {
            this.Next?.OnStopTimer(info);
        }

        /// <summary>
        /// Called when a machine has been halted.
        /// </summary>
        /// <param name="actorId">The id of the machine that has been halted.</param>
        /// <param name="inboxSize">Approximate size of the machine inbox.</param>
        public void OnHalt(ActorId actorId, int inboxSize)
        {
            this.Next?.OnHalt(actorId, inboxSize);

            string id = this.GetActorId(actorId);
            if (!this.CurrentStates.TryGetValue(id, out string currStateName))
            {
                currStateName = "Init";
            }

            this.CurrentStates[id] = "Halt";

            // transition to the Halt state
            var source = this.GetOrCreateChild(id, GetLabel(actorId, currStateName));
            var target = this.GetOrCreateChild(id, "Halt");
            this.Graph.GetOrCreateLink(source, target, "halt");
        }

        /// <summary>
        /// Called when a random result has been obtained.
        /// </summary>
        /// <param name="actorId">The id of the source machine, if any; otherwise, the runtime itself was the source.</param>
        /// <param name="result">The random result (may be bool or int).</param>
        public void OnRandom(ActorId actorId, object result)
        {
            this.Next?.OnRandom(actorId, result);
        }

        /// <summary>
        /// Called when a machine enters or exits a state.
        /// </summary>
        /// <param name="actorId">The id of the machine entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited.</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        public void OnMachineState(ActorId actorId, string stateName, bool isEntry)
        {
            this.Next?.OnMachineState(actorId, stateName, isEntry);
        }

        /// <summary>
        /// Called when a machine raises an event.
        /// </summary>
        /// <param name="actorId">The id of the machine raising the event.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="eventName">The name of the event being raised.</param>
        public void OnMachineEvent(ActorId actorId, string currStateName, string eventName)
        {
            this.Next?.OnMachineEvent(actorId, currStateName, eventName);

            // raising event to self.
            string id = this.GetActorId(actorId);
            if (!this.InBox.TryGetValue(id, out List<EventInfo> inbox))
            {
                inbox = new List<EventInfo>();
                this.InBox[id] = inbox;
            }

            inbox.Add(new EventInfo() { ActorId = id, State = currStateName, Event = eventName });
        }

        /// <summary>
        /// Called when a machine handled a raised event.
        /// </summary>
        /// <param name="actorId">The id of the machine handling the event.</param>
        /// <param name="currStateName">The name of the state in which the event is being handled.</param>
        /// <param name="eventName">The name of the event being handled.</param>
        public void OnHandleRaisedEvent(ActorId actorId, string currStateName, string eventName)
        {
            this.Next?.OnHandleRaisedEvent(actorId, currStateName, eventName);

            // we used the inbox to store raised event, but it should be the first one handled since
            // raised events are highest priority.
            string id = this.GetActorId(actorId);
            if (this.InBox.TryGetValue(id, out List<EventInfo> inbox))
            {
                for (int i = inbox.Count - 1; i >= 0; i--)
                {
                    EventInfo e = inbox[i];
                    if (e.Event == eventName)
                    {
                        // yay, found it so we can draw the complete link connecting the Sender state to this state!
                        var source = this.GetOrCreateChild(e.ActorId, e.State);
                        var target = this.GetOrCreateChild(id, GetLabel(actorId, currStateName));
                        this.GetOrCreateEventLink(source, target, e);
                        inbox.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Called when a machine executes an action.
        /// </summary>
        /// <param name="actorId">The id of the machine executing the action.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public void OnMachineAction(ActorId actorId, string currStateName, string actionName)
        {
            this.Next?.OnMachineAction(actorId, currStateName, actionName);
        }

        /// <summary>
        /// Called when a machine throws an exception
        /// </summary>
        /// <param name="actorId">The id of the machine that threw the exception.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        public void OnMachineExceptionThrown(ActorId actorId, string currStateName, string actionName, Exception ex)
        {
            this.Next?.OnMachineExceptionThrown(actorId, currStateName, actionName, ex);
        }

        /// <summary>
        /// Called when a machine's OnException method is used to handle a thrown exception
        /// </summary>
        /// <param name="actorId">The id of the machine that threw the exception.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        public void OnMachineExceptionHandled(ActorId actorId, string currStateName, string actionName, Exception ex)
        {
            this.Next?.OnMachineExceptionHandled(actorId, currStateName, actionName, ex);
        }

        /// <summary>
        /// Called when a monitor enters or exits a state.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor entering or exiting the state</param>
        /// <param name="monitorId">The ID of the monitor entering or exiting the state</param>
        /// <param name="stateName">The name of the state being entered or exited; if <paramref name="isInHotState"/>
        ///     is not null, then the temperature is appended to the statename in brackets, e.g. "stateName[hot]".</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        /// <param name="isInHotState">If true, the monitor is in a hot state; if false, the monitor is in a cold state;
        ///     else no liveness state is available.</param>
        public void OnMonitorState(string monitorTypeName, ActorId monitorId, string stateName, bool isEntry, bool? isInHotState)
        {
            this.Next?.OnMonitorState(monitorTypeName, monitorId, stateName, isEntry, isInHotState);

            string id = this.GetActorId(monitorId);
            // Monitors process events immediately (and does not call OnDequeue), so this state transition is a result of the only event in the inbox.
            if (this.InBox.TryGetValue(id, out List<EventInfo> inbox) && inbox.Count > 0)
            {
                var e = inbox[inbox.Count - 1];
                inbox.RemoveAt(inbox.Count - 1);
                // draw the link connecting the Sender state to this state!
                var source = this.GetOrCreateChild(e.ActorId, e.State);
                var target = this.GetOrCreateChild(id, GetLabel(monitorId, stateName));
                this.GetOrCreateEventLink(source, target, e);
            }
        }

        /// <summary>
        /// Called when a monitor is about to process or has raised an event.
        /// </summary>
        /// <param name="senderId">The sender of the event.</param>
        /// <param name="monitorTypeName">Name of type of the monitor that will process or has raised the event.</param>
        /// <param name="monitorId">ID of the monitor that will process or has raised the event</param>
        /// <param name="currStateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="isProcessing">If true, the monitor is processing the event; otherwise it has raised it.</param>
        public void OnMonitorEvent(ActorId senderId, string monitorTypeName, ActorId monitorId, string currStateName, string eventName, bool isProcessing)
        {
            this.Next?.OnMonitorEvent(senderId, monitorTypeName, monitorId, currStateName, eventName, isProcessing);

            // if sender is null then it means we are dealing with a Monitor call from external code.
            if (senderId == null)
            {
                senderId = this.GetExternalActorId(monitorId.Runtime);
            }

            string id = this.GetActorId(monitorId);
            if (!this.InBox.TryGetValue(id, out List<EventInfo> inbox))
            {
                inbox = new List<EventInfo>();
                this.InBox[id] = inbox;
            }

            inbox.Add(new EventInfo() { ActorId = senderId.ToString(), State = currStateName, Event = eventName });
        }

        /// <summary>
        /// Called when a monitor executes an action.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that is executing the action.</param>
        /// <param name="monitorId">ID of the monitor that is executing the action</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public void OnMonitorAction(string monitorTypeName, ActorId monitorId, string currStateName, string actionName)
        {
            this.Next?.OnMonitorAction(monitorTypeName, monitorId, currStateName, actionName);

            string id = this.GetActorId(monitorId);
            // Monitors process actions immediately, so this state transition is a result of the only event in the inbox.
            if (this.InBox.TryGetValue(id, out List<EventInfo> inbox) && inbox.Count > 0)
            {
                var e = inbox[inbox.Count - 1];
                inbox.RemoveAt(inbox.Count - 1);
                // draw the link connecting the Sender state to this state!
                var source = this.GetOrCreateChild(e.ActorId, e.State);
                var target = this.GetOrCreateChild(id, GetLabel(monitorId, currStateName));
                this.GetOrCreateEventLink(source, target, e);
            }
        }

        /// <summary>
        /// Called for general error reporting via pre-constructed text.
        /// </summary>
        /// <param name="text">The text of the error report.</param>
        public void OnError(string text)
        {
            this.Next?.OnError(text);
        }

        /// <summary>
        /// Return current graph and reset for next iteration.
        /// </summary>
        /// <returns>The graph.</returns>
        public Graph SnapshotGraph()
        {
            Graph result = this.CurrentGraph;
            // start fresh.
            this.CurrentGraph = null;
            return result;
        }

        /// <summary>
        /// Called for errors detected by a specific scheduling strategy.
        /// </summary>
        /// <param name="strategy">The scheduling strategy that was used.</param>
        /// <param name="strategyDescription">More information about the scheduling strategy.</param>
        public void OnStrategyError(SchedulingStrategy strategy, string strategyDescription)
        {
            this.Next?.OnStrategyError(strategy, strategyDescription);
        }

        private GraphNode GetOrCreateChild(string actorId, string stateName)
        {
            GraphNode parent = this.Graph.GetOrCreateNode(actorId);
            parent.AddAttribute("Group", "Expanded");
            GraphNode child = this.Graph.GetOrCreateNode(actorId + "." + stateName, stateName);
            this.Graph.GetOrCreateLink(parent, child, null, "Contains");
            return child;
        }

        /// <summary>
        /// Return a special ActorId representing external code (e.g. code that calls SendEvent).
        /// </summary>
        internal ActorId GetExternalActorId(IMachineRuntime runtime)
        {
            if (this.ExternalActorId == null)
            {
                this.ExternalActorId = new ActorId(typeof(ExternalCode), "External", (CoyoteRuntime)runtime, true);
            }

            return this.ExternalActorId;
        }

        private class ExternalCode
        {
        }

        private ActorId ExternalActorId;
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
            if (!this.InternalNodes.TryGetValue(newNode.Id, out GraphNode node))
            {
                node = newNode;
                this.InternalNodes.Add(newNode.Id, node);
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
            StringWriter writer = new StringWriter();
            this.WriteDgml(writer);
            return writer.ToString();
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
