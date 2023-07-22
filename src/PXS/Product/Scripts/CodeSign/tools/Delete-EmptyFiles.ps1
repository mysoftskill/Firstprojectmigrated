Param
(
    [Parameter(Mandatory=$true)]
    [String]
    $RootPath
)

if ((Test-Path $RootPath) -eq $false)
{
    throw "Invalid Root Path"
}

$items = get-childitem $RootPath -Recurse  | where-object { $_.GetType().Name -eq "FileInfo" } | where-object { $_.Length -eq 0 }

if (($items -ne $null) -and ($items.Length -gt 0))
{
    foreach ($item in $items)
    {
        Write-Host "Deleting $($item.Name)"
        $item.Delete()
    }
}