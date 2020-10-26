// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Benchmarking
{
    internal struct Benchmark
    {
        internal readonly string Name;
        internal readonly Type Type;

        internal Benchmark(string name, Type type)
        {
            this.Name = name;
            this.Type = type;
        }
    }
}
