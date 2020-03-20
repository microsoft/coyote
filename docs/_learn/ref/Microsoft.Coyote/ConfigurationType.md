---
layout: reference
section: learn
title: Configuration
permalink: /learn/ref/Microsoft.Coyote/ConfigurationType
---
# Configuration class

The Coyote project configurations.

```csharp
public class Configuration
```

## Public Members

| name | description |
| --- | --- |
| static [Create](Configuration/Create)() | Creates a new configuration with default values. |
| [IsDgmlGraphEnabled](Configuration/IsDgmlGraphEnabled) { get; } | If specified, requests a DGML graph of the iteration that contains a bug, if a bug is found. This is different from a coverage activity graph, as it will also show actor instances. |
| [IsVerbose](Configuration/IsVerbose) { get; } | If true, then messages are logged. |
| [IsXmlLogEnabled](Configuration/IsXmlLogEnabled) { get; } | Produce an XML formatted runtime log file. |
| [LivenessTemperatureThreshold](Configuration/LivenessTemperatureThreshold) { get; } | The liveness temperature threshold. If it is 0 then it is disabled. |
| [MaxFairSchedulingSteps](Configuration/MaxFairSchedulingSteps) { get; } | The maximum scheduling steps to explore for fair schedulers. By default this is set to 100,000 steps. |
| [MaxUnfairSchedulingSteps](Configuration/MaxUnfairSchedulingSteps) { get; } | The maximum scheduling steps to explore for unfair schedulers. By default this is set to 10,000 steps. |
| [RandomGeneratorSeed](Configuration/RandomGeneratorSeed) { get; } | Custom seed to be used by the random value generator. By default, this value is null indicating that no seed has been set. |
| [ReportActivityCoverage](Configuration/ReportActivityCoverage) { get; } | Enables activity coverage reporting of a Coyote program. |
| [SchedulingStrategy](Configuration/SchedulingStrategy) { get; } | The systematic testing strategy to use. |
| [StrategyBound](Configuration/StrategyBound) { get; } | A strategy-specific bound. |
| [TestingIterations](Configuration/TestingIterations) { get; } | Number of testing iterations. |
| [TimeoutDelay](Configuration/TimeoutDelay) { get; } | Value that controls the probability of triggering a timeout each time a built-in timer is scheduled during systematic testing. Decrease the value to increase the frequency of timeouts (e.g. a value of 1 corresponds to a 50% probability), or increase the value to decrease the frequency (e.g. a value of 10 corresponds to a 10% probability). By default this value is 10. |
| [WithActivityCoverageEnabled](Configuration/WithActivityCoverageEnabled)(…) | Updates the configuration with activity coverage enabled or disabled. |
| [WithDgmlGraphEnabled](Configuration/WithDgmlGraphEnabled)(…) | Updates the configuration with DGML graph generation enabled or disabled. |
| [WithLivenessTemperatureThreshold](Configuration/WithLivenessTemperatureThreshold)(…) | Updates the configuration with the specified liveness temperature threshold during systematic testing. If this value is 0 it disables liveness checking. |
| [WithMaxFairSchedulingSteps](Configuration/WithMaxFairSchedulingSteps)(…) | Updates the configuration with the specified number of fair scheduling steps to explore per iteration during systematic testing. |
| [WithMaxSchedulingSteps](Configuration/WithMaxSchedulingSteps)(…) | Updates the configuration with the specified number of scheduling steps to explore per iteration (for both fair and unfair schedulers) during systematic testing. |
| [WithMaxUnfairSchedulingSteps](Configuration/WithMaxUnfairSchedulingSteps)(…) | Updates the configuration with the specified number of unfair scheduling steps to explore per iteration during systematic testing. |
| [WithPCTStrategy](Configuration/WithPCTStrategy)(…) | Updates the configuration to use the PCT scheduling strategy during systematic testing. You can specify the number of priority switch points, which by default are 10. |
| [WithProbabilisticStrategy](Configuration/WithProbabilisticStrategy)(…) | Updates the configuration to use the probabilistic scheduling strategy during systematic testing. You can specify a value controlling the probability of each scheduling decision. This value is specified as the integer N in the equation 0.5 to the power of N. So for N=1, the probability is 0.5, for N=2 the probability is 0.25, N=3 you get 0.125, etc. By default, this value is 3. |
| [WithRandomGeneratorSeed](Configuration/WithRandomGeneratorSeed)(…) | Updates the seed used by the random value generator during systematic testing. |
| [WithRandomStrategy](Configuration/WithRandomStrategy)() | Updates the configuration to use the random scheduling strategy during systematic testing. |
| [WithTestingIterations](Configuration/WithTestingIterations)(…) | Updates the configuration with the specified number of iterations to run during systematic testing. |
| [WithTimeoutDelay](Configuration/WithTimeoutDelay)(…) | Updates the [`TimeoutDelay`](Configuration/TimeoutDelay) value that controls the probability of triggering a timeout each time a built-in timer is scheduled during systematic testing. This value is not a unit of time. |
| [WithVerbosityEnabled](Configuration/WithVerbosityEnabled)(…) | Updates the configuration with verbose output enabled or disabled. |
| [WithXmlLogEnabled](Configuration/WithXmlLogEnabled)(…) | Updates the configuration with XML log generation enabled or disabled. |

## Protected Members

| name | description |
| --- | --- |
| [Configuration](Configuration/Configuration)() | Initializes a new instance of the [`Configuration`](ConfigurationType) class. |

## See Also

* namespace [Microsoft.Coyote](../MicrosoftCoyoteNamespace)
* assembly [Microsoft.Coyote](../MicrosoftCoyoteAssembly)

<!-- DO NOT EDIT: generated by xmldocmd for Microsoft.Coyote.dll -->
