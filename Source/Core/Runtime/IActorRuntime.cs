// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Interface that exposes runtime methods for creating and executing actors.
    /// </summary>
    /// <remarks>
    /// See <see href="/coyote/learn/programming-models/actors/overview">Programming model: asynchronous actors</see> for more information.
    /// </remarks>
    public interface IActorRuntime : IDisposable
    {
        /// <summary>
        /// Used to log messages. Use <see cref="SetLogger"/>
        /// to replace the logger with a custom one.
        /// </summary>
        /// <remarks>
        /// See <see href="/coyote/learn/advanced/logging" >Logging</see> for more information.
        /// </remarks>
        TextWriter Logger { get; }

        /// <summary>
        /// Callback that is fired when the runtime throws an exception which includes failed assertions.
        /// </summary>
        event OnFailureHandler OnFailure;

        /// <summary>
        /// Callback that is fired when an event is dropped.
        /// </summary>
        event OnEventDroppedHandler OnEventDropped;

        /// <summary>
        /// Creates a fresh actor id that has not yet been bound to any actor.
        /// </summary>
        /// <param name="type">Type of the actor.</param>
        /// <param name="name">Optional name used for logging.</param>
        /// <returns>The result is the actor id.</returns>
        ActorId CreateActorId(Type type, string name = null);

        /// <summary>
        /// Creates a actor id that is uniquely tied to the specified unique name. The
        /// returned actor id can either be a fresh id (not yet bound to any actor), or
        /// it can be bound to a previously created actor. In the second case, this actor
        /// id can be directly used to communicate with the corresponding actor.
        /// </summary>
        /// <param name="type">Type of the actor.</param>
        /// <param name="name">Unique name used to create or get the actor id.</param>
        /// <returns>The result is the actor id.</returns>
        ActorId CreateActorIdFromName(Type type, string name);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and with the specified
        /// optional <see cref="Event"/>. This event can only be used to access its payload,
        /// and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the actor.</param>
        /// <param name="initialEvent">Optional event used during initialization.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>The result is the actor id.</returns>
        ActorId CreateActor(Type type, Event initialEvent = null, Guid opGroupId = default);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, and with the
        /// specified optional <see cref="Event"/>. This event can only be used to access
        /// its payload, and cannot be handled.
        /// </summary>
        /// <param name="type">Type of the actor.</param>
        /// <param name="name">Optional name used for logging.</param>
        /// <param name="initialEvent">Optional event used during initialization.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>The result is the actor id.</returns>
        ActorId CreateActor(Type type, string name, Event initialEvent = null, Guid opGroupId = default);

        /// <summary>
        /// Creates a new actor of the specified type, using the specified <see cref="ActorId"/>.
        /// This method optionally passes an <see cref="Event"/> to the new actor, which can only
        /// be used to access its payload, and cannot be handled.
        /// </summary>
        /// <param name="id">Unbound actor id.</param>
        /// <param name="type">Type of the actor.</param>
        /// <param name="initialEvent">Optional event used during initialization.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>The result is the actor id.</returns>
        ActorId CreateActor(ActorId id, Type type, Event initialEvent = null, Guid opGroupId = default);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and with the specified
        /// optional <see cref="Event"/>. This event can only be used to access its payload,
        /// and cannot be handled. The method returns only when the actor is initialized and
        /// the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the actor.</param>
        /// <param name="initialEvent">Optional event used during initialization.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the actor id.</returns>
        Task<ActorId> CreateActorAndExecuteAsync(Type type, Event initialEvent = null, Guid opGroupId = default);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, and with the
        /// specified optional <see cref="Event"/>. This event can only be used to access
        /// its payload, and cannot be handled. The method returns only when the actor is
        /// initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the actor.</param>
        /// <param name="name">Optional name used for logging.</param>
        /// <param name="initialEvent">Optional event used during initialization.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the actor id.</returns>
        Task<ActorId> CreateActorAndExecuteAsync(Type type, string name, Event initialEvent = null,
            Guid opGroupId = default);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/>, using the specified unbound
        /// actor id, and passes the specified optional <see cref="Event"/>. This event can only
        /// be used to access its payload, and cannot be handled. The method returns only when
        /// the actor is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        /// <param name="id">Unbound actor id.</param>
        /// <param name="type">Type of the actor.</param>
        /// <param name="initialEvent">Optional event used during initialization.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the actor id.</returns>
        Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, Event initialEvent = null,
            Guid opGroupId = default);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to an actor.
        /// </summary>
        /// <param name="targetId">The id of the target.</param>
        /// <param name="e">The event to send.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <param name="options">Optional configuration of a send operation.</param>
        void SendEvent(ActorId targetId, Event e, Guid opGroupId = default, SendOptions options = null);

        /// <summary>
        /// Sends an <see cref="Event"/> to an actor. Returns immediately if the target was already
        /// running. Otherwise blocks until the target handles the event and reaches quiescense.
        /// </summary>
        /// <param name="targetId">The id of the target.</param>
        /// <param name="e">The event to send.</param>
        /// <param name="opGroupId">Optional id that can be used to identify this operation.</param>
        /// <param name="options">Optional configuration of a send operation.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is true if
        /// the event was handled, false if the event was only enqueued.</returns>
        Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event e, Guid opGroupId = default, SendOptions options = null);

        /// <summary>
        /// Registers a new specification monitor of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">Type of the monitor.</param>
        void RegisterMonitor(Type type);

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        /// <typeparam name="T">Type of the monitor.</typeparam>
        /// <param name="e">Event</param>
        void InvokeMonitor<T>(Event e);

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        /// <param name="type">Type of the monitor.</param>
        /// <param name="e">Event</param>
        void InvokeMonitor(Type type, Event e);

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing.
        /// </summary>
        /// <returns>The nondeterministic boolean choice.</returns>
        bool Random();

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing. The value is used to generate a number
        /// in the range [0..maxValue), where 0 triggers true.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <returns>The nondeterministic boolean choice.</returns>
        bool Random(int maxValue);

        /// <summary>
        /// Returns a nondeterministic integer choice, that can be
        /// controlled during analysis or testing. The value is used
        /// to generate an integer in the range [0..maxValue).
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <returns>The nondeterministic integer choice.</returns>
        int RandomInteger(int maxValue);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        /// <param name="predicate">The predicate to check.</param>
        void Assert(bool predicate);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        /// <param name="predicate">The predicate to check.</param>
        /// <param name="s">The message to print if the assertion fails.</param>
        /// <param name="arg0">The first argument.</param>
        void Assert(bool predicate, string s, object arg0);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        /// <param name="predicate">The predicate to check.</param>
        /// <param name="s">The message to print if the assertion fails.</param>
        /// <param name="arg0">The first argument.</param>
        /// <param name="arg1">The second argument.</param>
        void Assert(bool predicate, string s, object arg0, object arg1);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        /// <param name="predicate">The predicate to check.</param>
        /// <param name="s">The message to print if the assertion fails.</param>
        /// <param name="arg0">The first argument.</param>
        /// <param name="arg1">The second argument.</param>
        /// <param name="arg2">The third argument.</param>
        void Assert(bool predicate, string s, object arg0, object arg1, object arg2);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        /// <param name="predicate">The predicate to check.</param>
        /// <param name="s">The message to print if the assertion fails.</param>
        /// <param name="args">The message arguments.</param>
        void Assert(bool predicate, string s, params object[] args);

        /// <summary>
        /// Returns the operation group id of the actor with the specified id. Returns <see cref="Guid.Empty"/>
        /// if the id is not set, or if the <see cref="ActorId"/> is not associated with this runtime. During
        /// testing, the runtime asserts that the specified actor is currently executing.
        /// </summary>
        /// <param name="currentActorId">The id of the currently executing actor.</param>
        /// <returns>The unique identifier.</returns>
        Guid GetCurrentOperationGroupId(ActorId currentActorId);

        /// <summary>
        /// Use this method to override the default <see cref="TextWriter"/> for logging messages.
        /// </summary>
        /// <param name="logger">The logger to install.</param>
        /// <returns>The previously installed logger.</returns>
        TextWriter SetLogger(TextWriter logger);

        /// <summary>
        /// Use this method to register an <see cref="IActorRuntimeLog"/>.
        /// </summary>
        /// <param name="log">The log writer to register.</param>
        void RegisterLog(IActorRuntimeLog log);

        /// <summary>
        /// Use this method to unregister a previously registered <see cref="IActorRuntimeLog"/>.
        /// </summary>
        /// <param name="log">The previously registered log writer to unregister.</param>
        void RemoveLog(IActorRuntimeLog log);

        /// <summary>
        /// Terminates the runtime and notifies each active actor to halt execution.
        /// </summary>
        void Stop();
    }
}
