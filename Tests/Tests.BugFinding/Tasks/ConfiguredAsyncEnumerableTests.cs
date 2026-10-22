// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
#if NET
    public class ConfiguredAsyncEnumerableTests : BaseBugFindingTest
    {
        public ConfiguredAsyncEnumerableTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestWithCancellation(bool asynchronous)
        {
            this.Test(async () =>
            {
                SharedEntry disposed = new SharedEntry();
                int sum = 0;
                await foreach (int item in EnumerateAsync(disposed, asynchronous).WithCancellation(default))
                {
                    sum += item;
                }

                Assert.Equal(3, sum);
                Assert.Equal(1, disposed.Value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(50));
        }

        [Theory(Timeout = 5000)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void TestConfigureAwait(bool asynchronous, bool continueOnCapturedContext)
        {
            this.Test(async () =>
            {
                SharedEntry disposed = new SharedEntry();
                int sum = 0;
                await foreach (int item in EnumerateAsync(disposed, asynchronous).ConfigureAwait(continueOnCapturedContext))
                {
                    sum += item;
                }

                Assert.Equal(3, sum);
                Assert.Equal(1, disposed.Value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(50));
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestChainedConfigurationAndCancellation(bool cancellationFirst)
        {
            this.Test(async () =>
            {
                using var source = new CancellationTokenSource();
                using var replacedSource = new CancellationTokenSource();
                replacedSource.Cancel();
                SharedEntry disposed = new SharedEntry();
                var items = cancellationFirst ?
                    EnumerateAsync(disposed, true).WithCancellation(replacedSource.Token).ConfigureAwait(false) :
                    EnumerateAsync(disposed, true).ConfigureAwait(false).WithCancellation(replacedSource.Token);
                items = items.ConfigureAwait(true).WithCancellation(source.Token);
                int count = 0;
                bool canceled = false;
                try
                {
                    await foreach (int item in items)
                    {
                        count++;
                        source.Cancel();
                    }
                }
                catch (OperationCanceledException exception)
                {
                    Assert.Equal(source.Token, exception.CancellationToken);
                    canceled = true;
                }

                Assert.True(canceled);
                Assert.Equal(1, count);
                Assert.Equal(1, disposed.Value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(50));
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestConfiguredAsyncDisposal(bool continueOnCapturedContext)
        {
            this.Test(async () =>
            {
                SharedEntry disposed = new SharedEntry();
                await using (new AsyncDisposable(disposed).ConfigureAwait(continueOnCapturedContext))
                {
                    Assert.Equal(0, disposed.Value);
                }

                Assert.Equal(1, disposed.Value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(50));
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestEarlyExitDisposesEnumerator(bool continueOnCapturedContext)
        {
            this.Test(async () =>
            {
                SharedEntry disposed = new SharedEntry();
                await foreach (int item in EnumerateAsync(disposed, true).ConfigureAwait(continueOnCapturedContext))
                {
                    Assert.Equal(1, item);
                    break;
                }

                Assert.Equal(1, disposed.Value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(50));
        }

        [Fact(Timeout = 5000)]
        public void TestConfiguredEnumerationFailureReplay()
        {
            this.TestWithError(async () =>
            {
                SharedEntry disposed = new SharedEntry();
                await foreach (int item in EnumerateAsync(disposed, true).WithCancellation(default).ConfigureAwait(false))
                {
                    Specification.Assert(item != 2, "Reached the second item.");
                }
            },
            configuration: this.GetConfiguration().WithTestingIterations(50),
            expectedError: "Reached the second item.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestConfiguredEnumerableInGenericConstructor()
        {
            using var cancellation = new CancellationTokenSource();
            var source = new EmptyEnumerable();
            var box = new Box<ConfiguredCancelableAsyncEnumerable<int>>(source.WithCancellation(cancellation.Token));
            Assert.NotNull(box);
            box.Value.GetAsyncEnumerator();
            Assert.Equal(cancellation.Token, source.ObservedToken);
        }

        [Fact(Timeout = 5000)]
        public void TestConfiguredEnumeratorInGenericConstructor()
        {
            var source = new EmptyEnumerable();
            var enumerator = source.WithCancellation(default).GetAsyncEnumerator();
            var box = new Box<ConfiguredCancelableAsyncEnumerable<int>.Enumerator>(enumerator);
            Assert.NotNull(box);
            Assert.Equal(0, box.Value.Current);
            box.Value.DisposeAsync().GetAwaiter().GetResult();
            Assert.True(source.IsDisposed);
        }

        [Fact(Timeout = 5000)]
        public void TestConfiguredDisposableInGenericConstructor()
        {
            var source = new EmptyEnumerable();
            var box = new Box<ConfiguredAsyncDisposable>(((IAsyncDisposable)source).ConfigureAwait(false));
            Assert.NotNull(box);
            box.Value.DisposeAsync().GetAwaiter().GetResult();
            Assert.True(source.IsDisposed);
        }

        [Fact(Timeout = 5000)]
        public void TestConfigurationMethodGroup()
        {
            using var cancellation = new CancellationTokenSource();
            var source = new EmptyEnumerable();
            Func<IAsyncEnumerable<int>, CancellationToken, ConfiguredCancelableAsyncEnumerable<int>> configure =
                TaskAsyncEnumerableExtensions.WithCancellation<int>;
            configure(source, cancellation.Token).GetAsyncEnumerator();
            Assert.Equal(cancellation.Token, source.ObservedToken);
        }

        [Fact(Timeout = 5000)]
        public void TestCustomConfigurationMethodGroup()
        {
            using var cancellation = new CancellationTokenSource();
            var source = new EmptyEnumerable();
            ConfigureCallback configure = TaskAsyncEnumerableExtensions.WithCancellation<int>;
            configure(source, cancellation.Token).GetAsyncEnumerator();
            Assert.Equal(cancellation.Token, source.ObservedToken);
        }

        private static async IAsyncEnumerable<int> EnumerateAsync(SharedEntry disposed, bool asynchronous,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            try
            {
                for (int i = 1; i <= 2; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (asynchronous)
                    {
                        await Task.Delay(1);
                    }

                    yield return i;
                }
            }
            finally
            {
                if (asynchronous)
                {
                    await Task.Delay(1);
                }

                disposed.Value++;
            }
        }

        private sealed class AsyncDisposable : IAsyncDisposable
        {
            private readonly SharedEntry Disposed;

            internal AsyncDisposable(SharedEntry disposed)
            {
                this.Disposed = disposed;
            }

            public async ValueTask DisposeAsync()
            {
                await Task.Delay(1);
                this.Disposed.Value++;
            }
        }

        private delegate ConfiguredCancelableAsyncEnumerable<int> ConfigureCallback(
            IAsyncEnumerable<int> source, CancellationToken cancellationToken);

        private sealed class Box<T>
        {
            internal T Value { get; }

            internal Box(T value)
            {
                this.Value = value;
            }
        }

        private sealed class EmptyEnumerable : IAsyncEnumerable<int>, IAsyncEnumerator<int>
        {
            public int Current => 0;

            internal CancellationToken ObservedToken { get; private set; }

            internal bool IsDisposed { get; private set; }

            public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                this.ObservedToken = cancellationToken;
                return this;
            }

            public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(false);

            public ValueTask DisposeAsync()
            {
                this.IsDisposed = true;
                return default;
            }
        }
    }
#endif
}
