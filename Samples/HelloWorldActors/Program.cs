// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;

namespace Microsoft.Coyote.Samples.HelloWorldActors
{
    public static class Program
    {
        public static void Main()
        {
            // Create a default configuration and a new Coyote runtime.
            var config = Configuration.Create();
            IActorRuntime runtime = RuntimeFactory.Create(config);
            Execute(runtime);

            runtime.OnFailure += OnRuntimeFailure;

            // Coyote actors run in separate Tasks, so this stops the program from terminating prematurely!
            Console.WriteLine("press ENTER to terminate...");
            Console.ReadLine();
        }

        private static void OnRuntimeFailure(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        /// <summary>
        /// In order to use Coyote test tool, you need a [Test] method. Such test methods can receive an
        /// IActorRuntime runtime as input.  During testing this will be a special type of runtime designed
        /// for testing.
        /// </summary>
        /// <param name="runtime">The runtime context</param>
        [Microsoft.Coyote.SystematicTesting.Test]
        public static void Execute(IActorRuntime runtime)
        {
            // In Coyote once an actor is created it lives forever until it is halted.
            runtime.CreateActor(typeof(TestActor));
        }

        /// <summary>
        /// This TestActor is designed to test our "Greeter" by sending it one or more RequestGreetingEvents.
        /// </summary>
        [OnEventDoAction(typeof(GreetingEvent), nameof(HandleGreeting))]
        private class TestActor : Actor
        {
            private ActorId GreeterId;
            private int Count;

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                // Create the Greeter and hold onto the returned ActorId.  The ActorId is not the
                // actual Greeter object instance, it is like a handle to the actor that is managed
                // by the Coyote actor runtime.
                this.GreeterId = this.CreateActor(typeof(Greeter));

                // Now request a random number of greetings.  The SendEvent call here queues up
                // work on the Greeter, but HandleGreeting will not be called until this method
                // is done.
                this.Count = 1 + this.RandomInteger(5);
                Console.WriteLine("Requesting {0} greeting{1}", this.Count, this.Count == 1 ? string.Empty : "s");
                for (int i = 0; i < this.Count; i++)
                {
                    this.SendEvent(this.GreeterId, new RequestGreetingEvent(this.Id));
                }

                return base.OnInitializeAsync(initialEvent);
            }

            private void HandleGreeting(Event e)
            {
                // this is perfectly thread safe, because all message handling in actors is
                // serialized within the Actor class.
                this.Count--;
                string greeting = ((GreetingEvent)e).Greeting;
                Console.WriteLine("Received greeting: {0}", greeting);
                this.Assert(this.Count >= 0, "Too many greetings returned!");
            }
        }
    }
}
