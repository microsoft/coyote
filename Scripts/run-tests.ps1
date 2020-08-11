param(
    [string]$dotnet = "dotnet",
    [ValidateSet("all", "netcoreapp3.1", "net47", "net48")]
    [string]$framework = "all",
    [ValidateSet("all", "production", "rewriting", "testing", "standalone")]
    [string]$test = "all",
    [string]$filter = "",
    [string]$logger = "",
    [ValidateSet("quiet", "minimal", "normal", "detailed", "diagnostic")]
    [string]$v = "normal"
)

Import-Module $PSScriptRoot/powershell/common.psm1 -Force

$frameworks = Get-ChildItem -Path "$PSScriptRoot/../Tests/bin" | Where-Object Name -CNotIn "netstandard2.0", "netstandard2.1" | Select-Object -expand Name

$targets = [ordered]@{
    "production" = "Production.Tests"
    "rewriting"  = "BinaryRewriting.Tests"
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

        $target = "$PSScriptRoot/../Tests/$($kvp.Value)/$($kvp.Value).csproj"
        Invoke-DotnetTest -dotnet $dotnet -project $($kvp.Name) -target $target -filter $filter -logger $logger -framework $f -verbosity $v

        if ($($kvp.Name) -eq "rewriting") {
            # First rewrite the test.
            $config_file = "$PSScriptRoot/../Tests/$($kvp.Value)/bin/$f/BinaryRewritingTests.coyote.json"
            Invoke-CoyoteTool -cmd "rewrite" -dotnet $dotnet -framework $f -target $config_file -keyFile $key_file

            # Run the rewritten test.
            $target = "$PSScriptRoot/../Tests/$($kvp.Value)/$($kvp.Value).csproj"
            Invoke-DotnetTest -dotnet $dotnet -project $($kvp.Name) -target $target -filter $filter -logger $logger -framework $f -verbosity $v
        }
        elseif ($($kvp.Name) -eq "standalone") {
            # First rewrite the test.
            $assembly = "$PSScriptRoot/../Tests/bin/$f/Microsoft.Coyote.Standalone.Tests.dll"
            Invoke-CoyoteTool -cmd "rewrite" -dotnet $dotnet -framework $f -target $assembly -key $key_file

            # Run the rewritten test.
            $target = "$PSScriptRoot/../Tests/$($kvp.Value)/$($kvp.Value).csproj"
            Invoke-DotnetTest -dotnet $dotnet -project $($kvp.Name) -target $target -filter $filter -logger $logger -framework $f -verbosity $v
            
            # Test that we can also rewrite a rewritten assembly and do no damage!
            Invoke-CoyoteTool -cmd "rewrite" -dotnet $dotnet -framework $f -target $assembly -key $key_file

            # Run the rewritten test again.
            $target = "$PSScriptRoot/../Tests/$($kvp.Value)/$($kvp.Value).csproj"
            Invoke-DotnetTest -dotnet $dotnet -project $($kvp.Name) -target $target -filter $filter -logger $logger -framework $f -verbosity $v
        }
    }
}

Write-Comment -prefix "." -text "Done" -color "green"
