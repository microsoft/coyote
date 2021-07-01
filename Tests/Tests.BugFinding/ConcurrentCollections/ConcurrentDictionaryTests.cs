// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class ConcurrentDictionaryTests : BaseBugFindingTest
    {
        public ConcurrentDictionaryTests(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
