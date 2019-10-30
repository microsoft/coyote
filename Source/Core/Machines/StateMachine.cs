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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Machines.Timers;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Utilities;

using EventInfo = Microsoft.Coyote.Runtime.EventInfo;

namespace Microsoft.Coyote.Machines
{
    /// <summary>
    /// Implements an asynchronous communicating state machine. Inherit from this class
    /// to declare states, state transitions and event handlers.
    /// </summary>
    public abstract class StateMachine : Actor
    {
        /// <summary>
        /// Map from machine types to a set of all possible states types.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, HashSet<Type>> StateTypeMap =
            new ConcurrentDictionary<Type, HashSet<Type>>();

        /// <summary>
        /// Map from machine types to a set of all available states.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, HashSet<MachineState>> StateMap =
            new ConcurrentDictionary<Type, HashSet<MachineState>>();

        /// <summary>
        /// Map from machine types to a set of all available actions.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Dictionary<string, MethodInfo>> MachineActionMap =
            new ConcurrentDictionary<Type, Dictionary<string, MethodInfo>>();

        /// <summary>
        /// Checks if the machine state is cached.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, bool> MachineStateCached =
            new ConcurrentDictionary<Type, bool>();

        /// <summary>
        /// Manages the state of the machine.
        /// </summary>
        internal IMachineStateManager StateManager { get; private set; }

        /// <summary>
        /// A stack of machine states. The state on the top of
        /// the stack represents the current state.
        /// </summary>
        private readonly Stack<MachineState> StateStack;

        /// <summary>
        /// A stack of maps that determine event handling action for
        /// each event type. These maps do not keep transition handlers.
        /// This stack has always the same height as StateStack.
        /// </summary>
        private readonly Stack<Dictionary<Type, EventActionHandler>> ActionHandlerStack;

        /// <summary>
        /// Dictionary containing all the current goto state transitions.
        /// </summary>
        internal Dictionary<Type, GotoStateTransition> GotoTransitions;

        /// <summary>
        /// Dictionary containing all the current push state transitions.
        /// </summary>
        internal Dictionary<Type, PushStateTransition> PushTransitions;

        /// <summary>
        /// Map from action names to cached delegates.
        /// </summary>
        private readonly Dictionary<string, CachedDelegate> ActionMap;

        /// <summary>
        /// The inbox of the machine. Incoming events are enqueued here.
        /// Events are dequeued to be processed.
        /// </summary>
        private IEventQueue Inbox;

        /// <summary>
        /// Map that contains the active timers.
        /// </summary>
        private readonly Dictionary<TimerInfo, IMachineTimer> Timers;

        /// <summary>
        /// Is the machine halted.
        /// </summary>
        internal volatile bool IsHalted;

        /// <summary>
        /// Is pop invoked in the current action.
        /// </summary>
        private bool IsPopInvoked;

        /// <summary>
        /// User OnException asked for the machine to be gracefully halted
        /// (suppressing the exception)
        /// </summary>
        private bool OnExceptionRequestedGracefulHalt;

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
        /// Gets the current action handler map.
        /// </summary>
        private Dictionary<Type, EventActionHandler> CurrentActionHandlerMap
        {
            get
            {
                if (this.ActionHandlerStack.Count == 0)
                {
                    return null;
                }

                return this.ActionHandlerStack.Peek();
            }
        }

        /// <summary>
        /// Gets the name of the current state.
        /// </summary>
        internal string CurrentStateName => NameResolver.GetQualifiedStateName(this.CurrentState);

        /// <summary>
        /// Gets the latest received <see cref="Event"/>, or null if
        /// no <see cref="Event"/> has been received.
        /// </summary>
        protected internal Event ReceivedEvent { get; private set; }

        /// <summary>
        /// Id used to identify subsequent operations performed by this machine. This value is
        /// initially either <see cref="Guid.Empty"/> or the <see cref="Guid"/> specified upon
        /// machine creation. This value is automatically set to the operation group id of the
        /// last dequeue, raise or receive operation, if it is not <see cref="Guid.Empty"/>.
        /// This value can also be manually set using the property.
        /// </summary>
        protected internal override Guid OperationGroupId
        {
            get => this.StateManager.OperationGroupId;

            set
            {
                this.StateManager.OperationGroupId = value;
            }
        }

        /// <summary>
        /// User-defined hashed state of the machine. Override to improve the
        /// accuracy of liveness checking when state-caching is enabled.
        /// </summary>
        protected virtual int HashedState => 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateMachine"/> class.
        /// </summary>
        protected StateMachine()
        {
            this.StateStack = new Stack<MachineState>();
            this.ActionHandlerStack = new Stack<Dictionary<Type, EventActionHandler>>();
            this.ActionMap = new Dictionary<string, CachedDelegate>();
            this.Timers = new Dictionary<TimerInfo, IMachineTimer>();
            this.IsHalted = false;
            this.IsPopInvoked = false;
            this.OnExceptionRequestedGracefulHalt = false;
        }

        /// <summary>
        /// Initializes this machine.
        /// </summary>
        internal void Initialize(CoyoteRuntime runtime, MachineId mid, IMachineStateManager stateManager, IEventQueue inbox)
        {
            this.Initialize(runtime, mid);
            this.StateManager = stateManager;
            this.Inbox = inbox;
        }

        /// <summary>
        /// Creates a new machine of the specified type and with the specified
        /// optional <see cref="Event"/>. This <see cref="Event"/> can only be
        /// used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="e">Optional initialization event.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>The unique machine id.</returns>
        protected MachineId CreateMachine(Type type, Event e = null, Guid opGroupId = default) =>
            this.Runtime.CreateMachine(null, type, null, e, this, opGroupId);

        /// <summary>
        /// Creates a new machine of the specified type and name, and with the
        /// specified optional <see cref="Event"/>. This <see cref="Event"/> can
        /// only be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Optional friendly machine name used for logging.</param>
        /// <param name="e">Optional initialization event.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>The unique machine id.</returns>
        protected MachineId CreateMachine(Type type, string friendlyName, Event e = null, Guid opGroupId = default) =>
            this.Runtime.CreateMachine(null, type, friendlyName, e, this, opGroupId);

