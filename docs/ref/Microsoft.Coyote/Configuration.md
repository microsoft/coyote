# Configuration class

The Coyote project configurations.

```csharp
public class Configuration
```

## Public Members

| name | description |
| --- | --- |
| static [Create](Configuration/Create.md)() | Creates a new configuration with default values. |
| [DeadlockTimeout](Configuration/DeadlockTimeout.md) { get; } | Value that controls how much time the deadlock monitor should wait during concurrency testing before reporting a potential deadlock. This value is in milliseconds. |
| [IsVerbose](Configuration/IsVerbose.md) { get; } | If true, then messages are logged. |
| [LivenessTemperatureThreshold](Configuration/LivenessTemperatureThreshold.md) { get; } | The liveness temperature threshold. If it is 0 then it is disabled. By default this value is assigned to [`MaxFairSchedulingSteps`](Configuration/MaxFairSchedulingSteps.md) / 2. |
| [LogLevel](Configuration/LogLevel.md) { get; } | The level of detail to provide in verbose logging. |
| [MaxFairSchedulingSteps](Configuration/MaxFairSchedulingSteps.md) { get; } | The maximum scheduling steps to explore for fair schedulers. By default this is set to 100,000 steps. |
| [MaxUnfairSchedulingSteps](Configuration/MaxUnfairSchedulingSteps.md) { get; } | The maximum scheduling steps to explore for unfair schedulers. By default this is set to 10,000 steps. |
| [RandomGeneratorSeed](Configuration/RandomGeneratorSeed.md) { get; } | Custom seed to be used by the random value generator. By default, this value is null indicating that no seed has been set. |
| [SchedulingStrategy](Configuration/SchedulingStrategy.md) { get; } | The systematic testing strategy to use. |
| [TestingIterations](Configuration/TestingIterations.md) { get; } | Number of testing iterations. |
| [TimeoutDelay](Configuration/TimeoutDelay.md) { get; } | Value that controls the probability of triggering a timeout each time an operation gets delayed or a built-in timer gets scheduled during systematic testing. Decrease the value to increase the frequency of timeouts (e.g. a value of 1 corresponds to a 50% probability), or increase the value to decrease the frequency (e.g. a value of 10 corresponds to a 10% probability). |
| [UncontrolledConcurrencyTimeout](Configuration/UncontrolledConcurrencyTimeout.md) { get; } | Value that controls how much time the runtime should wait for uncontrolled concurrency to resolve before continuing exploration. This value is in milliseconds. |
| [WithActivityCoverageEnabled](Configuration/WithActivityCoverageEnabled.md)(…) | Updates the configuration with activity coverage enabled or disabled. |
| [WithConcurrencyFuzzingEnabled](Configuration/WithConcurrencyFuzzingEnabled.md)(…) | Updates the configuration with concurrency fuzzing enabled or disabled. |
| [WithConcurrencyFuzzingFallbackEnabled](Configuration/WithConcurrencyFuzzingFallbackEnabled.md)(…) | Updates the configuration with concurrency fuzzing fallback enabled or disabled. |
| [WithDeadlockTimeout](Configuration/WithDeadlockTimeout.md)(…) | Updates the value that controls how much time the deadlock monitor should wait during concurrency testing before reporting a potential deadlock. |
| [WithDgmlGraphEnabled](Configuration/WithDgmlGraphEnabled.md)(…) | Updates the configuration with DGML graph generation enabled or disabled. |
| [WithIncrementalSeedGenerationEnabled](Configuration/WithIncrementalSeedGenerationEnabled.md)(…) | Updates the configuration with incremental seed generation enabled or disabled. |
| [WithLivenessTemperatureThreshold](Configuration/WithLivenessTemperatureThreshold.md)(…) | Updates the configuration with the specified liveness temperature threshold during systematic testing. If this value is 0 it disables liveness checking. It is not recommended to explicitly set this value, instead use the default value which is assigned to [`MaxFairSchedulingSteps`](Configuration/MaxFairSchedulingSteps.md) / 2. |
| [WithMaxSchedulingSteps](Configuration/WithMaxSchedulingSteps.md)(…) | Updates the configuration with the specified number of maximum scheduling steps to explore per iteration during systematic testing. The [`MaxUnfairSchedulingSteps`](Configuration/MaxUnfairSchedulingSteps.md) is assigned the *maxSteps* value, whereas the [`MaxFairSchedulingSteps`](Configuration/MaxFairSchedulingSteps.md) is assigned a value using the default heuristic, which is 10 * *maxSteps*. (2 methods) |
| [WithNoBugTraceRepro](Configuration/WithNoBugTraceRepro.md)(…) | Updates the configuration with the ability to reproduce bug traces enabled or disabled. Disabling reproducibility allows skipping errors due to uncontrolled concurrency, for example when the program is only partially rewritten, or there is external concurrency that is not mocked, or when the program uses an API that is not yet supported. |
| [WithPartiallyControlledConcurrencyEnabled](Configuration/WithPartiallyControlledConcurrencyEnabled.md)(…) | Updates the configuration with partial controlled concurrency enabled or disabled. |
| [WithPCTStrategy](Configuration/WithPCTStrategy.md)(…) | Updates the configuration to use the PCT scheduling strategy during systematic testing. You can specify the number of priority switch points, which by default are 10. |
| [WithProbabilisticStrategy](Configuration/WithProbabilisticStrategy.md)(…) | Updates the configuration to use the probabilistic scheduling strategy during systematic testing. You can specify a value controlling the probability of each scheduling decision. This value is specified as the integer N in the equation 0.5 to the power of N. So for N=1, the probability is 0.5, for N=2 the probability is 0.25, N=3 you get 0.125, etc. By default, this value is 3. |
| [WithRandomGeneratorSeed](Configuration/WithRandomGeneratorSeed.md)(…) | Updates the seed used by the random value generator during systematic testing. |
| [WithRandomStrategy](Configuration/WithRandomStrategy.md)() | Updates the configuration to use the random scheduling strategy during systematic testing. |
| [WithReplayStrategy](Configuration/WithReplayStrategy.md)(…) | Updates the configuration to use the replay scheduling strategy during systematic testing. This strategy replays the specified schedule trace to reproduce the same execution. |
| [WithRLStrategy](Configuration/WithRLStrategy.md)() | Updates the configuration to use the reinforcement learning (RL) scheduling strategy during systematic testing. |
| [WithTelemetryEnabled](Configuration/WithTelemetryEnabled.md)(…) | Updates the configuration with telemetry enabled or disabled. |
| [WithTestingIterations](Configuration/WithTestingIterations.md)(…) | Updates the configuration with the specified number of iterations to run during systematic testing. |
| [WithTestingTimeout](Configuration/WithTestingTimeout.md)(…) | Updates the configuration with the specified systematic testing timeout in seconds. |
| [WithTimeoutDelay](Configuration/WithTimeoutDelay.md)(…) | Updates the value that controls the probability of triggering a timeout each time an operation gets delayed or a built-in timer gets scheduled during systematic testing. |
| [WithUncontrolledConcurrencyTimeout](Configuration/WithUncontrolledConcurrencyTimeout.md)(…) | Updates the value that controls how much time the runtime should wait for uncontrolled concurrency to resolve before continuing exploration. |
| [WithVerbosityEnabled](Configuration/WithVerbosityEnabled.md)(…) | Updates the configuration with verbose output enabled or disabled. |
| [WithXmlLogEnabled](Configuration/WithXmlLogEnabled.md)(…) | Updates the configuration with XML log generation enabled or disabled. |

## Protected Members

| name | description |
| --- | --- |
| [Configuration](Configuration/Configuration.md)() | Initializes a new instance of the [`Configuration`](Configuration.md) class. |

## See Also

* namespace [Microsoft.Coyote](../Microsoft.CoyoteNamespace.md)
* assembly [Microsoft.Coyote](../Microsoft.Coyote.md)

<!-- DO NOT EDIT: generated by xmldocmd for Microsoft.Coyote.dll -->
