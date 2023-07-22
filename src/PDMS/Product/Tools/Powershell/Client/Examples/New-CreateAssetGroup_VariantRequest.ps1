using namespace Microsoft.PrivacyServices.DataManagement.Client
using namespace Microsoft.PrivacyServices.DataManagement.Client.Filters
using namespace Microsoft.PrivacyServices.DataManagement.Client.V2
using namespace Microsoft.PrivacyServices.Identity
##############################################################################
#.SYNOPSIS
# Create asset groups for given AssetGroupQualifiers. And create variant request for the created asset groups.
# Note: This script will only work for new Asset Group Qualifiers which do not exist in the system. And input file must contains less than or 
# equals 100 Asset Groups. If you have more than 100 Asset groups, please split the input into multiple files with 100 or fewer Asset Groups per file
# This script will not immediately populate the work Item URI. You can get the variant request details in 
# DataGrid - https://datagrid.microsoft.com/Tagging/VariantRequests
# For more information about the script, see the documentation (https://microsoft.sharepoint.com/:w:/r/sites/privacy/_layouts/15/Doc.aspx?sourcedoc=%7B139DBDEF-6D5C-403D-8EB8-D4B76C7CE2BC%7D&file=readme_CreateNewAssetGroupsVariantRequest.docx&action=default&mobileredirect=true)
#
#.DESCRIPTION
# To avoid calling the service with bad data. -ErrorAction Stop is recommended.
#
#.PARAMETER AssetGroupQualifiersFilePath
# The path to a file that contains a list of asset qualifiers. One entry per line.
#
#.PARAMETER OwnerId
# The OwnerId to use when creating the AssetGroups.
#
#.PARAMETER VariantDefinitionId
# The VariantDefinitionId to use when creating the variant request.
#
#.PARAMETER GeneralContractorAlias
# The GeneralContractorAlias to use when creating the variant request.
#
#.PARAMETER CelaContactAlias
# The CelaContactAlias to use when creating the variant request.
#
#.PARAMETER Location
# One of the following values: PPE, PROD
#
#.PARAMETER Force
# Whether or not the script should actually make changes. Default is FALSE. This means it runs in a preview mode by default.
#
#.EXAMPLE
# .\New-CreateAssetGroup_VariantRequest.ps1 -AssetGroupQualifiersFilePath '.\AssetGroupQualifiers.csv' -OwnerId "3a803621-fcaa-4727-899f-34c9398cb02b" -VariantDefinitionId "292f7eb2-80a5-4b75-935d-d92ec52306e5" -GeneralContractorAlias "<GCAlias>" -CelaContactAlias "<CELAAlias>" -Location PPE -ErrorAction Stop
##############################################################################
param(	
	[parameter(Mandatory=$true)]	
	$AssetGroupQualifiersFilePath, 
   
	[parameter(Mandatory=$true)]
	$OwnerId,

    [parameter(Mandatory=$true)]
    $VariantDefinitionId,

    [parameter(Mandatory=$true)]
    $GeneralContractorAlias,

    [parameter(Mandatory=$true)]
    $CelaContactAlias,

	[parameter(Mandatory=$true)]
	[ValidateSet('PROD','PPE')]
	$Location,    

    [switch]
	$Force
)

Import-Module PDMS

Connect-PdmsService -Location $Location

################################# Create AssetGroups for the given Asset Qualifiers #############################################
#failedAssetgroups array.
$failedAssetGroups = @()

#Failedoutput file
$failedResultFile = "$($PSScriptRoot)\VariantOutput\failedResult.txt"

#Created AssetGroups for given Qualifiers. Contains AssetGroupQualifier and AssetGroupId of the newly created asset groups.
$CreatedAssetGroupsFile = "$($PSScriptRoot)\VariantOutput\createdAssetGroups.csv"

#Information about the variant request that was created.
$CreatedVariantRequestFile = "$($PSScriptRoot)\VariantOutput\createdVariantRequest.txt"

#list of newly created AssetGroupQualifiers with AssetGroupId
$CreatedAssetGroups = @()

$StringGuid ="00000000-0000-0000-0000-000000000000"
$emptyGUID = [System.Guid]::New($StringGuid)

