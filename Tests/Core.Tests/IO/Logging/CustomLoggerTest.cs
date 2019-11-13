// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.TestingServices.Coverage;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests.IO
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
            runtime.CreateActor(typeof(M), new SetupEvent(tcs));

            await WaitAsync(tcs.Task);
            await Task.Delay(200);

            string expected = @"<CreateLog> 'Microsoft.Coyote.Core.Tests.IO.M()' was created by the runtime.
<StateLog> 'Microsoft.Coyote.Core.Tests.IO.M()' enters state 'Init'.
<ActionLog> 'Microsoft.Coyote.Core.Tests.IO.M()' invoked action 'InitOnEntry' in state 'Init'.
<CreateLog> 'Microsoft.Coyote.Core.Tests.IO.N()' was created by 'Microsoft.Coyote.Core.Tests.IO.M()'.
<StateLog> 'Microsoft.Coyote.Core.Tests.IO.N()' enters state 'Init'.
<ActionLog> 'Microsoft.Coyote.Core.Tests.IO.N()' invoked action 'InitOnEntry' in state 'Init'.
<SendLog> 'Microsoft.Coyote.Core.Tests.IO.M()' in state 'Init' sent event 'Microsoft.Coyote.Core.Tests.IO.E' to 'Microsoft.Coyote.Core.Tests.IO.N()'.
<EnqueueLog> 'Microsoft.Coyote.Core.Tests.IO.N()' enqueued event 'Microsoft.Coyote.Core.Tests.IO.E'.
<DequeueLog> 'Microsoft.Coyote.Core.Tests.IO.N()' dequeued event 'Microsoft.Coyote.Core.Tests.IO.E' in state 'Init'.
<GotoLog> 'Microsoft.Coyote.Core.Tests.IO.N()' is transitioning from state 'Init' to state 'Microsoft.Coyote.Core.Tests.IO.N.Act'.
<StateLog> 'Microsoft.Coyote.Core.Tests.IO.N()' exits state 'Init'.
<StateLog> 'Microsoft.Coyote.Core.Tests.IO.N()' enters state 'Act'.
<ActionLog> 'Microsoft.Coyote.Core.Tests.IO.N()' invoked action 'ActOnEntry' in state 'Act'.
<MonitorLog> Monitor 'S' with id 'Microsoft.Coyote.Core.Tests.IO.S()' is processing event 'Microsoft.Coyote.Core.Tests.IO.E' in state 'Init'.
<MonitorLog> Monitor 'Microsoft.Coyote.Core.Tests.IO.S' with id 'Microsoft.Coyote.Core.Tests.IO.S()' executed action 'Init[hot]' in state 'OnE'.
<MonitorLog> Monitor 'Microsoft.Coyote.Core.Tests.IO.S' with id 'Microsoft.Coyote.Core.Tests.IO.S()' raised event 'Microsoft.Coyote.Actors.GotoStateEvent' in state 'Init[hot]'.
<MonitorLog> Monitor 'Microsoft.Coyote.Core.Tests.IO.S' with id 'Microsoft.Coyote.Core.Tests.IO.S()' exits 'hot' state 'Init[hot]'.
<MonitorLog> Monitor 'Microsoft.Coyote.Core.Tests.IO.S' with id 'Microsoft.Coyote.Core.Tests.IO.S()' enters 'cold' state 'Done[cold]'.
<SendLog> 'Microsoft.Coyote.Core.Tests.IO.N()' in state 'Act' sent event 'Microsoft.Coyote.Core.Tests.IO.E' to 'Microsoft.Coyote.Core.Tests.IO.M()'.
<EnqueueLog> 'Microsoft.Coyote.Core.Tests.IO.M()' enqueued event 'Microsoft.Coyote.Core.Tests.IO.E'.
<DequeueLog> 'Microsoft.Coyote.Core.Tests.IO.M()' dequeued event 'Microsoft.Coyote.Core.Tests.IO.E' in state 'Init'.
<ActionLog> 'Microsoft.Coyote.Core.Tests.IO.M()' invoked action 'Act' in state 'Init'.
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
            runtime.CreateActor(typeof(M), new SetupEvent(tcs));

            await WaitAsync(tcs.Task);
            await Task.Delay(200);

            string graph = graphWriter.Graph.ToString().Replace("Microsoft.Coyote.Core.Tests.IO.", string.Empty);
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
            runtime.CreateActor(typeof(M), new SetupEvent(tcs));

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
