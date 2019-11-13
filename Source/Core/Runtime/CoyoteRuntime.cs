// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Threading;
using Microsoft.Coyote.Threading.Tasks;
using Monitor = Microsoft.Coyote.Specifications.Monitor;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Runtime for executing explicit and implicit asynchronous machines.
    /// </summary>
    internal abstract class CoyoteRuntime : IActorRuntime
    {
        /// <summary>
        /// Provides access to the runtime associated with the current execution context.
        /// </summary>
        internal static RuntimeProvider Provider { get; set; } = new RuntimeProvider();

        /// <summary>
        /// The configuration used by the runtime.
        /// </summary>
        internal readonly Configuration Configuration;

        /// <summary>
        /// Map from task ids to <see cref="ControlledTask"/> objects.
        /// </summary>
        protected readonly ConcurrentDictionary<int, ControlledTask> TaskMap;

        /// <summary>
        /// Monotonically increasing actor id counter.
        /// </summary>
        internal long ActorIdCounter;

        /// <summary>
        /// Monotonically increasing lock id counter.
        /// </summary>
        internal long LockIdCounter;

        /// <summary>
        /// Records if the runtime is running.
        /// </summary>
        internal volatile bool IsRunning;

        /// <summary>
        /// Returns the id of the currently executing <see cref="ControlledTask"/>.
        /// </summary>
        internal virtual int? CurrentTaskId => Task.CurrentId;

        /// <summary>
        /// The log writer.
        /// </summary>
        protected internal IActorRuntimeLog LogWriter { get; private set; }

        /// <summary>
        /// The installed logger.
        /// </summary>
        public ILogger Logger => this.LogWriter.Logger;

        /// <summary>
        /// Callback that is fired when the Coyote program throws an exception.
        /// </summary>
        public event OnFailureHandler OnFailure;

        /// <summary>
        /// Callback that is fired when a Coyote event is dropped.
        /// </summary>
        public event OnEventDroppedHandler OnEventDropped;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoyoteRuntime"/> class.
        /// </summary>
        protected CoyoteRuntime(Configuration configuration)
        {
            this.Configuration = configuration;
            this.TaskMap = new ConcurrentDictionary<int, ControlledTask>();
            this.ActorIdCounter = 0;
            this.LockIdCounter = 0;
            this.LogWriter = new ActorRuntimeLogWriter
            {
                Logger = configuration.IsVerbose ? (ILogger)new ConsoleLogger() : new NulLogger()
            };

            this.IsRunning = true;
        }

        /// <summary>
        /// Creates a fresh actor id that has not yet been bound to any actor.
        /// </summary>
        public ActorId CreateActorId(Type type, string name = null) => new ActorId(type, name, this);

        /// <summary>
        /// Creates a actor id that is uniquely tied to the specified unique name. The
        /// returned actor id can either be a fresh id (not yet bound to any actor), or
        /// it can be bound to a previously created actor. In the second case, this actor
        /// id can be directly used to communicate with the corresponding actor.
        /// </summary>
        public abstract ActorId CreateActorIdFromName(Type type, string name);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and with the specified
        /// optional <see cref="Event"/>. This event can only be used to access its payload,
        /// and cannot be handled.
        /// </summary>
        public abstract ActorId CreateActor(Type type, Event initialEvent = null, Guid opGroupId = default);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, and with the
        /// specified optional <see cref="Event"/>. This event can only be used to access
        /// its payload, and cannot be handled.
        /// </summary>
        public abstract ActorId CreateActor(Type type, string name, Event initialEvent = null, Guid opGroupId = default);

        /// <summary>
        /// Creates a new actor of the specified type, using the specified <see cref="ActorId"/>.
        /// This method optionally passes an <see cref="Event"/> to the new actor, which can only
        /// be used to access its payload, and cannot be handled.
        /// </summary>
        public abstract ActorId CreateActor(ActorId id, Type type, Event initialEvent = null, Guid opGroupId = default);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and with the specified
        /// optional <see cref="Event"/>. This event can only be used to access its payload,
        /// and cannot be handled. The method returns only when the actor is initialized and
        /// the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public abstract Task<ActorId> CreateActorAndExecuteAsync(Type type, Event initialEvent = null,
            Guid opGroupId = default);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, and with the
        /// specified optional <see cref="Event"/>. This event can only be used to access
        /// its payload, and cannot be handled. The method returns only when the actor is
        /// initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        public abstract Task<ActorId> CreateActorAndExecuteAsync(Type type, string name, Event initialEvent = null,
            Guid opGroupId = default);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/>, using the specified unbound
        /// actor id, and passes the specified optional <see cref="Event"/>. This event can only
        /// be used to access its payload, and cannot be handled. The method returns only when
        /// the actor is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        public abstract Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, Event initialEvent = null,
            Guid opGroupId = default);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to an actor.
        /// </summary>
        public abstract void SendEvent(ActorId targetId, Event e, Guid opGroupId = default, SendOptions options = null);

        /// <summary>
        /// Sends an <see cref="Event"/> to an actor. Returns immediately if the target was already
        /// running. Otherwise blocks until the target handles the event and reaches quiescense.
        /// </summary>
        public abstract Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event e, Guid opGroupId = default,
            SendOptions options = null);

        /// <summary>
        /// Registers a new specification monitor of the specified <see cref="Type"/>.
        /// </summary>
        public void RegisterMonitor(Type type)
        {
            this.TryCreateMonitor(type);
        }

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        public void InvokeMonitor<T>(Event e)
        {
            this.InvokeMonitor(typeof(T), e);
        }

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        public void InvokeMonitor(Type type, Event e)
        {
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot monitor a null event.");
            this.Monitor(type, null, e);
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing.
        /// </summary>
        public bool Random()
        {
            return this.GetNondeterministicBooleanChoice(null, 2);
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        public bool FairRandom(
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            var havocId = string.Format("Runtime_{0}_{1}_{2}",
                callerMemberName, callerFilePath, callerLineNumber.ToString());
            return this.GetFairNondeterministicBooleanChoice(null, havocId);
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing. The value is used to generate a number
        /// in the range [0..maxValue), where 0 triggers true.
        /// </summary>
        public bool Random(int maxValue)
        {
            return this.GetNondeterministicBooleanChoice(null, maxValue);
        }

        /// <summary>
        /// Returns a nondeterministic integer, that can be controlled during
        /// analysis or testing. The value is used to generate an integer in
        /// the range [0..maxValue).
        /// </summary>
        public int RandomInteger(int maxValue)
        {
            return this.GetNondeterministicIntegerChoice(null, maxValue);
        }

        /// <summary>
        /// Returns the operation group id of the actor with the specified id. Returns <see cref="Guid.Empty"/>
        /// if the id is not set, or if the <see cref="ActorId"/> is not associated with this runtime. During
        /// testing, the runtime asserts that the specified actor is currently executing.
        /// </summary>
        public abstract Guid GetCurrentOperationGroupId(ActorId currentActorId);

        /// <summary>
        /// Terminates the runtime and notifies each active actor to halt execution.
        /// </summary>
        public void Stop()
        {
            this.IsRunning = false;
        }

        /// <summary>
        /// Creates a new <see cref="Actor"/> of the specified <see cref="Type"/>.
        /// </summary>
        internal abstract ActorId CreateActor(ActorId id, Type type, string name, Event initialEvent,
            Actor creator, Guid opGroupId);

        /// <summary>
        /// Creates a new <see cref="Actor"/> of the specified <see cref="Type"/>. The method
        /// returns only when the actor is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        internal abstract Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, string name,
            Event initialEvent, Actor creator, Guid opGroupId);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to an actor.
        /// </summary>
        internal abstract void SendEvent(ActorId targetId, Event e, Actor sender, Guid opGroupId, SendOptions options);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to an actor. Returns immediately if the target was
        /// already running. Otherwise blocks until the target handles the event and reaches quiescense.
        /// </summary>
        internal abstract Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event e, Actor sender,
            Guid opGroupId, SendOptions options);

        /// <summary>
        /// Creates a new <see cref="ControlledTask"/> to execute the specified asynchronous work.
        /// </summary>
        internal abstract ControlledTask CreateControlledTask(Action action, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a new <see cref="ControlledTask"/> to execute the specified asynchronous work.
        /// </summary>
        internal abstract ControlledTask CreateControlledTask(Func<ControlledTask> function, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a new <see cref="ControlledTask{TResult}"/> to execute the specified asynchronous work.
        /// </summary>
        internal abstract ControlledTask<TResult> CreateControlledTask<TResult>(Func<TResult> function,
            CancellationToken cancellationToken);

        /// <summary>
        /// Creates a new <see cref="ControlledTask{TResult}"/> to execute the specified asynchronous work.
        /// </summary>
        internal abstract ControlledTask<TResult> CreateControlledTask<TResult>(Func<ControlledTask<TResult>> function,
            CancellationToken cancellationToken);

        /// <summary>
        /// Creates a new <see cref="ControlledTask"/> to execute the specified asynchronous delay.
        /// </summary>
        internal abstract ControlledTask CreateControlledTaskDelay(int millisecondsDelay, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a new <see cref="ControlledTask"/> to execute the specified asynchronous delay.
        /// </summary>
        internal abstract ControlledTask CreateControlledTaskDelay(TimeSpan delay, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> associated with a completion source.
        /// </summary>
        internal abstract ControlledTask CreateControlledTaskCompletionSource(Task task);

        /// <summary>
        /// Creates a <see cref="ControlledTask{TResult}"/> associated with a completion source.
        /// </summary>
        internal abstract ControlledTask<TResult> CreateControlledTaskCompletionSource<TResult>(Task<TResult> task);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        internal abstract ControlledTask WaitAllTasksAsync(IEnumerable<ControlledTask> tasks);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        internal abstract ControlledTask<TResult[]> WaitAllTasksAsync<TResult>(IEnumerable<ControlledTask<TResult>> tasks);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        internal abstract ControlledTask<ControlledTask> WaitAnyTaskAsync(IEnumerable<ControlledTask> tasks);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        internal abstract ControlledTask<ControlledTask<TResult>> WaitAnyTaskAsync<TResult>(IEnumerable<ControlledTask<TResult>> tasks);

        /// <summary>
        /// Waits for all of the provided <see cref="ControlledTask"/> objects to complete execution.
        /// </summary>
        internal abstract void WaitAllTasks(params ControlledTask[] tasks);

        /// <summary>
        /// Waits for all of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified number of milliseconds.
        /// </summary>
        internal abstract bool WaitAllTasks(ControlledTask[] tasks, int millisecondsTimeout);

        /// <summary>
        /// Waits for all of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified number of milliseconds or until a cancellation
        /// token is cancelled.
        /// </summary>
        internal abstract bool WaitAllTasks(ControlledTask[] tasks, int millisecondsTimeout, CancellationToken cancellationToken);

        /// <summary>
        /// Waits for all of the provided <see cref="ControlledTask"/> objects to complete
        /// execution unless the wait is cancelled.
        /// </summary>
        internal abstract void WaitAllTasks(ControlledTask[] tasks, CancellationToken cancellationToken);

        /// <summary>
        /// Waits for all of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified time interval.
        /// </summary>
        internal abstract bool WaitAllTasks(ControlledTask[] tasks, TimeSpan timeout);

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete execution.
        /// </summary>
        internal abstract int WaitAnyTask(params ControlledTask[] tasks);

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified number of milliseconds.
        /// </summary>
        internal abstract int WaitAnyTask(ControlledTask[] tasks, int millisecondsTimeout);

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified number of milliseconds or until a cancellation
        /// token is cancelled.
        /// </summary>
        internal abstract int WaitAnyTask(ControlledTask[] tasks, int millisecondsTimeout, CancellationToken cancellationToken);

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete
        /// execution unless the wait is cancelled.
        /// </summary>
        internal abstract int WaitAnyTask(ControlledTask[] tasks, CancellationToken cancellationToken);

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified time interval.
        /// </summary>
        internal abstract int WaitAnyTask(ControlledTask[] tasks, TimeSpan timeout);

        /// <summary>
        /// Creates a controlled awaiter that switches into a target environment.
        /// </summary>
        internal abstract ControlledYieldAwaitable.ControlledYieldAwaiter CreateControlledYieldAwaiter();

        /// <summary>
        /// Ends the wait for the completion of the yield operation.
        /// </summary>
        internal abstract void OnGetYieldResult(YieldAwaitable.YieldAwaiter awaiter);

        /// <summary>
        /// Sets the action to perform when the yield operation completes.
        /// </summary>
        internal abstract void OnYieldCompleted(Action continuation, YieldAwaitable.YieldAwaiter awaiter);

        /// <summary>
        /// Schedules the continuation action that is invoked when the yield operation completes.
        /// </summary>
        internal abstract void OnUnsafeYieldCompleted(Action continuation, YieldAwaitable.YieldAwaiter awaiter);

        /// <summary>
        /// Creates a mutual exclusion lock that is compatible with <see cref="ControlledTask"/> objects.
        /// </summary>
        internal abstract ControlledLock CreateControlledLock();

        /// <summary>
        /// Creates a new timer that sends a <see cref="TimerElapsedEvent"/> to its owner actor.
        /// </summary>
        internal abstract IActorTimer CreateActorTimer(TimerInfo info, Actor owner);

        /// <summary>
        /// Tries to create a new specification monitor of the specified <see cref="Type"/>.
        /// </summary>
        internal abstract void TryCreateMonitor(Type type);

        /// <summary>
        /// Invokes the specification monitor with the specified <see cref="Event"/>.
        /// </summary>
        internal abstract void Monitor(Type type, Actor sender, Event e);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public virtual void Assert(bool predicate)
        {
            if (!predicate)
            {
                throw new AssertionFailureException("Detected an assertion failure.");
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public virtual void Assert(bool predicate, string s, object arg0)
        {
            if (!predicate)
            {
                throw new AssertionFailureException(string.Format(CultureInfo.InvariantCulture, s, arg0.ToString()));
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public virtual void Assert(bool predicate, string s, object arg0, object arg1)
        {
            if (!predicate)
            {
                throw new AssertionFailureException(string.Format(CultureInfo.InvariantCulture, s, arg0.ToString(), arg1.ToString()));
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public virtual void Assert(bool predicate, string s, object arg0, object arg1, object arg2)
        {
            if (!predicate)
            {
                throw new AssertionFailureException(string.Format(CultureInfo.InvariantCulture, s, arg0.ToString(), arg1.ToString(), arg2.ToString()));
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        public virtual void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                throw new AssertionFailureException(string.Format(CultureInfo.InvariantCulture, s, args));
            }
        }

        /// <summary>
        /// Asserts that the currently executing controlled task is awaiting a controlled awaiter.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void AssertAwaitingControlledAwaiter<TAwaiter>(ref TAwaiter awaiter)
            where TAwaiter : INotifyCompletion
        {
        }

        /// <summary>
        /// Asserts that the currently executing controlled task is awaiting a controlled awaiter.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void AssertAwaitingUnsafeControlledAwaiter<TAwaiter>(ref TAwaiter awaiter)
            where TAwaiter : ICriticalNotifyCompletion
        {
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        internal abstract bool GetNondeterministicBooleanChoice(Actor caller, int maxValue);

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        internal abstract bool GetFairNondeterministicBooleanChoice(Actor caller, string uniqueId);

        /// <summary>
        /// Returns a nondeterministic integer choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        internal abstract int GetNondeterministicIntegerChoice(Actor caller, int maxValue);

        /// <summary>
        /// Injects a context switch point that can be systematically explored during testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void ExploreContextSwitch()
        {
        }

        /// <summary>
        /// Notifies that an actor invoked an action.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyInvokedAction(Actor actor, MethodInfo action, Event receivedEvent)
        {
        }

        /// <summary>
        /// Notifies that an actor completed invoking an action.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyCompletedAction(Actor actor, MethodInfo action, Event receivedEvent)
        {
        }

        /// <summary>
        /// Notifies that an actor dequeued an <see cref="Event"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyDequeuedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
        }

        /// <summary>
        /// Notifies that an actor dequeued the default <see cref="Event"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyDefaultEventDequeued(Actor actor)
        {
        }

        /// <summary>
        /// Notifies that the inbox of the specified actor is about to be
        /// checked to see if the default event handler should fire.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyDefaultEventHandlerCheck(Actor actor)
        {
        }

        /// <summary>
        /// Notifies that an actor raised an <see cref="Event"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyRaisedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
        }

        /// <summary>
        /// Notifies that an actor is handling a raised <see cref="Event"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyHandleRaisedEvent(Actor actor, Event e)
        {
        }

        /// <summary>
        /// Notifies that an actor called <see cref="Actor.ReceiveEventAsync(Type[])"/>
        /// or one of its overloaded methods.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyReceiveCalled(Actor actor)
        {
        }

        /// <summary>
        /// Notifies that an actor enqueued an event that it was waiting to receive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyReceivedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
        }

        /// <summary>
        /// Notifies that an actor received an event without waiting because the event
        /// was already in the inbox when the actor invoked the receive statement.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyReceivedEventWithoutWaiting(Actor actor, Event e, EventInfo eventInfo)
        {
        }

        /// <summary>
        /// Notifies that an actor is waiting for the specified task to complete.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyWaitTask(Actor actor, Task task)
        {
        }

        /// <summary>
        /// Notifies that an actor is waiting to receive an event of one of the specified types.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyWaitEvent(Actor actor, IEnumerable<Type> eventTypes)
        {
        }

        /// <summary>
        /// Notifies that an actor has halted.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyHalted(Actor actor)
        {
        }

        /// <summary>
        /// Notifies that a state machine entered a state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyEnteredState(StateMachine stateMachine)
        {
        }

        /// <summary>
        /// Notifies that a state machine exited a state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyExitedState(StateMachine stateMachine)
        {
        }

        /// <summary>
        /// Notifies that a state machine invoked pop.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyPop(StateMachine stateMachine)
        {
        }

        /// <summary>
        /// Notifies that a state machine invoked an action.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyInvokedOnEntryAction(StateMachine stateMachine, MethodInfo action, Event receivedEvent)
        {
        }

        /// <summary>
        /// Notifies that a state machine invoked an action.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyInvokedOnExitAction(StateMachine stateMachine, MethodInfo action, Event receivedEvent)
        {
        }

        /// <summary>
        /// Notifies that a state machine completed invoking an action.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyCompletedOnEntryAction(StateMachine stateMachine, MethodInfo action, Event receivedEvent)
        {
        }

        /// <summary>
        /// Notifies that a state machine completed invoking an action.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyCompletedOnExitAction(StateMachine stateMachine, MethodInfo action, Event receivedEvent)
        {
        }

        /// <summary>
        /// Notifies that a monitor entered a state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyEnteredState(Monitor monitor)
        {
        }

        /// <summary>
        /// Notifies that a monitor exited a state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyExitedState(Monitor monitor)
        {
        }

        /// <summary>
        /// Notifies that a monitor invoked an action.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyInvokedAction(Monitor monitor, MethodInfo action, Event receivedEvent)
        {
        }

        /// <summary>
        /// Notifies that a monitor raised an <see cref="Event"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void NotifyRaisedEvent(Monitor monitor, Event e, EventInfo eventInfo)
        {
        }

        /// <summary>
        /// Use this method to abstract the default <see cref="ActorRuntimeLogWriter"/>
        /// for logging runtime messages.
        /// </summary>
        public IActorRuntimeLog SetLogWriter(IActorRuntimeLog logWriter)
        {
            var logger = this.LogWriter.Logger;
            var prevLogWriter = this.LogWriter;
            this.LogWriter = logWriter ?? throw new InvalidOperationException("Cannot install a null log writer.");
            this.SetLogger(logger);
            return prevLogWriter;
        }

        /// <summary>
        /// Use this method to abstract the default <see cref="ILogger"/> for logging messages.
        /// </summary>
        public ILogger SetLogger(ILogger logger)
        {
            if (logger == null)
            {
                throw new InvalidOperationException("Cannot install a null logger, please use 'NulLogger' instead.");
            }
            else if (this.LogWriter == null)
            {
                throw new InvalidOperationException("Please call SetLogWriter before calling SetLogger.");
            }

            ILogger prevLogger = null;
            for (var writer = this.LogWriter; writer != null; writer = writer.Next)
            {
                prevLogger = writer.Logger;
                writer.Logger = logger;
            }

            return prevLogger;
        }

        /// <summary>
        /// Raises the <see cref="OnFailure"/> event with the specified <see cref="Exception"/>.
        /// </summary>
        protected internal void RaiseOnFailureEvent(Exception exception)
        {
            if (this.Configuration.AttachDebugger && exception is MachineActionExceptionFilterException &&
                !((exception as MachineActionExceptionFilterException).InnerException is RuntimeException))
            {
                System.Diagnostics.Debugger.Break();
                this.Configuration.AttachDebugger = false;
            }

            this.OnFailure?.Invoke(exception);
        }

        /// <summary>
        /// Tries to handle the specified dropped <see cref="Event"/>.
        /// </summary>
        internal void TryHandleDroppedEvent(Event e, ActorId id)
        {
            this.OnEventDropped?.Invoke(e, id);
        }

        /// <summary>
        /// Throws an <see cref="AssertionFailureException"/> exception
        /// containing the specified exception.
        /// </summary>
        internal virtual void WrapAndThrowException(Exception exception, string s, params object[] args)
        {
            throw (exception is AssertionFailureException)
                ? exception
                : new AssertionFailureException(string.Format(CultureInfo.InvariantCulture, s, args), exception);
        }

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.ActorIdCounter = 0;
            }
        }

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
