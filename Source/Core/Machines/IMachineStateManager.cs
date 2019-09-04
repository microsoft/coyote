// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Interface for managing the state of a machine.
    /// </summary>
    internal interface IMachineStateManager
    {
        /// <summary>
        /// True if the event handler of the machine is running, else false.
        /// </summary>
        bool IsEventHandlerRunning { get; set; }

        /// <summary>
        /// Id used to identify subsequent operations performed by the machine.
        /// </summary>
        Guid OperationGroupId { get; set; }

        /// <summary>
        /// Returns the cached state of the machine.
        /// </summary>
        int GetCachedState();

        /// <summary>
        /// Checks if the specified event is ignored in the current machine state.
        /// </summary>
        bool IsEventIgnoredInCurrentState(Event e, Guid opGroupId, EventInfo eventInfo);

        /// <summary>
        /// Checks if the specified event is deferred in the current machine state.
        /// </summary>
        bool IsEventDeferredInCurrentState(Event e, Guid opGroupId, EventInfo eventInfo);

        /// <summary>
        /// Checks if a default handler is installed in the current machine state.
        /// </summary>
        bool IsDefaultHandlerInstalledInCurrentState();

        /// <summary>
        /// Notifies the machine that an event has been enqueued.
        /// </summary>
        void OnEnqueueEvent(Event e, Guid opGroupId, EventInfo eventInfo);

        /// <summary>
        /// Notifies the machine that an event has been raised.
        /// </summary>
        void OnRaiseEvent(Event e, Guid opGroupId, EventInfo eventInfo);

        /// <summary>
        /// Notifies the machine that it is waiting to receive an event of one of the specified types.
        /// </summary>
        void OnWaitEvent(IEnumerable<Type> eventTypes);

        /// <summary>
        /// Notifies the machine that an event it was waiting to receive has been enqueued.
        /// </summary>
        void OnReceiveEvent(Event e, Guid opGroupId, EventInfo eventInfo);

        /// <summary>
        /// Notifies the machine that an event it was waiting to receive was already in the
        /// event queue when the machine invoked the receive statement.
        /// </summary>
        void OnReceiveEventWithoutWaiting(Event e, Guid opGroupId, EventInfo eventInfo);

        /// <summary>
        /// Notifies the machine that an event has been dropped.
        /// </summary>
        void OnDropEvent(Event e, Guid opGroupId, EventInfo eventInfo);

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        void Assert(bool predicate, string s, object arg0);

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        void Assert(bool predicate, string s, object arg0, object arg1);

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        void Assert(bool predicate, string s, object arg0, object arg1, object arg2);

        /// <summary>
        /// Asserts if the specified condition holds.
        /// </summary>
        void Assert(bool predicate, string s, params object[] args);
    }
}
