// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.TestingServices.Scheduling
{
    /// <summary>
    /// Interface for random number generators.
    /// </summary>
    public interface IRandomNumberGenerator
    {
        /// <summary>
        /// The seed currently used by the generator.
        /// </summary>
        int Seed { get; set; }

        /// <summary>
        /// Returns a non-negative random number.
        /// </summary>
        int Next();

        /// <summary>
        /// Returns a non-negative random number less than maxValue.
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound</param>
        int Next(int maxValue);
    }
}
