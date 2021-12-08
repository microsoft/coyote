// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class ArrayTypeRewritingTests : BaseRewritingTest
    {
        public ArrayTypeRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestEmptyObjectArrayRewriting()
        {
            _ = System.Array.Empty<object>();
        }

        [Fact(Timeout = 5000)]
        public void TestEmptyNestedObjectArrayRewriting()
        {
            _ = System.Array.Empty<object[]>();
        }

        [Fact(Timeout = 5000)]
        public void TestEmptyTaskRewriting()
        {
            _ = System.Array.Empty<Task>();
        }

        [Fact(Timeout = 5000)]
        public void TestEmptyGenericTaskRewriting()
        {
            _ = System.Array.Empty<Task<int>>();
        }

        [Fact(Timeout = 5000)]
        public void TestEmptyGenericTaskWithTaskAwaiterRewriting()
        {
            _ = System.Array.Empty<Task<TaskAwaiter>>();
        }

        [Fact(Timeout = 5000)]
        public void TestEmptyGenericTaskWithGenericTaskAwaiterRewriting()
        {
            _ = System.Array.Empty<Task<TaskAwaiter<int>>>();
        }

        [Fact(Timeout = 5000)]
        public void TestEmptyTaskAwaiterRewriting()
        {
            _ = System.Array.Empty<TaskAwaiter>();
        }

        [Fact(Timeout = 5000)]
        public void TestEmptyGenericTaskAwaiterRewriting()
        {
            _ = System.Array.Empty<TaskAwaiter<int>>();
        }

        [Fact(Timeout = 5000)]
        public void TestEmptyGenericTaskAwaiterWithTaskRewriting()
        {
            _ = System.Array.Empty<TaskAwaiter<Task>>();
        }

        [Fact(Timeout = 5000)]
        public void TestEmptyGenericTaskAwaiterWithGenericTaskRewriting()
        {
            _ = System.Array.Empty<TaskAwaiter<Task<int>>>();
        }
    }
}
