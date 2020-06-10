// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Production.Tests.Actors
{
    internal class MockActorManager : IActorManager
    {
        internal enum Notification
        {
            EnqueueEvent = 0,
            RaiseEvent,
            WaitEvent,
            ReceiveEvent,
            ReceiveEventWithoutWaiting,
            DropEvent
        }

        private readonly TextWriter Logger;
        private readonly Action<Notification, Event, EventInfo> Notify;
        private readonly Type[] IgnoredEvents;
        private readonly Type[] DeferredEvents;
        private readonly bool IsDefaultHandlerInstalled;

        public bool IsEventHandlerRunning { get; set; }

        public Guid OperationGroupId { get; set; }

        internal MockActorManager(TextWriter logger, Action<Notification, Event, EventInfo> notify,
            Type[] ignoredEvents = null, Type[] deferredEvents = null, bool isDefaultHandlerInstalled = false)
        {
            this.Logger = logger;
            this.Notify = notify;
            this.IgnoredEvents = ignoredEvents ?? Array.Empty<Type>();
            this.DeferredEvents = deferredEvents ?? Array.Empty<Type>();
            this.IsDefaultHandlerInstalled = isDefaultHandlerInstalled;
            this.IsEventHandlerRunning = true;
        }

        public int GetCachedState() => 0;

        public bool IsEventIgnored(Event e, Guid opGroupId, EventInfo eventInfo) =>
            this.IgnoredEvents.Contains(e.GetType());

        public bool IsEventDeferred(Event e, Guid opGroupId, EventInfo eventInfo) =>
            this.DeferredEvents.Contains(e.GetType());

        public bool IsDefaultHandlerAvailable() => this.IsDefaultHandlerInstalled;

        public void OnEnqueueEvent(Event e, Guid opGroupId, EventInfo eventInfo)
        {
            this.Logger.WriteLine("Enqueued event of type '{0}'.", e.GetType().FullName);
            this.Notify(Notification.EnqueueEvent, e, eventInfo);
        }

        public void OnRaiseEvent(Event e, Guid opGroupId, EventInfo eventInfo)
        {
            this.Logger.WriteLine("Raised event of type '{0}'.", e.GetType().FullName);
            this.Notify(Notification.RaiseEvent, e, eventInfo);
        }

        public void OnWaitEvent(IEnumerable<Type> eventTypes)
        {
            foreach (var type in eventTypes)
            {
                this.Logger.WriteLine("Waits to receive event of type '{0}'.", type.FullName);
            }

            this.Notify(Notification.WaitEvent, null, null);
        }

        public void OnReceiveEvent(Event e, Guid opGroupId, EventInfo eventInfo)
        {
            if (opGroupId != Guid.Empty)
            {
                // Inherit the operation group id of the receive operation, if it is non-empty.
                this.OperationGroupId = opGroupId;
            }

            this.Logger.WriteLine("Received event of type '{0}'.", e.GetType().FullName);
            this.Notify(Notification.ReceiveEvent, e, eventInfo);
        }

        public void OnReceiveEventWithoutWaiting(Event e, Guid opGroupId, EventInfo eventInfo)
        {
            if (opGroupId != Guid.Empty)
            {
                // Inherit the operation group id of the receive operation, if it is non-empty.
                this.OperationGroupId = opGroupId;
            }

            this.Logger.WriteLine("Received event of type '{0}' without waiting.", e.GetType().FullName);
            this.Notify(Notification.ReceiveEventWithoutWaiting, e, eventInfo);
        }

        public void OnDropEvent(Event e, Guid opGroupId, EventInfo eventInfo)
        {
            this.Logger.WriteLine("Dropped event of type '{0}'.", e.GetType().FullName);
            this.Notify(Notification.DropEvent, e, eventInfo);
        }

        public void Assert(bool predicate, string s, object arg0)
        {
            if (!predicate)
            {
                throw new AssertionFailureException(string.Format(s, arg0));
            }
        }

        public void Assert(bool predicate, string s, object arg0, object arg1)
        {
            if (!predicate)
            {
                throw new AssertionFailureException(string.Format(s, arg0, arg1));
            }
        }

        public void Assert(bool predicate, string s, object arg0, object arg1, object arg2)
        {
            if (!predicate)
            {
                throw new AssertionFailureException(string.Format(s, arg0, arg1, arg2));
            }
        }

        public void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                throw new AssertionFailureException(string.Format(s, args));
            }
        }
    }
}