$EmptyAssetGroups = @()
 

#-----------------------------------Get row count in given input file--------------------------------------------------#
$csvFile = Import-Csv -path $AssetGroupQualifiersFilePath
$NumRowsinCSV = $csvFile | Measure-Object
#----------------------------------------------------------------------------------------------------------------------#


#If the file contains more than 100 exit the script. 
if($NumRowsinCSV.Count -gt 100)
{
    Write-Host "Exiting the script because the input file contains more than 100 Asset Groups. Please split the input into multiple files with 100 or fewer Asset Groups per file."
    exit
}

Write-Host '----------------------------------- Creating Asset groups ----------------------------------------------'
#importing AssetGroupQualifiers and create AssetGroups
#Appending all Created assetGroupQualifiers and AssetGroupIds to array
$csvFile | ForEach-Object{ 
    $assetgroupqualifier = $_
    try{
        $qualifier = $assetgroupqualifier.AssetGroupQualifier 

        $ag = New-PdmsObject AssetGroup    
        $ag.OwnerId = $OwnerId
        $ag.Qualifier= $qualifier  

        if($Force)
        {
            $newAg = New-PdmsAssetGroup -Value $ag            
        }
        else
        {
            $newAg = $ag
            $newAg.Id = [GUID]::Empty
            $EmptyAssetGroups += $newAg       
        }

        $CreatedAssetGroups += $newAg          
    }

    catch [Exception]{
    # save the asset group qualifier from the exception
        $failedAssetGroups += $qualifier
        Write-Warning $_        
    }
}

if($failedAssetGroups.Length -gt 0){
    Write-Host '--------------- Failed Asset Group Qualifiers ---------------------'

    $failedAssetGroups | Out-File $failedResultFile -Force
    $failedAssetGroups

    #if any Failed Asset Groups found, remove created asset groups. 
    $CreatedAssetGroups | ForEach-Object{
        Remove-PdmsAssetGroup -Value $_
    }

    Write-Host "----- Asset Groups creation failed. Please look in $failedResultFile for Details. No new asset groups created, please correct the file and retry.-----------"
    exit
}

if($EmptyAssetGroups.Count -gt 0)
{
    Write-Host 'Script is running without Force. Total Asset Groups count: ' $EmptyAssetGroups.Count
    exit
} 
else
{
    Write-Host 'Total Asset Groups count: ' $CreatedAssetGroups.Count
}

################################# END Of Create AssetGroups for the given Asset Qualifiers #############################################


################################# Create Variant request for the above created Asset Groups ############################################

$VariantRelationshipObjectArray = @()


Write-Host '---------------Creating Variant request for Asset Group Qualifiers -----------------------------------------------'
# For each asset group in $CreatedAssetGroups, create variant relationship objects
$CreatedAssetGroups | ForEach-Object{
    $assetgroup = $_ 
    $assetGroupId =  $assetgroup.Id

    #If none of the asset group Id found in the file, Stop the below variant request creation.
    if($assetGroupId -eq "00000000-0000-0000-0000-000000000000")
    {
        $EmptyAssetGroups += $assetgroup.Qualifier.Value    
    }
    else
    {
        try
        {  
            # Get AssetGroupId and call PDMS AssetGroup details to check already have a variant or not.           
            $assetGroupDetails = Get-PdmsAssetGroup -Id $assetGroupId
        
            # Assign Qualifier and Id to AssetGroupQualifier and AssetGroupId variables.
            $assetGroupQualifier = $assetGroupDetails.Qualifier        
            $assetGroupId = $assetGroupDetails.Id                
            $assetGroupOwnerId = $assetGroupDetails.OwnerId                        

            # This actually contains the asset group id [and qualifiers] to which this variant is applied. 
            # We have to populate only the asset group id. We can use the following commands for this task:
            $VariantRelationshipObject = New-pdmsObject VariantRelationship                
            Set-PdmsProperty -Object $VariantRelationshipObject -Name AssetGroupId -Value $assetGroupId		        
            Set-PdmsProperty -Object $VariantRelationshipObject -Name AssetQualifier -Value $assetGroupQualifier
            $VariantRelationshipObjectArray += $VariantRelationshipObject

        }
        catch [Exception]{      
            Write-Warning $_  

            $failedAssetGroups += $assetgroupqualifiers.Qualifier
            $failedAssetGroups | Out-File $failedResultFile -Force
            $failedAssetGroups
    
            # if we get an exception, clean up the asset groups we created         

            $CreatedAssetGroups | ForEach-Object{
                Remove-PdmsAssetGroup -Value $_
            }
        
            Write-Host "----- Variant Request creation failed. Please look in $failedResultFile for Details. No new asset groups created, please correct the file and retry.-----------------"
            exit
        }
    }
}

