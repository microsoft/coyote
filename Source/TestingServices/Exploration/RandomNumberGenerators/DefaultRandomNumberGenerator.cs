// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.TestingServices.Scheduling
{
    /// <summary>
    /// Default random number generator that uses the <see cref="System.Random"/> generator.
    /// </summary>
    public class DefaultRandomNumberGenerator : IRandomNumberGenerator
    {
        /// <summary>
        /// Device for generating random numbers.
        /// </summary>
        private Random Random;

        /// <summary>
        /// The seed currently used by the generator.
        /// </summary>
        private int RandomSeed;

        /// <summary>
        /// The seed currently used by the generator.
        /// </summary>
        public int Seed
        {
            get => this.RandomSeed;

            set
            {
                this.RandomSeed = value;
                this.Random = new Random(this.RandomSeed);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRandomNumberGenerator"/> class.
        /// It uses a time-dependent seed.
        /// </summary>
        public DefaultRandomNumberGenerator()
        {
            this.RandomSeed = DateTime.Now.Millisecond;
            this.Random = new Random(this.RandomSeed);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRandomNumberGenerator"/> class.
        /// It uses the specified seed.
        /// </summary>
        public DefaultRandomNumberGenerator(int seed)
        {
            this.RandomSeed = seed;
            this.Random = new Random(seed);
        }

        /// <summary>
        /// Returns a non-negative random number.
        /// </summary>
        public int Next() => this.Random.Next();

        /// <summary>
        /// Returns a non-negative random number less than the specified max value.
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound.</param>
        public int Next(int maxValue) => this.Random.Next(maxValue);

        /// <summary>
        /// Returns a random floating-point number that is greater
        /// than or equal to 0.0, and less than 1.0.
        /// </summary>
        public double NextDouble() => this.Random.NextDouble();
    }
}
