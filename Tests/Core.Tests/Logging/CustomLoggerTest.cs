// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.Coyote.IO;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests
{
    public class CustomLoggerTest : BaseTest
    {
        public CustomLoggerTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class CustomLogger : MachineLogger
        {
            private StringBuilder StringBuilder;

            public CustomLogger(bool isVerbose)
                : base(isVerbose)
            {
                this.StringBuilder = new StringBuilder();
            }

            public override void Write(string value)
            {
                this.StringBuilder.Append(value);
            }

            /// <summary>
            /// Writes the text representation of the specified argument.
            /// </summary>
            public override void Write(string format, object arg0)
            {
                this.StringBuilder.AppendFormat(format, arg0.ToString());
            }

            /// <summary>
            /// Writes the text representation of the specified arguments.
            /// </summary>
            public override void Write(string format, object arg0, object arg1)
            {
                this.StringBuilder.AppendFormat(format, arg0.ToString(), arg1.ToString());
            }

            /// <summary>
            /// Writes the text representation of the specified arguments.
            /// </summary>
            public override void Write(string format, object arg0, object arg1, object arg2)
            {
                this.StringBuilder.AppendFormat(format, arg0.ToString(), arg1.ToString(), arg2.ToString());
            }

            public override void Write(string format, params object[] args)
            {
                this.StringBuilder.AppendFormat(format, args);
            }

            public override void WriteLine(string value)
            {
                this.StringBuilder.AppendLine(value);
            }

            /// <summary>
            /// Writes the text representation of the specified argument, followed by the
            /// current line terminator.
            /// </summary>
            public override void WriteLine(string format, object arg0)
            {
                this.StringBuilder.AppendFormat(format, arg0.ToString());
                this.StringBuilder.AppendLine();
            }

            /// <summary>
            /// Writes the text representation of the specified arguments, followed by the
            /// current line terminator.
            /// </summary>
            public override void WriteLine(string format, object arg0, object arg1)
            {
                this.StringBuilder.AppendFormat(format, arg0.ToString(), arg1.ToString());
                this.StringBuilder.AppendLine();
            }

            /// <summary>
            /// Writes the text representation of the specified arguments, followed by the
            /// current line terminator.
            /// </summary>
            public override void WriteLine(string format, object arg0, object arg1, object arg2)
            {
                this.StringBuilder.AppendFormat(format, arg0.ToString(), arg1.ToString(), arg2.ToString());
                this.StringBuilder.AppendLine();
            }

            public override void WriteLine(string format, params object[] args)
            {
                this.StringBuilder.AppendFormat(format, args);
                this.StringBuilder.AppendLine();
            }

            public override string ToString()
            {
                return this.StringBuilder.ToString();
            }

            public override void Dispose()
            {
                this.StringBuilder.Clear();
                this.StringBuilder = null;
            }
        }

        internal class Configure : Event
        {
            public TaskCompletionSource<bool> Tcs;

            public Configure(TaskCompletionSource<bool> tcs)
            {
                this.Tcs = tcs;
            }
        }

        internal class E : Event
        {
            public MachineId Id;

            public E(MachineId id)
            {
                this.Id = id;
            }
        }

        internal class Unit : Event
        {
        }

        private class M : Machine
        {
            private TaskCompletionSource<bool> Tcs;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Act))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Tcs = (this.ReceivedEvent as Configure).Tcs;
                var n = this.CreateMachine(typeof(N));
                this.Send(n, new E(this.Id));
            }

            private void Act()
            {
                this.Tcs.SetResult(true);
            }
        }

        private class N : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Act))]
            private class Init : MachineState
            {
            }

            private void Act()
            {
                MachineId m = (this.ReceivedEvent as E).Id;
                this.Send(m, new E(this.Id));
            }
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

            string expected = @"<CreateLog> Machine 'Microsoft.Coyote.Core.Tests.CustomLoggerTest+M()' was created by the runtime.
<StateLog> Machine 'Microsoft.Coyote.Core.Tests.CustomLoggerTest+M()' enters state 'Init'.
<ActionLog> Machine 'Microsoft.Coyote.Core.Tests.CustomLoggerTest+M()' in state 'Init' invoked action 'InitOnEntry'.
<CreateLog> Machine 'Microsoft.Coyote.Core.Tests.CustomLoggerTest+N()' was created by machine 'Microsoft.Coyote.Core.Tests.CustomLoggerTest+M()'.
<StateLog> Machine 'Microsoft.Coyote.Core.Tests.CustomLoggerTest+N()' enters state 'Init'.
<SendLog> Machine 'Microsoft.Coyote.Core.Tests.CustomLoggerTest+M()' in state 'Init' sent event 'Microsoft.Coyote.Core.Tests.CustomLoggerTest+E' to machine 'Microsoft.Coyote.Core.Tests.CustomLoggerTest+N()'.
<EnqueueLog> Machine 'Microsoft.Coyote.Core.Tests.CustomLoggerTest+N()' enqueued event 'Microsoft.Coyote.Core.Tests.CustomLoggerTest+E'.
<DequeueLog> Machine 'Microsoft.Coyote.Core.Tests.CustomLoggerTest+N()' in state 'Init' dequeued event 'Microsoft.Coyote.Core.Tests.CustomLoggerTest+E'.
<ActionLog> Machine 'Microsoft.Coyote.Core.Tests.CustomLoggerTest+N()' in state 'Init' invoked action 'Act'.
<SendLog> Machine 'Microsoft.Coyote.Core.Tests.CustomLoggerTest+N()' in state 'Init' sent event 'Microsoft.Coyote.Core.Tests.CustomLoggerTest+E' to machine 'Microsoft.Coyote.Core.Tests.CustomLoggerTest+M()'.
<EnqueueLog> Machine 'Microsoft.Coyote.Core.Tests.CustomLoggerTest+M()' enqueued event 'Microsoft.Coyote.Core.Tests.CustomLoggerTest+E'.
<DequeueLog> Machine 'Microsoft.Coyote.Core.Tests.CustomLoggerTest+M()' in state 'Init' dequeued event 'Microsoft.Coyote.Core.Tests.CustomLoggerTest+E'.
<ActionLog> Machine 'Microsoft.Coyote.Core.Tests.CustomLoggerTest+M()' in state 'Init' invoked action 'Act'.
";
            string actual = Regex.Replace(logger.ToString(), "[0-9]", string.Empty);

            HashSet<string> expectedSet = new HashSet<string>(Regex.Split(expected, "\r\n|\r|\n"));
            HashSet<string> actualSet = new HashSet<string>(Regex.Split(actual, "\r\n|\r|\n"));

            Assert.True(expectedSet.SetEquals(actualSet));

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
