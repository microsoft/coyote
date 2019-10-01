// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Coyote.Machines
{
    /// <summary>
    /// Abstract class representing a state of a Coyote machine.
    /// </summary>
    public abstract class MachineState
    {
        /// <summary>
        /// Attribute for declaring that a state of a machine
        /// is the start one.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class)]
        protected sealed class StartAttribute : Attribute
        {
        }

        /// <summary>
        /// Attribute for declaring what action to perform
        /// when entering a machine state.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class)]
        protected sealed class OnEntryAttribute : Attribute
        {
            /// <summary>
            /// Action name.
            /// </summary>
            internal readonly string Action;

            /// <summary>
            /// Initializes a new instance of the <see cref="OnEntryAttribute"/> class.
            /// </summary>
            /// <param name="actionName">Action name</param>
            public OnEntryAttribute(string actionName)
            {
                this.Action = actionName;
            }
        }

        /// <summary>
        /// Attribute for declaring what action to perform
        /// when exiting a machine state.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class)]
        protected sealed class OnExitAttribute : Attribute
        {
            /// <summary>
            /// Action name.
            /// </summary>
            internal string Action;

            /// <summary>
            /// Initializes a new instance of the <see cref="OnExitAttribute"/> class.
            /// </summary>
            /// <param name="actionName">Action name</param>
            public OnExitAttribute(string actionName)
            {
                this.Action = actionName;
            }
        }

        /// <summary>
        /// Attribute for declaring which state a machine should transition to
        /// when it receives an event in a given state.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        protected sealed class OnEventGotoStateAttribute : Attribute
        {
            /// <summary>
            /// Event type.
            /// </summary>
            internal readonly Type Event;

            /// <summary>
            /// State type.
            /// </summary>
            internal readonly Type State;

            /// <summary>
            /// Action name.
            /// </summary>
            internal readonly string Action;

            /// <summary>
            /// Initializes a new instance of the <see cref="OnEventGotoStateAttribute"/> class.
            /// </summary>
            /// <param name="eventType">Event type</param>
            /// <param name="stateType">State type</param>
            public OnEventGotoStateAttribute(Type eventType, Type stateType)
            {
                this.Event = eventType;
                this.State = stateType;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="OnEventGotoStateAttribute"/> class.
            /// </summary>
            /// <param name="eventType">Event type</param>
            /// <param name="stateType">State type</param>
            /// <param name="actionName">Name of action to perform on exit</param>
            public OnEventGotoStateAttribute(Type eventType, Type stateType, string actionName)
            {
                this.Event = eventType;
                this.State = stateType;
                this.Action = actionName;
            }
        }

        /// <summary>
        /// Attribute for declaring which state a machine should push transition to
        /// when it receives an event in a given state.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        protected sealed class OnEventPushStateAttribute : Attribute
        {
            /// <summary>
            /// Event type.
            /// </summary>
            internal Type Event;

            /// <summary>
            /// State type.
            /// </summary>
            internal Type State;

            /// <summary>
            /// Initializes a new instance of the <see cref="OnEventPushStateAttribute"/> class.
            /// </summary>
            /// <param name="eventType">Event type</param>
            /// <param name="stateType">State type</param>
            public OnEventPushStateAttribute(Type eventType, Type stateType)
            {
                this.Event = eventType;
                this.State = stateType;
            }
        }

        /// <summary>
        /// Attribute for declaring what action a machine should perform
        /// when it receives an event in a given state.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        protected sealed class OnEventDoActionAttribute : Attribute
        {
            /// <summary>
            /// Event type.
            /// </summary>
            internal Type Event;

            /// <summary>
            /// Action name.
            /// </summary>
            internal string Action;

            /// <summary>
            /// Initializes a new instance of the <see cref="OnEventDoActionAttribute"/> class.
            /// </summary>
            /// <param name="eventType">Event type</param>
            /// <param name="actionName">Action name</param>
            public OnEventDoActionAttribute(Type eventType, string actionName)
            {
                this.Event = eventType;
                this.Action = actionName;
            }
        }

        /// <summary>
        /// Attribute for declaring what events should be deferred in
        /// a machine state.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class)]
        protected sealed class DeferEventsAttribute : Attribute
        {
            /// <summary>
            /// Event types.
            /// </summary>
            internal Type[] Events;

            /// <summary>
            /// Initializes a new instance of the <see cref="DeferEventsAttribute"/> class.
            /// </summary>
            /// <param name="eventTypes">Event types</param>
            public DeferEventsAttribute(params Type[] eventTypes)
            {
                this.Events = eventTypes;
            }
        }

        /// <summary>
        /// Attribute for declaring what events should be ignored in
        /// a machine state.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class)]
        protected sealed class IgnoreEventsAttribute : Attribute
        {
            /// <summary>
            /// Event types.
            /// </summary>
            internal Type[] Events;

            /// <summary>
            /// Initializes a new instance of the <see cref="IgnoreEventsAttribute"/> class.
            /// </summary>
            /// <param name="eventTypes">Event types</param>
            public IgnoreEventsAttribute(params Type[] eventTypes)
            {
                this.Events = eventTypes;
            }
        }

        /// <summary>
        /// The entry action of the state.
        /// </summary>
        internal string EntryAction { get; private set; }

        /// <summary>
        /// The exit action of the state.
        /// </summary>
        internal string ExitAction { get; private set; }

        /// <summary>
        /// Dictionary containing all the goto state transitions.
        /// </summary>
        internal Dictionary<Type, GotoStateTransition> GotoTransitions;

        /// <summary>
        /// Dictionary containing all the push state transitions.
        /// </summary>
        internal Dictionary<Type, PushStateTransition> PushTransitions;

        /// <summary>
        /// Dictionary containing all the action bindings.
        /// </summary>
        internal Dictionary<Type, ActionBinding> ActionBindings;

        /// <summary>
        /// Set of ignored event types.
        /// </summary>
        internal HashSet<Type> IgnoredEvents;

        /// <summary>
        /// Set of deferred event types.
        /// </summary>
        internal HashSet<Type> DeferredEvents;

        /// <summary>
        /// True if this is the start state.
        /// </summary>
        internal bool IsStart { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineState"/> class.
        /// </summary>
        protected MachineState()
        {
        }

        /// <summary>
        /// Initializes the state.
        /// </summary>
        internal void InitializeState()
        {
            this.IsStart = false;

            this.GotoTransitions = new Dictionary<Type, GotoStateTransition>();
            this.PushTransitions = new Dictionary<Type, PushStateTransition>();
            this.ActionBindings = new Dictionary<Type, ActionBinding>();

            this.IgnoredEvents = new HashSet<Type>();
            this.DeferredEvents = new HashSet<Type>();

            if (this.GetType().GetCustomAttribute(typeof(OnEntryAttribute), true) is OnEntryAttribute entryAttribute)
            {
                this.EntryAction = entryAttribute.Action;
            }

            if (this.GetType().GetCustomAttribute(typeof(OnExitAttribute), true) is OnExitAttribute exitAttribute)
            {
                this.ExitAction = exitAttribute.Action;
            }

            if (this.GetType().IsDefined(typeof(StartAttribute), false))
            {
                this.IsStart = true;
            }

            // Events with already declared handlers.
            var handledEvents = new HashSet<Type>();

            // Install event handlers.
            this.InstallGotoTransitions(handledEvents);
            this.InstallPushTransitions(handledEvents);
            this.InstallActionHandlers(handledEvents);
            this.InstallIgnoreHandlers(handledEvents);
            this.InstallDeferHandlers(handledEvents);
        }

        /// <summary>
        /// Declares goto event handlers, if there are any.
        /// </summary>
        private void InstallGotoTransitions(HashSet<Type> handledEvents)
        {
            var gotoAttributes = this.GetType().GetCustomAttributes(typeof(OnEventGotoStateAttribute), false)
                as OnEventGotoStateAttribute[];

            foreach (var attr in gotoAttributes)
            {
                CheckEventHandlerAlreadyDeclared(attr.Event, handledEvents);

                if (attr.Action is null)
                {
                    this.GotoTransitions.Add(attr.Event, new GotoStateTransition(attr.State));
                }
                else
                {
                    this.GotoTransitions.Add(attr.Event, new GotoStateTransition(attr.State, attr.Action));
                }

                handledEvents.Add(attr.Event);
            }

            this.InheritGotoTransitions(this.GetType().BaseType, handledEvents);
        }

        /// <summary>
        /// Inherits goto event handlers from a base state, if there is one.
        /// </summary>
        private void InheritGotoTransitions(Type baseState, HashSet<Type> handledEvents)
        {
            if (!baseState.IsSubclassOf(typeof(MachineState)))
            {
                return;
            }

            var gotoAttributesInherited = baseState.GetCustomAttributes(typeof(OnEventGotoStateAttribute), false)
                as OnEventGotoStateAttribute[];

            var gotoTransitionsInherited = new Dictionary<Type, GotoStateTransition>();
            foreach (var attr in gotoAttributesInherited)
            {
                if (this.GotoTransitions.ContainsKey(attr.Event))
                {
                    continue;
                }

                CheckEventHandlerAlreadyInherited(attr.Event, baseState, handledEvents);

                if (attr.Action is null)
                {
                    gotoTransitionsInherited.Add(attr.Event, new GotoStateTransition(attr.State));
                }
                else
                {
                    gotoTransitionsInherited.Add(attr.Event, new GotoStateTransition(attr.State, attr.Action));
                }

                handledEvents.Add(attr.Event);
            }

            foreach (var kvp in gotoTransitionsInherited)
            {
                this.GotoTransitions.Add(kvp.Key, kvp.Value);
            }

            this.InheritGotoTransitions(baseState.BaseType, handledEvents);
        }

        /// <summary>
        /// Declares push event handlers, if there are any.
        /// </summary>
        private void InstallPushTransitions(HashSet<Type> handledEvents)
        {
            var pushAttributes = this.GetType().GetCustomAttributes(typeof(OnEventPushStateAttribute), false)
                as OnEventPushStateAttribute[];

            foreach (var attr in pushAttributes)
            {
                CheckEventHandlerAlreadyDeclared(attr.Event, handledEvents);

                this.PushTransitions.Add(attr.Event, new PushStateTransition(attr.State));
                handledEvents.Add(attr.Event);
            }

            this.InheritPushTransitions(this.GetType().BaseType, handledEvents);
        }

        /// <summary>
        /// Inherits push event handlers from a base state, if there is one.
        /// </summary>
        private void InheritPushTransitions(Type baseState, HashSet<Type> handledEvents)
        {
            if (!baseState.IsSubclassOf(typeof(MachineState)))
            {
                return;
            }

            var pushAttributesInherited = baseState.GetCustomAttributes(typeof(OnEventPushStateAttribute), false)
                as OnEventPushStateAttribute[];

            var pushTransitionsInherited = new Dictionary<Type, PushStateTransition>();
            foreach (var attr in pushAttributesInherited)
            {
                if (this.PushTransitions.ContainsKey(attr.Event))
                {
                    continue;
                }

                CheckEventHandlerAlreadyInherited(attr.Event, baseState, handledEvents);

                pushTransitionsInherited.Add(attr.Event, new PushStateTransition(attr.State));
                handledEvents.Add(attr.Event);
            }

            foreach (var kvp in pushTransitionsInherited)
            {
                this.PushTransitions.Add(kvp.Key, kvp.Value);
            }

            this.InheritPushTransitions(baseState.BaseType, handledEvents);
        }

        /// <summary>
        /// Declares action event handlers, if there are any.
        /// </summary>
        private void InstallActionHandlers(HashSet<Type> handledEvents)
        {
            var doAttributes = this.GetType().GetCustomAttributes(typeof(OnEventDoActionAttribute), false)
                as OnEventDoActionAttribute[];

            foreach (var attr in doAttributes)
            {
                CheckEventHandlerAlreadyDeclared(attr.Event, handledEvents);

                this.ActionBindings.Add(attr.Event, new ActionBinding(attr.Action));
                handledEvents.Add(attr.Event);
            }

            this.InheritActionHandlers(this.GetType().BaseType, handledEvents);
        }

        /// <summary>
        /// Inherits action event handlers from a base state, if there is one.
        /// </summary>
        private void InheritActionHandlers(Type baseState, HashSet<Type> handledEvents)
        {
            if (!baseState.IsSubclassOf(typeof(MachineState)))
            {
                return;
            }

            var doAttributesInherited = baseState.GetCustomAttributes(typeof(OnEventDoActionAttribute), false)
                as OnEventDoActionAttribute[];

            var actionBindingsInherited = new Dictionary<Type, ActionBinding>();
            foreach (var attr in doAttributesInherited)
            {
                if (this.ActionBindings.ContainsKey(attr.Event))
                {
                    continue;
                }

                CheckEventHandlerAlreadyInherited(attr.Event, baseState, handledEvents);

                actionBindingsInherited.Add(attr.Event, new ActionBinding(attr.Action));
                handledEvents.Add(attr.Event);
            }

            foreach (var kvp in actionBindingsInherited)
            {
                this.ActionBindings.Add(kvp.Key, kvp.Value);
            }

            this.InheritActionHandlers(baseState.BaseType, handledEvents);
        }

        /// <summary>
        /// Declares ignore event handlers, if there are any.
        /// </summary>
        private void InstallIgnoreHandlers(HashSet<Type> handledEvents)
        {
            if (this.GetType().GetCustomAttribute(typeof(IgnoreEventsAttribute), false) is IgnoreEventsAttribute ignoreEventsAttribute)
            {
                foreach (var e in ignoreEventsAttribute.Events)
                {
                    CheckEventHandlerAlreadyDeclared(e, handledEvents);
                }

                this.IgnoredEvents.UnionWith(ignoreEventsAttribute.Events);
                handledEvents.UnionWith(ignoreEventsAttribute.Events);
            }

            this.InheritIgnoreHandlers(this.GetType().BaseType, handledEvents);
        }

        /// <summary>
        /// Inherits ignore event handlers from a base state, if there is one.
        /// </summary>
        private void InheritIgnoreHandlers(Type baseState, HashSet<Type> handledEvents)
        {
            if (!baseState.IsSubclassOf(typeof(MachineState)))
            {
                return;
            }

            if (baseState.GetCustomAttribute(typeof(IgnoreEventsAttribute), false) is IgnoreEventsAttribute ignoreEventsAttribute)
            {
                foreach (var e in ignoreEventsAttribute.Events)
                {
                    if (this.IgnoredEvents.Contains(e))
                    {
                        continue;
                    }

                    CheckEventHandlerAlreadyInherited(e, baseState, handledEvents);
                }

                this.IgnoredEvents.UnionWith(ignoreEventsAttribute.Events);
                handledEvents.UnionWith(ignoreEventsAttribute.Events);
            }

            this.InheritIgnoreHandlers(baseState.BaseType, handledEvents);
        }

        /// <summary>
        /// Declares defer event handlers, if there are any.
        /// </summary>
        private void InstallDeferHandlers(HashSet<Type> handledEvents)
        {
            if (this.GetType().GetCustomAttribute(typeof(DeferEventsAttribute), false) is DeferEventsAttribute deferEventsAttribute)
            {
                foreach (var e in deferEventsAttribute.Events)
                {
                    CheckEventHandlerAlreadyDeclared(e, handledEvents);
                }

                this.DeferredEvents.UnionWith(deferEventsAttribute.Events);
                handledEvents.UnionWith(deferEventsAttribute.Events);
            }

            this.InheritDeferHandlers(this.GetType().BaseType, handledEvents);
        }

        /// <summary>
        /// Inherits defer event handlers from a base state, if there is one.
        /// </summary>
        private void InheritDeferHandlers(Type baseState, HashSet<Type> handledEvents)
        {
            if (!baseState.IsSubclassOf(typeof(MachineState)))
            {
                return;
            }

            if (baseState.GetCustomAttribute(typeof(DeferEventsAttribute), false) is DeferEventsAttribute deferEventsAttribute)
            {
                foreach (var e in deferEventsAttribute.Events)
                {
                    if (this.DeferredEvents.Contains(e))
                    {
                        continue;
                    }

                    CheckEventHandlerAlreadyInherited(e, baseState, handledEvents);
                }

                this.DeferredEvents.UnionWith(deferEventsAttribute.Events);
                handledEvents.UnionWith(deferEventsAttribute.Events);
            }

            this.InheritDeferHandlers(baseState.BaseType, handledEvents);
        }

        /// <summary>
        /// Checks if an event handler has been already declared.
        /// </summary>
        private static void CheckEventHandlerAlreadyDeclared(Type e, HashSet<Type> handledEvents)
        {
            if (handledEvents.Contains(e))
            {
                throw new InvalidOperationException($"declared multiple handlers for event '{e}'");
            }
        }

        /// <summary>
        /// Checks if an event handler has been already inherited.
        /// </summary>
        private static void CheckEventHandlerAlreadyInherited(Type e, Type baseState, HashSet<Type> handledEvents)
        {
            if (handledEvents.Contains(e))
            {
                throw new InvalidOperationException($"inherited multiple handlers for event '{e}' from state '{baseState}'");
            }
        }
    }
}
