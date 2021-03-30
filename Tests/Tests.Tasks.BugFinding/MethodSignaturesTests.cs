// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Tasks.BugFinding.Tests
{
    public class MethodSignaturesTests : BaseTaskTest
    {
        public MethodSignaturesTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestForCoyoteMethodSignatures()
        {
            foreach (var type in this.GetType().Assembly.GetTypes())
            {
                foreach (var method in type.GetMethods())
                {
                    if (method.GetCustomAttributes(typeof(FactAttribute), false).Any() ||
                        method.GetCustomAttributes(typeof(TheoryAttribute), false).Any())
                    {
                        Type returnType = method.ReturnType;
                        if (returnType.FullName.Contains("Coyote"))
                        {
                            Assert.False(true, string.Format("XUnit does not support Coyote return types in method: {0}.{1}", type.FullName, method.Name));
                        }
                    }
                }
            }
        }
    }
}
