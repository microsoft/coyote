// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Utilities;
using EventInfo = Microsoft.Coyote.Runtime.EventInfo;

namespace Microsoft.Coyote.Specifications
{
    /// <summary>
    /// Abstract class representing a specification monitor.
    /// </summary>
    /// <remarks>
    /// See <see href="/coyote/learn/specifications/overview">Specifications Overview</see> for more information.
    /// </remarks>
    public abstract class Monitor
    {
        /// <summary>
        /// Map from monitor types to a set of all possible states types.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, HashSet<Type>> StateTypeMap =
            new ConcurrentDictionary<Type, HashSet<Type>>();

        /// <summary>
        /// Map from monitor types to a set of all available states.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, HashSet<State>> StateMap =
            new ConcurrentDictionary<Type, HashSet<State>>();

        /// <summary>
        /// Map from monitor types to a set of all available actions.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Dictionary<string, MethodInfo>> MonitorActionMap =
            new ConcurrentDictionary<Type, Dictionary<string, MethodInfo>>();

        /// <summary>
        /// A cached array that contains a single event type.
        /// </summary>
        private static readonly Type[] SingleEventTypeArray = new Type[] { typeof(Event) };

        /// <summary>
        /// The runtime that executes this monitor.
        /// </summary>
        private CoyoteRuntime Runtime;

        /// <summary>
        /// The active monitor state.
        /// </summary>
        private State ActiveState;

        /// <summary>
        /// Dictionary containing all the current goto state transitions.
        /// </summary>
        internal Dictionary<Type, GotoStateTransition> GotoTransitions;

        /// <summary>
        /// Dictionary containing all the current action bindings.
        /// </summary>
        internal Dictionary<Type, ActionEventHandlerDeclaration> ActionBindings;

        /// <summary>
        /// Map from action names to cached action delegates.
        /// </summary>
        private readonly Dictionary<string, CachedDelegate> ActionMap;

        /// <summary>
        /// Set of currently ignored event types.
        /// </summary>
        private HashSet<Type> IgnoredEvents;

        /// <summary>
        /// A counter that increases in each step of the execution,
        /// as long as the monitor remains in a hot state. If the
        /// temperature reaches the specified limit, then a potential
        /// liveness bug has been found.
        /// </summary>
        private int LivenessTemperature;

        /// <summary>
        /// The unique monitor id.
        /// </summary>
        internal ActorId Id { get; private set; }

        /// <summary>
        /// Gets the name of this monitor.
        /// </summary>
        protected internal string Name => this.Id.Name;

        /// <summary>
        /// The logger installed to the runtime.
        /// </summary>
        /// <remarks>
        /// See <see href="/coyote/learn/advanced/logging" >Logging</see> for more information.
        /// </remarks>
        protected TextWriter Logger => this.Runtime.Logger;

        /// <summary>
        /// Gets the current state.
        /// </summary>
        protected internal Type CurrentState
        {
            get
            {
                if (this.ActiveState is null)
                {
                    return null;
                }

                return this.ActiveState.GetType();
            }
        }

        /// <summary>
        /// Gets the current state name.
        /// </summary>
        internal string CurrentStateName
        {
            get => NameResolver.GetQualifiedStateName(this.CurrentState);
        }

        /// <summary>
        /// Gets the current state name with temperature.
        /// </summary>
        internal string CurrentStateNameWithTemperature
        {
            get
            {
                return this.CurrentStateName +
                    (this.IsInHotState() ? "[hot]" :
                    this.IsInColdState() ? "[cold]" :
                    string.Empty);
            }
        }

        /// <summary>
        /// User-defined hashed state of the monitor. Override to improve the
        /// accuracy of stateful techniques during testing.
        /// </summary>
        protected virtual int HashedState => 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="Monitor"/> class.
        /// </summary>
        protected Monitor()
            : base()
        {
            this.ActionMap = new Dictionary<string, CachedDelegate>();
            this.LivenessTemperature = 0;
        }

