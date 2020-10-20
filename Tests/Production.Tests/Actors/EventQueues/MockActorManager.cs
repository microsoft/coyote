// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.IO;

namespace Microsoft.Coyote.Production.Tests.Actors
{
    internal class MockActorManager : ActorManager
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

        private readonly Action<Notification, Event, EventInfo> Notify;
        private readonly Type[] IgnoredEvents;
        private readonly Type[] DeferredEvents;
        private readonly bool IsDefaultHandlerInstalled;

        public override ILogger Logger { get; set; }

        internal MockActorManager(ILogger logger, Action<Notification, Event, EventInfo> notify,
            Type[] ignoredEvents = null, Type[] deferredEvents = null, bool isDefaultHandlerInstalled = false)
            : base(null, null, null)
        {
            this.Logger = logger;
            this.Notify = notify;
            this.IgnoredEvents = ignoredEvents ?? Array.Empty<Type>();
            this.DeferredEvents = deferredEvents ?? Array.Empty<Type>();
            this.IsDefaultHandlerInstalled = isDefaultHandlerInstalled;
            this.IsEventHandlerRunning = true;
        }

        internal override bool IsEventIgnored(Event e) => this.IgnoredEvents.Contains(e.GetType());

        internal override bool IsEventDeferred(Event e) => this.DeferredEvents.Contains(e.GetType());

        internal override bool IsDefaultHandlerAvailable() => this.IsDefaultHandlerInstalled;

        internal override void OnEnqueueEvent(Event e, EventGroup group, EventInfo eventInfo)
        {
            this.Logger.WriteLine("Enqueued event of type '{0}'.", e.GetType().FullName);
            this.Notify(Notification.EnqueueEvent, e, eventInfo);
        }

        internal override void OnRaiseEvent(Event e, EventGroup group, EventInfo eventInfo)
        {
            this.Logger.WriteLine("Raised event of type '{0}'.", e.GetType().FullName);
            this.Notify(Notification.RaiseEvent, e, eventInfo);
        }

        internal override void OnWaitEvent(IEnumerable<Type> eventTypes)
        {
            foreach (var type in eventTypes)
            {
                this.Logger.WriteLine("Waits to receive event of type '{0}'.", type.FullName);
            }

            this.Notify(Notification.WaitEvent, null, null);
        }

        internal override void OnReceiveEvent(Event e, EventGroup group, EventInfo eventInfo)
        {
            if (group != null)
            {
                // Inherit the event group of the receive operation, if it is non-empty.
                this.CurrentEventGroup = group;
            }

            this.Logger.WriteLine("Received event of type '{0}'.", e.GetType().FullName);
            this.Notify(Notification.ReceiveEvent, e, eventInfo);
        }

        internal override void OnReceiveEventWithoutWaiting(Event e, EventGroup group, EventInfo eventInfo)
        {
            if (group != null)
            {
                // Inherit the event group of the receive operation, if it is non-empty.
                this.CurrentEventGroup = group;
            }

            this.Logger.WriteLine("Received event of type '{0}' without waiting.", e.GetType().FullName);
            this.Notify(Notification.ReceiveEventWithoutWaiting, e, eventInfo);
        }

        internal override void OnDropEvent(Event e, EventGroup group, EventInfo eventInfo)
        {
            this.Logger.WriteLine("Dropped event of type '{0}'.", e.GetType().FullName);
            this.Notify(Notification.DropEvent, e, eventInfo);
        }
    }
}
