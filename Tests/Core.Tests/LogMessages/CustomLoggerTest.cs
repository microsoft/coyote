// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.TestingServices.Coverage;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests.LogMessages
{
    public class CustomLoggerTest : BaseTest
    {
        public CustomLoggerTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public async Task TestCustomLogger()
        {
            CustomLogger logger = new CustomLogger(true);

            Configuration config = Configuration.Create().WithVerbosityEnabled();
            config.EnableMonitorsInProduction = true;
            var runtime = ActorRuntimeFactory.Create(config);
            runtime.RegisterMonitor(typeof(S));

            runtime.SetLogger(logger);

            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateStateMachine(typeof(M), new Configure(tcs));

            await WaitAsync(tcs.Task);
            await Task.Delay(200);

            string expected = @"<CreateLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.M()' was created by the runtime.
<StateLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.M()' enters state 'Init'.
<ActionLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.M()' in state 'Init' invoked action 'InitOnEntry'.
<CreateLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.N()' was created by machine 'Microsoft.Coyote.Core.Tests.LogMessages.M()'.
<StateLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.N()' enters state 'Init'.
<ActionLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.N()' in state 'Init' invoked action 'InitOnEntry'.
<SendLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.M()' in state 'Init' sent event 'Microsoft.Coyote.Core.Tests.LogMessages.E' to machine 'Microsoft.Coyote.Core.Tests.LogMessages.N()'.
<EnqueueLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.N()' enqueued event 'Microsoft.Coyote.Core.Tests.LogMessages.E'.
<DequeueLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.N()' in state 'Init' dequeued event 'Microsoft.Coyote.Core.Tests.LogMessages.E'.
<GotoLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.N()' is transitioning from state 'Init' to state 'Microsoft.Coyote.Core.Tests.LogMessages.N.Act'.
<StateLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.N()' exits state 'Init'.
<StateLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.N()' enters state 'Act'.
<ActionLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.N()' in state 'Act' invoked action 'ActOnEntry'.
<MonitorLog> Monitor 'S' with id 'Microsoft.Coyote.Core.Tests.LogMessages.S()' in state 'Init' is processing event 'Microsoft.Coyote.Core.Tests.LogMessages.E'.
<MonitorLog> Monitor 'Microsoft.Coyote.Core.Tests.LogMessages.S' with id 'Microsoft.Coyote.Core.Tests.LogMessages.S()' in state 'OnE' executed action 'Init'.
<MonitorLog> Monitor 'Microsoft.Coyote.Core.Tests.LogMessages.S' with id 'Microsoft.Coyote.Core.Tests.LogMessages.S()' in state 'Init' raised event 'Microsoft.Coyote.Actors.GotoStateEvent'.
<MonitorLog> Monitor 'Microsoft.Coyote.Core.Tests.LogMessages.S' with id 'Microsoft.Coyote.Core.Tests.LogMessages.S()' exits 'hot' state 'Init[hot]'.
<MonitorLog> Monitor 'Microsoft.Coyote.Core.Tests.LogMessages.S' with id 'Microsoft.Coyote.Core.Tests.LogMessages.S()' enters 'cold' state 'Done[cold]'.
<SendLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.N()' in state 'Act' sent event 'Microsoft.Coyote.Core.Tests.LogMessages.E' to machine 'Microsoft.Coyote.Core.Tests.LogMessages.M()'.
<EnqueueLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.M()' enqueued event 'Microsoft.Coyote.Core.Tests.LogMessages.E'.
<DequeueLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.M()' in state 'Init' dequeued event 'Microsoft.Coyote.Core.Tests.LogMessages.E'.
<ActionLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.M()' in state 'Init' invoked action 'Act'.
";
            string actual = Regex.Replace(logger.ToString(), "[0-9]", string.Empty);
            Assert.Equal(expected, actual);

            logger.Dispose();
        }

        [Fact(Timeout=5000)]
        public async Task TestGraphLogger()
        {
            CustomLogger logger = new CustomLogger(true);

            Configuration config = Configuration.Create().WithVerbosityEnabled();
            var runtime = ActorRuntimeFactory.Create(config);

            var graphWriter = new ActorRuntimeLogGraph();
            runtime.SetLogWriter(graphWriter);
            runtime.SetLogger(logger);

            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateStateMachine(typeof(M), new Configure(tcs));

            await WaitAsync(tcs.Task);
            await Task.Delay(200);

            string graph = graphWriter.Graph.ToString().Replace("Microsoft.Coyote.Core.Tests.LogMessages.", string.Empty);
            string expected = @"<DirectedGraph xmlns='http://schemas.microsoft.com/vs/2009/dgml'>
  <Nodes>
    <Node Id='M(0)' Label='M(0)' Group='Expanded'/>
    <Node Id='M(0).Init' Label='Init'/>
    <Node Id='N(1)' Label='N(1)' Group='Expanded'/>
    <Node Id='N(1).Act' Label='Act'/>
    <Node Id='N(1).Init' Label='Init'/>
  </Nodes>
  <Links>
    <Link Source='M(0).Init' Target='N(1).Init' Label='E' EventId='E'/>
    <Link Source='M(0)' Target='M(0).Init' Category='Contains'/>
    <Link Source='N(1).Act' Target='M(0).Init' Label='E' EventId='E'/>
    <Link Source='N(1).Init' Target='N(1).Act' Label='E' EventId='E'/>
    <Link Source='N(1)' Target='N(1).Act' Category='Contains'/>
    <Link Source='N(1)' Target='N(1).Init' Category='Contains'/>
  </Links>
</DirectedGraph>
";

            Assert.Equal(expected, graph);

            logger.Dispose();
        }

        [Fact(Timeout=5000)]
        public async Task TestCustomLoggerNoVerbosity()
        {
            CustomLogger logger = new CustomLogger(false);

            var runtime = ActorRuntimeFactory.Create();
            runtime.SetLogger(logger);

            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateStateMachine(typeof(M), new Configure(tcs));

            await WaitAsync(tcs.Task);

            Assert.Equal(string.Empty, logger.ToString());

            logger.Dispose();
        }

        [Fact(Timeout=5000)]
        public void TestNullCustomLoggerFail()
        {
            this.Run(r =>
            {
                Assert.Throws<InvalidOperationException>(() => r.SetLogger(null));
            });
        }
    }
}
