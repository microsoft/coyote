// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.TestingServices.Coverage;
using Microsoft.Coyote.Tests.Common.IO;
using Microsoft.Coyote.Tests.Common.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests.Runtime
{
    public class CustomActorRuntimeLogTests : BaseTest
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

        internal class E : Event
        {
            public ActorId Id;

            public E(ActorId id)
            {
                this.Id = id;
            }
        }

        internal class M : StateMachine
        {
            private TaskCompletionSource<bool> Tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Act))]
            private class Init : State
            {
            }

            private async Task InitOnEntry(Event e)
            {
                this.Tcs = (e as SetupEvent).Tcs;
                var nTcs = new TaskCompletionSource<bool>();
                var n = this.CreateActor(typeof(N), new SetupEvent(nTcs));
                await nTcs.Task;
                this.SendEvent(n, new E(this.Id));
            }

            private void Act()
            {
                this.Tcs.SetResult(true);
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
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E), typeof(Act))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                var tcs = (e as SetupEvent).Tcs;
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

        [Fact(Timeout = 5000)]
        public async Task TestCustomLogger()
        {
            CustomLogger logger = new CustomLogger();
            Configuration config = Configuration.Create().WithVerbosityEnabled();

            var runtime = ActorRuntimeFactory.Create(config);
            runtime.SetLogger(logger);

            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateActor(typeof(M), new SetupEvent(tcs));

            await WaitAsync(tcs.Task);
            await Task.Delay(200);

            string expected = @"<CreateLog> M() was created by the runtime.
<StateLog> M() enters state 'Init'.
<ActionLog> M() invoked action 'InitOnEntry' in state 'Init'.
<CreateLog> N() was created by M().
<StateLog> N() enters state 'Init'.
<ActionLog> N() invoked action 'InitOnEntry' in state 'Init'.
<SendLog> M() in state 'Init' sent event 'E' to N().
<EnqueueLog> N() enqueued event 'E'.
<DequeueLog> N() dequeued event 'E' in state 'Init'.
<GotoLog> N() is transitioning from state 'Init' to state 'N.Act'.
<StateLog> N() exits state 'Init'.
<StateLog> N() enters state 'Act'.
<ActionLog> N() invoked action 'ActOnEntry' in state 'Act'.
<SendLog> N() in state 'Act' sent event 'E' to M().
<EnqueueLog> M() enqueued event 'E'.
<DequeueLog> M() dequeued event 'E' in state 'Init'.
<ActionLog> M() invoked action 'Act' in state 'Init'.
";

            string actual = RemoveNonDeterministicValuesFromReport(logger.ToString());
            Assert.Equal(expected, actual);

            logger.Dispose();
        }

        [Fact(Timeout = 5000)]
        public async Task TestGraphLogger()
        {
            CustomLogger logger = new CustomLogger();
            Configuration config = Configuration.Create().WithVerbosityEnabled();

            var graphBuilder = new ActorRuntimeLogGraphBuilder(false);
            var runtime = ActorRuntimeFactory.Create(config);
            runtime.RegisterLog(graphBuilder);
            runtime.SetLogger(logger);

            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateActor(typeof(M), new SetupEvent(tcs));

            await WaitAsync(tcs.Task);
            await Task.Delay(200);

            string expected = @"<DirectedGraph xmlns='http://schemas.microsoft.com/vs/2009/dgml'>
  <Nodes>
    <Node Id='M()' Category='StateMachine' Group='Expanded'/>
    <Node Id='M().Init' Label='Init'/>
    <Node Id='N()' Category='StateMachine' Group='Expanded'/>
    <Node Id='N().Act' Label='Act'/>
    <Node Id='N().Init' Label='Init'/>
  </Nodes>
  <Links>
    <Link Source='M().Init' Target='N().Init' Label='E' Index='' EventId='E' HandledBy='Init'/>
    <Link Source='M()' Target='M().Init' Category='Contains'/>
    <Link Source='N().Act' Target='M().Init' Label='E' Index='' EventId='E' HandledBy='Init'/>
    <Link Source='N().Init' Target='N().Act' Label='E' Index='' EventId='E' HandledBy='Init'/>
    <Link Source='N()' Target='N().Act' Category='Contains'/>
    <Link Source='N()' Target='N().Init' Category='Contains'/>
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

            var runtime = ActorRuntimeFactory.Create();
            runtime.SetLogger(logger);

            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateActor(typeof(M), new SetupEvent(tcs));

            await WaitAsync(tcs.Task);

            Assert.Equal("System.IO.TextWriter+NullTextWriter", logger.ToString());
        }

        [Fact(Timeout = 5000)]
        public async Task TestNullCustomLogger()
        {
            var runtime = ActorRuntimeFactory.Create();
            runtime.SetLogger(null);

            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateActor(typeof(M), new SetupEvent(tcs));

            await WaitAsync(tcs.Task);

            Assert.Equal("System.IO.TextWriter+NullTextWriter", runtime.Logger.ToString());
        }

        [Fact(Timeout = 5000)]
        public async Task TestCustomActorRuntimeLogFormatter()
        {
            Configuration config = Configuration.Create().WithVerbosityEnabled();
            config.IsMonitoringEnabledInInProduction = true;

            var runtime = ActorRuntimeFactory.Create(config);
            runtime.RegisterMonitor(typeof(S));
            runtime.SetLogger(null);
            var logger = new CustomActorRuntimeLog();
            runtime.RegisterLog(logger);

            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateActor(typeof(M), new SetupEvent(tcs));

            await WaitAsync(tcs.Task);
            await Task.Delay(200);

            string expected = @"CreateActor
StateTransition
CreateActor
StateTransition
StateTransition
StateTransition
";

            string actual = RemoveNonDeterministicValuesFromReport(logger.ToString());
            Assert.Equal(expected, actual);
        }
    }
}
