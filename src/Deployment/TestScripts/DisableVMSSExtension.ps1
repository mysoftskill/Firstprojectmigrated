Param
(
    [Parameter(Mandatory)]
    [string]$SubscriptionId,
    [Parameter(Mandatory)]
    [string]$RGName,
    [Parameter(Mandatory)]
    [string]$VMSSName,
    [Parameter(Mandatory)]
    [string]$VMSSExtensionName
)
function Disable-VMSSExtension {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)]
        [string]$rgName,
        [Parameter(Mandatory)]
        [string]$vmssName,
        [Parameter(Mandatory)]
        [string]$vmssExtensionName
    )
    # Find the virtual machine scale set within the resource group
    $vmss = Get-AzVmss -ResourceGroupName $rgName -VMScaleSetName $vmssName
    if ($null -eq $vmss)
    {
        Write-Host -ForegroundColor Red "Could not find $($vmssName) in $($rgName)"
    }
    else 
    {
        # Check for the existence of the extension before attempting to remove it
        $found = False
        foreach ($ext in $vmss.VirtualMachineProfile.ExtensionProfile.Extensions)
        {
            $found = $found -or $ext.Name -like "*$vmssExtensionName*"
        }
        if ($found)
        {
            Remove-AzVmssExtension -VirtualMachineScaleSet $vmss -Name $vmssExtensionName
            Update-AzVmss -ResourceGroupName $rgName -Name $vmssName -VirtualMachineScaleSet $vmss -AsJob
            Write-Host -ForegroundColor Green "Removing Extension: $($vmssExtensionName) from $($vmssName) in $($rgName)"
        }
        else 
        {
            Write-Host -ForegroundColor Red "Extension: $($vmssExtensionName) does not exist for $($vmssName) in $($rgName)"
        }
    }
}

## Log into ARM and select the correct subscription, as appropriate
Connect-AzAccount -subscriptionId $SubscriptionId
Disable-VMSSExtension -rgName $RGName -vmssName $VMSSName -vmssExtensionName $VMSSExtensionName

# Scipt Testing Notes
# The Remove-AzVmssExtension Command Documentation:
# https://docs.microsoft.com/en-us/powershell/module/az.compute/remove-azvmssextension?view=azps-5.9.0
#
# Test INT Cluster Used to Verify Safe Removal of Extension:
# https://ms.portal.azure.com/#@MSAzureCloud.onmicrosoft.com/resource/subscriptions/b4b176cf-fe78-4b59-bd1a-9b8c11536f4d/resourceGroups/PCF-INT-WESTUS2-CLUSTER-RG/providers/Microsoft.Compute/virtualMachineScaleSets/Worker/overview
#
# The commands below have been used to sucessfully remove the VMIaaSAntimalware extension from the PCF-INT-WESTUS2 Worker, Frontdoor, and Primary clusters:
# .\DisableVMSSExtension.ps1 -SubscriptionId "b4b176cf-fe78-4b59-bd1a-9b8c11536f4d" -RGName "PCF-INT-WESTUS2-CLUSTER-RG" -VMSSName "Primary" -VMSSExtensionName "VMIaaSAntimalware_vmNodeType0Name"
# .\DisableVMSSExtension.ps1 -SubscriptionId "b4b176cf-fe78-4b59-bd1a-9b8c11536f4d" -RGName "PCF-INT-WESTUS2-CLUSTER-RG" -VMSSName "Frontdoor" -VMSSExtensionName "VMIaaSAntimalware_vmNodeType1Name"
# .\DisableVMSSExtension.ps1 -SubscriptionId "b4b176cf-fe78-4b59-bd1a-9b8c11536f4d" -RGName "PCF-INT-WESTUS2-CLUSTER-RG" -VMSSName "Worker" -VMSSExtensionName "VMIaaSAntimalware_vmNodeType2Name"