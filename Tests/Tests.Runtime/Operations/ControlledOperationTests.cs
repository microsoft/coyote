// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Runtime.Tests
{
    public class ControlledOperationTests : BaseRuntimeTest
    {
        public ControlledOperationTests(ITestOutputHelper output)
            : base(output)
        {
        }

        // [Fact(Timeout = 5000)]
        public void TestThreadOperationInstrumentation()
        {
            this.Test(() =>
            {
                var operationId = Operation.CreateNext();
                Specification.Assert(operationId.HasValue, $"Unable to create next operation.");

                int value = 0;
                Thread thread;
                using (var handshakeSync = new ManualResetEventSlim(false))
                {
                    thread = new Thread(state =>
                    {
                        Operation.Start((ulong)state, handshakeSync);
                        value = 1;
                        Operation.Complete();
                        Operation.ScheduleNext();
                    });

                    Operation.WaitOperationStart(operationId.Value, handshakeSync);
                }

                thread.Start(operationId.Value);
                Operation.ScheduleNext();

                Operation.PauseUntilCompleted(operationId.Value);
                thread.Join();

                int expected = 1;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }
    }
}
