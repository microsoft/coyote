---
layout: reference
title: Code and activity coverage
section: learn
permalink: /learn/tools/coverage
---

## Code and activity coverage

Standard code coverage tools record the percentage of code lines that are actually executed by test cases. We additionally define the higher-level metric _P# Activity Coverage_ that reports state transitions and the percentage of possible events that are actually executed during a run of `Coyote` tester.

## Coyote coverage options

Running `Coyote /?` displays a summary of command-line options. Here is the section describing options to report code and activity coverage:
`````
Code and activity coverage options:
-----------------------------------
  -c, --coverage string       : Generate code coverage statistics (via VS instrumentation) with zero or more values equal to:
                                 code: Generate code coverage statistics (via VS instrumentation)
                                 activity: Generate activity (machine, event, etc.) coverage statistics
                                 activity-debug: Print activity coverage statistics with debug info
  -instr, --instrument string
                              : Additional file spec(s) to instrument for code coverage (wildcards supported)
  -instr-list, --instrument-list string
                              : File containing the paths to additional file(s) to instrument for code coverage, one per line,
                                wildcards supported, lines starting with '//' are skipped
`````

The following options from the "basic" section are also relevant:
```
  path                        : Path to the Coyote program to test
  -o, --outdir string         : Dump output to directory x (absolute path or relative to current directory
```

Detailed descriptions are provided in subsequent sections; here, we provide an overview to place those sections in context.

* Note that `--coverage` is the equivalent of specifying `--coverage code activity`.
* If `--coverage` or `--coverage code` is specified, all DLLs in the dependency chain between the `path` option and any `Microsoft.Coyote.*.dll` are instrumented.
* The `--instrument` options allow you specify other DLLs that don’t depend on Coyote but should also be instrumented (for example, utility libraries). File names can be absolute or relative to the `path` assembly being tested.
* `--coverage activity` and `--coverage activity-debug` do not instrument assemblies. In this case Coyote maintains the history of events and state transitions for reporting the coverage of the state machines being tested.

## Output file locations

By default, at the end of testing the report files are written to a directory named `Output\[assemblyToTest]\CoyoteOutput` in the same directory as the `path` option. If `--outdir outputDirectory` is specified, then the files are written to the directory `[outputDirectory]\CoyoteOutput`. In either case, history is retained for up to 10 previous runs:
  * If a directory named `...\CoyoteOutput9` exists it is removed.
  * Any directories named `...\CoyoteOutput[n]` (for n = 0 to 8) are renamed to `...\CoyoteOutput[n+1]`.
  * The new directory `...\CoyoteOutput` is created.

TODO: renaming existing output folders is insane, it kills output file path stability. We need a better system.
how about just ever increasing indexes instead?

Details of the report files that are created for the separate coverage types are provided in subsequent sections.

## Activity coverage

Activity coverage includes event coverage, which is defined in the following section, as well as a summary of machine states that were entered and exited and which state transitions occurred.

## Definition of event coverage

A tuple `(M, S, E)` is said to be _defined_ if state S of machine M is prepared to receive an event of type E, i.e., it has an action defined for the event.

A tuple `(M, S, E)` is said to be _covered_ by a test run if state S of machine M actually dequeues an event of type E during an execution.

Event coverage is the number of tuples covered divided by the number of tuples defined in the program. The higher this metric, the better testing exercised all these combinations in the program. As with other coverage metrics, obtaining 100% coverage may be unrealistic as it depends on the particular test harness being used.

## Activity coverage output files

