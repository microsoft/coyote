// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.TestingServices.Runtime
{
    /// <summary>
    /// Implements a test harness machine that executes the synchronous
    /// test entry point during systematic testing.
    /// </summary>
    internal sealed class TestHarnessMachine : AsyncMachine
    {
        /// <summary>
        /// The test method.
        /// </summary>
        private readonly Delegate TestMethod;

        /// <summary>
        /// The test name.
        /// </summary>
        internal readonly string TestName;

        /// <summary>
        /// Id used to identify subsequent operations performed by this machine.
        /// </summary>
        protected internal override Guid OperationGroupId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestHarnessMachine"/> class.
        /// </summary>
        internal TestHarnessMachine(Delegate testMethod, string testName)
            : this(testName)
        {
            this.TestMethod = testMethod;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestHarnessMachine"/> class.
        /// </summary>
        private TestHarnessMachine(string testName)
        {
            this.TestName = string.IsNullOrEmpty(testName) ? "anonymous test" : $"test '{testName}'";
        }

        /// <summary>
        /// Runs the test harness asynchronously.
        /// </summary>
        internal Task RunAsync()
        {
            this.Logger.WriteLine($"<TestHarnessLog> Running {this.TestName}.");

            try
            {
                if (this.TestMethod is Action<ICoyoteRuntime> testAction)
                {
                    testAction(this.Id.Runtime);
                    return Task.CompletedTask;
                }
                else if (this.TestMethod is Func<ICoyoteRuntime, Task> testFunction)
                {
                    return testFunction(this.Id.Runtime);
                }
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }

            return Task.CompletedTask;
        }
    }
}
