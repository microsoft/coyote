// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Production.Tests
{
    public abstract class BaseProductionTest : BaseTest
    {
        public BaseProductionTest(ITestOutputHelper output)
            : base(output)
        {
        }

        protected void Run(Action<IActorRuntime> test, Configuration configuration = null)
        {
            configuration = configuration ?? GetConfiguration();

            TextWriter logger;
            if (configuration.IsVerbose)
            {
                logger = new TestOutputLogger(this.TestOutput, true);
            }
            else
            {
                logger = TextWriter.Null;
            }

            try
            {
                var runtime = RuntimeFactory.Create(configuration);
                runtime.SetLogger(logger);
                test(runtime);
            }
            catch (Exception ex)
            {
                Assert.False(true, ex.Message + "\n" + ex.StackTrace);
            }
            finally
            {
                logger.Dispose();
            }
        }

        protected async Task RunAsync(Func<IActorRuntime, Task> test, Configuration configuration = null)
        {
            configuration = configuration ?? GetConfiguration();

            TextWriter logger;
            if (configuration.IsVerbose)
            {
                logger = new TestOutputLogger(this.TestOutput, true);
            }
            else
            {
                logger = TextWriter.Null;
            }

            try
            {
                var runtime = RuntimeFactory.Create(configuration);
                runtime.SetLogger(logger);
                await test(runtime);
            }
            catch (Exception ex)
            {
                Assert.False(true, ex.Message + "\n" + ex.StackTrace);
            }
            finally
            {
                logger.Dispose();
            }
        }

        protected static async Task WaitAsync(Task task, int millisecondsDelay = 5000)
        {
            await Task.WhenAny(task, Task.Delay(millisecondsDelay));
            Assert.True(task.IsCompleted);
        }

        protected static async Task<TResult> GetResultAsync<TResult>(Task<TResult> task, int millisecondsDelay = 5000)
        {
            await Task.WhenAny(task, Task.Delay(millisecondsDelay));
            Assert.True(task.IsCompleted);
            return await task;
        }

        protected static Configuration GetConfiguration()
        {
            return Configuration.Create();
        }
    }
}
