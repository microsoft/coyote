// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Coverage;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.IO;
using Microsoft.Coyote.Tests.Common.Runtime;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

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
            protected override async SystemTasks.Task OnInitializeAsync(Event e)
            {
                await base.OnInitializeAsync(e);
                var n = this.CreateActor(typeof(N));
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

            private void OnInitEntry()
            {
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

        [Fact(Timeout = 5000)]
        public void TestCustomLogger()
        {
            Configuration config = Configuration.Create().WithVerbosityEnabled();
            this.Test(new Func<IActorRuntime, Task>(async (runtime) =>
            {
                using (CustomLogger logger = new CustomLogger())
                {
                    runtime.SetLogger(logger);
                    var tcs = TaskCompletionSource.Create<bool>();
                    runtime.RegisterMonitor<TestMonitor>();
                    runtime.Monitor<TestMonitor>(new SetupEvent(tcs));
                    runtime.CreateActor(typeof(M));
                    await this.WaitAsync(tcs.Task);
                    await Task.Delay(200);
                    Assert.True(tcs.Task.IsCompleted, "The task await returned but the task is not completed???");

                    string expected = @"<CreateLog> TestMonitor was created.
<MonitorLog> TestMonitor enters state 'Init'.
<MonitorLog> TestMonitor is processing event 'SetupEvent' in state 'Init'.
<MonitorLog> TestMonitor executed action 'OnSetup' in state 'Init'.
<CreateLog> M() was created by task ''.
<CreateLog> N() was created by M().
<SendLog> M() in state '' sent event 'E' to N().
<EnqueueLog> N() enqueued event 'E'.
<StateLog> N() enters state 'Init'.
<ActionLog> N() invoked action 'OnInitEntry' in state 'Init'.
<DequeueLog> N() dequeued event 'E' in state 'Init'.
<GotoLog> N() is transitioning from state 'Init' to state 'N.Act'.
<StateLog> N() exits state 'Init'.
<StateLog> N() enters state 'Act'.
<ActionLog> N() invoked action 'ActOnEntry' in state 'Act'.
<SendLog> N() in state 'Act' sent event 'E' to M().
<EnqueueLog> M() enqueued event 'E'.
<DequeueLog> M() dequeued event 'E'.
<ActionLog> M() invoked action 'Act'.
<MonitorLog> TestMonitor is processing event 'CompletedEvent' in state 'Init'.
<MonitorLog> TestMonitor executed action 'OnCompleted' in state 'Init'.";

                    string actual = RemoveNonDeterministicValuesFromReport(logger.ToString());
                    expected = NormalizeNewLines(expected);
                    actual = SortLines(actual); // threading makes this non-deterministic otherwise.
                    expected = SortLines(expected);
                    Assert.Equal(expected, actual);
                }
            }), config);
        }

        [Fact(Timeout = 5000)]
        public void TestGraphLogger()
        {
            Configuration config = Configuration.Create().WithVerbosityEnabled();
            this.Test(new Func<IActorRuntime, Task>(async (runtime) =>
            {
                using (CustomLogger logger = new CustomLogger())
                {
                    runtime.SetLogger(logger);
                    var tcs = TaskCompletionSource.Create<bool>();
                    runtime.RegisterMonitor<TestMonitor>();
                    runtime.Monitor<TestMonitor>(new SetupEvent(tcs));
                    var graphBuilder = new ActorRuntimeLogGraphBuilder(false);
                    runtime.RegisterLog(graphBuilder);
                    runtime.CreateActor(typeof(M));
                    await this.WaitAsync(tcs.Task);
                    await Task.Delay(200);
                    Assert.True(tcs.Task.IsCompleted, "The task await returned but the task is not completed???");

                    string expected = @"<DirectedGraph xmlns='http://schemas.microsoft.com/vs/2009/dgml'>
  <Nodes>
    <Node Id='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+M(0)' Category='Actor' Group='Expanded'/>
    <Node Id='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+M(0).M(0)' Label='M(0)'/>
    <Node Id='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+N(1)' Category='StateMachine' Group='Expanded'/>
    <Node Id='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+N(1).Act' Label='Act'/>
    <Node Id='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+N(1).Init' Label='Init'/>
    <Node Id='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+TestMonitor' Group='Expanded'/>
    <Node Id='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+TestMonitor.Init' Label='Init'/>
  </Nodes>
  <Links>
    <Link Source='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+M(0)' Target='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+M(0).M(0)' Category='Contains'/>
    <Link Source='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+M(0).M(0)' Target='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+N(1).Init' Label='E' Index='0' EventId='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+E' HandledBy='Init'/>
    <Link Source='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+M(0).M(0)' Target='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+TestMonitor.Init' Label='CompletedEvent' Index='0' EventId='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+CompletedEvent'/>
    <Link Source='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+N(1)' Target='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+N(1).Act' Category='Contains'/>
    <Link Source='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+N(1)' Target='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+N(1).Init' Category='Contains'/>
    <Link Source='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+N(1).Act' Target='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+M(0).M(0)' Label='E' Index='0' EventId='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+E'/>
    <Link Source='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+N(1).Init' Target='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+N(1).Act' Label='E' Index='0' EventId='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+E' HandledBy='Init'/>
    <Link Source='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+TestMonitor' Target='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+TestMonitor.Init' Category='Contains'/>
    <Link Source='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+TestMonitor.Init' Target='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+TestMonitor.Init' Label='CompletedEvent' Index='0' EventId='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+CompletedEvent'/>
  </Links>
</DirectedGraph>
";

                    string dgml = graphBuilder.Graph.ToString();
                    string actual = RemoveNonDeterministicValuesFromReport(dgml);
                    expected = RemoveNonDeterministicValuesFromReport(expected);
                    Assert.Equal(expected, actual);
                }
            }), config);
        }

        [Fact(Timeout = 5000)]
        public void TestCustomLoggerNoVerbosity()
        {
            Configuration config = Configuration.Create();
            this.Test(new Func<IActorRuntime, Task>(async (runtime) =>
            {
                runtime.SetLogger(TextWriter.Null);
                var tcs = TaskCompletionSource.Create<bool>();
                runtime.RegisterMonitor<TestMonitor>();
                runtime.Monitor<TestMonitor>(new SetupEvent(tcs));
                runtime.CreateActor(typeof(M));
                await this.WaitAsync(tcs.Task);
                Assert.Equal("System.IO.TextWriter+NullTextWriter", runtime.Logger.ToString());
            }), config);
        }

        [Fact(Timeout = 5000)]
        public void TestNullCustomLogger()
        {
            Configuration config = Configuration.Create();
            this.Test(new Func<IActorRuntime, Task>(async (runtime) =>
            {
                var tcs = TaskCompletionSource.Create<bool>();
                runtime.RegisterMonitor<TestMonitor>();
                runtime.Monitor<TestMonitor>(new SetupEvent(tcs));
                runtime.SetLogger(null);
                runtime.CreateActor(typeof(M));
                await this.WaitAsync(tcs.Task);
                Assert.Equal("System.IO.TextWriter+NullTextWriter", runtime.Logger.ToString());
            }), config);
        }

        [Fact(Timeout = 5000)]
        public void TestCustomActorRuntimeLogFormatter()
        {
            Configuration config = Configuration.Create().WithVerbosityEnabled();
            this.Test(new Func<IActorRuntime, Task>(async (runtime) =>
            {
                var tcs = TaskCompletionSource.Create<bool>();
                runtime.RegisterMonitor<TestMonitor>();
                runtime.Monitor<TestMonitor>(new SetupEvent(tcs));
                runtime.RegisterMonitor<S>();
                runtime.SetLogger(null);

                var logger = new CustomActorRuntimeLog();
                runtime.RegisterLog(logger);

                runtime.CreateActor(typeof(M));

                await this.WaitAsync(tcs.Task, 5000);
                await Task.Delay(200);

                string expected = @"CreateActor
CreateStateMachine
StateTransition
StateTransition
StateTransition";
                string actual = RemoveNonDeterministicValuesFromReport(logger.ToString());
                expected = NormalizeNewLines(expected);
                Assert.Equal(expected, actual);
            }), config);
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

            protected override SystemTasks.Task OnInitializeAsync(Event initialEvent)
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
        public void TestGraphLoggerInstances()
        {
            Configuration config = Configuration.Create().WithVerbosityEnabled();
            this.Test(new Func<IActorRuntime, Task>(async (runtime) =>
            {
                using (CustomLogger logger = new CustomLogger())
                {
                    runtime.SetLogger(logger);

                    var graphBuilder = new ActorRuntimeLogGraphBuilder(false);

                    var tcs = TaskCompletionSource.Create<bool>();
                    runtime.RegisterMonitor<TestMonitor>();
                    runtime.Monitor<TestMonitor>(new SetupEvent(tcs));
                    runtime.RegisterLog(graphBuilder);

                    ActorId serverId = runtime.CreateActor(typeof(Server));
                    runtime.CreateActor(typeof(Client), new ClientSetupEvent(serverId));
                    runtime.CreateActor(typeof(Client), new ClientSetupEvent(serverId));
                    runtime.CreateActor(typeof(Client), new ClientSetupEvent(serverId));

                    await this.WaitAsync(tcs.Task);
                    await Task.Delay(1000);
                    Assert.True(tcs.Task.IsCompleted, "The task await returned but the task is not completed???");

                    string actual = graphBuilder.Graph.ToString();
                    actual = RemoveInstanceIds(actual);

                    Assert.Contains("<Node Id='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+Client().Client()' Label='Client()'/>", actual);
                    Assert.Contains("<Node Id='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+Server().Complete' Label='Complete'/>", actual);
                    Assert.Contains("<Node Id='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+TestMonitor.Init' Label='Init'/>", actual);
                }
            }), config);
        }

        [Fact(Timeout = 5000)]
        public void TestGraphLoggerCollapsed()
        {
            Configuration config = Configuration.Create().WithVerbosityEnabled();
            this.Test(new Func<IActorRuntime, Task>(async (runtime) =>
            {
                using (CustomLogger logger = new CustomLogger())
                {
                    runtime.SetLogger(logger);

                    var graphBuilder = new ActorRuntimeLogGraphBuilder(false);
                    graphBuilder.CollapseMachineInstances = true;

                    var tcs = TaskCompletionSource.Create<bool>();
                    runtime.RegisterMonitor<TestMonitor>();
                    runtime.Monitor<TestMonitor>(new SetupEvent(tcs));
                    runtime.RegisterLog(graphBuilder);

                    ActorId serverId = runtime.CreateActor(typeof(Server));
                    runtime.CreateActor(typeof(Client), new ClientSetupEvent(serverId));
                    runtime.CreateActor(typeof(Client), new ClientSetupEvent(serverId));
                    runtime.CreateActor(typeof(Client), new ClientSetupEvent(serverId));

                    await this.WaitAsync(tcs.Task, 5000);
                    await Task.Delay(1000);
                    Assert.True(tcs.Task.IsCompleted, "The task await returned but the task is not completed???");

                    string actual = graphBuilder.Graph.ToString();

                    Assert.Contains("<Node Id='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+Client.Client' Label='Client'/>", actual);
                    Assert.Contains("<Node Id='Microsoft.Coyote.Production.Tests.Actors.CustomActorRuntimeLogTests+Server.Complete' Label='Complete'/>", actual);
                }
            }), config);
        }
    }
}
