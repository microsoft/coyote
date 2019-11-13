// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests.IO
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
            runtime.CreateActor(typeof(M), new SetupEvent(tcs));

            await WaitAsync(tcs.Task);
            await Task.Delay(200);

            string expected = @"<CreateLog>.
<StateLog>.
<ActionLog> 'Microsoft.Coyote.Core.Tests.IO.M()' invoked action 'InitOnEntry' in state 'Init'.
<CreateLog>.
<StateLog>.
<ActionLog> 'Microsoft.Coyote.Core.Tests.IO.N()' invoked action 'InitOnEntry' in state 'Init'.
<DequeueLog> 'Microsoft.Coyote.Core.Tests.IO.N()' dequeued event 'Microsoft.Coyote.Core.Tests.IO.E' in state 'Init'.
<GotoLog> 'Microsoft.Coyote.Core.Tests.IO.N()' is transitioning from state 'Init' to state 'Microsoft.Coyote.Core.Tests.IO.N.Act'.
<StateLog>.
<StateLog>.
<ActionLog> 'Microsoft.Coyote.Core.Tests.IO.N()' invoked action 'ActOnEntry' in state 'Act'.
<DequeueLog> 'Microsoft.Coyote.Core.Tests.IO.M()' dequeued event 'Microsoft.Coyote.Core.Tests.IO.E' in state 'Init'.
<ActionLog> 'Microsoft.Coyote.Core.Tests.IO.M()' invoked action 'Act' in state 'Init'.
";
            string actual = Regex.Replace(logger.ToString(), "[0-9]", string.Empty);

            Assert.Equal(expected, actual);

            logger.Dispose();
        }
    }
}
