// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Coyote.Testing
{
    /// <summary>
    /// The enabled portfolio mode during testing.
    /// </summary>
    internal enum PortfolioMode
    {
        /// <summary>
        /// Portfolio mode is disabled.
        /// </summary>
        None = 0,

        /// <summary>
        /// Fair portfolio mode is enabled.
        /// </summary>
        Fair,

        /// <summary>
        /// Unfair portfolio mode is enabled.
        /// </summary>
        Unfair
    }

    /// <summary>
    /// Extension methods for the <see cref="PortfolioMode"/>.
    /// </summary>
    internal static class PortfolioModeExtensions
    {
        /// <summary>
        /// Returns true if the <see cref="PortfolioMode"/> is enabled, else false.
        /// </summary>
        internal static bool IsEnabled(this PortfolioMode mode) => mode != PortfolioMode.None;

        /// <summary>
        /// Returns true if the <see cref="PortfolioMode"/> is fair, else false.
        /// </summary>
        internal static bool IsFair(this PortfolioMode mode) => mode is PortfolioMode.Fair;

        /// <summary>
        /// Returns the <see cref="PortfolioMode"/> from the specified string.
        /// </summary>
        internal static PortfolioMode FromString(string mode) => mode switch
            {
                "none" => PortfolioMode.None,
                "fair" => PortfolioMode.Fair,
                "unfair" => PortfolioMode.Unfair,
                _ => throw new ArgumentOutOfRangeException($"The mode '{mode}' is not expected.")
            };
    }
}
