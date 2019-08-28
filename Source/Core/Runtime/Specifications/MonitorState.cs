// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote
{
    /// <summary>
    /// Abstract class representing a state of a Coyote monitor.
    /// </summary>
    public abstract class MonitorState
    {
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
        /// Dictionary containing all the action bindings.
        /// </summary>
        internal Dictionary<Type, ActionBinding> ActionBindings;

        /// <summary>
        /// Set of ignored event types.
        /// </summary>
        internal HashSet<Type> IgnoredEvents;

        /// <summary>
        /// True if this is the start state.
        /// </summary>
        internal bool IsStart { get; private set; }

        /// <summary>
        /// Returns true if this is a hot state.
        /// </summary>
        internal bool IsHot { get; private set; }

        /// <summary>
        /// Returns true if this is a cold state.
        /// </summary>
        internal bool IsCold { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorState"/> class.
        /// </summary>
        protected MonitorState()
        {
        }

        /// <summary>
        /// Initializes the state.
        /// </summary>
        internal void InitializeState()
        {
            this.IsStart = false;
            this.IsHot = false;
            this.IsCold = false;

            this.GotoTransitions = new Dictionary<Type, GotoStateTransition>();
            this.ActionBindings = new Dictionary<Type, ActionBinding>();

            this.IgnoredEvents = new HashSet<Type>();

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

            if (this.GetType().IsDefined(typeof(HotAttribute), false))
            {
                this.IsHot = true;
            }

            if (this.GetType().IsDefined(typeof(ColdAttribute), false))
            {
                this.IsCold = true;
            }

            // Events with already declared handlers.
            var handledEvents = new HashSet<Type>();

            // Install event handlers.
            this.InstallGotoTransitions(handledEvents);
            this.InstallActionHandlers(handledEvents);
            this.InstallIgnoreHandlers(handledEvents);
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
            if (!baseState.IsSubclassOf(typeof(MonitorState)))
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
            if (!baseState.IsSubclassOf(typeof(MonitorState)))
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
            if (!baseState.IsSubclassOf(typeof(MonitorState)))
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
