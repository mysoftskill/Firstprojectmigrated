Param
(
    [Parameter(Mandatory)]
    [string]$SubscriptionId,
    [Parameter(Mandatory)]
    [string]$UamiRGName,
    [Parameter(Mandatory)]
    [string]$UamiName,
    [Parameter(Mandatory)]
    [string]$RoleRGName,
    [Parameter(Mandatory)]
    [string]$RoleName,
    [Parameter(Mandatory)]
    [string]$StorageAccountName
)
function Add-UserRole {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)]
        [string]$subscriptionId,
        [Parameter(Mandatory)]
        [string]$uamiRGName,
        [Parameter(Mandatory)]
        [string]$uamiName,
        [Parameter(Mandatory)]
        [string]$roleRGName,
        [Parameter(Mandatory)]
        [string]$roleName,
        [Parameter(Mandatory)]
        [string]$storageAccountName
    )
    # Find the managed user identity
    $sp = Get-AzUserAssignedIdentity -ResourceGroupName $($uamiRGName) -Name $($uamiName)
    if ($null -eq $sp)
    {
        Write-Host -ForegroundColor Red "Could not find $($uamiName) in $($uamiRGName)"
    }
    else 
    {
        # Determine if the role already exists for the user identity
        $found = Get-AzRoleAssignment -ObjectId $sp.PrincipalId -RoleDefinitionName $($roleName) -Scope "/subscriptions/$subscriptionId/resourceGroups/$roleRGName/providers/Microsoft.Storage/storageAccounts/$storageAccountName"
        if (!$found)
        {
            New-AzRoleAssignment -ObjectId $sp.PrincipalId -RoleDefinitionName $($roleName) -Scope "/subscriptions/$subscriptionId/resourceGroups/$roleRGName/providers/Microsoft.Storage/storageAccounts/$storageAccountName"
            Write-Host -ForegroundColor Green "Adding Role: $($roleName) to $($sp.Name) in $($roleRGName)"
        }
        else 
        {
            Write-Host -ForegroundColor Red "Role: $($roleName) already exist for $($sp.Name) in $($roleRGName)"
        }
    }
}

## Install the required powershell modules
#Install-Module -Name Az.Resources
#Install-Module -Name Az.ManagedServiceIdentity

## Log into ARM and select the correct subscription, as appropriate
Connect-AzAccount -subscriptionId $SubscriptionId
Add-UserRole -subscriptionId $SubscriptionId -uamiRGName $UamiRGName -uamiName $UamiName -roleRGName $RoleRGName -roleName $RoleName -storageAccountName $StorageAccountName

# Scipt Testing Notes
# The New-AzRoleAssignment Command Documentation:
# https://docs.microsoft.com/en-us/powershell/module/az.Resources/New-azRoleAssignment?view=azps-6.2.0
#
# Test commands for Non_Prod:
# .\AddUserRole.ps1 -SubscriptionId "b4b176cf-fe78-4b59-bd1a-9b8c11536f4d" -UamiRGName "ADG-CS-WESTUS2-RG" -UamiName "ADG-CS-UAMI" -RoleRGName "pxstest" -RoleName "Storage Blob Data Contributor" -StorageAccountName "pxstest"
# .\AddUserRole.ps1 -SubscriptionId "b4b176cf-fe78-4b59-bd1a-9b8c11536f4d" -UamiRGName "ADG-CS-WESTUS2-RG" -UamiName "ADG-CS-UAMI" -RoleRGName "pxstest" -RoleName "Storage Queue Data Contributor" -StorageAccountName "pxstest"
#
# The commands below can be used to add the managed idenity roles for blob storage and queue storage in the sovereign clouds:
# MC - Storage Roles
# .\AddUserRole.ps1 -SubscriptionId "2e4d253c-c008-4adb-b8fe-fcd6bbdd1f17" -UamiRGName "NGPPROXY-MC-RG" -UamiName "ADGCS-MC-UAMI" -RoleRGName "NGPPROXY-MC-RG" -RoleName "Storage Blob Data Contributor" -StorageAccountName "ngpproxychinanorth"
# .\AddUserRole.ps1 -SubscriptionId "2e4d253c-c008-4adb-b8fe-fcd6bbdd1f17" -UamiRGName "NGPPROXY-MC-RG" -UamiName "ADGCS-MC-UAMI" -RoleRGName "NGPPROXY-MC-RG" -RoleName "Storage Queue Data Contributor" -StorageAccountName "ngpproxychinanorth2"
# FF - Storage Roles
# .\AddUserRole.ps1 -SubscriptionId "ebce915f-ff1f-4faf-be94-35fe15f0673b" -UamiRGName "NGPPROXY-FF-RG" -UamiName "ADGCS-FF-UAMI" -RoleRGName "NGPPROXY-FF-RG" -RoleName "Storage Blob Data Contributor" -StorageAccountName "ngpproxyusgovarizona"
# .\AddUserRole.ps1 -SubscriptionId "ebce915f-ff1f-4faf-be94-35fe15f0673b" -UamiRGName "NGPPROXY-FF-RG" -UamiName "ADGCS-FF-UAMI" -RoleRGName "NGPPROXY-FF-RG" -RoleName "Storage Queue Data Contributor" -StorageAccountName "ngpproxyusgovarizona2"