        /// <summary>
        /// Creates a new machine of the specified <see cref="Type"/> and name, using the specified
        /// unbound machine id, and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="mid">Unbound machine id.</param>
        /// <param name="type">Type of the machine.</param>
        /// <param name="friendlyName">Optional friendly machine name used for logging.</param>
        /// <param name="e">Optional initialization event.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        protected void CreateMachine(MachineId mid, Type type, string friendlyName, Event e = null, Guid opGroupId = default) =>
            this.Runtime.CreateMachine(mid, type, friendlyName, e, this, opGroupId);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to a machine.
        /// </summary>
        /// <param name="mid">The id of the target machine.</param>
        /// <param name="e">The event to send.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <param name="options">Optional configuration of a send operation.</param>
        protected void Send(MachineId mid, Event e, Guid opGroupId = default, SendOptions options = null) =>
            this.Runtime.SendEvent(mid, e, this, opGroupId, options);

        /// <summary>
        /// Raises an <see cref="Event"/> internally at the end of the current action.
        /// </summary>
        /// <param name="e">The event to raise.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        protected void Raise(Event e, Guid opGroupId = default)
        {
            this.Assert(!this.IsHalted, "Machine '{0}' invoked Raise while halted.", this.Id);
            this.Assert(e != null, "Machine '{0}' is raising a null event.", this.Id);

            // The operation group id of this operation is set using the following precedence:
            // (1) To the specified raise operation group id, if it is non-empty.
            // (2) To the operation group id of this machine.
            this.Inbox.Raise(e, opGroupId != Guid.Empty ? opGroupId : this.OperationGroupId);
        }

        /// <summary>
        /// Waits to receive an <see cref="Event"/> of the specified type
        /// that satisfies an optional predicate.
        /// </summary>
        /// <param name="eventType">The event type.</param>
        /// <param name="predicate">The optional predicate.</param>
        /// <returns>The received event.</returns>
        protected internal Task<Event> Receive(Type eventType, Func<Event, bool> predicate = null)
        {
            this.Assert(!this.IsHalted, "Machine '{0}' invoked Receive while halted.", this.Id);
            this.Runtime.NotifyReceiveCalled(this);
            return this.Inbox.ReceiveAsync(eventType, predicate);
        }

        /// <summary>
        /// Waits to receive an <see cref="Event"/> of the specified types.
        /// </summary>
        /// <param name="eventTypes">The event types to wait for.</param>
        /// <returns>The received event.</returns>
        protected internal Task<Event> Receive(params Type[] eventTypes)
        {
            this.Assert(!this.IsHalted, "Machine '{0}' invoked Receive while halted.", this.Id);
            this.Runtime.NotifyReceiveCalled(this);
            return this.Inbox.ReceiveAsync(eventTypes);
        }

        /// <summary>
        /// Waits to receive an <see cref="Event"/> of the specified types
        /// that satisfy the specified predicates.
        /// </summary>
        /// <param name="events">Event types and predicates.</param>
        /// <returns>The received event.</returns>
        protected internal Task<Event> Receive(params Tuple<Type, Func<Event, bool>>[] events)
        {
            this.Assert(!this.IsHalted, "Machine '{0}' invoked Receive while halted.", this.Id);
            this.Runtime.NotifyReceiveCalled(this);
            return this.Inbox.ReceiveAsync(events);
        }

        /// <summary>
        /// Transitions the machine to the specified <see cref="MachineState"/>
        /// at the end of the current action.
        /// </summary>
        /// <typeparam name="S">Type of the state.</typeparam>
        protected void Goto<S>()
            where S : MachineState
        {
#pragma warning disable 618
            this.Goto(typeof(S));
#pragma warning restore 618
        }

        /// <summary>
        /// Transitions the machine to the specified <see cref="MachineState"/>
        /// at the end of the current action.
        /// </summary>
        /// <param name="s">Type of the state.</param>
        [Obsolete("Goto(typeof(T)) is deprecated; use Goto<T>() instead.")]
        protected void Goto(Type s)
        {
            this.Assert(!this.IsHalted, "Machine '{0}' invoked Goto while halted.", this.Id);
            this.Assert(StateTypeMap[this.GetType()].Any(val => val.DeclaringType.Equals(s.DeclaringType) && val.Name.Equals(s.Name)),
                "Machine '{0}' is trying to transition to non-existing state '{1}'.", this.Id, s.Name);
            this.Raise(new GotoStateEvent(s));
        }

        /// <summary>
        /// Transitions the machine to the specified <see cref="MachineState"/>
        /// at the end of the current action, pushing current state on the stack.
        /// </summary>
        /// <typeparam name="S">Type of the state.</typeparam>
        protected void Push<S>()
            where S : MachineState
        {
#pragma warning disable 618
            this.Push(typeof(S));
#pragma warning restore 618
        }

        /// <summary>
        /// Transitions the machine to the specified <see cref="MachineState"/>
        /// at the end of the current action, pushing current state on the stack.
        /// </summary>
        /// <param name="s">Type of the state.</param>
        [Obsolete("Push(typeof(T)) is deprecated; use Push<T>() instead.")]
        protected void Push(Type s)
        {
            this.Assert(!this.IsHalted, "Machine '{0}' invoked Push while halted.", this.Id);
            this.Assert(StateTypeMap[this.GetType()].Any(val => val.DeclaringType.Equals(s.DeclaringType) && val.Name.Equals(s.Name)),
                "Machine '{0}' is trying to transition to non-existing state '{1}'.", this.Id, s.Name);
            this.Raise(new PushStateEvent(s));
        }

        /// <summary>
        /// Pops the current <see cref="MachineState"/> from the state stack
        /// at the end of the current action.
        /// </summary>
        protected void Pop()
        {
            this.Runtime.NotifyPop(this);
            this.IsPopInvoked = true;
        }

        /// <summary>
        /// Starts a timer that sends a <see cref="TimerElapsedEvent"/> to this machine after the
        /// specified due time. The timer accepts an optional payload to be used during timeout.
        /// The timer is automatically disposed after it timeouts. To manually stop and dispose
        /// the timer, invoke the <see cref="StopTimer"/> method.
        /// </summary>
        /// <param name="dueTime">The amount of time to wait before sending the first timeout event.</param>
        /// <param name="payload">Optional payload of the timeout event.</param>
        /// <returns>Handle that contains information about the timer.</returns>
        protected TimerInfo StartTimer(TimeSpan dueTime, object payload = null)
        {
            // The specified due time and period must be valid.
            this.Assert(dueTime.TotalMilliseconds >= 0, "Machine '{0}' registered a timer with a negative due time.", this.Id);
            return this.RegisterTimer(dueTime, Timeout.InfiniteTimeSpan, payload);
        }

