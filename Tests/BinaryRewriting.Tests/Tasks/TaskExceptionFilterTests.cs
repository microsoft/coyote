// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using Microsoft.Coyote.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BinaryRewriting.Tests.Tasks
{
    /// <summary>
    /// Tests that we can insert ExecutionCanceledException filter.
    /// </summary>
    public class TaskExceptionFilterTests : BaseProductionTest
    {
        public TaskExceptionFilterTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private static void TestFilterMethod()
        {
            // Test catch Exception
            try
            {
                throw new ExecutionCanceledException();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.GetType().FullName);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestAddExceptionFilter()
        {
            if (!this.IsSystematicTest)
            {
                // The non-rewritten code should catch the coyote exception.
                TestFilterMethod();
            }
            else
            {
                // The rewritten code should add a !(e is ExecutionCanceledException) filter
                // which should allow this exception to escape the catch block.
                this.RunWithException<ExecutionCanceledException>(TestFilterMethod);
            }
        }

        private static void TestFilterMethod2()
        {
            // Test catch RuntimeException
            try
            {
                throw new ExecutionCanceledException();
            }
            catch (RuntimeException ex)
            {
                Debug.WriteLine(ex.GetType().FullName);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestAddExceptionFilter2()
        {
            if (!this.IsSystematicTest)
            {
                // The non-rewritten code should catch the coyote exception..
                TestFilterMethod2();
            }
            else
            {
                // The rewritten code should add a !(e is ExecutionCanceledException) filter
                // which should allow this exception to escape the catch block.
                this.RunWithException<ExecutionCanceledException>(TestFilterMethod2);
            }
        }

        private static void TestFilterMethod3()
        {
            // Test catch all
            try
            {
                throw new ExecutionCanceledException();
            }
            catch
            {
                Debug.WriteLine("caught");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestAddExceptionFilter3()
        {
            if (!this.IsSystematicTest)
            {
                // The non-rewritten code should catch the coyote exception.
                TestFilterMethod3();
            }
            else
            {
                // The rewritten code should add a !(e is ExecutionCanceledException) filter
                // which should allow this exception to escape the catch block.
                this.RunWithException<ExecutionCanceledException>(TestFilterMethod3);
            }
        }

        private static void TestFilterMethod4()
        {
            // Test filter is unmodified if it is already correct!
            try
            {
                throw new ExecutionCanceledException();
            }
            catch (Exception ex) when (!(ex is ExecutionCanceledException))
            {
                Debug.WriteLine(ex.GetType().FullName);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestAddExceptionFilter4()
        {
            // The non-rewritten code should allow the ExecutionCanceledException through
            // and the rewritten code should be the same because the code should not be rewritten.
            this.RunWithException<ExecutionCanceledException>(TestFilterMethod4);
        }

        private static void TestFilterMethod5()
        {
            // Test more interesting filter is also unmodified if it is already correct!
            try
            {
                throw new ExecutionCanceledException();
            }
            catch (Exception ex) when (!(ex is NullReferenceException) && !(ex is ExecutionCanceledException))
            {
                Debug.WriteLine(ex.GetType().FullName);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestAddExceptionFilter5()
        {
            // The non-rewritten code should allow the ExecutionCanceledException through
            // and the rewritten code should be the same because the code should not be rewritten.
            this.RunWithException<ExecutionCanceledException>(TestFilterMethod5);
        }

        private static void TestFilterMethod6()
        {
            // Test more interesting filter is also unmodified if it is already correct!
            // Test we can parse a slightly different order of expressions in the filter.
            try
            {
                throw new ExecutionCanceledException();
            }
            catch (Exception ex) when (!(ex is ExecutionCanceledException) && !(ex is NullReferenceException))
            {
                Debug.WriteLine(ex.GetType().FullName);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestAddExceptionFilter6()
        {
            // The non-rewritten code should allow the ExecutionCanceledException through
            // and the rewritten code should be the same because the code should not be rewritten.
            this.RunWithException<ExecutionCanceledException>(TestFilterMethod6);
        }

        private static void TestComplexFilterMethod()
        {
            // This test case we cannot yet handle because filter is too complex.
            // This '|| debugging' expression causes the filter to catch ExecutionCanceledException
            // which is bad, but this is hard to fix.
            bool debugging = true;
            try
            {
                throw new ExecutionCanceledException();
            }
            catch (Exception ex) when (!(ex is ExecutionCanceledException) || debugging)
            {
                Debug.WriteLine(ex.GetType().FullName);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEditComplexFilter()
        {
            if (!this.IsSystematicTest)
            {
                // The non-rewritten code should catch the coyote exception..
                this.Test(TestComplexFilterMethod);
            }
            else
            {
                // The rewritten code should add a !(e is ExecutionCanceledException) filter
                // which should allow this exception to escape the catch block.
                this.RunWithException<ExecutionCanceledException>(TestComplexFilterMethod);
            }
        }

        private static void TestComplexFilterMethod2()
        {
            // This test case we cannot yet handle because filter is too complex.
            // This '&& debugging' expression causes the filter to catch ExecutionCanceledException
            // which is bad, but this is hard to fix.
            bool debugging = true;
            try
            {
                throw new ExecutionCanceledException();
            }
            catch (Exception ex) when (!(ex is NullReferenceException) && debugging)
            {
                Debug.WriteLine(ex.GetType().FullName);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEditComplexFilter2()
        {
            if (!this.IsSystematicTest)
            {
                // The non-rewritten code should catch the coyote exception..
                this.Test(TestComplexFilterMethod2);
            }
            else
            {
                // The rewritten code should add a !(e is ExecutionCanceledException) filter
                // which should allow this exception to escape the catch block.
                this.RunWithException<ExecutionCanceledException>(TestComplexFilterMethod2);
            }
        }

        private static void TestComplexFilterMethod3()
        {
            try
            {
                throw new ExecutionCanceledException();
            }
            catch (Exception ex) when (!(ex is NullReferenceException))
            {
                Debug.WriteLine(ex.GetType().FullName);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEditComplexFilter3()
        {
            if (!this.IsSystematicTest)
            {
                // The non-rewritten code should catch the coyote exception..
                this.Test(TestComplexFilterMethod3);
            }
            else
            {
                // The rewritten code should add a !(e is ExecutionCanceledException) filter
                // which should allow this exception to escape the catch block.
                this.RunWithException<ExecutionCanceledException>(TestComplexFilterMethod3);
            }
        }

        private static void TestComplexFilterMethod4()
        {
            // Test a crazy filter expression we cannot even parse...
            int x = 10;
            string suffix = "bad";
            try
            {
                Task.Run(() =>
                {
                    throw new ExecutionCanceledException();
                }).Wait();
            }
            catch (Exception ex) when (ex.GetType().Name != (x > 10 ? "Foo" : "Bar" + suffix))
            {
                Debug.WriteLine(ex.GetType().FullName);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEditComplexFilter4()
        {
            if (!this.IsSystematicTest)
            {
                // The non-rewritten code should catch the coyote exception.
                this.Test(TestComplexFilterMethod4);
            }
            else
            {
                // The rewritten code should add a !(e is ExecutionCanceledException) filter
                // which should allow this exception to escape the catch block.
                this.RunWithException<ExecutionCanceledException>(TestComplexFilterMethod4);
            }
        }

        private static void TestRethrowMethod()
        {
            // Test catch all, but it is ok because it does a rethrow,
            // so this code should be unmodified.
            try
            {
                throw new ExecutionCanceledException();
            }
            catch (Exception)
            {
                throw;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestIgnoreRethrowCase()
        {
            // The non-rewritten code should rethrow the exception
            // and the rewritten code should be the same because the code should not be rewritten.
            this.RunWithException<ExecutionCanceledException>(TestRethrowMethod);
        }

        private static void TestRethrowMethod2()
        {
            // Test catch all with specific filter for ExecutionCanceledException,
            // but it is ok because it does a rethrow, so this code should be unmodified.
            try
            {
                throw new ExecutionCanceledException();
            }
            catch (Exception ex) when (ex is ExecutionCanceledException)
            {
                throw;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestIgnoreRethrowCase2()
        {
            // The non-rewritten code should rethrow the exception
            // and the rewritten code should be the same because the code should not be rewritten.
            this.RunWithException<ExecutionCanceledException>(TestRethrowMethod2);
        }
    }
}
