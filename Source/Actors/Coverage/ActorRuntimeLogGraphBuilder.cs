// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Actors.Timers.Mocks;
using Microsoft.Coyote.Coverage;
using MonitorEvent = Microsoft.Coyote.Specifications.Monitor.Event;

namespace Microsoft.Coyote.Actors.Coverage
{
    /// <summary>
    /// Implements the <see cref="IActorRuntimeLog"/> and builds a directed graph
    /// from the recorded events and state transitions.
    /// </summary>
    internal class ActorRuntimeLogGraphBuilder : IActorRuntimeLog
    {
        private const string ActorCategory = "Actor";
        internal const string StateMachineCategory = "StateMachine";
        private const string MonitorCategory = "Monitor";
        internal const string ExternalCodeName = "ExternalCode";
        internal const string ExternalStateName = "ExternalState";

        /// <summary>
        /// The currently manipulated coverage graph.
        /// </summary>
        private CoverageGraph CurrentGraph;

        /// <summary>
        /// Current dequeued event.
        /// </summary>
        private readonly Dictionary<ActorId, EventInfo> Dequeued = new Dictionary<ActorId, EventInfo>();

        /// <summary>
        /// Halted state for given actor.
        /// </summary>
        private readonly Dictionary<ActorId, string> HaltedStates = new Dictionary<ActorId, string>();

        /// <summary>
        /// Merge events from node A to node B instead of making them separate links.
        /// </summary>
        internal bool MergeEventLinks { get; private set; }

        /// <summary>
        /// Set this boolean to true to get a collapsed graph showing only machine types, states and events.
        /// </summary>
        internal bool CollapseInstances { get; private set; }

        private class EventInfo
        {
            internal string Name;
            internal string Type;
            internal string State;
            internal string Event;
            internal string HandlingState;
        }

        private readonly Dictionary<string, List<EventInfo>> Inbox = new Dictionary<string, List<EventInfo>>();
        private static readonly Dictionary<string, string> EventAliases = new Dictionary<string, string>();
        private readonly HashSet<string> Namespaces = new HashSet<string>();
        private static readonly char[] TypeSeparators = new char[] { '.', '+' };

        /// <summary>
        /// Get or set the underlying logging object.
        /// </summary>
        /// <remarks>
        /// See <see href="/coyote/concepts/actors/logging">Logging</see> for more information.
        /// </remarks>
        internal TextWriter Logger { get; set; }

        /// <summary>
        /// Get the <see cref="CoverageGraph"/> object built by this logger.
        /// </summary>
        internal CoverageGraph Graph => this.CurrentGraph ?? new CoverageGraph();

        private class DoActionEvent : Event
        {
        }