        /// <summary>
        /// Initializes this monitor.
        /// </summary>
        /// <param name="runtime">The runtime that executes this monitor.</param>
        /// <param name="id">The monitor id.</param>
        internal void Initialize(CoyoteRuntime runtime, ActorId id)
        {
            this.Id = id;
            this.Runtime = runtime;
        }

        /// <summary>
        /// Raises an <see cref="Event"/> at the end of the current action.
        /// </summary>
        /// <param name="e">The event to raise.</param>
        /// <returns>The raise event transition.</returns>
        protected Transition RaiseEvent(Event e)
        {
            this.Assert(e != null, "{0} is raising a null event.", this.GetType().Name);
            return new Transition(Transition.Type.Raise, default, e);
        }

        /// <summary>
        /// Transitions the monitor to the specified <see cref="State"/>
        /// at the end of the current action.
        /// </summary>
        /// <typeparam name="S">Type of the state.</typeparam>
        /// <returns>The goto state transition.</returns>
        protected Transition GotoState<S>()
            where S : State =>
            this.GotoState(typeof(S));

        /// <summary>
        /// Transitions the monitor to the specified <see cref="State"/>
        /// at the end of the current action.
        /// </summary>
        /// <param name="state">Type of the state.</param>
        /// <returns>The goto state transition.</returns>
        protected Transition GotoState(Type state)
        {
            // If the state is not a state of the monitor, then report an error and exit.
            this.Assert(StateTypeMap[this.GetType()].Any(val => val.DeclaringType.Equals(state.DeclaringType) && val.Name.Equals(state.Name)),
                "{0} is trying to transition to non-existing state '{1}'.", this.GetType().Name, state.Name);
            return new Transition(Transition.Type.Goto, state, default);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        protected void Assert(bool predicate)
        {
            this.Runtime.Assert(predicate);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        protected void Assert(bool predicate, string s, params object[] args)
        {
            this.Runtime.Assert(predicate, s, args);
        }

        /// <summary>
        /// Notifies the monitor to handle the received event.
        /// </summary>
        /// <param name="sender">The sender of this event</param>
        /// <param name="e">The event to monitor.</param>
        internal void MonitorEvent(Actor sender, Event e)
        {
            string senderState = null;
            if (sender is StateMachine machine)
            {
                senderState = machine.CurrentStateName;
            }

            this.Runtime.LogWriter.LogMonitorProcessEvent(sender?.Id, senderState, this.GetType().Name,
                this.Id, this.CurrentStateName, e);
            this.HandleEvent(e);
        }

        /// <summary>
        /// Handles the given event.
        /// </summary>
        private void HandleEvent(Event e)
        {
            // Do not process an ignored event.
            if (this.IsEventIgnoredInCurrentState(e))
            {
                return;
            }

            while (true)
            {
                if (this.ActiveState is null)
                {
                    // If the event cannot be handled, then report an error and exit.
                    this.Assert(false, "{0} received event '{1}' that cannot be handled.",
                        this.GetType().Name, e.GetType().FullName);
                }

                // If current state cannot handle the event then null the state.
                if (!this.CanHandleEvent(e.GetType()))
                {
                    this.Runtime.NotifyExitedState(this);
                    this.ActiveState = null;
                    continue;
                }

                if (e.GetType() == typeof(GotoStateEvent))
                {
                    // Checks if the event is a goto state event.
                    Type targetState = (e as GotoStateEvent).State;
                    this.GotoState(targetState, null, e);
                }
                else if (this.GotoTransitions.ContainsKey(e.GetType()))
                {
                    // Checks if the event can trigger a goto state transition.
                    var transition = this.GotoTransitions[e.GetType()];
                    this.GotoState(transition.TargetState, transition.Lambda, e);
                }
                else if (this.GotoTransitions.ContainsKey(typeof(WildCardEvent)))
                {
                    // Checks if the event can trigger a goto state transition.
                    var transition = this.GotoTransitions[typeof(WildCardEvent)];
                    this.GotoState(transition.TargetState, transition.Lambda, e);
                }
                else if (this.ActionBindings.ContainsKey(e.GetType()))
                {
                    // Checks if the event can trigger an action.
                    var handler = this.ActionBindings[e.GetType()];
                    this.Do(handler.Name, e);
                }
                else if (this.ActionBindings.ContainsKey(typeof(WildCardEvent)))
                {
                    // Checks if the event can trigger an action.
                    var handler = this.ActionBindings[typeof(WildCardEvent)];
                    this.Do(handler.Name, e);
                }

                break;
            }
        }

        /// <summary>
        /// Checks if the specified event is ignored in the current monitor state.
        /// </summary>
        private bool IsEventIgnoredInCurrentState(Event e)
        {
            if (this.IgnoredEvents.Contains(e.GetType()) ||
                this.IgnoredEvents.Contains(typeof(WildCardEvent)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Invokes an action.
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough]
        private void Do(string actionName, Event e)
        {
            CachedDelegate cachedAction = this.ActionMap[actionName];
            this.Runtime.NotifyInvokedAction(this, cachedAction.MethodInfo, e);
            Transition transition = this.ExecuteAction(cachedAction, e);
            this.ApplyEventHandlerTransition(transition);
        }

        /// <summary>
        /// Executes the on entry function of the current state.
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough]
        private void ExecuteCurrentStateOnEntry(Event e)
        {
            this.Runtime.NotifyEnteredState(this);

            CachedDelegate entryAction = null;
            if (this.ActiveState.EntryAction != null)
            {
                entryAction = this.ActionMap[this.ActiveState.EntryAction];
            }

            // Invokes the entry action of the new state,
            // if there is one available.
            if (entryAction != null)
            {
                Transition transition = this.ExecuteAction(entryAction, e);
                this.ApplyEventHandlerTransition(transition);
            }
        }

        /// <summary>
        /// Executes the on exit function of the current state.
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough]
        private void ExecuteCurrentStateOnExit(string eventHandlerExitActionName, Event e)
        {
            this.Runtime.NotifyExitedState(this);

            CachedDelegate exitAction = null;
            if (this.ActiveState.ExitAction != null)
            {
                exitAction = this.ActionMap[this.ActiveState.ExitAction];
            }

            // Invokes the exit action of the current state,
            // if there is one available.
            if (exitAction != null)
            {
                Transition transition = this.ExecuteAction(exitAction, e);
                this.Assert(transition.TypeValue is Transition.Type.None,
                    "{0} has performed a '{1}' transition from an OnExit action.",
                    this.Id, transition.TypeValue);
                this.ApplyEventHandlerTransition(transition);
            }

            // Invokes the exit action of the event handler,
            // if there is one available.
            if (eventHandlerExitActionName != null)
            {
                CachedDelegate eventHandlerExitAction = this.ActionMap[eventHandlerExitActionName];
                Transition transition = this.ExecuteAction(eventHandlerExitAction, e);
                this.Assert(transition.TypeValue is Transition.Type.None,
                    "{0} has performed a '{1}' transition from an OnExit action.",
                    this.Id, transition.TypeValue);
                this.ApplyEventHandlerTransition(transition);
            }
        }

        /// <summary>
        /// Executes the specified action.
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough]
        private Transition ExecuteAction(CachedDelegate cachedAction, Event e)
        {
            try
            {
                if (cachedAction.Handler is Func<Event, Transition> funcWithEventAndResult)
                {
                    return funcWithEventAndResult(e);
                }
                else if (cachedAction.Handler is Func<Transition> funcWithResult)
                {
                    return funcWithResult();
                }
                else if (cachedAction.Handler is Action<Event> actionWithEvent)
                {
                    actionWithEvent(e);
                }
                else if (cachedAction.Handler is Action action)
                {
                    action();
                }
            }
            catch (Exception ex)
            {
                Exception innerException = ex;
                while (innerException is TargetInvocationException)
                {
                    innerException = innerException.InnerException;
                }

                if (innerException is AggregateException)
                {
                    innerException = innerException.InnerException;
                }

                if (innerException is ExecutionCanceledException ||
                    innerException is TaskSchedulerException)
                {
                    throw;
                }
                else
                {
                    // Reports the unhandled exception.
                    this.ReportUnhandledException(innerException, cachedAction.MethodInfo.Name);
                }
            }

            return Transition.None;
        }

        /// <summary>
        /// Applies the specified event handler transition.
        /// </summary>
        private void ApplyEventHandlerTransition(Transition transition)
        {
            if (transition.TypeValue is Transition.Type.Raise)
            {
                var e = transition.Event;
                var eventOrigin = new EventOriginInfo(this.Id, this.GetType().FullName,
                NameResolver.GetQualifiedStateName(this.CurrentState));
                EventInfo raisedEvent = new EventInfo(e, eventOrigin);
                this.Runtime.NotifyRaisedEvent(this, e, raisedEvent);
                this.HandleEvent(e);
            }
            else if (transition.TypeValue is Transition.Type.Goto)
            {
                var e = new GotoStateEvent(transition.State);
                var eventOrigin = new EventOriginInfo(this.Id, this.GetType().FullName,
                NameResolver.GetQualifiedStateName(this.CurrentState));
                EventInfo raisedEvent = new EventInfo(e, eventOrigin);
                this.Runtime.NotifyRaisedEvent(this, e, raisedEvent);
                this.HandleEvent(e);
            }
        }

        /// <summary>
        /// Performs a goto transition to the given state.
        /// </summary>
        private void GotoState(Type s, string onExitActionName, Event e)
        {
            // The monitor performs the on exit statements of the current state.
            this.ExecuteCurrentStateOnExit(onExitActionName, e);

            var nextState = StateMap[this.GetType()].First(val => val.GetType().Equals(s));
            this.ConfigureStateTransitions(nextState);

            // The monitor transitions to the new state.
            this.ActiveState = nextState;

            if (nextState.IsCold)
            {
                this.LivenessTemperature = 0;
            }

            // The monitor performs the on entry statements of the new state.
            this.ExecuteCurrentStateOnEntry(e);
        }

        /// <summary>
        /// Checks if the state can handle the given event type. An event
        /// can be handled if it is deferred, or leads to a transition or
        /// action binding.
        /// </summary>
        private bool CanHandleEvent(Type e)
        {
            if (this.GotoTransitions.ContainsKey(e) ||
                this.GotoTransitions.ContainsKey(typeof(WildCardEvent)) ||
                this.ActionBindings.ContainsKey(e) ||
                this.ActionBindings.ContainsKey(typeof(WildCardEvent)) ||
                e == typeof(GotoStateEvent))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks the liveness temperature of the monitor and report
        /// a potential liveness bug if the temperature passes the
        /// specified threshold. Only works in a liveness monitor.
        /// </summary>
        internal void CheckLivenessTemperature()
        {
            if (this.ActiveState.IsHot &&
                this.Runtime.Configuration.LivenessTemperatureThreshold > 0)
            {
                this.LivenessTemperature++;
                this.Runtime.Assert(
                    this.LivenessTemperature <= this.Runtime.
                    Configuration.LivenessTemperatureThreshold,
                    "{0} detected potential liveness bug in hot state '{1}'.",
                    this.GetType().Name, this.CurrentStateName);
            }
        }

        /// <summary>
        /// Checks the liveness temperature of the monitor and report
        /// a potential liveness bug if the temperature passes the
        /// specified threshold. Only works in a liveness monitor.
        /// </summary>
        internal void CheckLivenessTemperature(int livenessTemperature)
        {
            if (livenessTemperature > this.Runtime.Configuration.LivenessTemperatureThreshold)
            {
                this.Runtime.Assert(
                    livenessTemperature <= this.Runtime.Configuration.LivenessTemperatureThreshold,
                    $"{this.GetType().Name} detected infinite execution that violates a liveness property.");
            }
        }

        /// <summary>
        /// Returns true if the monitor is in a hot state.
        /// </summary>
        internal bool IsInHotState() => this.ActiveState.IsHot;

        /// <summary>
        /// Returns true if the monitor is in a hot state. Also outputs
        /// the name of the current state.
        /// </summary>
        internal bool IsInHotState(out string stateName)
        {
            stateName = this.CurrentStateName;
            return this.ActiveState.IsHot;
        }

        /// <summary>
        /// Returns true if the monitor is in a cold state.
        /// </summary>
        internal bool IsInColdState() => this.ActiveState.IsCold;

        /// <summary>
        /// Returns true if the monitor is in a cold state. Also outputs
        /// the name of the current state.
        /// </summary>
        internal bool IsInColdState(out string stateName)
        {
            stateName = this.CurrentStateName;
            return this.ActiveState.IsCold;
        }

        /// <summary>
        /// Returns a nullable boolean indicating liveness temperature: true for hot, false for cold, else null.
        /// </summary>
        internal bool? GetHotState()
        {
            return this.IsInHotState() ? true :
                this.IsInColdState() ? (bool?)false :
                null;
        }

        /// <summary>
        /// Returns the hashed state of this monitor.
        /// </summary>
        internal int GetHashedState()
        {
            unchecked
            {
                var hash = 19;

                hash = (hash * 31) + this.GetType().GetHashCode();
                hash = (hash * 31) + this.CurrentState.GetHashCode();

                // Adds the user-defined hashed state.
                hash = (hash * 31) + this.HashedState;

                return hash;
            }
        }

        /// <summary>
        /// Returns a string that represents the current monitor.
        /// </summary>
        public override string ToString() => this.GetType().Name;

        /// <summary>
        /// Transitions to the start state, and executes the
        /// entry action, if there is any.
        /// </summary>
        internal void GotoStartState()
        {
            this.ExecuteCurrentStateOnEntry(DefaultEvent.Instance);
        }

        /// <summary>
        /// Initializes information about the states of the monitor.
        /// </summary>
        internal void InitializeStateInformation()
        {
            Type monitorType = this.GetType();

            // Caches the available state types for this monitor type.
            if (StateTypeMap.TryAdd(monitorType, new HashSet<Type>()))
            {
                Type baseType = monitorType;
                while (baseType != typeof(Monitor))
                {
                    foreach (var s in baseType.GetNestedTypes(BindingFlags.Instance |
                        BindingFlags.NonPublic | BindingFlags.Public |
                        BindingFlags.DeclaredOnly))
                    {
                        this.ExtractStateTypes(s);
                    }

                    baseType = baseType.BaseType;
                }
            }

            // Caches the available state instances for this monitor type.
            if (StateMap.TryAdd(monitorType, new HashSet<State>()))
            {
                foreach (var type in StateTypeMap[monitorType])
                {
                    Type stateType = type;
                    if (type.IsAbstract)
                    {
                        continue;
                    }

                    if (type.IsGenericType)
                    {
                        // If the state type is generic (only possible if inherited by a
                        // generic monitor declaration), then iterate through the base
                        // monitor classes to identify the runtime generic type, and use
                        // it to instantiate the runtime state type. This type can be
                        // then used to create the state constructor.
                        Type declaringType = this.GetType();
                        while (!declaringType.IsGenericType ||
                            !type.DeclaringType.FullName.Equals(declaringType.FullName.Substring(
                            0, declaringType.FullName.IndexOf('['))))
                        {
                            declaringType = declaringType.BaseType;
                        }

                        if (declaringType.IsGenericType)
                        {
                            stateType = type.MakeGenericType(declaringType.GetGenericArguments());
                        }
                    }

                    ConstructorInfo constructor = stateType.GetConstructor(Type.EmptyTypes);
                    var lambda = Expression.Lambda<Func<State>>(Expression.New(constructor)).Compile();
                    State state = lambda();

                    state.InitializeState();

                    this.Assert(
                        (state.IsCold && !state.IsHot) ||
                        (!state.IsCold && state.IsHot) ||
                        (!state.IsCold && !state.IsHot),
                        "State '{0}' of {1} cannot be both cold and hot.", type.FullName, this.GetType().Name);

                    StateMap[monitorType].Add(state);
                }
            }

            // Caches the actions declarations for this monitor type.
            if (MonitorActionMap.TryAdd(monitorType, new Dictionary<string, MethodInfo>()))
            {
                foreach (var state in StateMap[monitorType])
                {
                    if (state.EntryAction != null &&
                        !MonitorActionMap[monitorType].ContainsKey(state.EntryAction))
                    {
                        MonitorActionMap[monitorType].Add(
                            state.EntryAction,
                            this.GetActionWithName(state.EntryAction));
                    }

                    if (state.ExitAction != null &&
                        !MonitorActionMap[monitorType].ContainsKey(state.ExitAction))
                    {
                        MonitorActionMap[monitorType].Add(
                            state.ExitAction,
                            this.GetActionWithName(state.ExitAction));
                    }

                    foreach (var transition in state.GotoTransitions)
                    {
                        if (transition.Value.Lambda != null &&
                            !MonitorActionMap[monitorType].ContainsKey(transition.Value.Lambda))
                        {
                            MonitorActionMap[monitorType].Add(
                                transition.Value.Lambda,
                                this.GetActionWithName(transition.Value.Lambda));
                        }
                    }

                    foreach (var action in state.ActionBindings)
                    {
                        if (!MonitorActionMap[monitorType].ContainsKey(action.Value.Name))
                        {
                            MonitorActionMap[monitorType].Add(
                                action.Value.Name,
                                this.GetActionWithName(action.Value.Name));
                        }
                    }
                }
            }

            // Populates the map of actions for this monitor instance.
            foreach (var kvp in MonitorActionMap[monitorType])
            {
                this.ActionMap.Add(kvp.Key, new CachedDelegate(kvp.Value, this));
            }

            var initialStates = StateMap[monitorType].Where(state => state.IsStart).ToList();
            this.Assert(initialStates.Count != 0, "{0} must declare a start state.", this.GetType().Name);
            this.Assert(initialStates.Count == 1, "{0} can not declare more than one start states.", this.GetType().Name);

            this.ConfigureStateTransitions(initialStates.Single());
            this.ActiveState = initialStates.Single();

            this.AssertStateValidity();
        }

        /// <summary>
        /// Processes a type, looking for monitor states.
        /// </summary>
        private void ExtractStateTypes(Type type)
        {
            Stack<Type> stack = new Stack<Type>();
            stack.Push(type);

            while (stack.Count > 0)
            {
                Type nextType = stack.Pop();

                if (nextType.IsClass && nextType.IsSubclassOf(typeof(State)))
                {
                    StateTypeMap[this.GetType()].Add(nextType);
                }
                else if (nextType.IsClass && nextType.IsSubclassOf(typeof(StateGroup)))
                {
                    // Adds the contents of the group of states to the stack.
                    foreach (var t in nextType.GetNestedTypes(BindingFlags.Instance |
                        BindingFlags.NonPublic | BindingFlags.Public |
                        BindingFlags.DeclaredOnly))
                    {
                        this.Assert(t.IsSubclassOf(typeof(StateGroup)) || t.IsSubclassOf(typeof(State)),
                            "'{0}' is neither a group of states nor a state.", t.Name);
                        stack.Push(t);
                    }
                }
            }
        }

        /// <summary>
        /// Configures the state transitions of the monitor.
        /// </summary>
        private void ConfigureStateTransitions(State state)
        {
            this.GotoTransitions = state.GotoTransitions;
            this.ActionBindings = state.ActionBindings;
            this.IgnoredEvents = state.IgnoredEvents;
        }

        /// <summary>
        /// Returns the action with the specified name.
        /// </summary>
        private MethodInfo GetActionWithName(string actionName)
        {
            MethodInfo action;
            Type monitorType = this.GetType();

            do
            {
                BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.FlattenHierarchy;
                action = monitorType.GetMethod(actionName, bindingFlags, Type.DefaultBinder, SingleEventTypeArray, null);
                if (action is null)
                {
                    action = monitorType.GetMethod(actionName, bindingFlags, Type.DefaultBinder, Array.Empty<Type>(), null);
                }

                monitorType = monitorType.BaseType;
            }
            while (action is null && monitorType != typeof(Monitor));

            this.Assert(action != null, "Cannot detect action declaration '{0}' in {1}.",
                actionName, this.GetType().Name);

            ParameterInfo[] parameters = action.GetParameters();
            this.Assert(parameters.Length is 0 ||
                (parameters.Length is 1 && parameters[0].ParameterType == typeof(Event)),
                "Action '{0}' in {1} must either accept no parameters or a single parameter of type 'Event'.",
                action.Name, this.GetType().Name);

            this.Assert(action.ReturnType == typeof(void) || action.ReturnType == typeof(Transition),
                "Action '{0}' in {1} must have 'void' or 'Transition' return type.",
                action.Name, this.GetType().Name);

            return action;
        }

        /// <summary>
        /// Check monitor for state related errors.
        /// </summary>
        private void AssertStateValidity()
        {
            this.Assert(StateTypeMap[this.GetType()].Count > 0, "{0} must have one or more states.", this.GetType().Name);
            this.Assert(this.ActiveState != null, "{0} must not have a null current state.", this.GetType().Name);
        }

        /// <summary>
        /// Wraps the unhandled exception inside an <see cref="AssertionFailureException"/>
        /// exception, and throws it to the user.
        /// </summary>
        private void ReportUnhandledException(Exception ex, string actionName)
        {
            var state = this.CurrentState is null ? "<unknown>" : this.CurrentStateName;
            this.Runtime.WrapAndThrowException(ex, $"Exception '{ex.GetType()}' was thrown " +
                $"in {this.GetType().Name} (state '{state}', action '{actionName}', '{ex.Source}'):\n" +
                $"   {ex.Message}\n" +
                $"The stack trace is:\n{ex.StackTrace}");
        }

        /// <summary>
        /// Returns the set of all states in the monitor (for code coverage).
        /// </summary>
        internal HashSet<string> GetAllStates()
        {
            this.Assert(StateMap.ContainsKey(this.GetType()), "{0} has not populated its states yet.", this.GetType().Name);

            var allStates = new HashSet<string>();
            foreach (var state in StateMap[this.GetType()])
            {
                allStates.Add(NameResolver.GetQualifiedStateName(state.GetType()));
            }

            return allStates;
        }

        /// <summary>
        /// Returns the set of all (states, registered event) pairs in the monitor (for code coverage).
        /// </summary>
        internal HashSet<Tuple<string, string>> GetAllStateEventPairs()
        {
            this.Assert(StateMap.ContainsKey(this.GetType()), "{0} has not populated its states yet.", this.GetType().Name);

            var pairs = new HashSet<Tuple<string, string>>();
            foreach (var state in StateMap[this.GetType()])
            {
                foreach (var binding in state.ActionBindings)
                {
                    pairs.Add(Tuple.Create(NameResolver.GetQualifiedStateName(state.GetType()), binding.Key.FullName));
                }

                foreach (var transition in state.GotoTransitions)
                {
                    pairs.Add(Tuple.Create(NameResolver.GetQualifiedStateName(state.GetType()), transition.Key.FullName));
                }
            }

            return pairs;
        }

        /// <summary>
        /// Defines the <see cref="Monitor"/> transition that is the
        /// result of executing an event handler.  Transitions are created by using
        /// <see cref="Monitor.GotoState{T}"/>, or <see cref="Monitor.RaiseEvent"/>.
        /// The Transition is processed by the Coyote runtime when
        /// an event handling method returns a Transition object.
        /// This means such a method can only do one such Transition per method call.
        /// If the method wants to do a conditional transition it can return
        /// Transition.None to indicate no transition is to be performed.
        /// </summary>
        public readonly struct Transition
        {
            /// <summary>
            /// The type of the transition.
            /// </summary>
            public readonly Type TypeValue;

            /// <summary>
            /// The target state of the transition, if there is one.
            /// </summary>
            internal readonly System.Type State;

            /// <summary>
            /// The event participating in the transition, if there is one.
            /// </summary>
            internal readonly Event Event;

            /// <summary>
            /// This special transition represents a transition that does not change the current <see cref="Monitor.State"/>.
            /// </summary>
            public static Transition None = default;

            /// <summary>
            /// Initializes a new instance of the <see cref="Transition"/> struct.
            /// </summary>
            /// <param name="type">The type of the transition.</param>
            /// <param name="state">The target state of the transition, if there is one.</param>
            /// <param name="e">The event participating in the transition, if there is one.</param>
            internal Transition(Type type, System.Type state, Event e)
            {
                this.TypeValue = type;
                this.State = state;
                this.Event = e;
            }

            /// <summary>
            /// Defines the type of a <see cref="Monitor"/> transition.
            /// </summary>
            public enum Type
            {
                /// <summary>
                /// A transition that does not change the <see cref="Monitor.State"/>.
                /// This is the value used by <see cref="Transition.None"/>.
                /// </summary>
                None = 0,

                /// <summary>
                /// A transition created by <see cref="Monitor.RaiseEvent(Event)"/> that raises an <see cref="Event"/> bypassing
                /// the <see cref="Monitor.State"/> inbox.
                /// </summary>
                Raise,

                /// <summary>
                /// A transition created by <see cref="Monitor.GotoState{S}"/> from the current <see cref="Monitor.State"/>
                /// to the specified <see cref="Monitor.State"/>.
                /// </summary>
                Goto
            }
        }

        /// <summary>
        /// Abstract class representing a state.
        /// </summary>
        public abstract class State
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
            internal Dictionary<Type, ActionEventHandlerDeclaration> ActionBindings;

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
            /// Initializes a new instance of the <see cref="State"/> class.
            /// </summary>
            protected State()
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
                this.ActionBindings = new Dictionary<Type, ActionEventHandlerDeclaration>();

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
                if (!baseState.IsSubclassOf(typeof(State)))
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

                    this.ActionBindings.Add(attr.Event, new ActionEventHandlerDeclaration(attr.Action));
                    handledEvents.Add(attr.Event);
                }

                this.InheritActionHandlers(this.GetType().BaseType, handledEvents);
            }

