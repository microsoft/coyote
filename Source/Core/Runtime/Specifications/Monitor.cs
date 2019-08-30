// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Utilities;

namespace Microsoft.Coyote
{
    /// <summary>
    /// Abstract class representing a Coyote monitor.
    /// </summary>
    public abstract class Monitor
    {
        /// <summary>
        /// Map from monitor types to a set of all
        /// possible states types.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, HashSet<Type>> StateTypeMap =
            new ConcurrentDictionary<Type, HashSet<Type>>();

        /// <summary>
        /// Map from monitor types to a set of all
        /// available states.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, HashSet<MonitorState>> StateMap =
            new ConcurrentDictionary<Type, HashSet<MonitorState>>();

        /// <summary>
        /// Map from monitor types to a set of all
        /// available actions.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Dictionary<string, MethodInfo>> MonitorActionMap =
            new ConcurrentDictionary<Type, Dictionary<string, MethodInfo>>();

        /// <summary>
        /// The runtime that executes this monitor.
        /// </summary>
        private MachineRuntime Runtime;

        /// <summary>
        /// The monitor state.
        /// </summary>
        private MonitorState State;

        /// <summary>
        /// Dictionary containing all the current goto state transitions.
        /// </summary>
        internal Dictionary<Type, GotoStateTransition> GotoTransitions;

        /// <summary>
        /// Dictionary containing all the current action bindings.
        /// </summary>
        internal Dictionary<Type, ActionBinding> ActionBindings;

        /// <summary>
        /// Map from action names to actions.
        /// </summary>
        private readonly Dictionary<string, MethodInfo> ActionMap;

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
        internal MachineId Id { get; private set; }

        /// <summary>
        /// Gets the name of this monitor.
        /// </summary>
        protected internal string Name => this.Id.Name;

        /// <summary>
        /// The logger installed to the Coyote runtime.
        /// </summary>
        protected ILogger Logger => this.Runtime.Logger;

