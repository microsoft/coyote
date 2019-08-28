// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Tests.Common;
using Xunit.Abstractions;

using BaseBugFindingTest = Microsoft.Coyote.TestingServices.Tests.BaseTest;
using BaseCoreTest = Microsoft.Coyote.Core.Tests.BaseTest;

namespace Microsoft.Coyote.Tests.Launcher
{
    public sealed class CoreTest : BaseCoreTest
    {
        public CoreTest(ITestOutputHelper output)
            : base(output)
        {
        }

#pragma warning disable CA1822 // Mark members as static
        public async Task Run()
        {
            await Task.CompletedTask;
        }
#pragma warning restore CA1822 // Mark members as static
    }

    public class BugFindingTest : BaseBugFindingTest
    {
        public BugFindingTest(ITestOutputHelper output)
            : base(output)
        {
        }

#pragma warning disable CA1801 // Parameter not used
        [Test]
        public static void Execute(ICoyoteRuntime r)
        {
        }
#pragma warning restore CA1801 // Parameter not used
    }

    public static class Assert
    {
        public static void True(bool predicate, string message = null)
        {
            if (!predicate)
            {
                throw new InvalidOperationException(message ?? string.Empty);
            }
        }

        public static void Equal<T>(T expected, T actual)
            where T : IEquatable<T>
        {
            True(expected.Equals(actual), $"actual '{actual}' != expected '{expected}'");
        }
    }

    public static class Program
    {
        private static async Task Main()
        {
            var test = new CoreTest(new TestConsoleLogger());
            await test.Run();
        }
    }
}
