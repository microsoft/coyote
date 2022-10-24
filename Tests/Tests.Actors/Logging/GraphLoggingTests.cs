// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors.Coverage;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.Tests.Logging
{
    public class GraphLoggingTests : BaseActorLoggingTests
    {
        public GraphLoggingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestGraphLogger()
        {
            Configuration config = this.GetConfiguration().WithVerbosityEnabled(VerbosityLevel.Info);
            this.Test(async runtime =>
            {
                runtime.RegisterMonitor<TestMonitor>();
                runtime.Monitor<TestMonitor>(new TestMonitor.SetupEvent());

                var graphBuilder = new ActorRuntimeLogGraphBuilder(false, false);
                runtime.RegisterLog(graphBuilder);

                runtime.CreateActor(typeof(M));
                await (runtime as IRuntimeExtension).WaitUntilQuiescenceAsync();

                string result = graphBuilder.Graph.ToString().RemoveNonDeterministicValues();
                string expected = StringExtensions.FormatLines(
                    "<DirectedGraph xmlns='http://schemas.microsoft.com/vs/2009/dgml'>",
                    "  <Nodes>",
                    $"    <Node Id='{typeof(M).FullName}(0)' Category='Actor' Group='Expanded'/>",
                    $"    <Node Id='{typeof(M).FullName}(0).M(0)' Label='M(0)'/>",
                    $"    <Node Id='{typeof(N).FullName}(1)' Category='StateMachine' Group='Expanded'/>",
                    $"    <Node Id='{typeof(N).FullName}(1).Act' Label='Act'/>",
                    $"    <Node Id='{typeof(N).FullName}(1).Init' Label='Init'/>",
                    $"    <Node Id='{typeof(TestMonitor).FullName}' Group='Expanded'/>",
                    $"    <Node Id='{typeof(TestMonitor).FullName}.Init' Label='Init'/>",
                    "  </Nodes>",
                    "  <Links>",
                    $"    <Link Source='{typeof(M).FullName}(0)' Target='{typeof(M).FullName}(0).M(0)' Category='Contains'/>",
                    $"    <Link Source='{typeof(M).FullName}(0)' Target='{typeof(N).FullName}(1)' Label='CreateActor' Index='0' EventId='CreateActor'/>",
                    $"    <Link Source='{typeof(M).FullName}(0).M(0)' Target='{typeof(N).FullName}(1).Init' Label='E' Index='0' EventId='{typeof(E).FullName}' HandledBy='Init'/>",
                    $"    <Link Source='{typeof(M).FullName}(0).M(0)' Target='{typeof(TestMonitor).FullName}.Init' Label='CompletedEvent' Index='0' EventId='{typeof(TestMonitor.CompletedEvent).FullName}'/>",
                    $"    <Link Source='{typeof(N).FullName}(1)' Target='{typeof(N).FullName}(1).Act' Category='Contains'/>",
                    $"    <Link Source='{typeof(N).FullName}(1)' Target='{typeof(N).FullName}(1).Init' Category='Contains'/>",
                    $"    <Link Source='{typeof(N).FullName}(1).Act' Target='{typeof(M).FullName}(0).M(0)' Label='E' Index='0' EventId='{typeof(E).FullName}'/>",
                    $"    <Link Source='{typeof(N).FullName}(1).Init' Target='{typeof(N).FullName}(1).Act' Label='E' Index='0' EventId='{typeof(E).FullName}' HandledBy='Init'/>",
                    $"    <Link Source='{typeof(TestMonitor).FullName}' Target='{typeof(TestMonitor).FullName}.Init' Category='Contains'/>",
                    $"    <Link Source='{typeof(TestMonitor).FullName}.Init' Target='{typeof(TestMonitor).FullName}.Init' Label='CompletedEvent' Index='0' EventId='{typeof(TestMonitor.CompletedEvent).FullName}'/>",
                    "  </Links>",
                    "</DirectedGraph>");
                expected = expected.RemoveNonDeterministicValues();

                this.TestOutput.WriteLine(result);
                Assert.Equal(expected, result);
            }, config);
        }

        internal class PingEvent : Event
        {
            public readonly ActorId Caller;

            public PingEvent(ActorId caller)
            {
                this.Caller = caller;
            }
        }

        internal class PongEvent : Event
        {
        }

        internal class ClientSetupEvent : Event
        {
            public readonly ActorId ServerId;

            public ClientSetupEvent(ActorId server)
            {
                this.ServerId = server;
            }
        }

        [OnEventDoAction(typeof(PongEvent), nameof(HandlePong))]
        internal class Client : Actor
        {
            public ActorId ServerId;

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.Logger.WriteLine("{0} initializing", this.Id);
                this.ServerId = ((ClientSetupEvent)initialEvent).ServerId;
                this.Logger.WriteLine("{0} sending ping event to server", this.Id);
                this.SendEvent(this.ServerId, new PingEvent(this.Id));
                return base.OnInitializeAsync(initialEvent);
            }

            private void HandlePong()
            {
                this.Logger.WriteLine("{0} received pong event", this.Id);
            }
        }

        internal class Server : StateMachine
        {
            private int Count;

            [Start]
            [OnEventGotoState(typeof(PingEvent), typeof(Pong))]
            private class Init : State
            {
            }

            [OnEntry(nameof(HandlePing))]
            [OnEventDoAction(typeof(PingEvent), nameof(HandlePing))]
            private class Pong : State
            {
            }

            private void HandlePing(Event e)
            {
                this.Count++;
                PingEvent ping = (PingEvent)e;
                this.Logger.WriteLine("Server handling ping");
                this.Logger.WriteLine("Server sending pong back to caller");
                this.SendEvent(ping.Caller, new PongEvent());

                if (this.Count is 3)
                {
                    this.RaiseGotoStateEvent<Complete>();
                }
            }

            [OnEntry(nameof(HandleComplete))]
            private class Complete : State
            {
            }

            private void HandleComplete()
            {
                this.Logger.WriteLine("Test Complete");
                this.Monitor<TestMonitor>(new TestMonitor.CompletedEvent());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestGraphLoggerInstances()
        {
            Configuration config = this.GetConfiguration().WithVerbosityEnabled(VerbosityLevel.Info);
            this.Test(async runtime =>
            {
                runtime.RegisterMonitor<TestMonitor>();
                runtime.Monitor<TestMonitor>(new TestMonitor.SetupEvent());

                var graphBuilder = new ActorRuntimeLogGraphBuilder(false, false);
                runtime.RegisterLog(graphBuilder);

                ActorId serverId = runtime.CreateActor(typeof(Server));
                runtime.CreateActor(typeof(Client), new ClientSetupEvent(serverId));
                runtime.CreateActor(typeof(Client), new ClientSetupEvent(serverId));
                runtime.CreateActor(typeof(Client), new ClientSetupEvent(serverId));

                await (runtime as IRuntimeExtension).WaitUntilQuiescenceAsync();

                string result = graphBuilder.Graph.ToString().RemoveInstanceIds();
                this.TestOutput.WriteLine(result);

                Assert.Contains($"<Node Id='{typeof(TestMonitor).FullName}.Init' Label='Init'/>", result);
                Assert.Contains($"<Node Id='{typeof(Client).FullName}().Client()' Label='Client()'/>", result);
                Assert.Contains($"<Node Id='{typeof(Server).FullName}().Complete' Label='Complete'/>", result);
            }, config);
        }

        [Fact(Timeout = 5000)]
        public void TestGraphLoggerCollapsed()
        {
            Configuration config = this.GetConfiguration().WithVerbosityEnabled(VerbosityLevel.Info);
            this.Test(async runtime =>
            {
                runtime.RegisterMonitor<TestMonitor>();
                runtime.Monitor<TestMonitor>(new TestMonitor.SetupEvent());

                var graphBuilder = new ActorRuntimeLogGraphBuilder(false, true);
                runtime.RegisterLog(graphBuilder);

                ActorId serverId = runtime.CreateActor(typeof(Server));
                runtime.CreateActor(typeof(Client), new ClientSetupEvent(serverId));
                runtime.CreateActor(typeof(Client), new ClientSetupEvent(serverId));
                runtime.CreateActor(typeof(Client), new ClientSetupEvent(serverId));

                await (runtime as IRuntimeExtension).WaitUntilQuiescenceAsync();

                string result = graphBuilder.Graph.ToString().RemoveInstanceIds();
                this.TestOutput.WriteLine(result);

                Assert.Contains($"<Node Id='{typeof(Client).FullName}.Client' Label='Client'/>", result);
                Assert.Contains($"<Node Id='{typeof(Server).FullName}.Complete' Label='Complete'/>", result);
            }, config);
        }
    }
}
