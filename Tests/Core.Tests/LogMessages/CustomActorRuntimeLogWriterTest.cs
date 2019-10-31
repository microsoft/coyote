// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests.LogMessages
{
    public class CustomActorRuntimeLogWriterTest : BaseTest
    {
        public CustomActorRuntimeLogWriterTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout=5000)]
        public async Task TestCustomActorRuntimeLogWriter()
        {
            CustomLogger logger = new CustomLogger(true);

            Configuration config = Configuration.Create().WithVerbosityEnabled();
            var runtime = ActorRuntimeFactory.Create(config);
            runtime.SetLogger(logger);
            runtime.SetLogWriter(new CustomActorRuntimeLogWriter());

            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateMachine(typeof(M), new Configure(tcs));

            await WaitAsync(tcs.Task);
            await Task.Delay(200);

            string expected = @"<CreateLog>.
<StateLog>.
<ActionLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.M()' in state 'Init' invoked action 'InitOnEntry'.
<CreateLog>.
<StateLog>.
<ActionLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.N()' in state 'Init' invoked action 'InitOnEntry'.
<DequeueLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.N()' in state 'Init' dequeued event 'Microsoft.Coyote.Core.Tests.LogMessages.E'.
<GotoLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.N()' is transitioning from state 'Init' to state 'Microsoft.Coyote.Core.Tests.LogMessages.N.Act'.
<StateLog>.
<StateLog>.
<ActionLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.N()' in state 'Act' invoked action 'ActOnEntry'.
<DequeueLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.M()' in state 'Init' dequeued event 'Microsoft.Coyote.Core.Tests.LogMessages.E'.
<ActionLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.M()' in state 'Init' invoked action 'Act'.
";
            string actual = Regex.Replace(logger.ToString(), "[0-9]", string.Empty);

            Assert.Equal(expected, actual);

            logger.Dispose();
        }
    }
}
