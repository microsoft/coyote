// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Coverage;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common.IO;
using Microsoft.Coyote.Tests.Common.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Production.Tests.Actors
{
    public class CustomActorRuntimeLogTests : BaseProductionTest
    {
        public CustomActorRuntimeLogTests(ITestOutputHelper output)
            : base(output)
        {
        }

        internal class SetupEvent : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public SetupEvent(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        internal class CompletedEvent : Event
        {
        }

        internal class TestMonitor : Monitor
        {
            private TaskCompletionSource<bool> Completed;

            [Start]
            [Cold]
            [OnEventDoAction(typeof(SetupEvent), nameof(OnSetup))]
            [OnEventDoAction(typeof(CompletedEvent), nameof(OnCompleted))]
            private class Init : State
            {
            }

            private void OnSetup(Event e)
            {
                this.Completed = ((SetupEvent)e).Tcs;
            }

            private void OnCompleted()
            {
                this.Completed.TrySetResult(true);
            }
        }

        internal class E : Event
        {
            public ActorId Id;

            public E(ActorId id)
            {
                this.Id = id;
            }
        }

        [OnEventDoAction(typeof(E), nameof(Act))]
        internal class M : Actor
        {
            protected override async Task OnInitializeAsync(Event e)
            {
                await base.OnInitializeAsync(e);
                var tcs = new TaskCompletionSource<bool>();
                var n = this.CreateActor(typeof(N), new SetupEvent(tcs));
                await tcs.Task;
                this.SendEvent(n, new E(this.Id));
            }

            private void Act()
            {
                this.Monitor<TestMonitor>(new CompletedEvent());
            }
        }

        internal class S : Monitor
        {
            [Start]
            [Hot]
            [OnEventDoAction(typeof(E), nameof(OnE))]
            private class Init : State
            {
            }

            [Cold]
            private class Done : State
            {
            }

            private void OnE() => this.RaiseGotoStateEvent<Done>();
        }

        internal class N : StateMachine
        {
            [Start]
            [OnEntry(nameof(OnInitEntry))]
            [OnEventGotoState(typeof(E), typeof(Act))]
            private class Init : State
            {
            }

            private void OnInitEntry(Event e)
            {
                TaskCompletionSource<bool> tcs = ((SetupEvent)e).Tcs;
                tcs.SetResult(true);
            }

            [OnEntry(nameof(ActOnEntry))]
            private class Act : State
            {
            }

            private void ActOnEntry(Event e)
            {
                this.Monitor<S>(e);
                ActorId m = (e as E).Id;
                this.SendEvent(m, new E(this.Id));
            }
        }

        private static IActorRuntime CreateTestRuntime(Configuration config, TaskCompletionSource<bool> tcs, TextWriter logger = null)
        {
            config.IsMonitoringEnabledInInProduction = true;
            IActorRuntime runtime = RuntimeFactory.Create(config);
            runtime.RegisterMonitor<TestMonitor>();
            runtime.Monitor<TestMonitor>(new SetupEvent(tcs));
            if (logger != null)
            {
                runtime.SetLogger(logger);
            }

            return runtime;
        }

        [Fact(Timeout = 5000)]
        public async Task TestCustomLogger()
        {
            CustomLogger logger = new CustomLogger();
            Configuration config = Configuration.Create().WithVerbosityEnabled();

            var tcs = new TaskCompletionSource<bool>();
            IActorRuntime runtime = CreateTestRuntime(config, tcs, logger);

            runtime.CreateActor(typeof(M));

            await WaitAsync(tcs.Task);
            await Task.Delay(200);

            string expected = @"<CreateLog> M() was created by task ''.
<CreateLog> N() was created by M().
<StateLog> N() enters state 'Init'.
<ActionLog> N() invoked action 'OnInitEntry' in state 'Init'.
<SendLog> M() in state '' sent event 'E' to N().
<EnqueueLog> N() enqueued event 'E'.
<DequeueLog> N() dequeued event 'E' in state 'Init'.
<GotoLog> N() is transitioning from state 'Init' to state 'N.Act'.
<StateLog> N() exits state 'Init'.
<StateLog> N() enters state 'Act'.
<ActionLog> N() invoked action 'ActOnEntry' in state 'Act'.
<SendLog> N() in state 'Act' sent event 'E' to M().
<EnqueueLog> M() enqueued event 'E'.
<DequeueLog> M() dequeued event 'E' in state ''.
<ActionLog> M() invoked action 'Act'.
<MonitorLog> TestMonitor is processing event 'CompletedEvent' in state 'Init'.
<MonitorLog> TestMonitor executed action 'Init[]' in state 'OnCompleted'.
";

            string actual = RemoveNonDeterministicValuesFromReport(logger.ToString());
            actual = SortLines(actual); // threading makes this non-deterministic otherwise.
            expected = SortLines(expected);
            Assert.Equal(expected, actual);

            logger.Dispose();
        }

        [Fact(Timeout = 5000)]
        public async Task TestGraphLogger()
        {
            CustomLogger logger = new CustomLogger();
            Configuration config = Configuration.Create().WithVerbosityEnabled();

            var graphBuilder = new ActorRuntimeLogGraphBuilder(false);
            var tcs = new TaskCompletionSource<bool>();
            IActorRuntime runtime = CreateTestRuntime(config, tcs, logger);
            runtime.RegisterLog(graphBuilder);

            runtime.CreateActor(typeof(M));

            await WaitAsync(tcs.Task);
            await Task.Delay(200);

            string expected = @"<DirectedGraph xmlns='http://schemas.microsoft.com/vs/2009/dgml'>
  <Nodes>
    <Node Id='M()' Category='Actor' Group='Expanded'/>
    <Node Id='M().M()' Label='M()'/>
    <Node Id='N()' Category='StateMachine' Group='Expanded'/>
    <Node Id='N().Act' Label='Act'/>
    <Node Id='N().Init' Label='Init'/>
    <Node Id='TestMonitor' Group='Expanded'/>
    <Node Id='TestMonitor.Init' Label='Init'/>
    <Node Id='TestMonitor.OnCompleted' Label='OnCompleted'/>
  </Nodes>
  <Links>
    <Link Source='M().M()' Target='N().Init' Label='E' Index='' EventId='E' HandledBy='Init'/>
    <Link Source='M().M()' Target='TestMonitor.Init' Label='CompletedEvent' Index='' EventId='CompletedEvent'/>
    <Link Source='M()' Target='M().M()' Category='Contains'/>
    <Link Source='N().Act' Target='M().M()' Label='E' Index='' EventId='E'/>
    <Link Source='N().Init' Target='N().Act' Label='E' Index='' EventId='E' HandledBy='Init'/>
    <Link Source='N()' Target='N().Act' Category='Contains'/>
    <Link Source='N()' Target='N().Init' Category='Contains'/>
    <Link Source='TestMonitor.Init' Target='TestMonitor.OnCompleted' Label='CompletedEvent' Index='' EventId='CompletedEvent'/>
    <Link Source='TestMonitor' Target='TestMonitor.Init' Category='Contains'/>
    <Link Source='TestMonitor' Target='TestMonitor.OnCompleted' Category='Contains'/>
  </Links>
</DirectedGraph>
";

            string actual = RemoveNonDeterministicValuesFromReport(graphBuilder.Graph.ToString());
            Assert.Equal(expected, actual);

            logger.Dispose();
        }

        [Fact(Timeout = 5000)]
        public async Task TestCustomLoggerNoVerbosity()
        {
            var logger = TextWriter.Null;

            var tcs = new TaskCompletionSource<bool>();
            IActorRuntime runtime = CreateTestRuntime(Configuration.Create(), tcs, logger);

            runtime.CreateActor(typeof(M));

            await WaitAsync(tcs.Task);

            Assert.Equal("System.IO.TextWriter+NullTextWriter", runtime.Logger.ToString());
        }

        [Fact(Timeout = 5000)]
        public async Task TestNullCustomLogger()
        {
            var tcs = new TaskCompletionSource<bool>();
            IActorRuntime runtime = CreateTestRuntime(Configuration.Create(), tcs);
            runtime.SetLogger(null);

            runtime.CreateActor(typeof(M));

            await WaitAsync(tcs.Task);

            Assert.Equal("System.IO.TextWriter+NullTextWriter", runtime.Logger.ToString());
        }

        [Fact(Timeout = 5000)]
        public async Task TestCustomActorRuntimeLogFormatter()
        {
            Configuration config = Configuration.Create().WithVerbosityEnabled();
            var tcs = new TaskCompletionSource<bool>();
            IActorRuntime runtime = CreateTestRuntime(config, tcs);
            runtime.RegisterMonitor<S>();
            runtime.SetLogger(null);

            var logger = new CustomActorRuntimeLog();
            runtime.RegisterLog(logger);

            runtime.CreateActor(typeof(M));

            await WaitAsync(tcs.Task, 5000);
            await Task.Delay(200);

            string expected = @"CreateActor
CreateStateMachine
StateTransition
StateTransition
StateTransition
";

            string actual = RemoveNonDeterministicValuesFromReport(logger.ToString());
            Assert.Equal(expected, actual);
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

                if (this.Count == 3)
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
                this.Monitor<TestMonitor>(new CompletedEvent());
            }
        }

        [Fact(Timeout = 5000)]
        public async Task TestGraphLoggerInstances()
        {
            CustomLogger logger = new CustomLogger();
            Configuration config = Configuration.Create().WithVerbosityEnabled();

            var graphBuilder = new ActorRuntimeLogGraphBuilder(false);

            var tcs = new TaskCompletionSource<bool>();
            IActorRuntime runtime = CreateTestRuntime(config, tcs, logger);
            runtime.RegisterLog(graphBuilder);

            ActorId serverId = runtime.CreateActor(typeof(Server));
            runtime.CreateActor(typeof(Client), new ClientSetupEvent(serverId));
            runtime.CreateActor(typeof(Client), new ClientSetupEvent(serverId));
            runtime.CreateActor(typeof(Client), new ClientSetupEvent(serverId));

            await WaitAsync(tcs.Task);
            await Task.Delay(1000);

            string actual = graphBuilder.Graph.ToString();

            Assert.Contains("<Node Id='Microsoft.Coyote.Production.Tests.Runtime.CustomActorRuntimeLogTests+Client(1).Client(1)' Label='Client(1)'/>", actual);
            Assert.Contains("<Node Id='Microsoft.Coyote.Production.Tests.Runtime.CustomActorRuntimeLogTests+Client(2).Client(2)' Label='Client(2)'/>", actual);
            Assert.Contains("<Node Id='Microsoft.Coyote.Production.Tests.Runtime.CustomActorRuntimeLogTests+Client(3).Client(3)' Label='Client(3)'/>", actual);
            Assert.Contains("<Node Id='Microsoft.Coyote.Production.Tests.Runtime.CustomActorRuntimeLogTests+Server(0).Complete' Label='Complete'/>", actual);

            logger.Dispose();
        }

        [Fact(Timeout = 5000000)]
        public async Task TestGraphLoggerCollapsed()
        {
            CustomLogger logger = new CustomLogger();
            Configuration config = Configuration.Create().WithVerbosityEnabled();

            var graphBuilder = new ActorRuntimeLogGraphBuilder(false);
            graphBuilder.CollapseMachineInstances = true;

            var tcs = new TaskCompletionSource<bool>();
            IActorRuntime runtime = CreateTestRuntime(config, tcs, logger);
            runtime.RegisterLog(graphBuilder);

            ActorId serverId = runtime.CreateActor(typeof(Server));
            runtime.CreateActor(typeof(Client), new ClientSetupEvent(serverId));
            runtime.CreateActor(typeof(Client), new ClientSetupEvent(serverId));
            runtime.CreateActor(typeof(Client), new ClientSetupEvent(serverId));

            await WaitAsync(tcs.Task, 5000000);
            await Task.Delay(1000);

            string actual = graphBuilder.Graph.ToString();

            Assert.Contains("<Node Id='Microsoft.Coyote.Production.Tests.Runtime.CustomActorRuntimeLogTests+Client.Client' Label='Client'/>", actual);
            Assert.Contains("<Node Id='Microsoft.Coyote.Production.Tests.Runtime.CustomActorRuntimeLogTests+Server.Complete' Label='Complete'/>", actual);

            logger.Dispose();
        }
    }
}
