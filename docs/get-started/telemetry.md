
## Telemetry in Coyote

The [coyote tool](using-coyote.md) collects usage data in order to help improve your experience. The
data is anonymous. It is collected by Microsoft and shared with the community.

You can opt-out of telemetry by setting the following environment variable to `1` or `true`:
```plain
set COYOTE_CLI_TELEMETRY_OPTOUT=1
```

First time usage of `coyote` is detected by the presence of the following file:

```plain
[Windows] %USERPROFILE%\AppData\Local\Microsoft\Coyote\CoyoteMachineId.txt
[Linux] $(HOME)/.microsoft/coyote/CoyoteMachineId.txt
```

This file contains a generated GUID representing the local machine which helps telemetry get some
vague idea of the number of folks that are using the `coyote` tool.

The following metrics are collected:
- the number of times `coyote test` is invoked and the time taken to complete each test.
- the number of times `coyote replay` is invoked and the time taken to complete the replay.
- the number of bugs reported by `coyote test`.
- whether or not `coyote` is running in the debugger, which is an indication the user is trying to
  debug an interesting bug found by coyote.
- the version of the .NET framework.

The telemetry also collects which operating system is being used (windows, linux, macOS).

This data is collected using [Azure Application
Insights](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview) and is
stored in an Azure account belonging to Microsoft and accessible only by the Microsoft Coyote team
members.

See also information about [data retention and
privacy](https://docs.microsoft.com/en-us/azure/azure-monitor/app/data-retention-privacy).