if($VariantRelationshipObjectArray.Length -ne 0 -and $EmptyAssetGroups.Length -eq 0)
{

    try{
        # create AssetGroup variant object. We can use the following commands for this task:
        $requestedVariant = New-pdmsObject AssetGroupVariant
        Set-PdmsProperty -Object $requestedVariant -Name VariantId -Value $variantDefinitionId    

        # Get DataOwnerName by calling PDMS
        $DataOwnerDetails = New-PdmsObject DataOwner
        $DataOwnerDetails = Get-PdmsDataOwner -Id $OwnerId
        $DataOwnerName = $DataOwnerDetails.Name  

        $RequesterAlias= $env:UserName    
    
        # Now create the variant request object. We can use the following commands for this task:
        $variantRequest = New-pdmsObject VariantRequest

        Set-PdmsProperty -Object $variantRequest -Name OwnerId -Value $OwnerId
        set-PdmsProperty -Object $variantRequest -Name OwnerName -Value $DataOwnerName   
        set-PdmsProperty -Object $variantRequest -Name RequesterAlias -Value $RequesterAlias   
        set-PdmsProperty -Object $variantRequest -Name GeneralContractorAlias -Value $GeneralContractorAlias
        set-PdmsProperty -Object $variantRequest -Name CelaContactAlias -Value $CelaContactAlias
        Set-PdmsProperty -Object $variantRequest -Name RequestedVariants -Value (New-PdmsArray @($requestedVariant))
        Set-PdmsProperty -Object $variantRequest -Name VariantRelationships -Value (New-PdmsArray $VariantRelationshipObjectArray)
        
        if ($Force) {
        $newVariantRequest = New-PdmsVariantRequest $variantRequest      
        }
        else {
            $newVariantRequest = $variantRequest
        }
        $newVariantRequest
    }
    catch [Exception]{
        $failedAssetGroups += $assetgroupqualifiers.Qualifier
        $failedAssetGroups | Out-File $failedResultFile -Force
        $failedAssetGroups

        # if we get an exception, clean up the asset groups we created
        $CreatedAssetGroups | ForEach-Object{
            Remove-PdmsAssetGroup -Value $_
        }            
        Write-Host "----- Variant Request creation failed. Please look in $failedResultFile for Details. No new asset groups created, please correct the file and retry---------------"
        exit
    }        
}
else
{    
    Write-Host "----- No variant relations ships were created.  Variant Request creation failed. -----------------"
    exit
}
    
################################# END of Create Variant request for the above created Asset Groups ############################################


Write-Host '--------------- AssetGroupQualifiers Details ---------------------'
#Output AssetGroupQualifier and AssetGroupId values to CSV file.
$CreatedAssetGroups | 
  	Select-Object @{
                        name='AssetGroupQualifier'
                        expr={$_.Qualifier.Value}
                    } ,
		@{
            name='AssetGroupId'
            expr={$_.Id}
        } |  Export-Csv -Path $CreatedAssetGroupsFile -NoTypeInformation -Force

Write-Host "----- Asset Groups created successfully. Please look in $CreatedAssetGroupsFile for Details. -----------------"

Write-Host '--------------- Variant Request Details ----------------------'
Get-PdmsVariantRequest -Id $newVariantRequest.Id | Out-File $CreatedVariantRequestFile -Force

# remove any previous output from the failure file
'' | Out-File $failedResultFile -Force

Write-Host "----- Variant Request created successfully. Please look in $CreatedVariantRequestFile for Details. View the variant request details in DataGrid - https://datagrid.microsoft.com/Tagging/VariantRequests-------------"