        static ActorRuntimeLogGraphBuilder()
        {
            EventAliases[typeof(GotoStateEvent).FullName] = "goto";
            EventAliases[typeof(HaltEvent).FullName] = "halt";
            EventAliases[typeof(DefaultEvent).FullName] = "default";
            EventAliases[typeof(DoActionEvent).FullName] = "do";
            EventAliases[typeof(PushStateEvent).FullName] = "push";
            EventAliases[typeof(PopStateEvent).FullName] = "pop";
            EventAliases[typeof(QuiescentEvent).FullName] = "quiescent";
            EventAliases[typeof(WildCardEvent).FullName] = "*";
            EventAliases[typeof(TimerElapsedEvent).FullName] = "timer_elapsed";
            EventAliases[typeof(TimerSetupEvent).FullName] = "timer_setup";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorRuntimeLogGraphBuilder"/> class.
        /// </summary>
        internal ActorRuntimeLogGraphBuilder(bool mergeEventLinks, bool collapseInstances)
        {
            this.MergeEventLinks = mergeEventLinks;
            this.CollapseInstances = collapseInstances;
            this.CurrentGraph = new CoverageGraph();
        }

        /// <inheritdoc/>
        public void OnCreateActor(ActorId id, string creatorName, string creatorType)
        {
            lock (this.Inbox)
            {
                var resolvedId = this.GetResolveActorId(id?.Name, id?.Type);
                CoverageGraph.Node node = this.Graph.GetOrCreateNode(resolvedId);
                node.Category = ActorCategory;

                if (!string.IsNullOrEmpty(creatorName))
                {
                    var creatorId = this.GetResolveActorId(creatorName, creatorType);
                    CoverageGraph.Node creator = this.Graph.GetOrCreateNode(creatorId);
                    this.GetOrCreateEventLink(creator, node, new EventInfo() { Event = "CreateActor" });
                }
            }
        }

        /// <inheritdoc/>
        public void OnCreateStateMachine(ActorId id, string creatorName, string creatorType)
        {
            lock (this.Inbox)
            {
                var resolvedId = this.GetResolveActorId(id?.Name, id?.Type);
                CoverageGraph.Node node = this.Graph.GetOrCreateNode(resolvedId);
                node.Category = StateMachineCategory;

                if (!string.IsNullOrEmpty(creatorName))
                {
                    var creatorId = this.GetResolveActorId(creatorName, creatorType);
                    CoverageGraph.Node creator = this.Graph.GetOrCreateNode(creatorId);
                    this.GetOrCreateEventLink(creator, node, new EventInfo() { Event = "CreateActor" });
                }
            }
        }

        /// <inheritdoc/>
        public void OnSendEvent(ActorId targetActorId, string senderName, string senderType, string senderStateName,
            Event e, Guid eventGroupId, bool isTargetHalted)
        {
            string eventName = e.GetType().FullName;
            this.AddEvent(targetActorId.Name, targetActorId.Type, senderName, senderType, senderStateName, eventName);
        }

        /// <inheritdoc/>
        public void OnRaiseEvent(ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            // Raising event to self.
            this.AddEvent(id.Name, id.Type, id.Name, id.Type, stateName, eventName);
        }

        /// <inheritdoc/>
        public void OnHandleRaisedEvent(ActorId id, string stateName, Event e)
        {
            lock (this.Inbox)
            {
                // We used the inbox to store raised event, but it should be the first one handled since
                // raised events are highest priority.
                string resolvedId = this.GetResolveActorId(id?.Name, id?.Type);
                lock (this.Inbox)
                {
                    if (this.Inbox.TryGetValue(resolvedId, out List<EventInfo> inbox))
                    {
                        string eventName = e.GetType().FullName;
                        for (int i = inbox.Count - 1; i >= 0; i--)
                        {
                            EventInfo info = inbox[i];
                            if (info.Event == eventName)
                            {
                                this.Dequeued[id] = info;
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void OnEnqueueEvent(ActorId id, Event e)
        {
        }

        /// <inheritdoc/>
        public void OnDequeueEvent(ActorId id, string stateName, Event e)
        {
            lock (this.Inbox)
            {
                var resolvedId = this.GetResolveActorId(id?.Name, id?.Type);
                string eventName = e.GetType().FullName;
                EventInfo info = this.PopEvent(resolvedId, eventName);
                if (info != null)
                {
                    this.Dequeued[id] = info;
                }
            }
        }

        private EventInfo PopEvent(string resolvedId, string eventName)
        {
            EventInfo result = null;
            lock (this.Inbox)
            {
                if (this.Inbox.TryGetValue(resolvedId, out List<EventInfo> inbox))
                {
                    for (int i = inbox.Count - 1; i >= 0; i--)
                    {
                        if (inbox[i].Event == eventName)
                        {
                            result = inbox[i];
                            inbox.RemoveAt(i);
                        }
                    }
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public void OnReceiveEvent(ActorId id, string stateName, Event e, bool wasBlocked)
        {
            string resolvedId = this.GetResolveActorId(id?.Name, id?.Type);
            lock (this.Inbox)
            {
                if (this.Inbox.TryGetValue(resolvedId, out List<EventInfo> inbox))
                {
                    string eventName = e.GetType().FullName;
                    for (int i = inbox.Count - 1; i >= 0; i--)
                    {
                        EventInfo info = inbox[i];
                        if (info.Event == eventName)
                        {
                            // Yay, found it so we can draw the complete link connecting the Sender state to this state!
                            string category = string.IsNullOrEmpty(stateName) ? ActorCategory : StateMachineCategory;
                            var source = this.GetOrCreateChild(info.Name, info.Type, info.State);
                            var target = this.GetOrCreateChild(id?.Name, id?.Type, category, stateName);
                            this.GetOrCreateEventLink(source, target, info);
                            inbox.RemoveAt(i);
                            break;
                        }
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
        public void OnStateTransition(ActorId id, string stateName, bool isEntry)
        {
            if (isEntry)
            {
                // record the fact we have entered this state
                this.GetOrCreateChild(id?.Name, id?.Type, stateName);
            }
        }

        /// <inheritdoc/>
        public void OnExecuteAction(ActorId id, string handlingStateName, string currentStateName, string actionName)
        {
            this.LinkTransition(typeof(DoActionEvent), id, handlingStateName, currentStateName, null);
        }

        /// <inheritdoc/>
        public void OnGotoState(ActorId id, string currentStateName, string newStateName)
        {
            this.LinkTransition(typeof(GotoStateEvent), id, currentStateName, currentStateName, newStateName);
        }

        /// <inheritdoc/>
        public void OnPushState(ActorId id, string currentStateName, string newStateName)
        {
            this.LinkTransition(typeof(PushStateEvent), id, currentStateName, currentStateName, newStateName);
        }

        /// <inheritdoc/>
        public void OnPopState(ActorId id, string currentStateName, string restoredStateName)
        {
            if (!string.IsNullOrEmpty(currentStateName))
            {
                this.LinkTransition(typeof(PopStateEvent), id, currentStateName,
                    currentStateName, restoredStateName);
            }
        }

        /// <inheritdoc/>
        public void OnHalt(ActorId id, int inboxSize)
        {
            lock (this.Inbox)
            {
                this.HaltedStates.TryGetValue(id, out string stateName);
                var target = this.GetOrCreateChild(id?.Name, id?.Type, "Halt", "Halt");

                // Transition to the Halt state.
                if (!string.IsNullOrEmpty(stateName))
                {
                    var source = this.GetOrCreateChild(id?.Name, id?.Type, stateName);
                    this.GetOrCreateEventLink(source, target, new EventInfo() { Event = typeof(HaltEvent).FullName });
                }
            }
        }

        private int? GetLinkIndex(CoverageGraph.Node source, CoverageGraph.Node target, string id)
        {
            if (this.MergeEventLinks)
            {
                return null;
            }

            return this.Graph.GetUniqueLinkIndex(source, target, id);
        }

        /// <inheritdoc/>
        public void OnDefaultEventHandler(ActorId id, string stateName)
        {
            lock (this.Inbox)
            {
                string resolvedId = this.GetResolveActorId(id?.Name, id?.Type);
                string eventName = typeof(DefaultEvent).FullName;
                this.AddEvent(id.Name, id.Type, id.Name, id.Type, stateName, eventName);
                this.Dequeued[id] = this.PopEvent(resolvedId, eventName);
            }
        }

        /// <inheritdoc/>
        public void OnEventHandlerTerminated(ActorId id, string stateName, DequeueStatus dequeueStatus)
        {
        }

        /// <inheritdoc/>
        public void OnPopStateUnhandledEvent(ActorId actorId, string currentStateName, Event e)
        {
            lock (this.Inbox)
            {
                if (e is HaltEvent)
                {
                    this.HaltedStates[actorId] = currentStateName;
                }
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
        public void OnCreateMonitor(string monitorType)
        {
            lock (this.Inbox)
            {
                CoverageGraph.Node node = this.Graph.GetOrCreateNode(monitorType, monitorType);
                node.Category = MonitorCategory;
            }
        }

        /// <inheritdoc/>
        public void OnMonitorExecuteAction(string monitorType, string stateName, string actionName)
        {
            // Monitors process actions immediately, so this state transition is a result of the only event in the inbox.
            lock (this.Inbox)
            {
                if (this.Inbox.TryGetValue(monitorType, out List<EventInfo> inbox) && inbox.Count > 0)
                {
                    var e = inbox[inbox.Count - 1];
                    inbox.RemoveAt(inbox.Count - 1);
                    // Draw the link connecting the Sender state to this state!
                    var source = this.GetOrCreateChild(e.Name, e.Type, e.State);
                    var target = this.GetOrCreateChild(monitorType, monitorType, stateName);
                    this.GetOrCreateEventLink(source, target, e);
                }
            }
        }

        /// <inheritdoc/>
        public void OnMonitorProcessEvent(string monitorType, string stateName, string senderName, string senderType,
            string senderStateName, MonitorEvent e)
        {
            lock (this.Inbox)
            {
                string eventName = e.GetType().FullName;

                // Now add a fake event for internal monitor state transition that might now happen as a result of this event,
                // storing the monitor's current state in this event.
                var info = this.AddEvent(monitorType, monitorType, monitorType, monitorType, stateName, eventName);

                // Draw the link connecting the Sender state to this state!
                var source = this.GetOrCreateChild(senderName, senderType, senderStateName);
                var target = this.GetOrCreateChild(monitorType, monitorType, stateName);
                this.GetOrCreateEventLink(source, target, info);
            }
        }

        /// <inheritdoc/>
        public void OnMonitorRaiseEvent(string monitorType, string stateName, MonitorEvent e)
        {
            // Raising event to self.
            string eventName = e.GetType().FullName;
            this.AddEvent(monitorType, monitorType, monitorType, monitorType, stateName, eventName);
        }

        /// <inheritdoc/>
        public void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState)
        {
            if (isEntry)
            {
                lock (this.Inbox)
                {
                    // Monitors process events immediately (and does not call OnDequeue), so this state transition is a result of
                    // the fake event we created in OnMonitorProcessEvent.
                    if (this.Inbox.TryGetValue(monitorType, out List<EventInfo> inbox) && inbox.Count > 0)
                    {
                        var info = inbox[inbox.Count - 1];
                        inbox.RemoveAt(inbox.Count - 1);

                        // draw the link connecting the current state to this new state!
                        var source = this.GetOrCreateChild(monitorType, monitorType, info.State);

                        var shortStateName = this.GetLabel(monitorType, monitorType, stateName);
                        if (isInHotState.HasValue)
                        {
                            string suffix = (isInHotState is true) ? "[hot]" : "[cold]";
                            shortStateName += suffix;
                        }

                        string label = shortStateName;
                        var target = this.GetOrCreateChild(monitorType, monitorType, shortStateName, label);

                        // In case this node was already created, we may need to override the label here now that
                        // we know this is a hot state. This is because, unfortunately, other OnMonitor* methods
                        // do not provide the isInHotState parameter.
                        target.Label = label;
                        this.GetOrCreateEventLink(source, target, info);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void OnMonitorError(string monitorType, string stateName, bool? isInHotState)
        {
            var source = this.GetOrCreateChild(monitorType, monitorType, stateName);
            source.Category = "Error";
        }

        /// <inheritdoc/>
        public void OnRandom(bool result, string callerName, string callerType)
        {
        }

        /// <inheritdoc/>
        public void OnRandom(int result, string callerName, string callerType)
        {
        }

        /// <inheritdoc/>
        public void OnAssertionFailure(string error)
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
        internal CoverageGraph SnapshotGraph(bool reset)
        {
            CoverageGraph result = this.CurrentGraph;
            if (reset)
            {
                // Reset the graph to start fresh.
                this.CurrentGraph = null;
            }

            return result;
        }

        private string GetResolveActorId(string name, string type)
        {
            if (type is null)
            {
                // The sender id can be null if an event is fired from non-actor code.
                return ExternalCodeName;
            }

            if (this.CollapseInstances)
            {
                return type;
            }

            return name;
        }

        private EventInfo AddEvent(string targetName, string targetType, string senderName, string senderType,
            string senderStateName, string eventName)
        {
            string targetId = this.GetResolveActorId(targetName, targetType);
            EventInfo info = null;
            lock (this.Inbox)
            {
                if (!this.Inbox.TryGetValue(targetId, out List<EventInfo> inbox))
                {
                    inbox = new List<EventInfo>();
                    this.Inbox[targetId] = inbox;
                }

                info = new EventInfo()
                {
                    Name = senderName ?? ExternalCodeName,
                    Type = senderType ?? ExternalCodeName,
                    State = senderStateName,
                    Event = eventName
                };

                inbox.Add(info);
            }

            return info;
        }

        private void LinkTransition(Type transitionType, ActorId id, string handlingStateName,
            string currentStateName, string newStateName)
        {
            string name = id.Name;
            string type = id.Type;
            lock (this.Inbox)
            {
                if (this.Dequeued.TryGetValue(id, out EventInfo info))
                {
                    // Event was dequeued, but now we know what state is handling this event, so connect the dots...
                    if (info.Type != type || info.Name != name || info.State != currentStateName)
                    {
                        var source = this.GetOrCreateChild(info.Name, info.Type, info.State);
                        var target = this.GetOrCreateChild(name, type, currentStateName);
                        info.HandlingState = handlingStateName;
                        this.GetOrCreateEventLink(source, target, info);
                    }
                }

                if (newStateName != null)
                {
                    // Then this is a goto or push and we can draw that link also.
                    var source = this.GetOrCreateChild(name, type, currentStateName);
                    var target = this.GetOrCreateChild(name, type, newStateName);
                    if (info is null)
                    {
                        info = new EventInfo { Event = transitionType.FullName };
                    }

                    this.GetOrCreateEventLink(source, target, info);
                }

                this.Dequeued.Remove(id);
            }
        }

        private CoverageGraph.Node GetOrCreateChild(string name, string type, string stateName, string label = null)
        {
            CoverageGraph.Node child = null;
            lock (this.Inbox)
            {
                this.AddNamespace(type);

                var initalStateName = stateName;

                // make label relative to fully qualified actor id (it's usually a nested class).
                stateName = this.GetLabel(name, type, stateName);

                string id = this.GetResolveActorId(name, type);
                CoverageGraph.Node parent = this.Graph.GetOrCreateNode(id);
                parent.AddAttribute("Group", "Expanded");

                if (string.IsNullOrEmpty(label))
                {
                    label = stateName ?? ExternalStateName;
                }

                if (!string.IsNullOrEmpty(stateName))
                {
                    id += "." + stateName;
                }

                child = this.Graph.GetOrCreateNode(id, label);
                this.Graph.GetOrCreateLink(parent, child, null, null, "Contains");
            }

            return child;
        }

        private CoverageGraph.Link GetOrCreateEventLink(CoverageGraph.Node source, CoverageGraph.Node target, EventInfo e)
        {
            CoverageGraph.Link link = null;
            lock (this.Inbox)
            {
                string label = this.GetEventLabel(e.Event);
                var index = this.GetLinkIndex(source, target, label);
                var category = GetEventCategory(e.Event);
                link = this.Graph.GetOrCreateLink(source, target, index, label, category);
                if (this.MergeEventLinks)
                {
                    if (link.AddListAttribute("EventIds", e.Event) > 1)
                    {
                        link.Label = "*";
                    }
                }
                else
                {
                    if (e.Event != null)
                    {
                        link.AddAttribute("EventId", e.Event);
                    }

                    if (e.HandlingState != null)
                    {
                        link.AddAttribute("HandledBy", e.HandlingState);
                    }
                }
            }

            return link;
        }

        private void AddNamespace(string type)
        {
            if (type != null && !this.Namespaces.Contains(type))
            {
                string typeName = type;
                int index = typeName.Length;
                do
                {
                    typeName = typeName.Substring(0, index);
                    this.Namespaces.Add(typeName);
                    index = typeName.LastIndexOfAny(TypeSeparators);
                }
                while (index > 0);
            }
        }

        private string GetLabel(string name, string type, string fullyQualifiedName)
        {
            if (type is null)
            {
                // external code
                return fullyQualifiedName;
            }

            this.AddNamespace(type);
            if (string.IsNullOrEmpty(fullyQualifiedName))
            {
                // then this is probably an Actor, not a StateMachine.  For Actors we can invent a state
                // name equal to the short name of the class, this then looks like a Constructor which is fine.
                fullyQualifiedName = this.CollapseInstances ? type : name;
            }

            var index = fullyQualifiedName.LastIndexOfAny(TypeSeparators);
            if (index > 0)
            {
                fullyQualifiedName = fullyQualifiedName.Substring(index).Trim('+').Trim('.');
            }

            return fullyQualifiedName;
        }

        private string GetEventLabel(string fullyQualifiedName)
        {
            if (EventAliases.TryGetValue(fullyQualifiedName, out string label))
            {
                return label;
            }

            int i = fullyQualifiedName.LastIndexOfAny(TypeSeparators);
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

        private static string GetEventCategory(string fullyQualifiedName)
        {
            if (EventAliases.TryGetValue(fullyQualifiedName, out string label))
            {
                return label;
            }

            return null;
        }
    }
}
