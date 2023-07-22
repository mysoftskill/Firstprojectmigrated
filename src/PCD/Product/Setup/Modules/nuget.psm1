function Restore-DevboxNugetDependencies() {
    Write-Host "-- Restoring NuGet packages for devbox setup..."

    & ..\Build\tools\nuget\nuget restore devbox.proj -PackagesDirectory "${env:NugetPackages}" 2>&1 | Out-File "$tempFolder\nuget-devbox.log" -Append

    if ($LASTEXITCODE -eq 0) {
        Write-Host "-- Successfully restored NuGet packages for devbox setup." -fore Green
    } else {
        throw "Failed to restore NuGet packages for devbox setup."
    }
}

function Restore-AllDependencies() {
    Write-Host "-- Restoring all dependencies..."

    & ..\Build\restore_devbox.cmd 2>&1 | Out-File "$tempFolder\restore-all.log" -Append

    if ($LASTEXITCODE -eq 0) {
        Write-Host "-- Successfully restored all dependencies." -fore Green
    } else {
        throw "Failed to restore all dependencies."
    }
}
