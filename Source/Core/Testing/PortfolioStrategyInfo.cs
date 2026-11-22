// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Testing
{
    /// <summary>
    /// The settings of an exploration strategy in the initial portfolio.
    /// </summary>
    internal sealed class PortfolioStrategyInfo
    {
        /// <summary>
        /// The name of the strategy.
        /// </summary>
        public string Strategy { get; }

        /// <summary>
        /// The bound used by this strategy, if any.
        /// </summary>
        public int? StrategyBound { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PortfolioStrategyInfo"/> class.
        /// </summary>
        internal PortfolioStrategyInfo(Strategy strategy)
        {
            this.Strategy = strategy.GetName();
            this.StrategyBound = strategy.GetStrategyBound();
        }
    }
}
