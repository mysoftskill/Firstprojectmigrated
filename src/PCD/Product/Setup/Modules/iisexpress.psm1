function Initialize-IISExpress() {
    Write-Host "-- Setting up IIS Express..."

    Set-WebSiteEndpointAcls "dev.manage.privacy.microsoft-int.com" $devboxUser

    Write-Host "Successfully configured IIS Express." -fore Green
}
