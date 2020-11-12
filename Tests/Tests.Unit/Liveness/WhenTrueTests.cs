// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Tests
{
    public class WhenTrueTests : BaseUnitTest
    {
        public WhenTrueTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public async Task TestWhenTrue()
        {
            SharedEntry entry = new SharedEntry();

            var task = Task.Run(async () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    if (i is 9)
                    {
                        entry.Value = 1;
                    }

                    await Task.Delay(2);
                }
            });

            await Specification.WhenTrue(() =>
            {
                if (entry.Value is 1)
                {
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            },
            () => entry.Value,
            TimeSpan.FromMilliseconds(2));

            await task;

            Assert.Equal(1, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestWhenTrueAsync()
        {
            SharedEntry entry = new SharedEntry();

            var task = Task.Run(async () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    if (i is 9)
                    {
                        entry.Value = 1;
                    }

                    await Task.Delay(2);
                }
            });

            await Specification.WhenTrue(async () =>
            {
                if (entry.Value is 1)
                {
                    return true;
                }

                await Task.Delay(2);
                return false;
            },
            () => entry.Value,
            TimeSpan.FromMilliseconds(2));

            await task;

            Assert.Equal(1, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestWhenTrueLongRunning()
        {
            SharedEntry entry = new SharedEntry();

            var task = Task.Run(async () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    await Task.Delay(2);
                    entry.Value++;
                }
            });

            await Specification.WhenTrue(async () =>
            {
                if (entry.Value is 10)
                {
                    return true;
                }

                await Task.Delay(2);
                return false;
            },
            () => entry.Value,
            TimeSpan.FromMilliseconds(2));

            await task;

            Assert.Equal(10, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenTrueCancellation()
        {
            Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                CancellationTokenSource tokenSource = new CancellationTokenSource(10);
                await Specification.WhenTrue(async () =>
                {
                    await Task.Delay(2);
                    return false;
                },
                () => 0,
                TimeSpan.FromMilliseconds(2),
                tokenSource.Token);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestWhenTrueWithException()
        {
            Assert.ThrowsAsync<NotSupportedException>(async () =>
            {
                await Specification.WhenTrue(() =>
                {
                    throw new NotSupportedException();
                },
                () => 0,
                TimeSpan.FromMilliseconds(2));
            });
        }
    }
}
