// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

        [Fact(Timeout=5000)]
        public async Task TestCustomLogger()
        {
            CustomLogger logger = new CustomLogger(true);

            Configuration config = Configuration.Create().WithVerbosityEnabled();
            var runtime = CoyoteRuntime.Create(config);
            runtime.SetLogger(logger);

            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateMachine(typeof(M), new Configure(tcs));

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
<ActionLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.N()' in state 'Init' invoked action 'Act'.
<SendLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.N()' in state 'Init' sent event 'Microsoft.Coyote.Core.Tests.LogMessages.E' to machine 'Microsoft.Coyote.Core.Tests.LogMessages.M()'.
<EnqueueLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.M()' enqueued event 'Microsoft.Coyote.Core.Tests.LogMessages.E'.
<DequeueLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.M()' in state 'Init' dequeued event 'Microsoft.Coyote.Core.Tests.LogMessages.E'.
<ActionLog> Machine 'Microsoft.Coyote.Core.Tests.LogMessages.M()' in state 'Init' invoked action 'Act'.
";
            string actual = Regex.Replace(logger.ToString(), "[0-9]", string.Empty);
            Assert.Equal(expected, actual);

            logger.Dispose();
        }

        [Fact(Timeout=5000)]
        public async Task TestCustomLoggerNoVerbosity()
        {
            CustomLogger logger = new CustomLogger(false);

            var runtime = CoyoteRuntime.Create();
            runtime.SetLogger(logger);

            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateMachine(typeof(M), new Configure(tcs));

            await WaitAsync(tcs.Task);

            Assert.Equal(string.Empty, logger.ToString());

            logger.Dispose();
        }

        [Fact(Timeout=5000)]
        public void TestNullCustomLoggerFail()
        {
            this.Run(r =>
            {
                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => r.SetLogger(null));
                Assert.Equal("Cannot install a null logger.", ex.Message);
            });
        }
    }
}
