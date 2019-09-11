param(
    [string]$dotnet="dotnet",
    [ValidateSet("all","netcoreapp2.1","net46","net47")]
    [string]$framework="all",
    [ValidateSet("all","core","testing-services","shared-objects")]
    [string]$test="all",
    [string]$filter="",
    [ValidateSet("quiet","minimal","normal","detailed","diagnostic")]
    [string]$v="normal"
)

Import-Module $PSScriptRoot\powershell\common.psm1

$frameworks = "netcoreapp2.1", "net46", "net47"

$targets = [ordered]@{
    "core" = "Core.Tests"
    "testing-services" = "TestingServices.Tests"
    "shared-objects" = "SharedObjects.Tests"
}

Write-Comment -prefix "." -text "Running the Coyote tests" -color "yellow"
foreach ($kvp in $targets.GetEnumerator()) {
    if (($test -ne "all") -and ($test -ne $($kvp.Name))) {
        continue
    }

    foreach ($f in $frameworks) {
        if (($framework -ne "all") -and ($f -ne $framework)) {
            continue
        }

        $target = "$PSScriptRoot\..\Tests\$($kvp.Value)\$($kvp.Value).csproj"
        Invoke-DotnetTest -dotnet $dotnet -project $($kvp.Name) -target $target -filter $filter -framework $f -verbosity $v
    }
}

Write-Comment -prefix "." -text "Done" -color "green"
