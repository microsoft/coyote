// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests.Exceptions
{
    /// <summary>
    /// Tests that we can insert an <see cref="ThreadInterruptedException"/> filter.
    /// </summary>
    public class TaskExceptionFilterTests : BaseRewritingTest
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
                throw new ThreadInterruptedException();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.GetType().FullName);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestAddExceptionFilter()
        {
            // The rewritten code should add a !(e is ThreadInterruptedException) filter
            // which should allow this exception to escape the catch block.
            this.RunTestWithException<ThreadInterruptedException>(TestFilterMethod);
        }

        private static void TestFilterMethod2()
        {
            // Test catch RuntimeException
            try
            {
                throw new ThreadInterruptedException();
            }
            catch (RuntimeException ex)
            {
                Debug.WriteLine(ex.GetType().FullName);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestAddExceptionFilter2()
        {
            // The rewritten code should add a !(e is ThreadInterruptedException) filter
            // which should allow this exception to escape the catch block.
            this.RunTestWithException<ThreadInterruptedException>(TestFilterMethod2);
        }

        private static void TestFilterMethod3()
        {
            // Test catch all
            try
            {
                throw new ThreadInterruptedException();
            }
            catch
            {
                Debug.WriteLine("caught");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestAddExceptionFilter3()
        {
            // The rewritten code should add a !(e is ThreadInterruptedException) filter
            // which should allow this exception to escape the catch block.
            this.RunTestWithException<ThreadInterruptedException>(TestFilterMethod3);
        }

        private static void TestFilterMethod4()
        {
            // Test filter is unmodified if it is already correct!
            try
            {
                throw new ThreadInterruptedException();
            }
            catch (Exception ex) when (!(ex is ThreadInterruptedException))
            {
                Debug.WriteLine(ex.GetType().FullName);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestAddExceptionFilter4()
        {
            // The non-rewritten code should allow the ThreadInterruptedException through
            // and the rewritten code should be the same because the code should not be rewritten.
            this.RunTestWithException<ThreadInterruptedException>(TestFilterMethod4);
        }

        private static void TestFilterMethod5()
        {
            // Test more interesting filter is also unmodified if it is already correct!
            try
            {
                throw new ThreadInterruptedException();
            }
            catch (Exception ex) when (!(ex is NullReferenceException) && !(ex is ThreadInterruptedException))
            {
                Debug.WriteLine(ex.GetType().FullName);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestAddExceptionFilter5()
        {
            // The non-rewritten code should allow the ThreadInterruptedException through
            // and the rewritten code should be the same because the code should not be rewritten.
            this.RunTestWithException<ThreadInterruptedException>(TestFilterMethod5);
        }

        private static void TestFilterMethod6()
        {
            // Test more interesting filter is also unmodified if it is already correct!
            // Test we can parse a slightly different order of expressions in the filter.
            try
            {
                throw new ThreadInterruptedException();
            }
            catch (Exception ex) when (!(ex is ThreadInterruptedException) && !(ex is NullReferenceException))
            {
                Debug.WriteLine(ex.GetType().FullName);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestAddExceptionFilter6()
        {
            // The non-rewritten code should allow the ThreadInterruptedException through
            // and the rewritten code should be the same because the code should not be rewritten.
            this.RunTestWithException<ThreadInterruptedException>(TestFilterMethod6);
        }

        private static void TestComplexFilterMethod()
        {
            // This test case we cannot yet handle because filter is too complex.
            // This '|| debugging' expression causes the filter to catch ThreadInterruptedException
            // which is bad, but this is hard to fix.
            bool debugging = true;
            try
            {
                throw new ThreadInterruptedException();
            }
            catch (Exception ex) when (!(ex is ThreadInterruptedException) || debugging)
            {
                Debug.WriteLine(ex.GetType().FullName);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEditComplexFilter()
        {
            // The rewritten code should add a !(e is ThreadInterruptedException) filter
            // which should allow this exception to escape the catch block.
            this.RunTestWithException<ThreadInterruptedException>(TestComplexFilterMethod);
        }

        private static void TestComplexFilterMethod2()
        {
            // This test case we cannot yet handle because filter is too complex.
            // This '&& debugging' expression causes the filter to catch ThreadInterruptedException
            // which is bad, but this is hard to fix.
            bool debugging = true;
            try
            {
                throw new ThreadInterruptedException();
            }
            catch (Exception ex) when (!(ex is NullReferenceException) && debugging)
            {
                Debug.WriteLine(ex.GetType().FullName);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEditComplexFilter2()
        {
            // The rewritten code should add a !(e is ThreadInterruptedException) filter
            // which should allow this exception to escape the catch block.
            this.RunTestWithException<ThreadInterruptedException>(TestComplexFilterMethod2);
        }

        private static void TestComplexFilterMethod3()
        {
            try
            {
                throw new ThreadInterruptedException();
            }
            catch (Exception ex) when (!(ex is NullReferenceException))
            {
                Debug.WriteLine(ex.GetType().FullName);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEditComplexFilter3()
        {
            // The rewritten code should add a !(e is ThreadInterruptedException) filter
            // which should allow this exception to escape the catch block.
            this.RunTestWithException<ThreadInterruptedException>(TestComplexFilterMethod3);
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
                    throw new ThreadInterruptedException();
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
            // The rewritten code should add a !(e is ThreadInterruptedException) filter
            // which should allow this exception to escape the catch block.
            this.RunTestWithException<ThreadInterruptedException>(TestComplexFilterMethod4);
        }

        private static void TestRethrowMethod()
        {
            // Test catch all, but it is ok because it does a rethrow,
            // so this code should be unmodified.
            try
            {
                throw new ThreadInterruptedException();
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
            this.RunTestWithException<ThreadInterruptedException>(TestRethrowMethod);
        }

        private static void TestRethrowMethod2()
        {
            // Test catch all with specific filter for ThreadInterruptedException,
            // but it is ok because it does a rethrow, so this code should be unmodified.
            try
            {
                throw new ThreadInterruptedException();
            }
            catch (Exception ex) when (ex is ThreadInterruptedException)
            {
                throw;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestIgnoreRethrowCase2()
        {
            // The non-rewritten code should rethrow the exception
            // and the rewritten code should be the same because the code should not be rewritten.
            this.RunTestWithException<ThreadInterruptedException>(TestRethrowMethod2);
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
            this.RunTestWithException<InvalidOperationException>(TestConditionalTryCatchMethod);
        }

        private static void TestMultiCatchBlockMethod()
        {
            // Test we can handle multiple catch blocks.
            bool exceptionHandled = false;
            try
            {
                throw new InvalidOperationException();
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                exceptionHandled = true;
                throw;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Assert.True(exceptionHandled, "Exception was not handled.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMultiCatchBlock()
        {
            this.RunTestWithException<InvalidOperationException>(TestMultiCatchBlockMethod);
        }

        private static void TestMultiCatchFilterMethod()
        {
            // Test we can handle multiple catch blocks with a filter.
            bool exceptionHandled = false;
            try
            {
                throw new InvalidOperationException();
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (Exception e) when (!(e is NotSupportedException))
            {
                exceptionHandled = true;
                throw;
            }
            finally
            {
                Assert.True(exceptionHandled, "Exception was not handled.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMultiCatchFilter()
        {
            this.RunTestWithException<InvalidOperationException>(TestMultiCatchFilterMethod);
        }

        private static void TestMultiCatchBlockWithFilterMethod()
        {
            // Test we can handle multiple catch blocks with a filter.
            bool exceptionHandled = false;
            try
            {
                throw new InvalidOperationException();
            }
            catch (NullReferenceException)
            {
                throw;
            }
            catch (Exception e) when (!(e is NullReferenceException))
            {
                exceptionHandled = true;
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            finally
            {
                Assert.True(exceptionHandled, "Exception was not handled.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMultiCatchBlockWithFilter()
        {
            this.RunTestWithException<InvalidOperationException>(TestMultiCatchBlockWithFilterMethod);
        }

        private static void TestExceptionHandlerInsideLockMethod()
        {
            object l = new object();
            lock (l)
            {
                bool exceptionHandled = false;
                try
                {
                    throw new InvalidOperationException();
                }
                catch (Exception)
                {
                    exceptionHandled = true;
                    throw;
                }
                finally
                {
                    Assert.True(exceptionHandled, "Exception was not handled.");
                }
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExceptionHandlerInsideLock()
        {
            this.RunTestWithException<InvalidOperationException>(TestExceptionHandlerInsideLockMethod);
        }

        private static void TestTryUsingTryMethod()
        {
            bool exceptionHandled = false;
            try
            {
                using var s = new StringWriter();
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
            catch (Exception)
            {
                exceptionHandled = true;
            }
            finally
            {
                Assert.True(exceptionHandled, "Exception was not handled.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestTryUsingTry()
        {
            this.Test(TestTryUsingTryMethod);
        }
    }
}
