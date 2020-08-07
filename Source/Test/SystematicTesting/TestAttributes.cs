// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Attribute for declaring the entry point to
    /// a Coyote program test.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestAttribute : Attribute
    {
    }

    /// <summary>
    /// Attribute for declaring the initialization
    /// method to be called before testing starts.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestInitAttribute : Attribute
    {
    }

    /// <summary>
    /// Attribute for declaring a cleanup method to be
    /// called when all test iterations terminate.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestDisposeAttribute : Attribute
    {
    }

    /// <summary>
    /// Attribute for declaring a cleanup method to be
    /// called when each test iteration terminates.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestIterationDisposeAttribute : Attribute
    {
    }

    /// <summary>
    /// Attribute for declaring the factory method that creates
    /// the Coyote testing runtime. This is an advanced feature,
    /// only to be used for replacing the original Coyote testing
    /// runtime with an alternative implementation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class TestRuntimeCreateAttribute : Attribute
    {
    }

    /// <summary>
    /// Attribute for monitoring when an assembly is rewritten
    /// by coyote.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TestRewrittenAttribute : Attribute
    {
        /// <summary>
        /// Whether assembly has been rewritten.
        /// </summary>
        public bool Rewritten;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRewrittenAttribute"/> class.
        /// </summary>
        public TestRewrittenAttribute(bool rewritten)
        {
            this.Rewritten = rewritten;
        }
    }
}
