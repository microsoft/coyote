// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Utilities;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Type that implements a state machine actor. Inherit from this class to declare
    /// a custom actor with states, state transitions and event handlers.
    /// </summary>
    public abstract class StateMachine : Actor
    {
        /// <summary>
        /// Cache of actor types to a map of action names to action declarations.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Dictionary<string, MethodInfo>> ActionCache =
            new ConcurrentDictionary<Type, Dictionary<string, MethodInfo>>();

        /// <summary>
        /// Cache of state machine types to a set of all possible states types.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, HashSet<Type>> StateTypeCache =
            new ConcurrentDictionary<Type, HashSet<Type>>();

        /// <summary>
        /// Cache of state machine types to a set of all available state instances.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, HashSet<State>> StateInstanceCache =
            new ConcurrentDictionary<Type, HashSet<State>>();

        /// <summary>
        /// A stack of states. The state on the top of the stack represents the current state.
        /// </summary>
        private readonly Stack<State> StateStack;

        /// <summary>
        /// A stack of maps that determine how to handle each event type. These
        /// maps do not keep transition handlers. This stack has always the same
        /// height as <see cref="StateStack"/>.
        /// </summary>
        private readonly Stack<Dictionary<Type, EventHandlerDeclaration>> EventHandlerStack;

        /// <summary>
        /// Map from action names to cached action delegates.
        /// </summary>
        private readonly Dictionary<string, CachedDelegate> ActionMap;

        /// <summary>
        /// Dictionary containing all the current goto state transitions.
        /// </summary>
        internal Dictionary<Type, GotoStateTransition> GotoTransitions;

        /// <summary>
        /// Dictionary containing all the current push state transitions.
        /// </summary>
        internal Dictionary<Type, PushStateTransition> PushTransitions;

        /// <summary>
        /// Gets the <see cref="Type"/> of the current state.
        /// </summary>
        protected internal Type CurrentState
        {
            get
            {
                if (this.StateStack.Count == 0)
                {
                    return null;
                }

                return this.StateStack.Peek().GetType();
            }
        }

        /// <summary>
        /// Map from event types to installed event handlers in the current state.
        /// </summary>
        private Dictionary<Type, EventHandlerDeclaration> CurrentStateEventHandlers
        {
            get
            {
                if (this.EventHandlerStack.Count == 0)
                {
                    return null;
                }

                return this.EventHandlerStack.Peek();
            }
        }

        /// <summary>
        /// Gets the name of the current state.
        /// </summary>
        internal string CurrentStateName => NameResolver.GetQualifiedStateName(this.CurrentState);

        /// <summary>
        /// Initializes a new instance of the <see cref="StateMachine"/> class.
        /// </summary>
        protected StateMachine()
            : base()
        {
            this.StateStack = new Stack<State>();
            this.EventHandlerStack = new Stack<Dictionary<Type, EventHandlerDeclaration>>();
            this.ActionMap = new Dictionary<string, CachedDelegate>();
        }

        /// <summary>
        /// Initializes the actor with the specified optional event.
        /// </summary>
        /// <param name="initialEvent">Optional event used for initialization.</param>
        internal override async Task InitializeAsync(Event initialEvent)
        {
            // Invoke the custom initializer, if there is one.
            await this.InvokeUserCallbackAsync(UserCallbackType.OnInitialize, initialEvent);

            // Execute the entry action of the start state, if there is one.
            await this.ExecuteCurrentStateOnEntryAsync(initialEvent);
            if (this.CurrentStatus is Status.Halting)
            {
                await this.HaltAsync(initialEvent);
            }
        }

        /// <summary>
        /// Creates a transition that raises the specified <see cref="Event"/>
        /// at the end of the current action.
        /// </summary>
        /// <param name="e">The event to raise.</param>
        /// <returns>The raise event transition.</returns>
        protected Transition RaiseEvent(Event e)
        {
            this.Assert(this.CurrentStatus is Status.Active, "'{0}' invoked RaiseEvent while halting.", this.Id);
            this.Assert(e != null, "'{0}' is raising a null event.", this.Id);
            return new Transition(Transition.Type.RaiseEvent, default, e);
        }

        /// <summary>
        /// Creates a <see cref="StateMachine.Transition"/> that pops the current <see cref="State"/>
        /// and pushes the specified <see cref="State"/> to the state stack
        /// at the end of the current action.
        /// </summary>
        /// <typeparam name="S">Type of the state.</typeparam>
        /// <returns>The goto state transition.</returns>
        protected Transition GotoState<S>()
            where S : State =>
            this.GotoState(typeof(S));

        /// <summary>
        /// Creates a transition that pops the current <see cref="State"/>
        /// and pushes the specified <see cref="State"/> to the state stack
        /// at the end of the current action.
        /// </summary>
        /// <param name="state">Type of the state.</param>
        /// <returns>The goto state transition.</returns>
        protected Transition GotoState(Type state)
        {
            this.Assert(this.CurrentStatus is Status.Active, "'{0}' invoked GotoState while halting.", this.Id);
            this.Assert(StateTypeCache[this.GetType()].Any(val => val.DeclaringType.Equals(state.DeclaringType) && val.Name.Equals(state.Name)),
                "'{0}' is trying to transition to non-existing state '{1}'.", this.Id, state.Name);
            return new Transition(Transition.Type.GotoState, state, default);
        }

        /// <summary>
        /// Creates a transition that pushes the specified <see cref="State"/> to
        /// the state stack at the end of the current action.
        /// </summary>
        /// <typeparam name="S">Type of the state.</typeparam>
        /// <returns>The push state transition.</returns>
        protected Transition PushState<S>()
            where S : State =>
            this.PushState(typeof(S));

        /// <summary>
        /// Creates a transition that pushes the specified <see cref="State"/> to
        /// the state stack at the end of the current action.
        /// </summary>
        /// <param name="state">Type of the state.</param>
        /// <returns>The push state transition.</returns>
        protected Transition PushState(Type state)
        {
            this.Assert(this.CurrentStatus is Status.Active, "'{0}' invoked PushState while halting.", this.Id);
            this.Assert(StateTypeCache[this.GetType()].Any(val => val.DeclaringType.Equals(state.DeclaringType) && val.Name.Equals(state.Name)),
                "'{0}' is trying to transition to non-existing state '{1}'.", this.Id, state.Name);
            return new Transition(Transition.Type.PushState, state, default);
        }

        /// <summary>
        /// Creates a transition that pops the current <see cref="State"/>
        /// from the state stack at the end of the current action.
        /// </summary>
        /// <returns>The pop state transition.</returns>
        protected Transition PopState()
        {
            this.Assert(this.CurrentStatus is Status.Active, "'{0}' invoked PopState while halting.", this.Id);
            return new Transition(Transition.Type.PopState, null, default);
        }

        /// <summary>
        /// Creates a transition that halts the state machine
        /// at the end of the current action.
        /// </summary>
        /// <returns>The halt transition.</returns>
        protected new Transition Halt()
        {
            this.Assert(this.CurrentStatus is Status.Active, "'{0}' invoked Halt while halting.", this.Id);
            return new Transition(Transition.Type.Halt, null, default);
        }

        /// <summary>
        /// Asynchronous callback that is invoked when the actor finishes handling a dequeued
        /// event, unless the handler of the dequeued event raised an event or caused the actor
        /// to halt (either normally or due to an exception). Unless this callback raises an
        /// event, the actor will either become idle or dequeue the next event from its inbox.
        /// </summary>
        /// <param name="e">The event that was handled.</param>
        protected override Task OnEventHandledAsync(Event e) => Task.CompletedTask;

        /// <summary>
        /// Handles the specified <see cref="Event"/>.
        /// </summary>
        private protected override async Task HandleEventAsync(Event e)
        {
            Type currentState = this.CurrentState;

            while (true)
            {
                if (this.CurrentState is null)
                {
                    // If the stack of states is empty then halt or fail the state machine.
                    if (e is HaltEvent)
                    {
                        // If it is the halt event, then change the actor status to halting.
                        this.CurrentStatus = Status.Halting;
                        break;
                    }

                    string currentStateName = NameResolver.GetQualifiedStateName(currentState);
                    await this.InvokeUserCallbackAsync(UserCallbackType.OnEventUnhandled, e, currentStateName);
                    if (this.CurrentStatus is Status.Active)
                    {
                        // If the event cannot be handled then report an error, else halt gracefully.
                        var ex = new UnhandledEventException(e, currentStateName, "Unhandled Event");
                        bool isHalting = this.OnUnhandledEventExceptionHandler(ex, e);
                        this.Assert(isHalting, "'{0}' received event '{1}' that cannot be handled.",
                            this.Id, e.GetType().FullName);
                    }

                    break;
                }

                if (e is GotoStateEvent gotoStateEvent)
                {
                    await this.GotoStateAsync(gotoStateEvent.State, null, e);
                }
                else if (e is PushStateEvent pushStateEvent)
                {
                    await this.PushStateAsync(pushStateEvent.State, e);
                }
                else if (this.GotoTransitions.ContainsKey(e.GetType()))
                {
                    var transition = this.GotoTransitions[e.GetType()];
                    await this.GotoStateAsync(transition.TargetState, transition.Lambda, e);
                }
                else if (this.GotoTransitions.ContainsKey(typeof(WildCardEvent)))
                {
                    var transition = this.GotoTransitions[typeof(WildCardEvent)];
                    await this.GotoStateAsync(transition.TargetState, transition.Lambda, e);
                }
                else if (this.PushTransitions.ContainsKey(e.GetType()))
                {
                    Type targetState = this.PushTransitions[e.GetType()].TargetState;
                    await this.PushStateAsync(targetState, e);
                }
                else if (this.PushTransitions.ContainsKey(typeof(WildCardEvent)))
                {
                    Type targetState = this.PushTransitions[typeof(WildCardEvent)].TargetState;
                    await this.PushStateAsync(targetState, e);
                }
                else if ((this.CurrentStateEventHandlers.TryGetValue(e.GetType(), out EventHandlerDeclaration eventHandler) ||
                    this.CurrentStateEventHandlers.TryGetValue(typeof(WildCardEvent), out eventHandler)) &&
                    eventHandler is ActionEventHandlerDeclaration actionEventHandler)
                {
                    CachedDelegate cachedAction = this.ActionMap[actionEventHandler.Name];
                    this.Runtime.NotifyInvokedAction(this, cachedAction.MethodInfo, e);
                    Transition transition = await this.InvokeActionAsync(cachedAction, e);
                    await this.ApplyEventHandlerTransitionAsync(transition, e);
                }
                else
                {
                    // If the current state cannot handle the event.
                    await this.ExecuteCurrentStateOnExitAsync(null, e);
                    if (this.CurrentStatus is Status.Active)
                    {
                        this.Runtime.LogWriter.LogPopUnhandledEvent(this.Id, this.CurrentStateName, e);
                        this.DoStatePop();
                        continue;
                    }
                }

                break;
            }
        }

        /// <summary>
        /// Invokes the specified action delegate.
        /// </summary>
        private async Task<Transition> InvokeActionAsync(CachedDelegate cachedAction, Event e)
        {
            try
            {
                if (cachedAction.IsAsync)
                {
                    Task task = null;
                    if (cachedAction.Handler is Func<Event, Task<Transition>> taskFuncWithEventAndResult)
                    {
                        task = taskFuncWithEventAndResult(e);
                    }
                    else if (cachedAction.Handler is Func<Task<Transition>> taskFuncWithResult)
                    {
                        task = taskFuncWithResult();
                    }
                    else if (cachedAction.Handler is Func<Event, Task> taskFuncWithEvent)
                    {
                        task = taskFuncWithEvent(e);
                    }
                    else if (cachedAction.Handler is Func<Task> taskFunc)
                    {
                        task = taskFunc();
                    }

                    this.Runtime.NotifyWaitTask(this, task);

                    // We have no reliable stack for awaited operations.
                    if (task is Task<Transition> taskWithResult)
                    {
                        return await taskWithResult;
                    }
                    else
                    {
                        await task;
                    }
                }
                else if (cachedAction.Handler is Func<Event, Transition> funcWithEventAndResult)
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
            catch (Exception ex) when (this.OnExceptionHandler(ex, cachedAction.MethodInfo.Name, e))
            {
                // User handled the exception.
                await this.OnExceptionHandledAsync(ex, e);
            }
            catch (Exception ex) when (!cachedAction.IsAsync && this.InvokeOnFailureExceptionFilter(cachedAction, ex))
            {
                // Use an exception filter to call OnFailure before the stack
                // has been unwound. If the exception filter does not fail-fast,
                // it returns false to process the exception normally.
            }
            catch (Exception ex)
            {
                await this.TryHandleActionInvocationExceptionAsync(ex, cachedAction.MethodInfo.Name);
            }

            return Transition.None;
        }

        /// <summary>
        /// Executes the on entry action of the current state.
        /// </summary>
        private async Task ExecuteCurrentStateOnEntryAsync(Event e)
        {
            this.Runtime.NotifyEnteredState(this);

            CachedDelegate entryAction = null;
            if (this.StateStack.Peek().EntryAction != null)
            {
                entryAction = this.ActionMap[this.StateStack.Peek().EntryAction];
            }

            // Invokes the entry action of the new state, if there is one available.
            if (entryAction != null)
            {
                this.Runtime.NotifyInvokedOnEntryAction(this, entryAction.MethodInfo, e);
                Transition transition = await this.InvokeActionAsync(entryAction, e);
                await this.ApplyEventHandlerTransitionAsync(transition, e);
            }
        }

        /// <summary>
        /// Executes the on exit action of the current state.
        /// </summary>
        private async Task ExecuteCurrentStateOnExitAsync(string eventHandlerExitActionName, Event e)
        {
            this.Runtime.NotifyExitedState(this);

            CachedDelegate exitAction = null;
            if (this.StateStack.Peek().ExitAction != null)
            {
                exitAction = this.ActionMap[this.StateStack.Peek().ExitAction];
            }

            // Invokes the exit action of the current state,
            // if there is one available.
            if (exitAction != null)
            {
                this.Runtime.NotifyInvokedOnExitAction(this, exitAction.MethodInfo, e);
                Transition transition = await this.InvokeActionAsync(exitAction, e);
                this.Assert(transition.TypeValue is Transition.Type.Default ||
                    transition.TypeValue is Transition.Type.Halt,
                    "'{0}' has performed a '{1}' transition from an OnExit action.",
                    this.Id, transition.TypeValue);
                await this.ApplyEventHandlerTransitionAsync(transition, e);
            }

            // Invokes the exit action of the event handler,
            // if there is one available.
            if (eventHandlerExitActionName != null && this.CurrentStatus is Status.Active)
            {
                CachedDelegate eventHandlerExitAction = this.ActionMap[eventHandlerExitActionName];
                this.Runtime.NotifyInvokedOnExitAction(this, eventHandlerExitAction.MethodInfo, e);
                Transition transition = await this.InvokeActionAsync(eventHandlerExitAction, e);
                this.Assert(transition.TypeValue is Transition.Type.Default ||
                    transition.TypeValue is Transition.Type.Halt,
                    "'{0}' has performed a '{1}' transition from an OnExit action.",
                    this.Id, transition.TypeValue);
                await this.ApplyEventHandlerTransitionAsync(transition, e);
            }
        }

        /// <summary>
        /// Applies the specified event handler transition.
        /// </summary>
        private async Task ApplyEventHandlerTransitionAsync(Transition transition, Event e)
        {
            if (transition.TypeValue is Transition.Type.RaiseEvent)
            {
                this.Inbox.RaiseEvent(transition.Event, this.OperationGroupId);
            }
            else if (transition.TypeValue is Transition.Type.GotoState)
            {
                this.Inbox.RaiseEvent(new GotoStateEvent(transition.State), this.OperationGroupId);
            }
            else if (transition.TypeValue is Transition.Type.PushState)
            {
                this.Inbox.RaiseEvent(new PushStateEvent(transition.State), this.OperationGroupId);
            }
            else if (transition.TypeValue is Transition.Type.PopState)
            {
                var prevStateName = this.CurrentStateName;
                this.Runtime.NotifyPopState(this);

                // The state machine performs the on exit action of the current state.
                await this.ExecuteCurrentStateOnExitAsync(null, e);
                if (this.CurrentStatus is Status.Active)
                {
                    this.DoStatePop();
                    this.Runtime.LogWriter.LogPopState(this.Id, prevStateName, this.CurrentStateName);
                    this.Assert(this.CurrentState != null, "'{0}' popped its state with no matching push state.", this.Id);
                }
            }
            else if (transition.TypeValue is Transition.Type.Halt)
            {
                // If it is the halt transition, then change the actor status to halting.
                this.CurrentStatus = Status.Halting;
            }
        }

        /// <summary>
        /// Performs a goto transition to the specified state.
        /// </summary>
        private async Task GotoStateAsync(Type s, string onExitActionName, Event e)
        {
            this.Runtime.LogWriter.LogGotoState(this.Id, this.CurrentStateName,
                $"{s.DeclaringType}.{NameResolver.GetStateNameForLogging(s)}");

            // The state machine performs the on exit action of the current state.
            await this.ExecuteCurrentStateOnExitAsync(onExitActionName, e);
            if (this.CurrentStatus is Status.Active)
            {
                this.DoStatePop();

                // The state machine transitions to the new state.
                var nextState = StateInstanceCache[this.GetType()].First(val => val.GetType().Equals(s));
                this.DoStatePush(nextState);

                // The state machine performs the on entry action of the new state.
                await this.ExecuteCurrentStateOnEntryAsync(e);
            }
        }

        /// <summary>
        /// Performs a push transition to the specified state.
        /// </summary>
        private async Task PushStateAsync(Type s, Event e)
        {
            this.Runtime.LogWriter.LogPushState(this.Id, this.CurrentStateName, s.FullName);

            var nextState = StateInstanceCache[this.GetType()].First(val => val.GetType().Equals(s));
            this.DoStatePush(nextState);

            // The state machine performs the on entry statements of the new state.
            await this.ExecuteCurrentStateOnEntryAsync(e);
        }

        /// <summary>
        /// Configures the state transitions of the state machine when a state is pushed into the stack.
        /// </summary>
        private void DoStatePush(State state)
        {
            this.GotoTransitions = state.GotoTransitions;
            this.PushTransitions = state.PushTransitions;

            // Gets existing map for actions.
            var eventHandlerMap = this.CurrentStateEventHandlers is null ?
                new Dictionary<Type, EventHandlerDeclaration>() :
                new Dictionary<Type, EventHandlerDeclaration>(this.CurrentStateEventHandlers);

            // Updates the map with defer annotations.
            foreach (var deferredEvent in state.DeferredEvents)
            {
                if (deferredEvent.Equals(typeof(WildCardEvent)))
                {
                    eventHandlerMap.Clear();
                    eventHandlerMap[deferredEvent] = new DeferEventHandlerDeclaration();
                    break;
                }

                eventHandlerMap[deferredEvent] = new DeferEventHandlerDeclaration();
            }

            // Updates the map with action annotations.
            foreach (var actionBinding in state.ActionBindings)
            {
                if (actionBinding.Key.Equals(typeof(WildCardEvent)))
                {
                    eventHandlerMap.Clear();
                    eventHandlerMap[actionBinding.Key] = actionBinding.Value;
                    break;
                }

                eventHandlerMap[actionBinding.Key] = actionBinding.Value;
            }

            // Updates the map with ignore annotations.
            foreach (var ignoredEvent in state.IgnoredEvents)
            {
                if (ignoredEvent.Equals(typeof(WildCardEvent)))
                {
                    eventHandlerMap.Clear();
                    eventHandlerMap[ignoredEvent] = new IgnoreEventHandlerDeclaration();
                    break;
                }

                eventHandlerMap[ignoredEvent] = new IgnoreEventHandlerDeclaration();
            }

            // Removes the events on which push transitions are defined.
            foreach (var eventType in this.PushTransitions.Keys)
            {
                if (eventType.Equals(typeof(WildCardEvent)))
                {
                    eventHandlerMap.Clear();
                    break;
                }

                eventHandlerMap.Remove(eventType);
            }

            // Removes the events on which goto transitions are defined.
            foreach (var eventType in this.GotoTransitions.Keys)
            {
                if (eventType.Equals(typeof(WildCardEvent)))
                {
                    eventHandlerMap.Clear();
                    break;
                }

                eventHandlerMap.Remove(eventType);
            }

            this.StateStack.Push(state);
            this.EventHandlerStack.Push(eventHandlerMap);
        }

        /// <summary>
        /// Configures the state transitions of the state machine
        /// when a state is popped.
        /// </summary>
        private void DoStatePop()
        {
            this.StateStack.Pop();
            this.EventHandlerStack.Pop();

            if (this.StateStack.Count > 0)
            {
                this.GotoTransitions = this.StateStack.Peek().GotoTransitions;
                this.PushTransitions = this.StateStack.Peek().PushTransitions;
            }
            else
            {
                this.GotoTransitions = null;
                this.PushTransitions = null;
            }
        }

        /// <summary>
        /// Checks if the specified event is ignored in the current state.
        /// </summary>
        internal bool IsEventIgnoredInCurrentState(Event e)
        {
            if (e is TimerElapsedEvent timeoutEvent && !this.Timers.ContainsKey(timeoutEvent.Info))
            {
                // The timer that created this timeout event is not active.
                return true;
            }

            Type eventType = e.GetType();

            if (eventType.IsGenericType)
            {
                var genericTypeDefinition = eventType.GetGenericTypeDefinition();
                foreach (var kvp in this.CurrentStateEventHandlers)
                {
                    if (!(kvp.Value is IgnoreEventHandlerDeclaration))
                    {
                        continue;
                    }

                    // TODO: make sure this logic and/or simplify.
                    if (kvp.Key.IsGenericType && kvp.Key.GetGenericTypeDefinition().Equals(
                        genericTypeDefinition.GetGenericTypeDefinition()))
                    {
                        return true;
                    }
                }
            }

            // If a transition is defined, then the event is not ignored.
            if (this.GotoTransitions.ContainsKey(eventType) ||
                this.PushTransitions.ContainsKey(eventType) ||
                this.GotoTransitions.ContainsKey(typeof(WildCardEvent)) ||
                this.PushTransitions.ContainsKey(typeof(WildCardEvent)))
            {
                return false;
            }

            if (this.CurrentStateEventHandlers.ContainsKey(eventType))
            {
                return this.CurrentStateEventHandlers[eventType] is IgnoreEventHandlerDeclaration;
            }

            if (this.CurrentStateEventHandlers.ContainsKey(typeof(WildCardEvent)) &&
                this.CurrentStateEventHandlers[typeof(WildCardEvent)] is IgnoreEventHandlerDeclaration)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the specified event is deferred in the current state.
        /// </summary>
        internal bool IsEventDeferredInCurrentState(Event e)
        {
            Type eventType = e.GetType();

            // If a transition is defined, then the event is not deferred.
            if (this.GotoTransitions.ContainsKey(eventType) || this.PushTransitions.ContainsKey(eventType) ||
                this.GotoTransitions.ContainsKey(typeof(WildCardEvent)) ||
                this.PushTransitions.ContainsKey(typeof(WildCardEvent)))
            {
                return false;
            }

            if (this.CurrentStateEventHandlers.ContainsKey(eventType))
            {
                return this.CurrentStateEventHandlers[eventType] is DeferEventHandlerDeclaration;
            }

            if (this.CurrentStateEventHandlers.ContainsKey(typeof(WildCardEvent)) &&
                this.CurrentStateEventHandlers[typeof(WildCardEvent)] is DeferEventHandlerDeclaration)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a default handler is installed in current state.
        /// </summary>
        internal bool IsDefaultHandlerInstalledInCurrentState() =>
            this.CurrentStateEventHandlers.ContainsKey(typeof(DefaultEvent)) ||
            this.GotoTransitions.ContainsKey(typeof(DefaultEvent)) ||
            this.PushTransitions.ContainsKey(typeof(DefaultEvent));

        /// <summary>
        /// Returns the cached state of this state machine.
        /// </summary>
        internal override int GetCachedState()
        {
            unchecked
            {
                var hash = 19;
                hash = (hash * 31) + this.GetType().GetHashCode();
                hash = (hash * 31) + this.Id.Value.GetHashCode();
                hash = (hash * 31) + this.IsHalted.GetHashCode();

                hash = (hash * 31) + this.Manager.GetCachedState();

                foreach (var state in this.StateStack)
                {
                    hash = (hash * 31) + state.GetType().GetHashCode();
                }

                hash = (hash * 31) + this.Inbox.GetCachedState();

                if (this.Runtime.Configuration.EnableUserDefinedStateHashing)
                {
                    // Adds the user-defined hashed state.
                    hash = (hash * 31) + this.HashedState;
                }

                return hash;
            }
        }

        /// <summary>
        /// Extracts user declarations and setups the event handlers and state transitions.
        /// </summary>
        internal override void SetupEventHandlers()
        {
            Type stateMachineType = this.GetType();
            if (IsTypeDeclarationCached.TryAdd(stateMachineType, false))
            {
                // Caches the available state types for this state machine type.
                if (StateTypeCache.TryAdd(stateMachineType, new HashSet<Type>()))
                {
                    Type baseType = stateMachineType;
                    while (baseType != typeof(StateMachine))
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

                // Caches the available state instances for this state machine type.
                if (StateInstanceCache.TryAdd(stateMachineType, new HashSet<State>()))
                {
                    foreach (var type in StateTypeCache[stateMachineType])
                    {
                        Type stateType = type;
                        if (type.IsAbstract)
                        {
                            continue;
                        }

                        if (type.IsGenericType)
                        {
                            // If the state type is generic (only possible if inherited by a generic state
                            // machine declaration), then iterate through the base state machine classes to
                            // identify the runtime generic type, and use it to instantiate the runtime state
                            // type. This type can be then used to create the state constructor.
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
                        var lambda = Expression.Lambda<Func<State>>(
                            Expression.New(constructor)).Compile();
                        State state = lambda();

                        try
                        {
                            state.InitializeState();
                        }
                        catch (InvalidOperationException ex)
                        {
                            this.Assert(false, "'{0}' {1} in state '{2}'.", this.Id, ex.Message, state);
                        }

                        StateInstanceCache[stateMachineType].Add(state);
                    }
                }

                // Caches the action declarations for this state machine type.
                if (ActionCache.TryAdd(stateMachineType, new Dictionary<string, MethodInfo>()))
                {
                    foreach (var state in StateInstanceCache[stateMachineType])
                    {
                        if (state.EntryAction != null &&
                            !ActionCache[stateMachineType].ContainsKey(state.EntryAction))
                        {
                            ActionCache[stateMachineType].Add(
                                state.EntryAction,
                                this.GetActionWithName(state.EntryAction));
                        }

                        if (state.ExitAction != null &&
                            !ActionCache[stateMachineType].ContainsKey(state.ExitAction))
                        {
                            ActionCache[stateMachineType].Add(
                                state.ExitAction,
                                this.GetActionWithName(state.ExitAction));
                        }

                        foreach (var transition in state.GotoTransitions)
                        {
                            if (transition.Value.Lambda != null &&
                                !ActionCache[stateMachineType].ContainsKey(transition.Value.Lambda))
                            {
                                ActionCache[stateMachineType].Add(
                                    transition.Value.Lambda,
                                    this.GetActionWithName(transition.Value.Lambda));
                            }
                        }

                        foreach (var action in state.ActionBindings)
                        {
                            if (!ActionCache[stateMachineType].ContainsKey(action.Value.Name))
                            {
                                ActionCache[stateMachineType].Add(
                                    action.Value.Name,
                                    this.GetActionWithName(action.Value.Name));
                            }
                        }
                    }
                }

                // This state machine type has been cached.
                lock (IsTypeDeclarationCached)
                {
                    IsTypeDeclarationCached[stateMachineType] = true;
                    System.Threading.Monitor.PulseAll(IsTypeDeclarationCached);
                }
            }
            else if (!IsTypeDeclarationCached[stateMachineType])
            {
                lock (IsTypeDeclarationCached)
                {
                    while (!IsTypeDeclarationCached[stateMachineType])
                    {
                        System.Threading.Monitor.Wait(IsTypeDeclarationCached);
                    }
                }
            }

            // Populates the map of event handlers for this state machine instance.
            foreach (var kvp in ActionCache[stateMachineType])
            {
                this.ActionMap.Add(kvp.Key, new CachedDelegate(kvp.Value, this));
            }

            var initialStates = StateInstanceCache[stateMachineType].Where(state => state.IsStart).ToList();
            this.Assert(initialStates.Count != 0, "'{0}' must declare a start state.", this.Id);
            this.Assert(initialStates.Count is 1, "'{0}' can not declare more than one start states.", this.Id);

            this.DoStatePush(initialStates[0]);
            this.AssertStateValidity();
        }

        /// <summary>
        /// Returns the type of the state at the specified state
        /// stack index, if there is one.
        /// </summary>
        internal Type GetStateTypeAtStackIndex(int index)
        {
            return this.StateStack.ElementAtOrDefault(index)?.GetType();
        }

        /// <summary>
        /// Processes a type, looking for states.
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
                    StateTypeCache[this.GetType()].Add(nextType);
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
        /// Returns the set of all states in the state machine (for code coverage).
        /// </summary>
        internal HashSet<string> GetAllStates()
        {
            this.Assert(StateInstanceCache.ContainsKey(this.GetType()), "'{0}' hasn't populated its states yet.", this.Id);

            var allStates = new HashSet<string>();
            foreach (var state in StateInstanceCache[this.GetType()])
            {
                allStates.Add(NameResolver.GetQualifiedStateName(state.GetType()));
            }

            return allStates;
        }

        /// <summary>
        /// Returns the set of all (states, registered event) pairs in the state machine (for code coverage).
        /// </summary>
        internal HashSet<Tuple<string, string>> GetAllStateEventPairs()
        {
            this.Assert(StateInstanceCache.ContainsKey(this.GetType()), "'{0}' hasn't populated its states yet.", this.Id);

            var pairs = new HashSet<Tuple<string, string>>();
            foreach (var state in StateInstanceCache[this.GetType()])
            {
                foreach (var binding in state.ActionBindings)
                {
                    pairs.Add(Tuple.Create(NameResolver.GetQualifiedStateName(state.GetType()), binding.Key.FullName));
                }

                foreach (var transition in state.GotoTransitions)
                {
                    pairs.Add(Tuple.Create(NameResolver.GetQualifiedStateName(state.GetType()), transition.Key.FullName));
                }

                foreach (var pushtransition in state.PushTransitions)
                {
                    pairs.Add(Tuple.Create(NameResolver.GetQualifiedStateName(state.GetType()), pushtransition.Key.FullName));
                }
            }

            return pairs;
        }

        /// <summary>
        /// Checks the state machine for state related errors.
        /// </summary>
        private void AssertStateValidity()
        {
            this.Assert(StateTypeCache[this.GetType()].Count > 0, "'{0}' must have one or more states.", this.Id);
            this.Assert(this.StateStack.Peek() != null, "'{0}' must not have a null current state.", this.Id);
        }

        /// <summary>
        /// Checks the validity of the specified action.
        /// </summary>
        private protected override void AssertActionValidity(MethodInfo action)
        {
            ParameterInfo[] parameters = action.GetParameters();
            this.Assert(parameters.Length is 0 ||
                (parameters.Length is 1 && parameters[0].ParameterType == typeof(Event)),
                "Action '{0}' in '{1}' must either accept no parameters or a single parameter of type 'Event'.",
                action.Name, this.GetType().Name);

            // Check if the action is an 'async' method.
            if (action.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null)
            {
                this.Assert(action.ReturnType == typeof(Task) || action.ReturnType == typeof(Task<Transition>),
                    "Async action '{0}' in '{1}' must have 'Task' or 'Task<Transition>' return type.",
                    action.Name, this.GetType().Name);
            }
            else
            {
                this.Assert(action.ReturnType == typeof(void) || action.ReturnType == typeof(Transition),
                    "Action '{0}' in '{1}' must have 'void' or 'Transition' return type.",
                    action.Name, this.GetType().Name);
            }
        }

        /// <summary>
        /// Returns the formatted strint to be used with a fair nondeterministic boolean choice.
        /// </summary>
        private protected override string FormatFairRandom(string callerMemberName, string callerFilePath, int callerLineNumber) =>
            string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}_{3}_{4}",
                this.Id.Name, this.CurrentStateName, callerMemberName, callerFilePath, callerLineNumber.ToString());

        /// <summary>
        /// Wraps the unhandled exception inside an <see cref="AssertionFailureException"/>
        /// exception, and throws it to the user.
        /// </summary>
        private protected override void ReportUnhandledException(Exception ex, string actionName)
        {
            string state = "<unknown>";
            if (this.CurrentState != null)
            {
                state = this.CurrentStateName;
            }

            this.Runtime.WrapAndThrowException(ex, $"Exception '{ex.GetType()}' was thrown " +
                $"in '{this.Id}', state '{state}', action '{actionName}', " +
                $"'{ex.Source}':\n" +
                $"   {ex.Message}\n" +
                $"The stack trace is:\n{ex.StackTrace}");
        }

        /// <summary>
        /// Defines the <see cref="StateMachine"/> transition that is the
        /// result of executing an event handler.  Transitions are created by using
        /// <see cref="StateMachine.GotoState{T}"/>, <see cref="StateMachine.RaiseEvent"/>, <see cref="StateMachine.PushState{T}"/> or
        /// <see cref="StateMachine.PopState"/> and <see cref="StateMachine.Halt"/>.
        /// The Transition is processed by the Coyote runtime when
        /// an event handling method of a StateMachine returns a Transition object.
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
            /// This special transition represents a transition that does not change the current <see cref="StateMachine.State"/>.
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
            /// Defines the type of a <see cref="StateMachine"/> transition.
            /// </summary>
            public enum Type
            {
                /// <summary>
                /// A transition that does not change the <see cref="StateMachine.State"/>.
                /// This is the value used by <see cref="Transition.None"/>.
                /// </summary>
                Default = 0,

                /// <summary>
                /// A transition created by <see cref="StateMachine.RaiseEvent(Event)"/> that raises an <see cref="Event"/> bypassing
                /// the <see cref="StateMachine.State"/> inbox.
                /// </summary>
                RaiseEvent,

                /// <summary>
                /// A transition created by <see cref="StateMachine.GotoState{S}"/> that pops the current <see cref="StateMachine.State"/>
                /// and pushes the specified <see cref="StateMachine.State"/> on the
                /// stack of <see cref="StateMachine"/> states.
                /// </summary>
                GotoState,

                /// <summary>
                /// A transition created by <see cref="StateMachine.PushState{S}"/> that pushes the specified <see cref="StateMachine.State"/>
                /// on the stack of <see cref="StateMachine"/> states.
                /// </summary>
                PushState,

                /// <summary>
                /// A transition created by <see cref="StateMachine.PopState"/> that pops the current <see cref="StateMachine.State"/>
                /// from the stack of <see cref="StateMachine"/> states.
                /// </summary>
                PopState,

                /// <summary>
                /// A transition created by <see cref="StateMachine.Halt"/> that halts the <see cref="StateMachine"/>.
                /// </summary>
                Halt
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
            /// Map containing all goto state transitions.
            /// </summary>
            internal Dictionary<Type, GotoStateTransition> GotoTransitions;

            /// <summary>
            /// Map containing all push state transitions.
            /// </summary>
            internal Dictionary<Type, PushStateTransition> PushTransitions;

            /// <summary>
            /// Map containing all action bindings.
            /// </summary>
            internal Dictionary<Type, ActionEventHandlerDeclaration> ActionBindings;

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

                this.GotoTransitions = new Dictionary<Type, GotoStateTransition>();
                this.PushTransitions = new Dictionary<Type, PushStateTransition>();
                this.ActionBindings = new Dictionary<Type, ActionEventHandlerDeclaration>();

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
                this.InstallActionBindings(handledEvents);
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
                if (!baseState.IsSubclassOf(typeof(State)))
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
            /// Installs action bindings, if there are any.
            /// </summary>
            private void InstallActionBindings(HashSet<Type> handledEvents)
            {
                var doAttributes = this.GetType().GetCustomAttributes(typeof(OnEventDoActionAttribute), false)
                    as OnEventDoActionAttribute[];

                foreach (var attr in doAttributes)
                {
                    CheckEventHandlerAlreadyDeclared(attr.Event, handledEvents);

                    this.ActionBindings.Add(attr.Event, new ActionEventHandlerDeclaration(attr.Action));
                    handledEvents.Add(attr.Event);
                }

                this.InheritActionBindings(this.GetType().BaseType, handledEvents);
            }

            /// <summary>
            /// Inherits action bindings from a base state, if there is one.
            /// </summary>
            private void InheritActionBindings(Type baseState, HashSet<Type> handledEvents)
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

                this.InheritActionBindings(baseState.BaseType, handledEvents);
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
                if (!baseState.IsSubclassOf(typeof(State)))
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

            /// <summary>
            /// Attribute for declaring the state that a state machine transitions upon creation.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class)]
            protected sealed class StartAttribute : Attribute
            {
            }

            /// <summary>
            /// Attribute for declaring what action to perform when entering a state.
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
                /// <param name="actionName">The name of the action to execute.</param>
                public OnEntryAttribute(string actionName)
                {
                    this.Action = actionName;
                }
            }

            /// <summary>
            /// Attribute for declaring what action to perform when exiting a state.
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
                /// <param name="actionName">The name of the action to execute.</param>
                public OnExitAttribute(string actionName)
                {
                    this.Action = actionName;
                }
            }

            /// <summary>
            /// Attribute for declaring a goto state transition when the state machine
            /// is in the specified state and dequeues an event of the specified type.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
            protected sealed class OnEventGotoStateAttribute : Attribute
            {
                /// <summary>
                /// The type of the dequeued event.
                /// </summary>
                internal readonly Type Event;

                /// <summary>
                /// The type of the state.
                /// </summary>
                internal readonly Type State;

                /// <summary>
                /// Action name.
                /// </summary>
                internal readonly string Action;

                /// <summary>
                /// Initializes a new instance of the <see cref="OnEventGotoStateAttribute"/> class.
                /// </summary>
                /// <param name="eventType">The type of the dequeued event.</param>
                /// <param name="stateType">The type of the state.</param>
                public OnEventGotoStateAttribute(Type eventType, Type stateType)
                {
                    this.Event = eventType;
                    this.State = stateType;
                }

                /// <summary>
                /// Initializes a new instance of the <see cref="OnEventGotoStateAttribute"/> class.
                /// </summary>
                /// <param name="eventType">The type of the dequeued event.</param>
                /// <param name="stateType">The type of the state.</param>
                /// <param name="actionName">Name of action to perform on exit.</param>
                public OnEventGotoStateAttribute(Type eventType, Type stateType, string actionName)
                {
                    this.Event = eventType;
                    this.State = stateType;
                    this.Action = actionName;
                }
            }

            /// <summary>
            /// Attribute for declaring a push state transition when the state machine
            /// is in the specified state and dequeues an event of the specified type.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
            protected sealed class OnEventPushStateAttribute : Attribute
            {
                /// <summary>
                /// The type of the dequeued event.
                /// </summary>
                internal Type Event;

                /// <summary>
                /// The type of the state.
                /// </summary>
                internal Type State;

                /// <summary>
                /// Initializes a new instance of the <see cref="OnEventPushStateAttribute"/> class.
                /// </summary>
                /// <param name="eventType">The type of the dequeued event.</param>
                /// <param name="stateType">The type of the state.</param>
                public OnEventPushStateAttribute(Type eventType, Type stateType)
                {
                    this.Event = eventType;
                    this.State = stateType;
                }
            }

            /// <summary>
            /// Attribute for declaring which action should be invoked when the state machine
            /// is in the specified state to handle a dequeued event of the specified type.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
            protected sealed class OnEventDoActionAttribute : Attribute
            {
                /// <summary>
                /// The type of the dequeued event.
                /// </summary>
                internal Type Event;

                /// <summary>
                /// The name of the action to invoke.
                /// </summary>
                internal string Action;

                /// <summary>
                /// Initializes a new instance of the <see cref="OnEventDoActionAttribute"/> class.
                /// </summary>
                /// <param name="eventType">The type of the dequeued event.</param>
                /// <param name="actionName">The name of the action to invoke.</param>
                public OnEventDoActionAttribute(Type eventType, string actionName)
                {
                    this.Event = eventType;
                    this.Action = actionName;
                }
            }

            /// <summary>
            /// Attribute for declaring what events should be deferred in a state.
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
            /// Attribute for declaring what events should be ignored in a state.
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
        }

        /// <summary>
        /// Abstract class used for representing a group of related states.
        /// </summary>
        public abstract class StateGroup
        {
        }
    }
}
