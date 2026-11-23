// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET8_0_OR_GREATER
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class ExternMethodRewritingTests : BaseRewritingTest
    {
        public ExternMethodRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingUnsafeAccessorMethod()
        {
            // An extern method has no IL, so rewriting must leave it without a body, which the runtime requires.
            MethodInfo accessor = typeof(ExternMethodRewritingTests).GetMethod(nameof(GetValue), BindingFlags.NonPublic | BindingFlags.Static);
            Assert.Null(accessor.GetMethodBody());

            Holder holder = new Holder(1);
            Assert.Equal(1, GetValue(holder));
            GetValue(holder) = 42;
            Assert.Equal(42, holder.Value);
        }

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "value")]
        private static extern ref int GetValue(Holder holder);

        private sealed class Holder
        {
            private int value;

            internal Holder(int value) => this.value = value;

            internal int Value => this.value;
        }
    }
}
#endif