        /// <summary>
        /// Starts a periodic timer that sends a <see cref="TimerElapsedEvent"/> to this machine
        /// after the specified due time, and then repeats after each specified period. The timer
        /// accepts an optional payload to be used during timeout. The timer can be stopped by
        /// invoking the <see cref="StopTimer"/> method.
        /// </summary>
        /// <param name="dueTime">The amount of time to wait before sending the first timeout event.</param>
        /// <param name="period">The time interval between timeout events.</param>
        /// <param name="payload">Optional payload of the timeout event.</param>
        /// <returns>Handle that contains information about the timer.</returns>
        protected TimerInfo StartPeriodicTimer(TimeSpan dueTime, TimeSpan period, object payload = null)
        {
            // The specified due time and period must be valid.
            this.Assert(dueTime.TotalMilliseconds >= 0, "Machine '{0}' registered a periodic timer with a negative due time.", this.Id);
            this.Assert(period.TotalMilliseconds >= 0, "Machine '{0}' registered a periodic timer with a negative period.", this.Id);
            return this.RegisterTimer(dueTime, period, payload);
        }

        /// <summary>
        /// Stops and disposes the specified timer.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        protected void StopTimer(TimerInfo info)
        {
            this.Assert(info.OwnerId == this.Id, "Machine '{0}' is not allowed to dispose timer '{1}', which is owned by machine '{2}'.",
                this.Id, info, info.OwnerId);
            this.UnregisterTimer(info);
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>The controlled nondeterministic choice.</returns>
        protected bool Random()
        {
            return this.Runtime.GetNondeterministicBooleanChoice(this, 2);
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing. The value is used
        /// to generate a number in the range [0..maxValue), where 0
        /// triggers true.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <returns>The controlled nondeterministic choice.</returns>
        protected bool Random(int maxValue)
        {
            return this.Runtime.GetNondeterministicBooleanChoice(this, maxValue);
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>The controlled nondeterministic choice.</returns>
        protected bool FairRandom(
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            var havocId = string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}_{3}_{4}",
                this.Id.Name, this.CurrentStateName, callerMemberName, callerFilePath, callerLineNumber.ToString());
            return this.Runtime.GetFairNondeterministicBooleanChoice(this, havocId);
        }

        /// <summary>
        /// Returns a nondeterministic integer, that can be controlled during
        /// analysis or testing. The value is used to generate an integer in
        /// the range [0..maxValue).
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <returns>The controlled nondeterministic integer.</returns>
        protected int RandomInteger(int maxValue)
        {
            return this.Runtime.GetNondeterministicIntegerChoice(this, maxValue);
        }

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        /// <typeparam name="T">Type of the monitor.</typeparam>
        /// <param name="e">The event to send.</param>
        protected void Monitor<T>(Event e)
        {
            this.Monitor(typeof(T), e);
        }

        /// <summary>
        /// Invokes the specified monitor with the specified event.
        /// </summary>
        /// <param name="type">Type of the monitor.</param>
        /// <param name="e">The event to send.</param>
        protected void Monitor(Type type, Event e)
        {
            this.Assert(e != null, "Machine '{0}' is sending a null event.", this.Id);
            this.Runtime.Monitor(type, this, e);
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
        protected void Assert(bool predicate, string s, object arg0)
        {
            this.Runtime.Assert(predicate, s, arg0);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        protected void Assert(bool predicate, string s, object arg0, object arg1)
        {
            this.Runtime.Assert(predicate, s, arg0, arg1);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        protected void Assert(bool predicate, string s, object arg0, object arg1, object arg2)
        {
            this.Runtime.Assert(predicate, s, arg0, arg1, arg2);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        protected void Assert(bool predicate, string s, params object[] args)
        {
            this.Runtime.Assert(predicate, s, args);
        }

        /// <summary>
        /// Enqueues the specified event and its metadata.
        /// </summary>
        internal EnqueueStatus Enqueue(Event e, Guid opGroupId, EventInfo info)
        {
            if (this.IsHalted)
            {
                return EnqueueStatus.Dropped;
            }

            return this.Inbox.Enqueue(e, opGroupId, info);
        }

        /// <summary>
        /// Runs the event handler. The handler terminates if there
        /// is no next event to process or if the machine is halted.
        /// </summary>
        internal async Task RunEventHandlerAsync()
        {
            if (this.IsHalted)
            {
                return;
            }

            Event lastDequeuedEvent = null;
            while (!this.IsHalted && this.Runtime.IsRunning)
            {
                (DequeueStatus status, Event e, Guid opGroupId, EventInfo info) = this.Inbox.Dequeue();
                if (opGroupId != Guid.Empty)
                {
                    // Inherit the operation group id of the dequeue or raise operation, if it is non-empty.
                    this.StateManager.OperationGroupId = opGroupId;
                }

                if (status is DequeueStatus.Success)
                {
                    // Notify the runtime for a new event to handle. This is only used
                    // during bug-finding and operation bounding, because the runtime
                    // has to schedule a machine when a new operation is dequeued.
                    this.Runtime.NotifyDequeuedEvent(this, e, info);
                }
                else if (status is DequeueStatus.Raised)
                {
                    this.Runtime.NotifyHandleRaisedEvent(this, e);
                }
                else if (status is DequeueStatus.Default)
                {
                    this.Runtime.LogWriter.OnDefault(this.Id, this.CurrentStateName);

                    // If the default event was handled, then notify the runtime.
                    // This is only used during bug-finding, because the runtime
                    // has to schedule a machine between default handlers.
                    this.Runtime.NotifyDefaultHandlerFired(this);
                }
                else if (status is DequeueStatus.NotAvailable)
                {
                    break;
                }

                // Assigns the received event.
                this.ReceivedEvent = e;

                if (status is DequeueStatus.Success)
                {
                    // Inform the user of a successful dequeue once ReceivedEvent is set.
                    lastDequeuedEvent = e;
                    await this.ExecuteUserCallbackAsync(EventHandlerStatus.EventDequeued, lastDequeuedEvent);
                }

                if (e is TimerElapsedEvent timeoutEvent &&
                    timeoutEvent.Info.Period.TotalMilliseconds < 0)
                {
                    // If the timer is not periodic, then dispose it.
                    this.UnregisterTimer(timeoutEvent.Info);
                }

                // Handles next event.
                if (!this.IsHalted)
                {
                    await this.HandleEvent(e);
                }

                if (!this.Inbox.IsEventRaised && lastDequeuedEvent != null && !this.IsHalted)
                {
                    // Inform the user that the machine is done handling the current event.
                    // The machine will either go idle or dequeue its next event.
                    await this.ExecuteUserCallbackAsync(EventHandlerStatus.EventHandled, lastDequeuedEvent);
                    lastDequeuedEvent = null;
                }
            }
        }

        /// <summary>
        /// Handles the specified <see cref="Event"/>.
        /// </summary>
        private async Task HandleEvent(Event e)
        {
            Type currentState = this.CurrentState;

            while (true)
            {
                if (this.CurrentState is null)
                {
                    // If the stack of states is empty and the event
                    // is halt, then terminate the machine.
                    if (e.GetType().Equals(typeof(Halt)))
                    {
                        this.HaltMachine();
                        return;
                    }

                    string currentStateName = NameResolver.GetQualifiedStateName(currentState);
                    await this.ExecuteUserCallbackAsync(EventHandlerStatus.EventUnhandled, e, currentStateName);
                    if (this.IsHalted)
                    {
                        // Invoking a user callback caused the machine to halt.
                        return;
                    }

                    var unhandledEx = new UnhandledEventException(currentStateName, e, "Unhandled Event");
                    if (this.OnUnhandledEventExceptionHandler(nameof(this.HandleEvent), unhandledEx))
                    {
                        this.HaltMachine();
                        return;
                    }
                    else
                    {
                        // If the event cannot be handled then report an error and exit.
                        this.Assert(false, "Machine '{0}' received event '{1}' that cannot be handled.",
                            this.Id, e.GetType().FullName);
                    }
                }

                if (e.GetType() == typeof(GotoStateEvent))
                {
                    // Checks if the event is a goto state event.
                    Type targetState = (e as GotoStateEvent).State;
                    await this.GotoState(targetState, null);
                }
                else if (e.GetType() == typeof(PushStateEvent))
                {
                    // Checks if the event is a push state event.
                    Type targetState = (e as PushStateEvent).State;
                    await this.PushState(targetState);
                }
                else if (this.GotoTransitions.ContainsKey(e.GetType()))
                {
                    // Checks if the event can trigger a goto state transition.
                    var transition = this.GotoTransitions[e.GetType()];
                    await this.GotoState(transition.TargetState, transition.Lambda);
                }
                else if (this.GotoTransitions.ContainsKey(typeof(WildCardEvent)))
                {
                    var transition = this.GotoTransitions[typeof(WildCardEvent)];
                    await this.GotoState(transition.TargetState, transition.Lambda);
                }
                else if (this.PushTransitions.ContainsKey(e.GetType()))
                {
                    // Checks if the event can trigger a push state transition.
                    Type targetState = this.PushTransitions[e.GetType()].TargetState;
                    await this.PushState(targetState);
                }
                else if (this.PushTransitions.ContainsKey(typeof(WildCardEvent)))
                {
                    Type targetState = this.PushTransitions[typeof(WildCardEvent)].TargetState;
                    await this.PushState(targetState);
                }
                else if (this.CurrentActionHandlerMap.ContainsKey(e.GetType()) &&
                    this.CurrentActionHandlerMap[e.GetType()] is ActionBinding)
                {
                    // Checks if the event can trigger an action.
                    var handler = this.CurrentActionHandlerMap[e.GetType()] as ActionBinding;
                    await this.Do(handler.Name);
                }
                else if (this.CurrentActionHandlerMap.ContainsKey(typeof(WildCardEvent))
                    && this.CurrentActionHandlerMap[typeof(WildCardEvent)] is ActionBinding)
                {
                    var handler = this.CurrentActionHandlerMap[typeof(WildCardEvent)] as ActionBinding;
                    await this.Do(handler.Name);
                }
                else
                {
                    // If the current state cannot handle the event.
                    await this.ExecuteCurrentStateOnExit(null);
                    if (this.IsHalted)
                    {
                        return;
                    }

                    this.DoStatePop();
                    this.Runtime.LogWriter.OnPopUnhandledEvent(this.Id, this.CurrentStateName, e.GetType().FullName);
                    continue;
                }

                break;
            }
        }

        /// <summary>
        /// Invokes an action.
        /// </summary>
        private async Task Do(string actionName)
        {
            CachedDelegate cachedAction = this.ActionMap[actionName];
            this.Runtime.NotifyInvokedAction(this, cachedAction.MethodInfo, this.ReceivedEvent);
            await this.ExecuteAction(cachedAction);
            this.Runtime.NotifyCompletedAction(this, cachedAction.MethodInfo, this.ReceivedEvent);

            if (this.IsPopInvoked)
            {
                // Performs the state transition, if pop was invoked during the action.
                await this.PopState();
            }
        }

        /// <summary>
        /// Executes the on entry action of the current state.
        /// </summary>
        private async Task ExecuteCurrentStateOnEntry()
        {
            this.Runtime.NotifyEnteredState(this);

            CachedDelegate entryAction = null;
            if (this.StateStack.Peek().EntryAction != null)
            {
                entryAction = this.ActionMap[this.StateStack.Peek().EntryAction];
            }

            // Invokes the entry action of the new state,
            // if there is one available.
            if (entryAction != null)
            {
                this.Runtime.NotifyInvokedOnEntryAction(this, entryAction.MethodInfo, this.ReceivedEvent);
                await this.ExecuteAction(entryAction);
                this.Runtime.NotifyCompletedOnEntryAction(this, entryAction.MethodInfo, this.ReceivedEvent);
            }

            if (this.IsPopInvoked)
            {
                // Performs the state transition, if pop was invoked during the action.
                await this.PopState();
            }
        }

        /// <summary>
        /// Executes the on exit action of the current state.
        /// </summary>
        /// <param name="eventHandlerExitActionName">Action name</param>
        private async Task ExecuteCurrentStateOnExit(string eventHandlerExitActionName)
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
                this.Runtime.NotifyInvokedOnExitAction(this, exitAction.MethodInfo, this.ReceivedEvent);
                await this.ExecuteAction(exitAction);
                this.Runtime.NotifyCompletedOnExitAction(this, exitAction.MethodInfo, this.ReceivedEvent);
            }

            // Invokes the exit action of the event handler,
            // if there is one available.
            if (eventHandlerExitActionName != null)
            {
                CachedDelegate eventHandlerExitAction = this.ActionMap[eventHandlerExitActionName];
                this.Runtime.NotifyInvokedOnExitAction(this, eventHandlerExitAction.MethodInfo, this.ReceivedEvent);
                await this.ExecuteAction(eventHandlerExitAction);
                this.Runtime.NotifyCompletedOnExitAction(this, eventHandlerExitAction.MethodInfo, this.ReceivedEvent);
            }
        }

        /// <summary>
        /// An exception filter that calls <see cref="CoyoteRuntime.OnFailure"/>,
        /// which can choose to fast-fail the app to get a full dump.
        /// </summary>
        /// <param name="action">The machine action being executed when the failure occurred.</param>
        /// <param name="ex">The exception being tested.</param>
        private bool InvokeOnFailureExceptionFilter(CachedDelegate action, Exception ex)
        {
            // This is called within the exception filter so the stack has not yet been unwound.
            // If OnFailure does not fail-fast, return false to process the exception normally.
            this.Runtime.RaiseOnFailureEvent(new MachineActionExceptionFilterException(action.MethodInfo.Name, ex));
            return false;
        }

        /// <summary>
        /// Executes the specified action.
        /// </summary>
        private async Task ExecuteAction(CachedDelegate cachedAction)
        {
            try
            {
                if (cachedAction.Handler is Action action)
                {
                    // Use an exception filter to call OnFailure before the stack has been unwound.
                    try
                    {
                        action();
                    }
                    catch (Exception ex) when (this.OnExceptionHandler(cachedAction.MethodInfo.Name, ex))
                    {
                        // User handled the exception, return normally.
                    }
                    catch (Exception ex) when (!this.OnExceptionRequestedGracefulHalt && this.InvokeOnFailureExceptionFilter(cachedAction, ex))
                    {
                        // If InvokeOnFailureExceptionFilter does not fail-fast, it returns
                        // false to process the exception normally.
                    }
                }
                else if (cachedAction.Handler is Func<Task> taskFunc)
                {
                    try
                    {
                        // We have no reliable stack for awaited operations.
                        Task task = taskFunc();
                        this.Runtime.NotifyWaitTask(this, task);
                        await task;
                    }
                    catch (Exception ex) when (this.OnExceptionHandler(cachedAction.MethodInfo.Name, ex))
                    {
                        // User handled the exception, return normally.
                    }
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

                if (innerException is ExecutionCanceledException)
                {
                    this.IsHalted = true;
                    Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from machine '{this.Id}'.");
                }
                else if (innerException is TaskSchedulerException)
                {
                    this.IsHalted = true;
                    Debug.WriteLine($"<Exception> TaskSchedulerException was thrown from machine '{this.Id}'.");
                }
                else if (this.OnExceptionRequestedGracefulHalt)
                {
                    // Gracefully halt.
                    this.HaltMachine();
                }
                else
                {
                    // Reports the unhandled exception.
                    this.ReportUnhandledException(innerException, cachedAction.MethodInfo.Name);
                }
            }
        }

        /// <summary>
        /// Executes the specified event handler user callback.
        /// </summary>
        private Task ExecuteUserCallbackAsync(EventHandlerStatus eventHandlerStatus, Event lastDequeuedEvent, string currentState = default)
        {
            try
            {
                if (eventHandlerStatus is EventHandlerStatus.EventDequeued)
                {
                    try
                    {
                        return this.OnEventDequeueAsync(lastDequeuedEvent);
                    }
                    catch (Exception ex) when (this.OnExceptionHandler(nameof(this.OnEventDequeueAsync), ex))
                    {
                        // User handled the exception, return normally.
                    }
                }
                else if (eventHandlerStatus is EventHandlerStatus.EventHandled)
                {
                    try
                    {
                        return this.OnEventHandledAsync(lastDequeuedEvent);
                    }
                    catch (Exception ex) when (this.OnExceptionHandler(nameof(this.OnEventHandledAsync), ex))
                    {
                        // User handled the exception, return normally.
                    }
                }
                else if (eventHandlerStatus is EventHandlerStatus.EventUnhandled)
                {
                    try
                    {
                        return this.OnEventUnhandledAsync(lastDequeuedEvent, currentState);
                    }
                    catch (Exception ex) when (this.OnExceptionHandler(nameof(this.OnEventUnhandledAsync), ex))
                    {
                        // User handled the exception, return normally.
                    }
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

                if (innerException is ExecutionCanceledException)
                {
                    this.IsHalted = true;
                    Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from machine '{this.Id}'.");
                }
                else if (innerException is TaskSchedulerException)
                {
                    this.IsHalted = true;
                    Debug.WriteLine($"<Exception> TaskSchedulerException was thrown from machine '{this.Id}'.");
                }
                else if (this.OnExceptionRequestedGracefulHalt)
                {
                    // Gracefully halt.
                    this.HaltMachine();
                }
                else
                {
                    // Reports the unhandled exception.
                    if (eventHandlerStatus is EventHandlerStatus.EventDequeued)
                    {
                        this.ReportUnhandledException(innerException, nameof(this.OnEventDequeueAsync));
                    }
                    else if (eventHandlerStatus is EventHandlerStatus.EventHandled)
                    {
                        this.ReportUnhandledException(innerException, nameof(this.OnEventHandledAsync));
                    }
                    else if (eventHandlerStatus is EventHandlerStatus.EventUnhandled)
                    {
                        this.ReportUnhandledException(innerException, nameof(this.OnEventUnhandledAsync));
                    }
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Performs a goto transition to the specified state.
        /// </summary>
        private async Task GotoState(Type s, string onExitActionName)
        {
            this.Runtime.LogWriter.OnGoto(this.Id, this.CurrentStateName,
                $"{s.DeclaringType}.{NameResolver.GetStateNameForLogging(s)}");

            // The machine performs the on exit action of the current state.
            await this.ExecuteCurrentStateOnExit(onExitActionName);
            if (this.IsHalted)
            {
                return;
            }

            this.DoStatePop();

            var nextState = StateMap[this.GetType()].First(val
                => val.GetType().Equals(s));

            // The machine transitions to the new state.
            this.DoStatePush(nextState);

            // The machine performs the on entry action of the new state.
            await this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// Performs a push transition to the specified state.
        /// </summary>
        private async Task PushState(Type s)
        {
            this.Runtime.LogWriter.OnPush(this.Id, this.CurrentStateName, s.FullName);

            var nextState = StateMap[this.GetType()].First(val => val.GetType().Equals(s));
            this.DoStatePush(nextState);

            // The machine performs the on entry statements of the new state.
            await this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// Performs a pop transition from the current state.
        /// </summary>
        private async Task PopState()
        {
            this.IsPopInvoked = false;
            var prevStateName = this.CurrentStateName;

            // The machine performs the on exit action of the current state.
            await this.ExecuteCurrentStateOnExit(null);
            if (this.IsHalted)
            {
                return;
            }

            this.DoStatePop();
            this.Runtime.LogWriter.OnPop(this.Id, prevStateName, this.CurrentStateName);

            // Watch out for an extra pop.
            this.Assert(this.CurrentState != null, "Machine '{0}' popped with no matching push.", this.Id);
        }

        /// <summary>
        /// Configures the state transitions of the machine when a state is pushed into the stack.
        /// </summary>
        private void DoStatePush(MachineState state)
        {
            this.GotoTransitions = state.GotoTransitions;
            this.PushTransitions = state.PushTransitions;

            // Gets existing map for actions.
            var eventHandlerMap = this.CurrentActionHandlerMap is null ?
                new Dictionary<Type, EventActionHandler>() :
                new Dictionary<Type, EventActionHandler>(this.CurrentActionHandlerMap);

            // Updates the map with defer annotations.
            foreach (var deferredEvent in state.DeferredEvents)
            {
                if (deferredEvent.Equals(typeof(WildCardEvent)))
                {
                    eventHandlerMap.Clear();
                    eventHandlerMap[deferredEvent] = new DeferAction();
                    break;
                }

                eventHandlerMap[deferredEvent] = new DeferAction();
            }

            // Updates the map with actions.
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

            // Updates the map with ignores.
            foreach (var ignoredEvent in state.IgnoredEvents)
            {
                if (ignoredEvent.Equals(typeof(WildCardEvent)))
                {
                    eventHandlerMap.Clear();
                    eventHandlerMap[ignoredEvent] = new IgnoreAction();
                    break;
                }

                eventHandlerMap[ignoredEvent] = new IgnoreAction();
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
            this.ActionHandlerStack.Push(eventHandlerMap);
        }

        /// <summary>
        /// Configures the state transitions of the machine
        /// when a state is popped.
        /// </summary>
        private void DoStatePop()
        {
            this.StateStack.Pop();
            this.ActionHandlerStack.Pop();

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
        /// Checks if the specified event is ignored in the current machine state.
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
                foreach (var kvp in this.CurrentActionHandlerMap)
                {
                    if (!(kvp.Value is IgnoreAction))
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

            if (this.CurrentActionHandlerMap.ContainsKey(eventType))
            {
                return this.CurrentActionHandlerMap[eventType] is IgnoreAction;
            }

            if (this.CurrentActionHandlerMap.ContainsKey(typeof(WildCardEvent)) &&
                this.CurrentActionHandlerMap[typeof(WildCardEvent)] is IgnoreAction)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the specified event is deferred in the current machine state.
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

            if (this.CurrentActionHandlerMap.ContainsKey(eventType))
            {
                return this.CurrentActionHandlerMap[eventType] is DeferAction;
            }

            if (this.CurrentActionHandlerMap.ContainsKey(typeof(WildCardEvent)) &&
                this.CurrentActionHandlerMap[typeof(WildCardEvent)] is DeferAction)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a default handler is installed in current state.
        /// </summary>
        internal bool IsDefaultHandlerInstalledInCurrentState() =>
            this.CurrentActionHandlerMap.ContainsKey(typeof(Default)) ||
                this.GotoTransitions.ContainsKey(typeof(Default)) ||
                this.PushTransitions.ContainsKey(typeof(Default));

        /// <summary>
        /// Returns the cached state of this machine.
        /// </summary>
        internal override int GetCachedState()
        {
            unchecked
            {
                var hash = 19;
                hash = (hash * 31) + this.GetType().GetHashCode();
                hash = (hash * 31) + this.Id.Value.GetHashCode();
                hash = (hash * 31) + this.IsHalted.GetHashCode();

                hash = (hash * 31) + this.StateManager.GetCachedState();

                foreach (var state in this.StateStack)
                {
                    hash = (hash * 31) + state.GetType().GetHashCode();
                }

                hash = (hash * 31) + this.Inbox.GetCachedState();

                if (this.Runtime.Configuration.EnableUserDefinedStateHashing)
                {
                    // Adds the user-defined hashed machine state.
                    hash = (hash * 31) + this.HashedState;
                }

                return hash;
            }
        }

        /// <summary>
        /// Transitions to the start state, and executes the
        /// entry action, if there is any.
        /// </summary>
        internal Task GotoStartState(Event e)
        {
            this.ReceivedEvent = e;
            return this.ExecuteCurrentStateOnEntry();
        }

        /// <summary>
        /// Initializes information about the states of the machine.
        /// </summary>
        internal void InitializeStateInformation()
        {
            Type machineType = this.GetType();

            if (MachineStateCached.TryAdd(machineType, false))
            {
                // Caches the available state types for this machine type.
                if (StateTypeMap.TryAdd(machineType, new HashSet<Type>()))
                {
                    Type baseType = machineType;
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

                // Caches the available state instances for this machine type.
                if (StateMap.TryAdd(machineType, new HashSet<MachineState>()))
                {
                    foreach (var type in StateTypeMap[machineType])
                    {
                        Type stateType = type;
                        if (type.IsAbstract)
                        {
                            continue;
                        }

                        if (type.IsGenericType)
                        {
                            // If the state type is generic (only possible if inherited by a
                            // generic machine declaration), then iterate through the base
                            // machine classes to identify the runtime generic type, and use
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
                        var lambda = Expression.Lambda<Func<MachineState>>(
                            Expression.New(constructor)).Compile();
                        MachineState state = lambda();

                        try
                        {
                            state.InitializeState();
                        }
                        catch (InvalidOperationException ex)
                        {
                            this.Assert(false, "Machine '{0}' {1} in state '{2}'.", this.Id, ex.Message, state);
                        }

                        StateMap[machineType].Add(state);
                    }
                }

                // Caches the actions declarations for this machine type.
                if (MachineActionMap.TryAdd(machineType, new Dictionary<string, MethodInfo>()))
                {
                    foreach (var state in StateMap[machineType])
                    {
                        if (state.EntryAction != null &&
                            !MachineActionMap[machineType].ContainsKey(state.EntryAction))
                        {
                            MachineActionMap[machineType].Add(
                                state.EntryAction,
                                this.GetActionWithName(state.EntryAction));
                        }

                        if (state.ExitAction != null &&
                            !MachineActionMap[machineType].ContainsKey(state.ExitAction))
                        {
                            MachineActionMap[machineType].Add(
                                state.ExitAction,
                                this.GetActionWithName(state.ExitAction));
                        }

                        foreach (var transition in state.GotoTransitions)
                        {
                            if (transition.Value.Lambda != null &&
                                !MachineActionMap[machineType].ContainsKey(transition.Value.Lambda))
                            {
                                MachineActionMap[machineType].Add(
                                    transition.Value.Lambda,
                                    this.GetActionWithName(transition.Value.Lambda));
                            }
                        }

                        foreach (var action in state.ActionBindings)
                        {
                            if (!MachineActionMap[machineType].ContainsKey(action.Value.Name))
                            {
                                MachineActionMap[machineType].Add(
                                    action.Value.Name,
                                    this.GetActionWithName(action.Value.Name));
                            }
                        }
                    }
                }

                // Cache completed.
                lock (MachineStateCached)
                {
                    MachineStateCached[machineType] = true;
                    System.Threading.Monitor.PulseAll(MachineStateCached);
                }
            }
            else if (!MachineStateCached[machineType])
            {
                lock (MachineStateCached)
                {
                    while (!MachineStateCached[machineType])
                    {
                        System.Threading.Monitor.Wait(MachineStateCached);
                    }
                }
            }

            // Populates the map of actions for this machine instance.
            foreach (var kvp in MachineActionMap[machineType])
            {
                this.ActionMap.Add(kvp.Key, new CachedDelegate(kvp.Value, this));
            }

            var initialStates = StateMap[machineType].Where(state => state.IsStart).ToList();
            this.Assert(initialStates.Count != 0, "Machine '{0}' must declare a start state.", this.Id);
            this.Assert(initialStates.Count is 1, "Machine '{0}' can not declare more than one start states.", this.Id);

            this.DoStatePush(initialStates[0]);

            this.AssertStateValidity();
        }

        /// <summary>
        /// Registers a new timer using the specified configuration.
        /// </summary>
        private TimerInfo RegisterTimer(TimeSpan dueTime, TimeSpan period, object payload)
        {
            var info = new TimerInfo(this.Id, dueTime, period, payload);
            var timer = this.Runtime.CreateMachineTimer(info, this);
            this.Runtime.LogWriter.OnCreateTimer(info);
            this.Timers.Add(info, timer);
            return info;
        }

        /// <summary>
        /// Unregisters the specified timer.
        /// </summary>
        private void UnregisterTimer(TimerInfo info)
        {
            if (!this.Timers.TryGetValue(info, out IMachineTimer timer))
            {
                this.Assert(info.OwnerId == this.Id, "Timer '{0}' is already disposed.", info);
            }

            this.Runtime.LogWriter.OnStopTimer(info);
            this.Timers.Remove(info);
            timer.Dispose();
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
        /// Processes a type, looking for machine states.
        /// </summary>
        private void ExtractStateTypes(Type type)
        {
            Stack<Type> stack = new Stack<Type>();
            stack.Push(type);

            while (stack.Count > 0)
            {
                Type nextType = stack.Pop();

                if (nextType.IsClass && nextType.IsSubclassOf(typeof(MachineState)))
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
                        this.Assert(t.IsSubclassOf(typeof(StateGroup)) || t.IsSubclassOf(typeof(MachineState)),
                            "'{0}' is neither a group of states nor a state.", t.Name);
                        stack.Push(t);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the action with the specified name.
        /// </summary>
        private MethodInfo GetActionWithName(string actionName)
        {
            MethodInfo method;
            Type machineType = this.GetType();

            do
            {
                method = machineType.GetMethod(actionName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                    Type.DefaultBinder, Array.Empty<Type>(), null);
                machineType = machineType.BaseType;
            }
            while (method is null && machineType != typeof(StateMachine));

            this.Assert(method != null, "Cannot detect action declaration '{0}' in machine '{1}'.", actionName, this.GetType().Name);
            this.Assert(method.GetParameters().Length is 0, "Action '{0}' in machine '{1}' must have 0 formal parameters.",
                method.Name, this.GetType().Name);

            if (method.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null)
            {
                this.Assert(method.ReturnType == typeof(Task),
                    "Async action '{0}' in machine '{1}' must have 'Task' return type.",
                    method.Name, this.GetType().Name);
            }
            else
            {
                this.Assert(method.ReturnType == typeof(void), "Action '{0}' in machine '{1}' must have 'void' return type.",
                    method.Name, this.GetType().Name);
            }

            return method;
        }

        /// <summary>
        /// Returns the set of all states in the machine (for code coverage).
        /// </summary>
        internal HashSet<string> GetAllStates()
        {
            this.Assert(StateMap.ContainsKey(this.GetType()), "Machine '{0}' hasn't populated its states yet.", this.Id);

            var allStates = new HashSet<string>();
            foreach (var state in StateMap[this.GetType()])
            {
                allStates.Add(NameResolver.GetQualifiedStateName(state.GetType()));
            }

            return allStates;
        }

        /// <summary>
        /// Returns the set of all (states, registered event) pairs in the machine (for code coverage).
        /// </summary>
        internal HashSet<Tuple<string, string>> GetAllStateEventPairs()
        {
            this.Assert(StateMap.ContainsKey(this.GetType()), "Machine '{0}' hasn't populated its states yet.", this.Id);

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

                foreach (var pushtransition in state.PushTransitions)
                {
                    pairs.Add(Tuple.Create(NameResolver.GetQualifiedStateName(state.GetType()), pushtransition.Key.FullName));
                }
            }

            return pairs;
        }

        /// <summary>
        /// Check machine for state related errors.
        /// </summary>
        private void AssertStateValidity()
        {
            this.Assert(StateTypeMap[this.GetType()].Count > 0, "Machine '{0}' must have one or more states.", this.Id);
            this.Assert(this.StateStack.Peek() != null, "Machine '{0}' must not have a null current state.", this.Id);
        }

        /// <summary>
        /// Wraps the unhandled exception inside an <see cref="AssertionFailureException"/>
        /// exception, and throws it to the user.
        /// </summary>
        private void ReportUnhandledException(Exception ex, string actionName)
        {
            string state = "<unknown>";
            if (this.CurrentState != null)
            {
                state = this.CurrentStateName;
            }

            this.Runtime.WrapAndThrowException(ex, $"Exception '{ex.GetType()}' was thrown " +
                $"in machine '{this.Id}', state '{state}', action '{actionName}', " +
                $"'{ex.Source}':\n" +
                $"   {ex.Message}\n" +
                $"The stack trace is:\n{ex.StackTrace}");
        }

        /// <summary>
        /// Invokes user callback when a machine receives an event that it cannot handle.
        /// </summary>
        /// <param name="methodName">The handler (outermost) that threw the exception.</param>
        /// <param name="ex">The exception thrown by the machine.</param>
        /// <returns>False if the exception should continue to get thrown, true if the machine should gracefully halt.</returns>
        private bool OnUnhandledEventExceptionHandler(string methodName, UnhandledEventException ex)
        {
            this.Runtime.LogWriter.OnMachineExceptionThrown(this.Id, ex.CurrentStateName, methodName, ex);

            var ret = this.OnException(methodName, ex);
            this.OnExceptionRequestedGracefulHalt = false;
            switch (ret)
            {
                case OnExceptionOutcome.HaltMachine:
                case OnExceptionOutcome.HandledException:
                    this.Runtime.LogWriter.OnMachineExceptionHandled(this.Id, ex.CurrentStateName, methodName, ex);
                    this.OnExceptionRequestedGracefulHalt = true;
                    return true;
                case OnExceptionOutcome.ThrowException:
                    return false;
            }

            return false;
        }

        /// <summary>
        /// Invokes user callback when a machine throws an exception.
        /// </summary>
        /// <param name="methodName">The handler (outermost) that threw the exception.</param>
        /// <param name="ex">The exception thrown by the machine.</param>
        /// <returns>False if the exception should continue to get thrown, true if it was handled in this method.</returns>
        private bool OnExceptionHandler(string methodName, Exception ex)
        {
            if (ex is ExecutionCanceledException)
            {
                // Internal exception, used during testing.
                return false;
            }

            this.Runtime.LogWriter.OnMachineExceptionThrown(this.Id, this.CurrentStateName, methodName, ex);

            var ret = this.OnException(methodName, ex);
            this.OnExceptionRequestedGracefulHalt = false;

            switch (ret)
            {
                case OnExceptionOutcome.ThrowException:
                    return false;
                case OnExceptionOutcome.HandledException:
                    this.Runtime.LogWriter.OnMachineExceptionHandled(this.Id, this.CurrentStateName, methodName, ex);
                    return true;
                case OnExceptionOutcome.HaltMachine:
                    this.OnExceptionRequestedGracefulHalt = true;
                    return false;
            }

            return false;
        }

        /// <summary>
        /// User callback when a machine throws an exception.
        /// </summary>
        /// <param name="methodName">The handler (outermost) that threw the exception.</param>
        /// <param name="ex">The exception thrown by the machine.</param>
        /// <returns>The action that the runtime should take.</returns>
        protected virtual OnExceptionOutcome OnException(string methodName, Exception ex)
        {
            return OnExceptionOutcome.ThrowException;
        }

        /// <summary>
        /// User callback that is invoked when the machine successfully dequeues
        /// an event from its inbox. This method is not called when the dequeue
        /// happens via a Receive statement.
        /// </summary>
        /// <param name="e">The event that was dequeued.</param>
        protected virtual Task OnEventDequeueAsync(Event e) => Task.CompletedTask;

        /// <summary>
        /// User callback that is invoked when the machine finishes handling a dequeued event,
        /// unless the handler of the dequeued event raised an event or caused the machine to
        /// halt (either normally or due to an exception). Unless this callback raises an event,
        /// the machine will either become idle or dequeue the next event from its inbox.
        /// </summary>
        /// <param name="e">The event that was handled.</param>
        protected virtual Task OnEventHandledAsync(Event e) => Task.CompletedTask;

        /// <summary>
        /// User callback that is invoked when the machine receives an event that it is not prepared
        /// to handle. The callback is invoked first, after which the machine will necessarily throw
        /// an <see cref="UnhandledEventException"/>
        /// </summary>
        /// <param name="e">The event that was unhandled.</param>
        /// <param name="currentState">The state of the machine when the event was dequeued.</param>
        protected virtual Task OnEventUnhandledAsync(Event e, string currentState) => Task.CompletedTask;

        /// <summary>
        /// User callback that is invoked when a machine halts.
        /// </summary>
        protected virtual void OnHalt()
        {
        }

        /// <summary>
        /// Resets the static caches.
        /// </summary>
        internal static void ResetCaches()
        {
            StateTypeMap.Clear();
            StateMap.Clear();
            MachineActionMap.Clear();
        }

        /// <summary>
        /// Halts the machine.
        /// </summary>
        private void HaltMachine()
        {
            this.IsHalted = true;
            this.ReceivedEvent = null;

            // Close the inbox, which will stop any subsequent enqueues.
            this.Inbox.Close();

            this.Runtime.LogWriter.OnHalt(this.Id, this.Inbox.Size);
            this.Runtime.NotifyHalted(this);

            // Dispose any held resources.
            this.Inbox.Dispose();
            foreach (var timer in this.Timers.Keys.ToList())
            {
                this.UnregisterTimer(timer);
            }

            // Invoke user callback.
            this.OnHalt();
        }
    }
}
