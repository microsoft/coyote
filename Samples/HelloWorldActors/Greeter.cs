// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Samples.HelloWorldActors
{
    /// <summary>
    /// This is a Coyote Actor that handles a RequestGreetingEvent and responds
    /// with a GreetingEvent.
    /// </summary>
    [OnEventDoAction(typeof(RequestGreetingEvent), nameof(HandleGreeting))]
    public class Greeter : Actor
    {
        /// <summary>
        /// This method is called when this actor receives a RequestGreetingEvent.
        /// </summary>
        /// <param name="e">The event should be of type RequestGreetingEvent.</param>
        private void HandleGreeting(Event e)
        {
            if (e is RequestGreetingEvent ge)
            {
                string greeting = this.RandomBoolean() ? "Hello World!" : "Good Morning";
                this.SendEvent(ge.Caller, new GreetingEvent(greeting));
                if (this.RandomInteger(10) is 0)
                {
                    // bug: a 1 in 10 chance of sending too many greetings.
                    this.SendEvent(ge.Caller, new GreetingEvent(greeting));
                }
            }
        }
    }
}
