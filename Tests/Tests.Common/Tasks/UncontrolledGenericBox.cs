// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Tests.Common.Tasks
{
    /// <summary>
    /// An unrewritten generic container for testing caller-supplied type arguments.
    /// </summary>
    /// <typeparam name="T">The contained value type.</typeparam>
    public class UncontrolledGenericBox<T>
    {
        public T Value { get; }

        public UncontrolledGenericBox(T value)
        {
            this.Value = value;
        }

        public TResult Select<TResult>(Func<T, TResult> selector) => selector(this.Value);
    }
}
