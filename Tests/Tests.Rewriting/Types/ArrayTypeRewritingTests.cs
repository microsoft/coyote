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
            this.Test(() =>
            {
                _ = System.Array.Empty<object>();
            });
        }

        [Fact(Timeout = 5000)]
        public void TestEmptyNestedObjectArrayRewriting()
        {
            this.Test(() =>
            {
                _ = System.Array.Empty<object[]>();
            });
        }

        [Fact(Timeout = 5000)]
        public void TestEmptyTaskRewriting()
        {
            this.Test(() =>
            {
                _ = System.Array.Empty<Task>();
            });
        }

        [Fact(Timeout = 5000)]
        public void TestEmptyGenericTaskRewriting()
        {
            this.Test(() =>
            {
                _ = System.Array.Empty<Task<int>>();
            });
        }

        [Fact(Timeout = 5000)]
        public void TestEmptyGenericTaskWithTaskAwaiterRewriting()
        {
            this.Test(() =>
            {
                _ = System.Array.Empty<Task<TaskAwaiter>>();
            });
        }

        [Fact(Timeout = 5000)]
        public void TestEmptyGenericTaskWithGenericTaskAwaiterRewriting()
        {
            this.Test(() =>
            {
                _ = System.Array.Empty<Task<TaskAwaiter<int>>>();
            });
        }

        [Fact(Timeout = 5000)]
        public void TestEmptyTaskAwaiterRewriting()
        {
            this.Test(() =>
            {
                _ = System.Array.Empty<TaskAwaiter>();
            });
        }

        [Fact(Timeout = 5000)]
        public void TestEmptyGenericTaskAwaiterRewriting()
        {
            this.Test(() =>
            {
                _ = System.Array.Empty<TaskAwaiter<int>>();
            });
        }

        [Fact(Timeout = 5000)]
        public void TestEmptyGenericTaskAwaiterWithTaskRewriting()
        {
            this.Test(() =>
            {
                _ = System.Array.Empty<TaskAwaiter<Task>>();
            });
        }

        [Fact(Timeout = 5000)]
        public void TestEmptyGenericTaskAwaiterWithGenericTaskRewriting()
        {
            this.Test(() =>
            {
                _ = System.Array.Empty<TaskAwaiter<Task<int>>>();
            });
        }
    }
}
