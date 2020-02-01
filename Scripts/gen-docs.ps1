# PowerShell v2

$PSScriptRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition

$CoyoteRoot = Split-Path $PSScriptRoot

if (-Not (Test-Path -Path "$CoyoteRoot\bin\net46\Microsoft.Coyote.dll"))
{
    throw "please build coyote project first"
}

$target = "$CoyoteRoot\docs\_learn\ref"

Write-Host "processing inherit docs under $CoyoteRoot\bin"
InheritDoc --base $CoyoteRoot\bin\net46 -o

Write-Host "Generating new markdown under $target"
XmlDocMarkdown --namespace Microsoft.Coyote "$CoyoteRoot\bin\net46\Microsoft.Coyote.dll" "$target" --front-matter "$CoyoteRoot\docs\assets\data\_front.md" --visibility protected --toc --toc-prefix /learn/ref --clean
$toc = "$CoyoteRoot\docs\_data\sidebar-learn.yml"

Write-Host "Merging $toc..."
# Now merge the new toc
$newtoc = Get-Content -Path "$CoyoteRoot\docs\_learn\ref\toc.yml"

$oldtoc = Get-Content -Path $toc

$found = $False
$start = "- title: API documentation"
$stop = "- title: Resources"
$merged = @()

for ($i = 0; $i -lt $oldtoc.Length; $i++)
{
    $line = $oldtoc[$i]
    if ($line -eq $start)
    {
        $found = $True
        $merged += $line
        $merged += $oldtoc[$i + 1]
        $i = $i + 2  # skip to "- name: Microsoft.Coyote"
        for ($j = 4; $j -lt $newtoc.Length; $j++)
        {
            $line = $newtoc[$j]
            $merged += $line
        }

        # skip to the end of the api documentation

        for (;$i -lt $oldtoc.Length; $i++)
        {
            if ($oldtoc[$i] -eq $stop)
            {
                $i = $i - 1;
                break;
            }
        }
    } else {
        $merged += $line
    }
}

if (-Not $found)
{
    throw "Did not find start item: $start"
}
else
{
    Write-Host "Generating new $toc..."
    Set-Content -Path "$toc" -Value $merged
}
