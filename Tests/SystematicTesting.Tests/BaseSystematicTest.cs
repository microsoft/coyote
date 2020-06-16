// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tests.Common;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests
{
    public abstract class BaseSystematicTest : BaseTest
    {
        public BaseSystematicTest(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool SystematicTest => true;

        /// <summary>
        /// For tests expecting uncontrolled task assertions, use these as the expectedErrors array.
        /// </summary>
        public static string[] GetUncontrolledTaskErrorMessages()
        {
            return new string[]
            {
                "Controlled task '' is trying to wait for an uncontrolled task or awaiter to complete. Please " +
                "make sure to avoid using concurrency APIs () inside actor handlers. If you are using external " +
                "libraries that are executing concurrently, you will need to mock them during testing.",
                "Uncontrolled task '' invoked a runtime method. Please make sure to avoid using concurrency APIs () " +
                "inside actor handlers or controlled tasks. If you are using external libraries that are executing " +
                "concurrently, you will need to mock them during testing.",
                "Controlled task '' is trying to wait for an uncontrolled task or awaiter to complete. " +
                "Please make sure to use Coyote APIs to express concurrency ()."
            };
        }
    }
}
