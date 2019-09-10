// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests.LogMessages
{
    public class CustomLogWriterTest : BaseTest
    {
        public CustomLogWriterTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout=5000)]
        public async Task TestCustomLogWriter()
        {
            CustomLogger logger = new CustomLogger(true);

            Configuration config = Configuration.Create().WithVerbosityEnabled();
            var runtime = CoyoteRuntime.Create(config);
            runtime.SetLogger(logger);
            runtime.SetLogWriter(new CustomLogWriter());

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
<ActionLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.N()' in state 'Init' invoked action 'Act'.
<DequeueLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.M()' in state 'Init' dequeued event 'Microsoft.Coyote.Core.Tests.LogMessages.E'.
<ActionLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.M()' in state 'Init' invoked action 'Act'.
";
            string actual = Regex.Replace(logger.ToString(), "[0-9]", string.Empty);
            Assert.Equal(expected, actual);

            logger.Dispose();
        }
    }
}
