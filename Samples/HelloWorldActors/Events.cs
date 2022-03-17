// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Samples.HelloWorldActors
{
    /// <summary>
    /// This is a Coyote Event used to request a greeting.
    /// Events in Coyote are strongly typed and have their own lifetime outside of
    /// the scope of any particular call stack, because they can be queued in
    /// an Actor inbox.
    /// </summary>
    internal class RequestGreetingEvent : Event
    {
        public readonly ActorId Caller;

        public RequestGreetingEvent(ActorId caller)
        {
            this.Caller = caller;
        }
    }

    /// <summary>
    /// This is a Coyote Event returned in response to a RequestGreetingEvent.
    /// An event can contain any data you want.
    /// </summary>
    internal class GreetingEvent : Event
    {
        public readonly string Greeting;

        public GreetingEvent(string greeting)
        {
            this.Greeting = greeting;
        }
    }
}