        /// <summary>
        /// Gets the current state.
        /// </summary>
        protected internal Type CurrentState
        {
            get
            {
                if (this.State is null)
                {
                    return null;
                }

                return this.State.GetType();
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
        /// Returns a nullable boolean indicating liveness temperature: true for hot, false for cold, else null.
        /// </summary>
        internal bool? GetHotState()
        {
            return this.IsInHotState() ? true :
                this.IsInColdState() ? (bool?)false :
                null;
        }

        /// <summary>
        /// Gets the latest received event, or null if no event
        /// has been received.
        /// </summary>
        protected internal Event ReceivedEvent { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Monitor"/> class.
        /// </summary>
        protected Monitor()
            : base()
        {
            this.ActionMap = new Dictionary<string, MethodInfo>();
            this.LivenessTemperature = 0;
        }

        /// <summary>
        /// Initializes this monitor.
        /// </summary>
        /// <param name="runtime">The runtime that executes this monitor.</param>
        /// <param name="mid">The monitor id.</param>
        internal void Initialize(MachineRuntime runtime, MachineId mid)
        {
            this.Id = mid;
            this.Runtime = runtime;
        }

        /// <summary>
        /// Returns from the execution context, and transitions
        /// the monitor to the given <see cref="MonitorState"/>.
        /// </summary>
        /// <typeparam name="S">Type of the state.</typeparam>
        protected void Goto<S>()
            where S : MonitorState
        {
#pragma warning disable 618
            this.Goto(typeof(S));
#pragma warning restore 618
        }

        /// <summary>
        /// Returns from the execution context, and transitions
        /// the monitor to the given <see cref="MonitorState"/>.
        /// </summary>
        /// <param name="s">Type of the state.</param>
        [Obsolete("Goto(typeof(T)) is deprecated; use Goto<T>() instead.")]
        protected void Goto(Type s)
        {
            // If the state is not a state of the monitor, then report an error and exit.
            this.Assert(StateTypeMap[this.GetType()].Any(val => val.DeclaringType.Equals(s.DeclaringType) && val.Name.Equals(s.Name)),
                "Monitor '{0}' is trying to transition to non-existing state '{1}'.", this.GetType().Name, s.Name);
            this.Raise(new GotoStateEvent(s));
        }

        /// <summary>
        /// Raises an <see cref="Event"/> internally and returns from the execution context.
        /// </summary>
        /// <param name="e">The event to raise.</param>
        protected void Raise(Event e)
        {
            // If the event is null, then report an error and exit.
            this.Assert(e != null, "Monitor '{0}' is raising a null event.", this.GetType().Name);

            var eventOrigin = new EventOriginInfo(this.Id, this.GetType().FullName, NameResolver.GetQualifiedStateName(this.CurrentState));
            EventInfo raisedEvent = new EventInfo(e, eventOrigin);
            this.Runtime.NotifyRaisedEvent(this, e, raisedEvent);
            this.HandleEvent(e);
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
        /// <param name="e">The event to monitor.</param>
        internal void MonitorEvent(Event e)
        {
            this.Runtime.LogWriter.OnMonitorEvent(this.GetType().Name, this.Id, this.CurrentStateName,
                e.GetType().FullName, isProcessing: true);
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

            // Assigns the receieved event.
            this.ReceivedEvent = e;

            while (true)
            {
                if (this.State is null)
                {
                    // If the event cannot be handled, then report an error and exit.
                    this.Assert(false, "Monitor '{0}' received event '{1}' that cannot be handled.",
                        this.GetType().Name, e.GetType().FullName);
                }

                // If current state cannot handle the event then null the state.
                if (!this.CanHandleEvent(e.GetType()))
                {
                    this.Runtime.NotifyExitedState(this);
                    this.State = null;
                    continue;
                }

                if (e.GetType() == typeof(GotoStateEvent))
                {
                    // Checks if the event is a goto state event.
                    Type targetState = (e as GotoStateEvent).State;
                    this.GotoState(targetState, null);
                }
                else if (this.GotoTransitions.ContainsKey(e.GetType()))
                {
                    // Checks if the event can trigger a goto state transition.
                    var transition = this.GotoTransitions[e.GetType()];
                    this.GotoState(transition.TargetState, transition.Lambda);
                }
                else if (this.GotoTransitions.ContainsKey(typeof(WildCardEvent)))
                {
                    // Checks if the event can trigger a goto state transition.
                    var transition = this.GotoTransitions[typeof(WildCardEvent)];
                    this.GotoState(transition.TargetState, transition.Lambda);
                }
                else if (this.ActionBindings.ContainsKey(e.GetType()))
                {
                    // Checks if the event can trigger an action.
                    var handler = this.ActionBindings[e.GetType()];
                    this.Do(handler.Name);
                }
                else if (this.ActionBindings.ContainsKey(typeof(WildCardEvent)))
                {
                    // Checks if the event can trigger an action.
                    var handler = this.ActionBindings[typeof(WildCardEvent)];
                    this.Do(handler.Name);
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
        private void Do(string actionName)
        {
            MethodInfo action = this.ActionMap[actionName];
            this.Runtime.NotifyInvokedAction(this, action, this.ReceivedEvent);
            this.ExecuteAction(action);
        }

        /// <summary>
        /// Executes the on entry function of the current state.
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough]
        private void ExecuteCurrentStateOnEntry()
        {
            this.Runtime.NotifyEnteredState(this);

            MethodInfo entryAction = null;
            if (this.State.EntryAction != null)
            {
                entryAction = this.ActionMap[this.State.EntryAction];
            }

            // Invokes the entry action of the new state,
            // if there is one available.
            if (entryAction != null)
            {
                this.ExecuteAction(entryAction);
            }
        }

        /// <summary>
        /// Executes the on exit function of the current state.
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough]
        private void ExecuteCurrentStateOnExit(string eventHandlerExitActionName)
        {
            this.Runtime.NotifyExitedState(this);

            MethodInfo exitAction = null;
            if (this.State.ExitAction != null)
            {
                exitAction = this.ActionMap[this.State.ExitAction];
            }

            // Invokes the exit action of the current state,
            // if there is one available.
            if (exitAction != null)
            {
                this.ExecuteAction(exitAction);
            }

            // Invokes the exit action of the event handler,
            // if there is one available.
            if (eventHandlerExitActionName != null)
            {
                MethodInfo eventHandlerExitAction = this.ActionMap[eventHandlerExitActionName];
                this.ExecuteAction(eventHandlerExitAction);
            }
        }

        /// <summary>
        /// Executes the specified action.
        /// </summary>
        [System.Diagnostics.DebuggerStepThrough]
        private void ExecuteAction(MethodInfo action)
        {
            try
            {
                action.Invoke(this, null);
            }
            catch (ExecutionCanceledException ex)
            {
#pragma warning disable CA2200 // Rethrow to preserve stack details.
                throw ex;
#pragma warning restore CA2200 // Rethrow to preserve stack details.
            }
            catch (TaskSchedulerException ex)
            {
#pragma warning disable CA2200 // Rethrow to preserve stack details.
                throw ex;
#pragma warning restore CA2200 // Rethrow to preserve stack details.
            }
            catch (Exception ex)
            {
                // Reports the unhandled exception.
                this.ReportUnhandledException(ex, action.Name);
            }
        }

        /// <summary>
        /// Performs a goto transition to the given state.
        /// </summary>
        private void GotoState(Type s, string onExitActionName)
        {
            // The monitor performs the on exit statements of the current state.
            this.ExecuteCurrentStateOnExit(onExitActionName);

            var nextState = StateMap[this.GetType()].First(val => val.GetType().Equals(s));
            this.ConfigureStateTransitions(nextState);

            // The monitor transitions to the new state.
            this.State = nextState;

            if (nextState.IsCold)
            {
                this.LivenessTemperature = 0;
            }

            // The monitor performs the on entry statements of the new state.
            this.ExecuteCurrentStateOnEntry();
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
            if (this.State.IsHot &&
                this.Runtime.Configuration.LivenessTemperatureThreshold > 0)
            {
                this.LivenessTemperature++;
                this.Runtime.Assert(
                    this.LivenessTemperature <= this.Runtime.
                    Configuration.LivenessTemperatureThreshold,
                    "Monitor '{0}' detected potential liveness bug in hot state '{1}'.",
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
                    $"Monitor '{this.GetType().Name}' detected infinite execution that violates a liveness property.");
            }
        }

        /// <summary>
        /// Returns true if the monitor is in a hot state.
        /// </summary>
        internal bool IsInHotState() => this.State.IsHot;

        /// <summary>
        /// Returns true if the monitor is in a hot state. Also outputs
        /// the name of the current state.
        /// </summary>
        internal bool IsInHotState(out string stateName)
        {
            stateName = this.CurrentStateName;
            return this.State.IsHot;
        }

        /// <summary>
        /// Returns true if the monitor is in a cold state.
        /// </summary>
        internal bool IsInColdState() => this.State.IsCold;

        /// <summary>
        /// Returns true if the monitor is in a cold state. Also outputs
        /// the name of the current state.
        /// </summary>
        internal bool IsInColdState(out string stateName)
        {
            stateName = this.CurrentStateName;
            return this.State.IsCold;
        }

        /// <summary>
        /// Returns the hashed state of this monitor.
        /// </summary>
        protected virtual int GetHashedState()
        {
            return 0;
        }

        /// <summary>
        /// Returns the cached state of this monitor.
        /// </summary>
        internal int GetCachedState()
        {
            unchecked
            {
                var hash = 19;

                hash = (hash * 31) + this.GetType().GetHashCode();
                hash = (hash * 31) + this.CurrentState.GetHashCode();

                // Adds the user-defined hashed state.
                hash = (hash * 31) + this.GetHashedState();

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
            this.ExecuteCurrentStateOnEntry();
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
            if (StateMap.TryAdd(monitorType, new HashSet<MonitorState>()))
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
                    var lambda = Expression.Lambda<Func<MonitorState>>(
                        Expression.New(constructor)).Compile();
                    MonitorState state = lambda();

                    state.InitializeState();

                    this.Assert(
                        (state.IsCold && !state.IsHot) ||
                        (!state.IsCold && state.IsHot) ||
                        (!state.IsCold && !state.IsHot),
                        "State '{0}' of monitor '{1}' cannot be both cold and hot.", type.FullName, this.GetType().Name);

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
                this.ActionMap.Add(kvp.Key, kvp.Value);
            }

            var initialStates = StateMap[monitorType].Where(state => state.IsStart).ToList();
            this.Assert(initialStates.Count != 0, "Monitor '{0}' must declare a start state.", this.GetType().Name);
            this.Assert(initialStates.Count == 1, "Monitor '{0}' can not declare more than one start states.", this.GetType().Name);

            this.ConfigureStateTransitions(initialStates.Single());
            this.State = initialStates.Single();

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

                if (nextType.IsClass && nextType.IsSubclassOf(typeof(MonitorState)))
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
                        this.Assert(t.IsSubclassOf(typeof(StateGroup)) || t.IsSubclassOf(typeof(MonitorState)),
                            "'{0}' is neither a group of states nor a state.", t.Name);
                        stack.Push(t);
                    }
                }
            }
        }

        /// <summary>
        /// Configures the state transitions of the monitor.
        /// </summary>
        private void ConfigureStateTransitions(MonitorState state)
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
            MethodInfo method;
            Type monitorType = this.GetType();

            do
            {
                method = monitorType.GetMethod(
                    actionName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                    Type.DefaultBinder, Array.Empty<Type>(), null);
                monitorType = monitorType.BaseType;
            }
            while (method is null && monitorType != typeof(Monitor));

            this.Assert(method != null, "Cannot detect action declaration '{0}' in monitor '{1}'.",
                actionName, this.GetType().Name);
            this.Assert(method.GetParameters().Length == 0, "Action '{0}' in monitor '{1}' must have 0 formal parameters.",
                method.Name, this.GetType().Name);
            this.Assert(method.ReturnType == typeof(void), "Action '{0}' in monitor '{1}' must have 'void' return type.",
                method.Name, this.GetType().Name);

            return method;
        }

        /// <summary>
        /// Check monitor for state related errors.
        /// </summary>
        private void AssertStateValidity()
        {
            this.Assert(StateTypeMap[this.GetType()].Count > 0, "Monitor '{0}' must have one or more states.", this.GetType().Name);
            this.Assert(this.State != null, "Monitor '{0}' must not have a null current state.", this.GetType().Name);
        }

        /// <summary>
        /// Wraps the unhandled exception inside an <see cref="AssertionFailureException"/>
        /// exception, and throws it to the user.
        /// </summary>
        private void ReportUnhandledException(Exception ex, string actionName)
        {
            var state = this.CurrentState is null ? "<unknown>" : this.CurrentStateName;
            this.Runtime.WrapAndThrowException(ex, $"Exception '{ex.GetType()}' was thrown " +
                $"in monitor '{this.GetType().Name}', state '{state}', action '{actionName}', " +
                $"'{ex.Source}':\n" +
                $"   {ex.Message}\n" +
                $"The stack trace is:\n{ex.StackTrace}");
        }

        /// <summary>
        /// Returns the set of all states in the monitor (for code coverage).
        /// </summary>
        internal HashSet<string> GetAllStates()
        {
            this.Assert(StateMap.ContainsKey(this.GetType()), "Monitor '{0}' hasn't populated its states yet.", this.GetType().Name);

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
            this.Assert(StateMap.ContainsKey(this.GetType()), "Monitor '{0}' hasn't populated its states yet.", this.GetType().Name);

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
        /// Resets the static caches.
        /// </summary>
        internal static void ResetCaches()
        {
            StateTypeMap.Clear();
            StateMap.Clear();
            MonitorActionMap.Clear();
        }
    }
}
