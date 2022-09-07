# Configuration class

The Coyote runtime and testing configuration.

```csharp
public class Configuration
```

## Public Members

| name | description |
| --- | --- |
| static [Create](Configuration/Create.md)() | Creates a new configuration with default values. |
| [DeadlockTimeout](Configuration/DeadlockTimeout.md) { get; } | Value that controls how much time the deadlock monitor should wait during concurrency testing before reporting a potential deadlock. This value is in milliseconds. |
| [IsVerbose](Configuration/IsVerbose.md) { get; } | If true, then messages are logged. |
| [LogLevel](Configuration/LogLevel.md) { get; } | The level of detail to provide in verbose logging. |
| [MaxFairSchedulingSteps](Configuration/MaxFairSchedulingSteps.md) { get; } | The maximum scheduling steps to explore for fair schedulers. By default this is set to 100,000 steps. |
| [MaxUnfairSchedulingSteps](Configuration/MaxUnfairSchedulingSteps.md) { get; } | The maximum scheduling steps to explore for unfair schedulers. By default this is set to 10,000 steps. |
| [RandomGeneratorSeed](Configuration/RandomGeneratorSeed.md) { get; } | Custom seed to be used by the random value generator. By default, this value is null indicating that no seed has been set. |
| [SchedulingStrategy](Configuration/SchedulingStrategy.md) { get; } | The systematic testing strategy to use. |
| [TestingIterations](Configuration/TestingIterations.md) { get; } | Number of testing iterations. |
| [TimeoutDelay](Configuration/TimeoutDelay.md) { get; } | Value that controls the probability of triggering a timeout each time an operation gets delayed or a built-in timer gets scheduled during systematic testing. Decrease the value to increase the frequency of timeouts (e.g. a value of 1 corresponds to a 50% probability), or increase the value to decrease the frequency (e.g. a value of 10 corresponds to a 10% probability). |
| [WithActivityCoverageReported](Configuration/WithActivityCoverageReported.md)(…) | Updates the configuration to enable or disable reporting activity coverage. |
| [WithDeadlockTimeout](Configuration/WithDeadlockTimeout.md)(…) | Updates the value that controls how much time the deadlock monitor should wait during concurrency testing before reporting a potential deadlock. |
| [WithDebugLoggingEnabled](Configuration/WithDebugLoggingEnabled.md)(…) | Updates the configuration with debug logging enabled or disabled. |
| [WithLivenessTemperatureThreshold](Configuration/WithLivenessTemperatureThreshold.md)(…) | Updates the configuration with the specified liveness temperature threshold during systematic testing. If this value is 0 it disables liveness checking. It is not recommended to explicitly set this value, instead use the default value which is assigned to [`MaxFairSchedulingSteps`](./Configuration/MaxFairSchedulingSteps.md) / 2. |
| [WithMaxSchedulingSteps](Configuration/WithMaxSchedulingSteps.md)(…) | Updates the configuration with the specified number of maximum scheduling steps to explore per iteration during systematic testing. The [`MaxUnfairSchedulingSteps`](./Configuration/MaxUnfairSchedulingSteps.md) is assigned the *maxSteps* value, whereas the [`MaxFairSchedulingSteps`](./Configuration/MaxFairSchedulingSteps.md) is assigned a value using the default heuristic, which is 10 * *maxSteps*. (2 methods) |
| [WithNoBugTraceRepro](Configuration/WithNoBugTraceRepro.md)(…) | Updates the configuration with the ability to reproduce bug traces enabled or disabled. Disabling reproducibility allows skipping errors due to uncontrolled concurrency, for example when the program is only partially rewritten, or there is external concurrency that is not mocked, or when the program uses an API that is not yet supported. |
| [WithPartiallyControlledConcurrencyAllowed](Configuration/WithPartiallyControlledConcurrencyAllowed.md)(…) | Updates the configuration with partially controlled concurrency allowed or disallowed. |
| [WithPotentialDeadlocksReportedAsBugs](Configuration/WithPotentialDeadlocksReportedAsBugs.md)(…) | Updates the value that controls if potential deadlocks should be reported as bugs. |
| [WithPrioritizationStrategy](Configuration/WithPrioritizationStrategy.md)(…) | Updates the configuration to use the priority-based scheduling strategy during systematic testing. You can specify if you want to enable liveness checking, which is disabled by default, and an upper bound of possible priority changes, which by default can be up to 10. |
| [WithProbabilisticStrategy](Configuration/WithProbabilisticStrategy.md)(…) | Updates the configuration to use the probabilistic scheduling strategy during systematic testing. You can specify a value controlling the probability of each scheduling decision. This value is specified as the integer N in the equation 0.5 to the power of N. So for N=1, the probability is 0.5, for N=2 the probability is 0.25, N=3 you get 0.125, etc. By default, this value is 3. |
| [WithRandomGeneratorSeed](Configuration/WithRandomGeneratorSeed.md)(…) | Updates the seed used by the random value generator during systematic testing. |
| [WithRandomStrategy](Configuration/WithRandomStrategy.md)() | Updates the configuration to use the random scheduling strategy during systematic testing. |
| [WithReplayStrategy](Configuration/WithReplayStrategy.md)(…) | Updates the configuration to use the replay scheduling strategy during systematic testing. This strategy replays the specified schedule trace to reproduce the same execution. |
| [WithRLStrategy](Configuration/WithRLStrategy.md)() | Updates the configuration to use the reinforcement learning (RL) scheduling strategy during systematic testing. |
| [WithSharedStateReductionEnabled](Configuration/WithSharedStateReductionEnabled.md)(…) | Updates the configuration with shared state reduction enabled or disabled. If this reduction strategy is enabled, then the runtime will attempt to reduce the schedule space by taking into account any 'READ' and 'WRITE' operations declared by invoking [`Read`](../Microsoft.Coyote.Runtime/SchedulingPoint/Read.md) and [`Write`](../Microsoft.Coyote.Runtime/SchedulingPoint/Write.md). |
| [WithSystematicFuzzingEnabled](Configuration/WithSystematicFuzzingEnabled.md)(…) | Updates the configuration with systematic fuzzing enabled or disabled. |
| [WithSystematicFuzzingFallbackEnabled](Configuration/WithSystematicFuzzingFallbackEnabled.md)(…) | Updates the configuration with systematic fuzzing fallback enabled or disabled. |
| [WithTelemetryEnabled](Configuration/WithTelemetryEnabled.md)(…) | Updates the configuration with telemetry enabled or disabled. |
| [WithTestingIterations](Configuration/WithTestingIterations.md)(…) | Updates the configuration with the specified number of iterations to run during systematic testing. |
| [WithTestingTimeout](Configuration/WithTestingTimeout.md)(…) | Updates the configuration with the specified systematic testing timeout in seconds. |
| [WithTimeoutDelay](Configuration/WithTimeoutDelay.md)(…) | Updates the value that controls the probability of triggering a timeout each time an operation gets delayed or a built-in timer gets scheduled during systematic testing. |
| [WithTraceVisualizationEnabled](Configuration/WithTraceVisualizationEnabled.md)(…) | Updates the configuration with trace visualization enabled or disabled. If enabled, the testing engine can produce a DGML graph representing an execution leading up to a bug. |
| [WithUncontrolledConcurrencyResolutionTimeout](Configuration/WithUncontrolledConcurrencyResolutionTimeout.md)(…) | Updates the values that control how much time the runtime should wait for each instance of uncontrolled concurrency to resolve before continuing exploration. The *attempts* parameter controls how many times to check if uncontrolled concurrency has resolved, whereas the *delay* parameter controls how long the runtime waits between each retry. |
| [WithVerbosityEnabled](Configuration/WithVerbosityEnabled.md)(…) | Updates the configuration with verbose output enabled or disabled. |
| [WithXmlLogEnabled](Configuration/WithXmlLogEnabled.md)(…) | Updates the configuration with XML log generation enabled or disabled. |

## Protected Members

| name | description |
| --- | --- |
| [Configuration](Configuration/Configuration.md)() | Initializes a new instance of the [`Configuration`](./Configuration.md) class. |

## See Also

* namespace [Microsoft.Coyote](../Microsoft.CoyoteNamespace.md)
* assembly [Microsoft.Coyote](../Microsoft.Coyote.md)

<!-- DO NOT EDIT: generated by xmldocmd for Microsoft.Coyote.dll -->
