// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BinaryRewriting.Tests.Tasks.Generics
{
    public class GenericTests : BaseProductionTest
    {
        public GenericTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public class GenericClass<T>
        {
            public async Task<T> RunTest(T arg)
            {
                await Task.Delay(Convert.ToInt32(arg));
                return arg;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestGenericClass()
        {
            this.Test(async () =>
            {
                var t = new GenericClass<float>();
                float delay = 3.1415f;
                var result = await t.RunTest(delay);
                Specification.Assert(result == delay, "Value is {0} instead of {1}.", result, delay);
            });
        }

        public class GenericNestedClass<T>
        {
            public class Body<TDelay>
            {
                public async Task<string> Foo(T format, TDelay arg)
                {
                    await Task.Delay(Convert.ToInt32(arg));
                    return string.Format(format.ToString(), arg);
                }
            }

            public async Task<string> RunTest(T arg, int delay)
            {
                var b = new Body<int>();
                return await b.Foo(arg, delay);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestGenericNestedClass()
        {
            this.Test(async () =>
            {
                var t = new GenericNestedClass<string>();
                int delay = 12;
                var result = await t.RunTest("Delay is {0}", delay);
                var expected = string.Format("Delay is {0}", delay);
                Specification.Assert(result == expected, "Value is {0} instead of {1}.", result, expected);
            });
        }

        public class GenericNestedMethod<T>
        {
            public class Body<TDelay>
            {
                public async Task<string> Foo<TStuff>(T format, TDelay arg, TStuff x)
                {
                    await Task.Delay(Convert.ToInt32(arg));
                    return string.Format(format.ToString(), arg, x);
                }
            }

            public async Task<string> RunTest<TStuff>(T arg, int delay, TStuff x)
            {
                var b = new Body<int>();
                return await b.Foo(arg, delay, x);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestGenericNestedMethod()
        {
            this.Test(async () =>
            {
                var t = new GenericNestedMethod<string>();
                int delay = 12;
                var today = DateTime.Today;
                var result = await t.RunTest<DateTime>("Delay is {0} on {1}", delay, today);
                var expected = string.Format("Delay is {0} on {1}", delay, today);
                Specification.Assert(result == expected, "Value is {0} instead of {1}.", result, expected);
            });
        }

        public class GenericGenericResult<T>
        {
            public class Body<TDelay>
            {
                public string Result;

                public async Task Foo<TStuff>(T format, TDelay arg, TStuff x)
                {
                    await Task.Delay(Convert.ToInt32(arg));
                    this.Result = string.Format(format.ToString(), arg, x);
                }
            }

            public async Task<Body<int>> RunTest<TStuff>(T arg, int delay, TStuff x)
            {
                var b = new Body<int>();
                await b.Foo(arg, delay, x);
                return b;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestGenericResult()
        {
            this.Test(async () =>
            {
                var t = new GenericGenericResult<string>();
                int delay = 12;
                var today = DateTime.Today;
                var result = await t.RunTest<DateTime>("Delay is {0} on {1}", delay, today);
                var expected = string.Format("Delay is {0} on {1}", delay, today);
                Specification.Assert(result.Result == expected, "Value is {0} instead of {1}.", result.Result, expected);
            });
        }
    }
}
