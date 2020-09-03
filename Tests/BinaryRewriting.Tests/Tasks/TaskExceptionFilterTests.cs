// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
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

        private static void TestConditionalTryCatchMethod()
        {
            // Test conditional branch around try/catch is fixed up when the rewritten code
            // causes this branch instruction to have to be modified from brtrue_s to brtrue.
            StringBuilder sb = new StringBuilder();
            bool something = true;
            if (something)
            {
                try
                {
                    sb.AppendLine(string.Format("This is the try block {0}, {1}", "foo", 123));
                    throw new InvalidOperationException();
                }
                catch (Exception ex)
                {
                    sb.AppendLine(string.Format("This is the catch block {0}, {1}", ex.Message, 123));
                    if (ex.InnerException != null)
                    {
                        sb.AppendLine(string.Format("This is the inner exception {0}, {1}", ex.InnerException.Message, 123));
                    }

                    throw;
                }
            }
        }

        [Fact(Timeout = 5000)]
        public void TestConditionalTryCatch()
        {
            this.RunWithException<InvalidOperationException>(TestConditionalTryCatchMethod);
        }

        private static void TestMultiCatchBlockMethod()
        {
            // Test we can handle multiple catch blocks.
            try
            {
                throw new InvalidOperationException();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception)
            {
                Console.WriteLine("exception handled");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMultiCatchBlock()
        {
            this.RunWithException<InvalidOperationException>(TestMultiCatchBlockMethod);
        }

        private static void TestMultiCatchFilterMethod()
        {
            // Test we can handle multiple catch blocks with a filter.
            try
            {
                throw new InvalidOperationException();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception e) when (!(e is NullReferenceException))
            {
                Console.WriteLine("exception handled");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMultiCatchFilter()
        {
            this.RunWithException<InvalidOperationException>(TestMultiCatchFilterMethod);
        }

        private static void TestMultiCatchBlockWithFilterMethod()
        {
            // Test we can handle multiple catch blocks with a filter.
            try
            {
                throw new InvalidOperationException();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception e) when (!(e is NullReferenceException))
            {
                Console.WriteLine("exception handled");
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("Don't care!");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMultiCatchBlockWithFilter()
        {
            this.RunWithException<InvalidOperationException>(TestMultiCatchBlockWithFilterMethod);
        }

        private static void TestExceptionHandlerInsideLockMethod()
        {
            object l = new object();

            lock (l)
            {
                try
                {
                    throw new InvalidOperationException();
                }
                catch (Exception)
                {
                    Console.WriteLine("exception handled");
                    throw;
                }
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionHandlerInsideLock()
        {
            this.RunWithException<InvalidOperationException>(TestExceptionHandlerInsideLockMethod);
        }

        private static void TestTryUsingTryMethod()
        {
            try
            {
                using (var s = new StringWriter())
                {
                    try
                    {
                        throw new InvalidOperationException();
                    }
                    catch (Exception)
                    {
                        s.Close();
                        throw;
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("exception handled");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestTryUsingTry()
        {
            this.Test(TestTryUsingTryMethod);
        }
    }
}