If the option `--coverage`, `--coverage activity`, or `--coverage activity-debug` is passed to `Coyote`, the following files will be written to the [output directory](#output-file-locations) (for example, given `path` equal to `PingPong.CoyoteLanguage.exe`):
* `PingPong.CoyoteLanguage.coverage.txt`. This file contains the Event Coverage metric along with a breakdown per machine and per state. It also summarizes other useful coverage information.
* `PingPong.CoyoteLanguage.dgml`. This file contains the Event Coverage visualization as described below.
* `PingPong.CoyoteLanguage.sci`. This is the serialized CoverageInfo for the coverage run. 'sci' files from multiple runs can be passed to `CoyoteCoverageReportMerger.exe` to create a merged report.
* If `--coverage activity-debug` was specified, then there will also be a Debug directory containing the same files as above for each process, with the filename qualified by a sequential process id, e.g: `PingPong.CoyoteLanguage.coverage_0.txt`

Note that while `--coverage` is a shortcut for specifying both `--coverage code` and `--coverage activity`, you must specify `/coverage:activity-debug` explicitly.

## Activity coverage visualization example

The activity coverage can additionally be displayed in [DGML](https://en.wikipedia.org/wiki/DGML) format. Run `Coyote` as described in the [`Coyote` examples](#coyote test-examples) section below. This produces a file in the DGML format as described in the [activity coverage output files](#activity-coverage-output-files) section. Open the file using Visual Studio. It captures machines, states and transitions witnessed during the testing of the program. The file also contains inter-machine transitions. These transitions are usually auto-hidden when opened in Visual Studio, but visible when one clicks on a state.

![](/Coyote/assets/images/PingPongVisualization.png)

## Code coverage

For code coverage, `Coyote` instruments the `path` assembly and the binaries it depends upon via `VSInstr.exe`. `VSPerfCmd.exe` is launched while the test runs, and is terminated when the test is complete.

`VSPerfCmd.exe` collects data from all running processes, so do not run multiple coverage tests at the same time.

## Code coverage binary instrumentation

`Coyote` instruments the following binaries (via `VSInstr.exe`):
* `path` assembly: this is the assembly name passed via `path` command line option.
* Each DLL in the dependency graph between the `path` assembly and a `Microsoft.Coyote.dll`.
* Any additional assemblies specified by one of the `/instr` options.

By default the VS 2019 tools are used. These are set in `coyote.exe.config` and are based on the environment variable $(DevEnvDir) which
is automatically defined if y ou use a Visual Studio Developer Command Prompt. The actual paths can be overridden by environment variables
with the same names as the app settings:
- `VSInstrToolPath`
- `VSPerfCmdToolPath`

## Code coverage output files

If the option `--coverage` or `--coverage code` is passed to `Coyote`, the following files will be written to the [output directory](#output-file-locations) (for example, if `path` is `PingPong.CoyoteLanguage.exe`):
* `PingPong.CoyoteLanguage.coverage`. This file contains the code coverage data that can be read by Visual Studio. To do so, load it as a file into VS, then select Mixed Debugging.
* `PingPong.CoyoteLanguage.instr.exe` and `PingPong.CoyoteLanguage.instr.pdb`. These are the instrumented binaries for the `path` assembly.
* The instrumented `.dll` and `.pdb` for each DLL in the dependency graph between the `path` assembly and `Microsoft.Coyote.dll`, as well as any additional assemblies specified by one of the `--instr` options.

The instrumented binaries are retained because VS requires the matching instrumented binaries to be able to load the `.coverage` file. The source code may become out of sync, but the summary information displayed in VS will still be accurate. This allows evaluating the code-coverage impact of changes over time.

## Coyote test examples

Change to the Samples directory.

First build all the samples as described [here](https://github.com/p-org/Coyote/tree/master/Samples), or only the `PingPong.CoyoteLanguage` example: `msbuild PingPong\PingPong.CoyoteLanguage`

Then run `coyote.exe` with one of the coverage flags, as well as the other options you want. Here are some minimal examples:
```
..\bin\net46\coyote.exe test.\bin\net46\PingPong.CoyoteLanguage.exe -i 10 --coverage
```
This will create the directory `.\bin\net46\Output\PingPong.CoyoteLanguage.exe\CoyoteOutput`,
then it generates coverage files for both activity and code coverage.

```
..\bin\net46\coyote.exe test .\bin\net46\PingPong.CoyoteLanguage.exe --i 10 -coverage activity  -o C:\Coyote_Coverage\PingPongAsLanguage
```
This will create the directory `C:\Coyote_Coverage\PingPongAsLanguage\CoyoteOutput`,
then it generates only activity coverage.

```
..\bin\net46\coyote.exe test .\bin\net46\PingPong.CoyoteLanguage.exe -i 10 --coverage code activity-debug
```
This generates code and activity coverage, including debug activity output.
