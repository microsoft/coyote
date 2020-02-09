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
| [IsXmlLogEnabled](Configuration/IsXmlLogEnabled) { get; } | Produce an XML formatted runtime log file. |
| [MaxSchedulingSteps](Configuration/MaxSchedulingSteps) { set; } | The maximum scheduling steps to explore for both fair and unfair schedulers. By default there is no bound. |
| [AssemblyToBeAnalyzed](Configuration/AssemblyToBeAnalyzed) | The assembly to be analyzed for bugs. |
| [AttachDebugger](Configuration/AttachDebugger) | Attaches the debugger during trace replay. |
| [CoinFlipBound](Configuration/CoinFlipBound) | Coin-flip bound. By default it is 2. |
| [ConsiderDepthBoundHitAsBug](Configuration/ConsiderDepthBoundHitAsBug) | If true, then the Coyote tester will consider an execution that hits the depth bound as buggy. |
| [CustomActorRuntimeLogType](Configuration/CustomActorRuntimeLogType) | If specified, requests a custom runtime log to be used instead of the default. This is the AssemblyQualifiedName of the type to load. |
| [DebugActivityCoverage](Configuration/DebugActivityCoverage) | Enables activity coverage debugging. |
| [EnableColoredConsoleOutput](Configuration/EnableColoredConsoleOutput) | Enables colored console output. |
| [EnableDebugging](Configuration/EnableDebugging) | Enables debugging. |
| [EnableProfiling](Configuration/EnableProfiling) | Enables profiling. |
| [IncrementalSchedulingSeed](Configuration/IncrementalSchedulingSeed) | If true, the seed will increment in each testing iteration. |
| [IsDgmlBugGraph](Configuration/IsDgmlBugGraph) | Is DGML graph showing all test iterations or just one "bug" iteration. False means all, and True means only the iteration containing a bug. |
| [IsLivenessCheckingEnabled](Configuration/IsLivenessCheckingEnabled) | If this option is enabled, liveness checking is enabled during bug-finding. |
| [IsMonitoringEnabledInInProduction](Configuration/IsMonitoringEnabledInInProduction) | If this option is enabled, (safety) monitors are used in the production runtime. |
| [IsProgramStateHashingEnabled](Configuration/IsProgramStateHashingEnabled) | If this option is enabled, the tester is hashing the program state. |
| [IsVerbose](Configuration/IsVerbose) | If true, then messages are logged. |
| [LivenessTemperatureThreshold](Configuration/LivenessTemperatureThreshold) | The liveness temperature threshold. If it is 0 then it is disabled. |
| [MaxFairSchedulingSteps](Configuration/MaxFairSchedulingSteps) | The maximum scheduling steps to explore for fair schedulers. By default there is no bound. |
| [MaxUnfairSchedulingSteps](Configuration/MaxUnfairSchedulingSteps) | The maximum scheduling steps to explore for unfair schedulers. By default there is no bound. |
| [OutputFilePath](Configuration/OutputFilePath) | The output path. |
| [ParallelBugFindingTasks](Configuration/ParallelBugFindingTasks) | Number of parallel bug-finding tasks. By default it is 1 task. |
| [ParallelDebug](Configuration/ParallelDebug) | Put a debug prompt at the beginning of each child TestProcess. |
| [PerformFullExploration](Configuration/PerformFullExploration) | If true, the Coyote tester performs a full exploration, and does not stop when it finds a bug. |
| [PrioritySwitchBound](Configuration/PrioritySwitchBound) | The priority switch bound. By default it is 2. Used by priority-based schedulers. |
| [RandomValueGeneratorSeed](Configuration/RandomValueGeneratorSeed) | Custom seed to be used by the random value generator. By default, this value is null indicating that no seed has been set. |
| [ReportActivityCoverage](Configuration/ReportActivityCoverage) | Enables activity coverage reporting of a Coyote program. |
| [ReportCodeCoverage](Configuration/ReportCodeCoverage) | Enables code coverage reporting of a Coyote program. |
| [RunAsParallelBugFindingTask](Configuration/RunAsParallelBugFindingTask) | Runs this process as a parallel bug-finding task. |
| [RuntimeGeneration](Configuration/RuntimeGeneration) | The current runtime generation. |
| [SafetyPrefixBound](Configuration/SafetyPrefixBound) | Safety prefix bound. By default it is 0. |
| [ScheduleFile](Configuration/ScheduleFile) | The schedule file to be replayed. |
| [SchedulingIterations](Configuration/SchedulingIterations) | Number of scheduling iterations. |
| [SchedulingStrategy](Configuration/SchedulingStrategy) | Scheduling strategy to use with the Coyote tester. |
| [TestingProcessId](Configuration/TestingProcessId) | The unique testing process id. |
| [TestingRuntimeAssembly](Configuration/TestingRuntimeAssembly) | The assembly that contains the testing runtime. By default it is empty, which uses the default testing runtime of Coyote. |
| [TestingSchedulerEndPoint](Configuration/TestingSchedulerEndPoint) | The testing scheduler unique endpoint. |
| [TestingSchedulerIpAddress](Configuration/TestingSchedulerIpAddress) | Specify ip address if you want to use something other than localhost. |
| [TestMethodName](Configuration/TestMethodName) | Test method to be used. |
| [Timeout](Configuration/Timeout) | Timeout in seconds. |
| [TimeoutDelay](Configuration/TimeoutDelay) | The timeout delay used during testing. By default it is 1. Increase to the make timeouts less frequent. |
| [ToolCommand](Configuration/ToolCommand) | The user-specified command to perform by the Coyote tool. |
| [UserExplicitlySetMaxFairSchedulingSteps](Configuration/UserExplicitlySetMaxFairSchedulingSteps) | True if the user has explicitly set the fair scheduling steps bound. |
| [WaitForTestingProcesses](Configuration/WaitForTestingProcesses) | Do not automatically launch the TestingProcesses in parallel mode, instead wait for them to be launched independently. |
| [WithMaxSteps](Configuration/WithMaxSteps)(…) | Updates the configuration with the specified number of scheduling steps to perform per iteration (for both fair and unfair schedulers). |
| [WithNumberOfIterations](Configuration/WithNumberOfIterations)(…) | Updates the configuration with the specified number of iterations to perform. |
| [WithStrategy](Configuration/WithStrategy)(…) | Updates the configuration with the specified scheduling strategy. |
| [WithVerbosityEnabled](Configuration/WithVerbosityEnabled)(…) | Updates the configuration with verbose output enabled or disabled. |

## Protected Members

| name | description |
| --- | --- |
| [Configuration](Configuration/Configuration)() | Initializes a new instance of the [`Configuration`](ConfigurationType) class. |

## See Also

* namespace [Microsoft.Coyote](../MicrosoftCoyoteNamespace)
* assembly [Microsoft.Coyote](../MicrosoftCoyoteAssembly.md)

<!-- DO NOT EDIT: generated by xmldocmd for Microsoft.Coyote.dll -->