            /// <summary>
            /// Inherits action event handlers from a base state, if there is one.
            /// </summary>
            private void InheritActionHandlers(Type baseState, HashSet<Type> handledEvents)
            {
                if (!baseState.IsSubclassOf(typeof(State)))
                {
                    return;
                }

                var doAttributesInherited = baseState.GetCustomAttributes(typeof(OnEventDoActionAttribute), false)
                    as OnEventDoActionAttribute[];

                var actionBindingsInherited = new Dictionary<Type, ActionEventHandlerDeclaration>();
                foreach (var attr in doAttributesInherited)
                {
                    if (this.ActionBindings.ContainsKey(attr.Event))
                    {
                        continue;
                    }

                    CheckEventHandlerAlreadyInherited(attr.Event, baseState, handledEvents);

                    actionBindingsInherited.Add(attr.Event, new ActionEventHandlerDeclaration(attr.Action));
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
                if (!baseState.IsSubclassOf(typeof(State)))
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

            /// <summary>
            /// Attribute for declaring that a state of a monitor
            /// is the start one.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class)]
            protected sealed class StartAttribute : Attribute
            {
            }

            /// <summary>
            /// Attribute for declaring what action to perform
            /// when entering a monitor state.
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
            /// when exiting a monitor state.
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
            /// Attribute for declaring which state a monitor should transition to
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
            /// Attribute for declaring what action a monitor should perform
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
            /// Attribute for declaring what events should be ignored in
            /// a monitor state.
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
            /// Attribute for declaring a cold monitor state. A monitor that
            /// is in a cold state satisfies a liveness property.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class)]
            protected sealed class ColdAttribute : Attribute
            {
            }

            /// <summary>
            /// Attribute for declaring a hot monitor state. A monitor that
            /// is in a hot state violates a liveness property.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class)]
            protected sealed class HotAttribute : Attribute
            {
            }
        }

        /// <summary>
        /// Abstract class used for representing a group of related states.
        /// </summary>
        public abstract class StateGroup
        {
        }
    }
}
