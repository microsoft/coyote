
## Track code and actor activity coverage

Standard code coverage tools record the percentage of code lines that are actually executed by test
cases. Coyote additionally defines the higher-level metric _Actor Activity Coverage_ that reports
state transitions and the percentage of possible events that are actually executed during a run of
`coyote` tester.

### Coyote coverage options

Running `coyote /?` displays a summary of command-line options. Here is the section describing
options to report activity coverage:

```plain
Coverage options:
-----------------------------------
  -c, --coverage string       
        : Generate coverage reports if supported for the programming model used by the test.
```

Detailed descriptions are provided in subsequent sections. The following provides a quick overview.

* If `--coverage` is specified, Coyote will collect the history of all events and state transitions
  for reporting the coverage of the actors and state machines being tested.
* Coverage is not currently available for programming models other than actors.

### Output file locations

By default, at the end of testing the report files are written to a directory named
`Output/[assemblyToTest]/CoyoteOutput` in the directory specified by the `path` argument. If
`--outdir outputDirectory` is specified, then the files are written to the directory
`[outputDirectory]/CoyoteOutput`.

Details of the report files that are created for the separate coverage types are provided in
subsequent sections.

### Actor activity coverage

Actor activity coverage includes event coverage, which is defined in the following section, as well
as a summary of states that were entered and exited and which state transitions occurred.

### Definition of event coverage

A tuple `(M, S, E)` is said to be _defined_ if state S of machine M is prepared to receive an event
of type E, i.e., it has an action defined for the event.

A tuple `(M, S, E)` is said to be _covered_ by a test run if state S of machine M actually dequeues
an event of type E during an execution.

Event coverage is the number of tuples covered divided by the number of tuples defined in the
program. The higher this metric, the better testing exercised all these combinations in the
program. As with other coverage metrics, obtaining 100% coverage may be unrealistic as it depends
on the particular test harness being used.

### Activity coverage output files

If the option `--coverage`, `--coverage activity`, or `--coverage activity-debug` is passed to
`coyote`, the following files will be written to the [output directory](#output-file-locations) (for
example, given `path` equal to `PingPong.exe`):
* `PingPong.coverage.txt`. This file contains the Event Coverage metric along with a breakdown per
  machine and per state. It also summarizes other useful coverage information.
* `PingPong.dgml`. This file contains the Event Coverage visualization as described below.
* `PingPong.coverage.ser`. This is the serialized `CoverageInfo` object for the test run. Such
  `.coverage.ser` files from multiple runs can be passed to `CoyoteCoverageReportMerger.exe` to
  create a merged report.
* If `--coverage activity-debug` was specified, then there will also be a Debug directory containing
  the same files as above for each process, with the filename qualified by a sequential process id,
  e.g: `PingPong.coverage_0.txt`

Note that while `--coverage` is a shortcut for specifying both `--coverage code` and `--coverage
activity`, you must specify `--coverage activity-debug` explicitly.

### Activity coverage visualization example

The activity coverage can additionally be displayed in [DGML](https://en.wikipedia.org/wiki/DGML)
format. Run `coyote` as described in the [`coyote` examples](#coyote-test-examples) section below.
This produces a file in the DGML format as described in the [activity coverage output
files](#activity-coverage-output-files) section. Open the file using Visual Studio. It captures
machines, states and transitions witnessed during the testing of the program. The file also contains
inter-machine transitions. These transitions are usually auto-hidden when opened in Visual Studio,
but visible when you click on a state.

### Coyote test examples

First build the Coyote samples by following the instructions
[here](https://github.com/microsoft/coyote/tree/main/Samples/README.md).

Then run `coyote` with one of the coverage flags, as well as the other options you want. Here are
some minimal examples:

```plain
coyote test ./bin/net7.0/Monitors.exe -i 10 --coverage
```

This will create the directory `./bin/net7.0/Output/Monitors.exe/CoyoteOutput/`, then it
generates coverage files for code coverage which you can load into Visual Studio to see the results.

```plain
coyote test ./bin/net7.0/Monitors.exe -i 10 -coverage activity  -o "/Coyote_Coverage/Monitors"
```

This will create the directory `/Coyote_Coverage/Monitors/CoyoteOutput`, then it generates only
activity coverage.
