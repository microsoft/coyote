﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

using Common = Microsoft.Coyote.Tests.Common;

namespace Microsoft.Coyote.Core.Tests
{
    public abstract class BaseTest : Common.BaseTest
    {
        public BaseTest(ITestOutputHelper output)
            : base(output)
        {
        }

        protected void Run(Action<ICoyoteRuntime> test, Configuration configuration = null)
        {
            configuration = configuration ?? GetConfiguration();

            ILogger logger;
            if (configuration.IsVerbose)
            {
                logger = new TestOutputLogger(this.TestOutput, true);
            }
            else
            {
                logger = new NulLogger();
            }

            try
            {
                var runtime = CoyoteRuntime.Create(configuration);
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

        protected async Task RunAsync(Func<ICoyoteRuntime, Task> test, Configuration configuration = null)
        {
            configuration = configuration ?? GetConfiguration();

            ILogger logger;
            if (configuration.IsVerbose)
            {
                logger = new TestOutputLogger(this.TestOutput, true);
            }
            else
            {
                logger = new NulLogger();
            }

            try
            {
                var runtime = CoyoteRuntime.Create(configuration);
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
