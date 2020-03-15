// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Production.Tests.Actors
{
    public class ActorInheritanceTests : BaseProductionTest
    {
        public ActorInheritanceTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        private class E3 : Event
        {
        }

        private class E4 : Event
        {
        }

        private class CompletedEvent : Event
        {
        }

        private class ConfigEvent : Event
        {
            public StringWriter Log = new StringWriter();
            public TaskCompletionSource<bool> Completed = new TaskCompletionSource<bool>();
        }

        [OnEventDoAction(typeof(E1), nameof(HandleE1))]
        [OnEventDoAction(typeof(E3), nameof(HandleE3))]
        [OnEventDoAction(typeof(CompletedEvent), nameof(HandleCompleted))]
        private class BaseActor : Actor
        {
            public StringWriter Log;
            private TaskCompletionSource<bool> Completed;

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                if (initialEvent is ConfigEvent config)
                {
                    this.Log = config.Log;
                    this.Completed = config.Completed;
                }

                return base.OnInitializeAsync(initialEvent);
            }

            private void HandleE1()
            {
                this.Log.WriteLine("BaseActor handling E1");
            }

            private void HandleE3()
            {
                this.Log.WriteLine("BaseActor handling E3");
            }

            protected void HandleE4()
            {
                this.Log.WriteLine("Inherited handling of E4");
            }

            private void HandleCompleted()
            {
                this.Completed.SetResult(true);
            }
        }

        [OnEventDoAction(typeof(E1), nameof(HandleE1))]
        [OnEventDoAction(typeof(E2), nameof(HandleE2))]
        [OnEventDoAction(typeof(E4), nameof(HandleE4))]
        private class ActorSubclass : BaseActor
        {
            private void HandleE1()
            {
                this.Log.WriteLine("ActorSubclass handling E1");
            }

            private void HandleE2()
            {
                this.Log.WriteLine("ActorSubclass handling E2");
            }
        }

        private static string NormalizeNewLines(string text)
        {
            return text.Replace("\r\n", "\n");
        }

        [Fact(Timeout = 5000)]
        public async Task TestActorInheritance()
        {
            var runtime = ActorRuntimeFactory.Create();
            var config = new ConfigEvent();
            var actor = runtime.CreateActor(typeof(ActorSubclass), config);
            runtime.SendEvent(actor, new E1());
            runtime.SendEvent(actor, new E2());
            runtime.SendEvent(actor, new E3());
            runtime.SendEvent(actor, new E4());
            runtime.SendEvent(actor, new CompletedEvent());
            await config.Completed.Task;

            var actual = NormalizeNewLines(config.Log.ToString());
            var expected = NormalizeNewLines(@"ActorSubclass handling E1
ActorSubclass handling E2
BaseActor handling E3
Inherited handling of E4
");
            Assert.Equal(expected, actual);
        }
    }
}
