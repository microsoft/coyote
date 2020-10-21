param(
    [string]$dotnet = "dotnet",
    [ValidateSet("all", "netcoreapp3.1", "net47", "net48", "net5.0")]
    [string]$framework = "all",
    [ValidateSet("all", "actors", "tasks", "rewriting", "testing", "standalone")]
    [string]$test = "all",
    [string]$filter = "",
    [string]$logger = "",
    [ValidateSet("quiet", "minimal", "normal", "detailed", "diagnostic")]
    [string]$v = "normal"
)

Import-Module $PSScriptRoot/powershell/common.psm1 -Force

$frameworks = Get-ChildItem -Path "$PSScriptRoot/../Tests/bin" | Where-Object Name -CNotIn "netstandard2.0", "netstandard2.1" | Select-Object -expand Name

$targets = [ordered]@{
    "actors" = "Actors.Tests"
    "tasks" = "Tasks.Tests"
    "rewriting"  = "Rewriting.Tests"
    "testing"    = "SystematicTesting.Tests"
    "standalone" = "Standalone.Tests"
}

$key_file = "$PSScriptRoot/../Common/Key.snk"

[System.Environment]::SetEnvironmentVariable('COYOTE_CLI_TELEMETRY_OPTOUT', '1')

Write-Comment -prefix "." -text "Running the Coyote tests" -color "yellow"
foreach ($kvp in $targets.GetEnumerator()) {
    if (($test -ne "all") -and ($test -ne $($kvp.Name))) {
        continue
    }

    foreach ($f in $frameworks) {
        if (($framework -ne "all") -and ($f -ne $framework)) {
            continue
        }

        $rewriting_target = ""
        if ($($kvp.Name) -eq "rewriting") {
            $rewriting_target = "$PSScriptRoot/../Tests/$($kvp.Value)/bin/$f/RewritingTests.coyote.json"
        }
        elseif ($($kvp.Name) -eq "standalone") {
            $rewriting_target = "$PSScriptRoot/../Tests/bin/$f/Microsoft.Coyote.Standalone.Tests.dll"
        }

        if ($rewriting_target -ne "") {
            # Rewrite the test.
            Invoke-CoyoteTool -cmd "rewrite" -dotnet $dotnet -framework $f -target $rewriting_target -key $key_file
            
            # Try rewrite again to make sure we can skip a rewritten assembly and do no damage!
            Invoke-CoyoteTool -cmd "rewrite" -dotnet $dotnet -framework $f -target $rewriting_target -key $key_file
        }

        # Run the (rewritten) test.
        $target = "$PSScriptRoot/../Tests/$($kvp.Value)/$($kvp.Value).csproj"
        Invoke-DotnetTest -dotnet $dotnet -project $($kvp.Name) -target $target -filter $filter -logger $logger -framework $f -verbosity $v
    }
}

Write-Comment -prefix "." -text "Done" -color "green"
