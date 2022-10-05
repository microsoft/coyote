// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Interface that exposes runtime methods for creating and executing actors.
    /// </summary>
    /// <remarks>
    /// See <see href="/coyote/concepts/actors/overview">Programming
    /// model: asynchronous actors</see> for more information.
    /// </remarks>
    public interface IActorRuntime : ICoyoteRuntime
    {
        /// <summary>
        /// Callback that is fired when an actor has halted.
        /// </summary>
        event OnActorHaltedHandler OnActorHalted;

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
        /// optional <see cref="Event"/>. This event is given to the <see cref="Actor.InitializeAsync"/>
        /// method on the new actor.
        /// </summary>
        /// <param name="type">Type of the actor.</param>
        /// <param name="initialEvent">Optional event used during initialization.</param>
        /// <param name="eventGroup">An optional event group associated with the new Actor.</param>
        /// <returns>The result is the actor id.</returns>
        ActorId CreateActor(Type type, Event initialEvent = null, EventGroup eventGroup = null);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, and with the
        /// specified optional <see cref="Event"/>. This event is given to the <see cref="Actor.InitializeAsync"/>
        /// method on the new actor.
        /// </summary>
        /// <param name="type">Type of the actor.</param>
        /// <param name="name">Optional name used for logging.</param>
        /// <param name="initialEvent">Optional event used during initialization.</param>
        /// <param name="eventGroup">An optional event group associated with the new Actor.</param>
        /// <returns>The result is the actor id.</returns>
        ActorId CreateActor(Type type, string name, Event initialEvent = null, EventGroup eventGroup = null);

        /// <summary>
        /// Creates a new actor of the specified type, using the specified <see cref="ActorId"/>.
        /// This method optionally passes an <see cref="Event"/>. This event is given to the
        /// InitializeAsync method on the new actor.
        /// </summary>
        /// <param name="id">Unbound actor id.</param>
        /// <param name="type">Type of the actor.</param>
        /// <param name="initialEvent">Optional event used during initialization.</param>
        /// <param name="eventGroup">An optional event group associated with the new Actor.</param>
        /// <returns>The result is the actor id.</returns>
        ActorId CreateActor(ActorId id, Type type, Event initialEvent = null, EventGroup eventGroup = null);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and with the specified
        /// optional <see cref="Event"/>. This event is given to the <see cref="Actor.InitializeAsync"/>
        /// method on the new actor. The method returns only when the actor is initialized and
        /// the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the actor.</param>
        /// <param name="initialEvent">Optional event used during initialization.</param>
        /// <param name="eventGroup">An optional event group associated with the new Actor.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the actor id.</returns>
        [Obsolete("Use AwaitableEventGroup<T> instead to coordinate completion of CreateActor operations")]
        Task<ActorId> CreateActorAndExecuteAsync(Type type, Event initialEvent = null, EventGroup eventGroup = null);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, and with the
        /// specified optional <see cref="Event"/>. This event is given to the <see cref="Actor.InitializeAsync"/>
        /// method on the new actor. The method returns only when the actor is
        /// initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        /// <param name="type">Type of the actor.</param>
        /// <param name="name">Optional name used for logging.</param>
        /// <param name="initialEvent">Optional event used during initialization.</param>
        /// <param name="eventGroup">An optional event group associated with the new Actor.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the actor id.</returns>
        [Obsolete("Use AwaitableEventGroup<T> instead to coordinate completion of CreateActor operations")]
        Task<ActorId> CreateActorAndExecuteAsync(Type type, string name, Event initialEvent = null,
            EventGroup eventGroup = null);

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/>, using the specified unbound
        /// actor id, and passes the specified optional <see cref="Event"/>. This event is given to
        /// the InitializeAsync method on the new actor. The method returns only when
        /// the actor is initialized and the <see cref="Event"/> (if any)
        /// is handled.
        /// </summary>
        /// <param name="id">Unbound actor id.</param>
        /// <param name="type">Type of the actor.</param>
        /// <param name="initialEvent">Optional event used during initialization.</param>
        /// <param name="eventGroup">An optional event group associated with the new Actor.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the actor id.</returns>
        [Obsolete("Use AwaitableEventGroup<T> instead to coordinate completion of CreateActor operations")]
        Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, Event initialEvent = null,
            EventGroup eventGroup = null);

        /// <summary>
        /// Sends an asynchronous <see cref="Event"/> to an actor.
        /// </summary>
        /// <param name="targetId">The id of the target.</param>
        /// <param name="e">The event to send.</param>
        /// <param name="eventGroup">An optional event group associated with this Actor.</param>
        /// <param name="options">Optional configuration of a send operation.</param>
        void SendEvent(ActorId targetId, Event e, EventGroup eventGroup = null, SendOptions options = null);

        /// <summary>
        /// Sends an <see cref="Event"/> to an actor. Returns immediately if the target was already
        /// running. Otherwise blocks until the target handles the event and reaches quiescense.
        /// </summary>
        /// <param name="targetId">The id of the target.</param>
        /// <param name="e">The event to send.</param>
        /// <param name="eventGroup">An optional event group associated with the new Actor.</param>
        /// <param name="options">Optional configuration of a send operation.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is true if
        /// the event was handled, false if the event was only enqueued.</returns>
        [Obsolete("Use AwaitableEventGroup<T> instead to coordinate completion of SendEvent operations")]
        Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event e, EventGroup eventGroup = null, SendOptions options = null);

        /// <summary>
        /// Returns the current <see cref="EventGroup"/> of the actor with the specified id. Returns null
        /// if the id is not set, or if the <see cref="ActorId"/> is not associated with this runtime. During
        /// testing, the runtime asserts that the specified actor is currently executing.
        /// </summary>
        /// <param name="currentActorId">The id of the currently executing actor.</param>
        /// <returns>The current EventGroup or null.</returns>
        EventGroup GetCurrentEventGroup(ActorId currentActorId);

        /// <summary>
        /// Returns the execution status of the actor with the specified <see cref="ActorId"/>.
        /// </summary>
        /// <param name="id">The id of the actor.</param>
        /// <returns>The execution status.</returns>
        /// <remarks>
        /// This method is not thread-safe.
        /// </remarks>
        ActorExecutionStatus GetActorExecutionStatus(ActorId id);

        /// <summary>
        /// Returns the current count of active actors managed by this runtime.
        /// </summary>
        /// <returns>The current count of active actors.</returns>
        /// <remarks>
        /// This method is not thread-safe.
        /// </remarks>
        int GetCurrentActorCount();

        /// <summary>
        /// The old way of setting the <see cref="ICoyoteRuntime.Logger"/> property.
        /// </summary>
        /// <remarks>
        /// The new way is to just set the Logger property to an <see cref="ILogger"/> object.
        /// This method is only here for compatibility and has a minor perf impact as it has to
        /// wrap the writer in an object that implements the <see cref="ILogger"/> interface.
        /// </remarks>
        /// <param name="writer">The writer to use for logging.</param>
        /// <returns>The previously installed logger.</returns>
        [Obsolete("Please set the Logger property directly instead of calling this method.")]
        TextWriter SetLogger(TextWriter writer);
    }
}
