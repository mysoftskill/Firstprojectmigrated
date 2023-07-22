<#
 
 This file contains utility functions for performing safe connection string and key rotations for Azure resources whose secrets are stored in
 Azure Key Vault.

 Supported resource types:
     1) Storage account keys/connection strings
     2) CosmosDb keys/connection strings
     3) Event Hub keys/connection strings

 Background:
     SOC2 certification requires periodic key rotation (90 days in frequency). All of the above resources have primary and secondary keys.
     This means that each of those keys must be rotated at 90 day frequency, so there will be one rotation each 45 days to form a primary-secondonary-primary pattern.

     Key Vault stores the current active key for a resource. This can be either the primary or the secondary key. Imagine the following schedule:

     Day |   Active Key 

     0   |     Primary0
     44  |     Primary0
     45  |   Secondary0 
     89  |   Secondary0
     90  |     Primary1 
     135 |   Secondary1

     At day 90, the original primary key becomes unusable, since the primary key gets rotated. At day 90, the secondary key that was rotated on day 45 becomes deprecated,
     and dat day 135 that key will be obsolete.

     Secret Rotation for privacy services needs to be done carefully, since in some cases we give out SAS tokens to partners (non-Cosmos export). For these cases,
     we must be sure not to over-rotate, since a too-eager rotation will invalidate existant SAS tokens. Therefore, we want to rotate at the 45 day mark,
     which gives SAS tokens a time to live of at least 45 days, which exceeds the lifespan of a DSR. Shorter time periods may make some scenarios complicated.
 
 Prerequisites:
    A resource group with a single Azure Key Vault that has the "KeyRotationKeyVault" tag applied.
    This key vault must be populated with a list of secrets, each of which has a value that corresponds to a current key of a resource in the resource group.

 Utility Functions:
    AutoTag-Secrets: 
        Locates the given key vault, reads the secrets and their values, 
        reads the resources in the resource group, and tries to find matches between keys. 
        It then tags the secret in the key vault as pointing at a particular resource.
        The tags it applies are:
            'ResourceType', which indicates the type of resource a secret points to (eventhub/storageaccount/cosmosdb/external)
            'ResourceName', which indicates the name (within the resource) group of the resource. This does not necessarily correspond to the name of the secret.


    Validate-SecretTags:
        Examines the key vault and the tags within it. Ensures that:
            1) All secrets point to a valid resource
            2) Each resource is pointed to by at most one key (it would be dangerous to rotate keys otherwise, since we might double-rotate)
            3) All resources in the asset group are tracked by a key (this is a warning)

    Rotate-Secrets:
        This is the real purpose of all of this, to rotate keys! It invokes Validate-SecretTags first and will
        not proceed if there are errors.

        Rotate-Secrets works by iterating through the secrets in the key vault, and looking up their associated resource.
        It first checks to make sure enough time has elapsed for the secret to be rotated, based on the time it was uploaded to
        key vault. This is configurable by a parameter, but should be left at 45 days under normal circumstances.

        It then compares the active key in key vault with the current primary and secondary keys for the resource. If the primary or secondary
        key matches, it will rotate the opposite key, and update key vault with the newly rotated key. If no key matches what is in key vault,
        it will throw an exception and refuse to do work. This is a safety measure, and manual intervention will be required.

        Let's talk about safety. It's been 45 days and it's time to rotate keys. The script sees that the current active key for resource R
        is the secondary key, so it decides to rotate the primary key. The primary key gets rotated successfully, but updating the secret in AKV
        fails. What happens now? Since the 45 day comparison timestamp is based on when the secret was added to AKV, a re-run of the 
        script will still observe that the primary key needs to be rotated. So the script will rotate again, and re-attempt to upload to key vault. 
        This time it succeeds, and all is normal. It won't be rotated again for 45 days. This is why it's important to use the key vault timestamps
        to figure out when to rotate. If in the previous example, we had used the resource log's timestamps, it would have seen that a rotation 
        already happened despite the failure to upload to AKV.
 #>


#region Helpers

Function PromptForYesNoChoice
{
    param ([string] $title, [string] $message)
    
    $choices = New-Object Collections.ObjectModel.Collection[Management.Automation.Host.ChoiceDescription]
    $choices.Add((New-Object Management.Automation.Host.ChoiceDescription -ArgumentList '&Yes'))
    $choices.Add((New-Object Management.Automation.Host.ChoiceDescription -ArgumentList '&No'))

    $decision = $Host.UI.PromptForChoice($title, $message, $choices, 1)
    if ($decision -eq 0) 
    {
      return $true
    } 
    else 
    {
      return $false
    }
}

# Returns a normalized, unique name for a storage account
Function GetStorageAccountPath
{
    param ([string] $storageAccountName)

    return "StorageAccount/$storageAccountName".ToLowerInvariant()
}

# Returns a normalized, unique name for an event hub SAS token
Function GetEventHubSASTokenPath
{
    param ([string] $eventHubNamespace, [string] $sasTokenName, [string] $eventHubName = $null)

    if ([string]::IsNullOrWhiteSpace($eventHubName))
    {
        return "EventHubNamespace/$eventHubNamespace/key/$sasTokenName".ToLowerInvariant()
    }
    else
    {
        return "EventHubNamespace/$eventHubNamespace/eventHub/$eventHubName/key/$sasTokenName".ToLowerInvariant()
    }
}


# Returns a normalized, unique name for a CosmosDb instance
Function GetCosmosDbPath
{
    param ([string] $cosmosDbName)
    return "CosmosDb/$cosmosDbName".ToLowerInvariant()
}

Function Ensure-AzureAccountLoggedIn
{
    param ([string] $subscriptionName)

    try
    {
        $ctx = Get-AzContext -ErrorAction Continue
        Write-Host -ForegroundColor Green "You are already logged in! CTX=$($ctx.Name)"
    }
    catch [System.Management.Automation.PSInvalidOperationException]
    {
        Login-AzAccount    
    }
    
    Write-Host -ForegroundColor Green "Setting AzContext to subscription: $subscriptionName"
    Set-AzContext -SubscriptionName $subscriptionName
}

Function Get-ResourceGroup
{   
    param ([string] $resourceGroupName)
     
    Write-Host -ForegroundColor Green "Looking up resource group: $resourceGroupName"
    $rg = Get-AzResourceGroup -ResourceGroupName $resourceGroupName
    if ($rg -eq $null)
    {
        throw "Couldn't find resource group $resourceGroupName"
    }

    return $rg
}

Function Get-TaggedKeyVault
{
    param ([string] $resourceGroupName)

    # Find the key vault. It will have pointers to the rest of our resources that we rotate.
    Write-Host -ForegroundColor Green "Looking up Key Rotation Key Vault within the resource group: $resourceGroupName"
    $keyVault =  Get-AzResource `
        -ResourceType "Microsoft.KeyVault/vaults" `
        -ResourceGroupName $resourceGroupName `
        | Where-Object { $_.Tags -ne $null -and $_.Tags.ContainsKey("KeyRotationKeyVault") }

    if ($keyVault.Count -ne 1)
    {
        throw "There must be exactly one key vault with the 'KeyRotationKeyVault' tag"
    }

    return $keyVault
}

#region Connection String Formatters

Function Get-CosmosDbConnectionString
{
    param(
        [Parameter(Mandatory=$true)][string] $accountName,
        [Parameter(Mandatory=$true)][string] $key)

    return "AccountEndpoint=https://$accountName.documents.azure.com:443/;AccountKey=$key;"
}

Function Get-StorageConnectionString
{
    param(
        [Parameter(Mandatory=$true)][string] $accountName,
        [Parameter(Mandatory=$true)][string] $key)

    return "DefaultEndpointsProtocol=https;AccountName=$accountName;AccountKey=$key;EndpointSuffix=core.windows.net"
}

#endregion

#region Getters for keysets

Function Get-StorageAccountKeys
{
    param ([string]$resourceGroupName, [string]$resourceName, $stateBundle)

    $keys = Get-AzStorageAccountKey -ResourceGroupName $resourceGroupName -Name $resourceName

    $primarykey = ($keys | Where-Object { $_.KeyName -eq "key1" }).Value
    $secondaryKey = ($keys | Where-Object { $_.KeyName -eq "key2" }).Value

    return @{
        primaryKey=$primaryKey;
        primaryConnectionString=(Get-StorageConnectionString -accountName $resourceName -key $primarykey);
        secondaryKey=$secondaryKey;
        secondaryConnectionString=(Get-StorageConnectionString -accountName $resourceName -key $secondaryKey);
    }
}

Function Get-CosmosDbKeys
{
    param ([string]$resourceGroupName, [string]$resourceName, $stateBundle)

    $keys = Invoke-AzResourceAction -Action listKeys `
                                    -ResourceType "Microsoft.DocumentDb/databaseAccounts" `
                                    -ResourceGroupName $resourceGroupName `
                                    -Name $resourceName `
                                    -Force
        
    return @{
        primaryKey=$keys.primaryMasterKey;
        primaryConnectionString=(Get-CosmosDbConnectionString -accountName $resourceName -key $keys.primaryMasterKey);
        secondaryKey=$keys.secondaryMasterKey;
        secondaryConnectionString=(Get-CosmosDbConnectionString -accountName $resourceName -key $keys.secondaryMasterKey);
    }
}

Function Get-EventHubKeys
{
    param ([string]$resourceGroupName, [string]$resourceName, $stateBundle)

    $keys = Invoke-AzResourceAction -ResourceGroupName $resourceGroupName -ResourceType $stateBundle.ResourceType -ResourceName $stateBundle.ResourceName -Action listKeys -Force
        
    # happily, event hub returns the same structure of response. However, let's be explicit.
    return @{
        primaryKey=$keys.primaryKey;
        primaryConnectionString=$keys.primaryConnectionString;
        secondaryKey=$keys.secondaryKey;
        secondaryConnectionString=$keys.secondaryConnectionString;
    }
}

#endregion

#region Rotation Functions for single keys

Function Rotate-SingleStorageAccountKey
{
    param ([string]$resourceGroupName, [string]$resourceName, [bool]$rotatePrimary, $stateBundle)
    $toRotate = if ($rotatePrimary) {"key1"} else {"key2"}
    $capture = New-AzStorageAccountKey -ResourceGroupName $resourceGroupName -Name $storageAccountName -KeyName $toRotate
}

Function Rotate-SingleCosmosDbKey
{
    param ([string]$resourceGroupName, [string]$resourceName, [bool]$rotatePrimary, $stateBundle)
    $toRotate = if ($rotatePrimary) {"Primary"} else {"Secondary"}
    $capture = Invoke-AzResourceAction `
        -Action regenerateKey `
        -ResourceType "Microsoft.DocumentDb/databaseAccounts" `
        -ResourceGroupName $resourceGroupName `
        -Name $resourceName `
        -Parameters @{"keyKind"=$toRotate} `
        -Force
}

Function Rotate-SingleEventHubKey
{
    param ([string]$resourceGroupName, [string]$resourceName, [bool]$rotatePrimary, $stateBundle)
    $toRotate = if($rotatePrimary) {"PrimaryKey"} else {"SecondaryKey"} 
    $capture = Invoke-AzResourceAction `
        -ResourceGroupName $resourceGroupName `
        -ResourceType $stateBundle.ResourceType `
        -ResourceName $stateBundle.ResourceName `
        -Action regenerateKeys `
        -Parameters @{"keyType"=$toRotate} `
        -Force
}

#endregion

Function Rotate-GenericSecret
{
    param(
        # The secret from Key Vault
        [Parameter(Mandatory=$true)][Microsoft.Azure.Commands.KeyVault.Models.PSKeyVaultSecretIdentityItem]$secret,

        # The name of the resource in the resource group
        [Parameter(Mandatory=$true)][string] $resourceName,

        # The name of the resource group
        [Parameter(Mandatory=$true)][string] $resourceGroupName,

        # A callback with the following signature:
        #  (string resourceGroupName, string resourceName, object stateBundle) -> @{
        #       primaryKey: "string"
        #       primaryConnectionString: "string"
        #       secondaryKey: "string"
        #       secondaryConnectionString: "string"
        #   }
        [Parameter(Mandatory=$true)] $listKeysCallback,

        # A void callback with the following signature:
        # (string resourceGroupName, string resourceName, bool rotatePrimary, object stateBundle) -> (void)
        [Parameter(Mandatory=$true)] $rotateKeyCallback,

        # A state bundle passed into the callbacks.
        [Parameter(Mandatory=$false)] $stateBundle = @{})

    $secretValue = $secret.SecretValueText
    if ([string]::IsNullOrWhiteSpace($secretValue))
    {
        throw "Error reading KV key for resource '$resourceName'. Existing secret is null/empty."
    }

    Write-Host -ForegroundColor Green "Reading keys for resource '$resourceName'"
    $keys = Invoke-Command $listKeysCallback -ArgumentList $resourceGroupName,$resourceName,$stateBundle

    if ([string]::IsNullOrWhiteSpace($keys.primaryKey) -or [string]::IsNullOrWhiteSpace($keys.secondaryKey))
    {
        throw "Error reading keys for resource '$resourceName'. Must have primary/secondary"
    }

    $isConnectionString = $false
    $rotatePrimary = $false

    if ($secretValue -eq $keys.primaryKey -or $secretValue -eq $keys.primaryConnectionString)
    {
        # Primary is the current key, so we need to rotate secondary.
        $rotatePrimary = $false
        $isConnectionString = $keys.primaryConnectionString -eq $secretValue
    }
    elseif ($secretValue -eq $keys.secondaryKey -or $secretValue -eq $keys.secondaryConnectionString)
    {
        # Secondary is the current key, so we need to rotate primary.
        $rotatePrimary = $true
        $isConnectionString = $keys.secondaryConnectionString -eq $secretValue
    }
    else
    {
        throw "Unable to determine active key for current resource. Unable to rotate. Please check tags and whether the resource exists."
    }

    # rotate the key
    Write-Host -ForegroundColor Green "Rotating key for resource '$resourceName'. IsConnectionString=$isConnectionString, RotatePrimary=$rotatePrimary"
    $capture = Invoke-Command $rotateKeyCallback -ArgumentList $resourceGroupName,$resourceName,$rotatePrimary,$stateBundle

    # reload the keys
    Write-Host -ForegroundColor Green "Reloading resource '$resourceName' keys after rotation"
    $newKeys = Invoke-Command $listKeysCallback -ArgumentList $resourceGroupName,$resourceName,$stateBundle

    $newKeyValue = $null

    if ($rotatePrimary)
    {
        # primary is the new key
        if ($isConnectionString)
        {
            $newKeyValue = $newKeys.primaryConnectionString
        }
        else
        {
            $newKeyValue = $newKeys.primaryKey
        }
    }
    else
    {
        # secondary is the new key
        if ($isConnectionString)
        {
            $newKeyValue = $newKeys.secondaryConnectionString
        }
        else
        {
            $newKeyValue = $newKeys.secondaryKey
        }
    }

    if ([string]::IsNullOrWhiteSpace($newKeyValue))
    {
        throw "Unexpected error! New key is null/whitespace after rotating."
    }

    $newKeyValue = ConvertTo-SecureString -String $newKeyValue -AsPlainText -Force

    Write-Host -ForegroundColor Green "Uploading new key for resource '$resourceName' to key vault..."
    $newSecret = Set-AzKeyVaultSecret -VaultName $secret.VaultName -Name $secret.Name -Tags $secret.Tags -SecretValue $newKeyValue

    Write-Host -ForegroundColor Green "Rotated key for resource '$resourceName'. You're a champion."
}

Function Rotate-StorageAccount
{
    param (
        [Parameter(Mandatory=$true)][Microsoft.Azure.Commands.KeyVault.Models.PSKeyVaultSecretIdentityItem]$secret,
        [Parameter(Mandatory=$true)][string]$storageAccountName,
        [Parameter(Mandatory=$true)][string]$resourceGroupName
    )

    Rotate-GenericSecret -secret $secret -resourceName $storageAccountName -resourceGroupName $resourceGroupName -listKeysCallback ${function:Get-StorageAccountKeys} -rotateKeyCallback ${function:Rotate-SingleStorageAccountKey}
}

Function Rotate-CosmosDb
{
    param (
        [Parameter(Mandatory=$true)][Microsoft.Azure.Commands.KeyVault.Models.PSKeyVaultSecretIdentityItem]$secret,
        [Parameter(Mandatory=$true)][string]$cosmosDbName,
        [Parameter(Mandatory=$true)][string]$resourceGroupName
    )

    Rotate-GenericSecret `
        -secret $secret `
        -resourceName $cosmosDbName `
        -resourceGroupName $resourceGroupName `
        -listKeysCallback ${function:Get-CosmosDbKeys} `
        -rotateKeyCallback ${function:Rotate-SingleCosmosDbKey}
}

Function Rotate-EventHubKey
{
    param (
        [Parameter(Mandatory=$true)][Microsoft.Azure.Commands.KeyVault.Models.PSKeyVaultSecretIdentityItem]$secret,
        [Parameter(Mandatory=$true)][string]$eventHubNamespace,
        [Parameter(Mandatory=$true)][string]$resourceGroupName
    )
    
    if ($secret.Tags.ContainsKey("SharedAccessKeyName") -eq $false)
    {
        throw "Event Hubs must indicate the name of the key to rotate in the 'SharedAccessKeyName' tag. The default key is named 'RootSharedAccessKey'."
    }

    $keyName = $secret.Tags["SharedAccessKeyName"].Trim()

    $resourceType = "Microsoft.EventHub/Namespaces/AuthorizationRules"
    $resourceName = "$eventHubNamespace/$keyName"

    if ($secret.Tags.ContainsKey("EventHubName"))
    {
        $eventHubName = $secret.Tags["EventHubName"]
        $resourceType = "Microsoft.EventHub/Namespaces/EventHubs/AuthorizationRules"
        $resourceName = "$eventHubNamespace/$eventHubName/$keyName"
    }

    $stateBundle = @{
        ResourceType=$resourceType;
        ResourceName=$resourceName;
    }

    Rotate-GenericSecret `
        -secret $secret `
        -resourceName $eventHubNamespace `
        -resourceGroupName $resourceGroupName `
        -listKeysCallback ${function:Get-EventHubKeys} `
        -rotateKeyCallback ${function:Rotate-SingleEventHubKey} `
        -stateBundle $stateBundle
}

#endregion

# Function that examines the key vault's secrets and validates the tags on each item. Specifically, it checks that...
# 1) All secrest have the correct tagging
# 2) Secrets all point to real azure resources in the same asset group
# 3) Each secret points to a real azure resource
# 4) Each resource is pointed to by one secret.
Function Validate-SecretTags
{
    param(
        [Parameter(Mandatory=$true)][string]$subscriptionName,
        [Parameter(Mandatory=$true)][string]$resourceGroupName)
        
    $ErrorActionPreference = "Stop"

    Ensure-AzureAccountLoggedIn -subscriptionName $subscriptionName
    $rg = Get-ResourceGroup -resourceGroupName $resourceGroupName
    $keyVault = Get-TaggedKeyVault -resourceGroupName $resourceGroupName

    Write-Host -ForegroundColor Green "Reading keyvault secret metadata..."
    $secrets = Get-AzKeyVaultSecret -VaultName $keyVault.Name

    # Prefetch the list of resources so we can verify that tags point at the right resources.
    Write-Host -ForegroundColor Green "Querying resource group assets..."
    $eventHubNamespaces = Get-AzResource -ResourceGroupName $rg.ResourceGroupName -ResourceType Microsoft.EventHub/namespaces
    $cosmosDbs = Get-AzResource -ResourceGroupName $rg.ResourceGroupName -ResourceType Microsoft.DocumentDb/databaseAccounts
    $storageAccounts = Get-AzResource -ResourceGroupName $rg.ResourceGroupName -ResourceType Microsoft.Storage/storageAccounts

    # Keep a set of "ref counts" to ensure that we (a) don't reference the same artifact twice from AKV, which could make key rotation disastrous,
    # and (b) so we are aware of whether there are secrets that are not managed by AKV
    Write-Host -ForegroundColor Green "Building ref count map"
    $refCounts = @{}

    $hasError = $false

    foreach ($storageAccount in $storageAccounts)
    {
        $normalizedName = GetStorageAccountPath -storageAccountName $storageAccount.Name
        $refCounts[$normalizedName] = 0
    }

    foreach ($cosmosDb in $cosmosDbs)
    {
        $normalizedName = GetCosmosDbPath -cosmosDbName $cosmosDb.Name
        $refCounts[$normalizedName] = 0
    }

    foreach ($eventHubNamespace in $eventHubNamespaces)
    {
        $eventHubs = Get-AzEventHub -ResourceGroupName $rg.ResourceGroupName -Namespace $eventHubNamespace.Name

        # Look for keys on individual event hub instances within the namespace
        foreach ($eventHub in $eventHubs)
        {
            $rules = Get-AzEventHubAuthorizationRule -ResourceGroupName $rg.ResourceGroupName -Namespace $eventHubNamespace.Name -Eventhub $eventHub.Name
            foreach ($rule in $rules)
            {
                $normalizedName = GetEventHubSASTokenPath -eventHubNamespace $eventHubNamespace.Name -eventHubName $eventHub.Name -sasTokenName $rule.Name
                $refCounts[$normalizedName] = 0
            }
        }

        # Look for keys at the namespace level
        $rules = Get-AzEventHubAuthorizationRule -ResourceGroupName $rg.ResourceGroupName -Namespace $eventHubNamespace.Name
        foreach ($rule in $rules)
        {
            $normalizedName = GetEventHubSASTokenPath -eventHubNamespace $eventHubNamespace.Name -sasTokenName $rule.Name
            $refCounts[$normalizedName] = 0
        }
    }


    foreach ($secret in $secrets)
    {        
        $name = $secret.Name

        Write-Host -ForegroundColor Green "Examining Secret '$name'..."

        if ($secret.ContentType -eq "application/x-pkcs12")
        {
            Write-Host -ForegroundColor Green "Secret $name is a certificate; not asking for tags."
            continue
        }

        [string]$resourceType = $null
        [string]$resourceName = $null

        if ($secret.Tags -ne $null)
        {
            $resourceType = [string]$secret.Tags["ResourceType"]
            $resourceName = [string]$secret.Tags["ResourceName"]
        }

        if ($resourceType -eq "external")
        {
            if ($secret.Tags.Count -eq 1)
            {
                Write-Host -ForegroundColor Green "Secret $name's ResourceType is External; and found no other tags, so skipping."
            }
            else
            {
                Write-Host -ForegroundColor Red "ERR: Secret $name has External resource type, but contains other tags besides 'ResourceType'. These tags should be removed."
                $hasError = $true
            }
        }
        elseif ($resourceType -eq "storageaccount")
        {
            $normalizedName = GetStorageAccountPath -storageAccountName $resourceName
            if ($refCounts.ContainsKey($normalizedName))
            {
                $refCounts[$normalizedName]++
            }
            else
            {
                Write-Host -ForegroundColor Red "Key Vault secret '$name' refers to a Storage Account that appears to not exist: $normalizedName"
                $hasError = $true
                continue
            }
        }
        elseif ($resourceType -eq "cosmosdb")
        {            
            $normalizedName = GetCosmosDbPath -cosmosDbName $resourceName
            if ($refCounts.ContainsKey($normalizedName))
            {
                $refCounts[$normalizedName]++
            }
            else
            {
                Write-Host -ForegroundColor Red "Key Vault secret '$name' refers to a CosmosDb account that appears to not exist: $normalizedName"
                $hasError = $true
                continue
            }
        }
        elseif ($resourceType -eq "eventhub")
        {   
            $sasKeyName = $secret.Tags["SharedAccessKeyName"]
            $eventHubName = $secret.Tags["EventHubName"]

            if ($sasKeyName -eq $null)
            {
                Write-Host -ForegroundColor Red "Secret $name is for an event hub namespace, but is missing the 'SharedAccessKeyName' tag.";
                $hasError = $true
                continue
            }

            $normalizedName = GetEventHubSASTokenPath -eventHubNamespace $resourceName -eventHubName $eventHubName -sasTokenName $sasKeyName
            if ($refCounts.ContainsKey($normalizedName))
            {
                $refCounts[$normalizedName]++
            }
            else
            {
                Write-Host -ForegroundColor Red "Key Vault secret '$name' refers to a Event Hub SAS Token that appears to not exist: $normalizedName"
                $hasError = $true
                continue
            }
        }
        else
        {
            Write-Host -ForegroundColor Red "Secret '$name' does not have a 'ResourceType' tag."
            $hasError = $true
        }
    }

    #Finally, take a run through the ref counts and make sure everything looks reasonable.
    Write-Host ""
    Write-Host -ForegroundColor Green "Examining ref counts of assets. Our goal is here is to ensure that no asset is pointed to more than once in Key Vault"
    foreach ($key in $refCounts.Keys)
    {
        $count = $refCounts[$key]
        if ($count -eq 0)
        {
            Write-Host -ForegroundColor Yellow "WARN: Resource '$key' is not referenced from any secret in Key Vault. Consider removing this resource."
        }
        elseif ($count -eq 1)
        {
            Write-Host -ForegroundColor Green "INFO: Resource '$key' is only referenced from one Key Vault secret. Good job!"
        }
        elseif ($count -gt 1)
        {
            Write-Host -ForegroundColor Red "ERR: Resource '$key' is referenced from more than one secret in Key Vault. This is an error and rotation should not proceed until it is resolved."
            $hasError = $true
        }
        else
        {
            Write-Host -ForegroundColor Red "ERR: Resource '$key' has an unexpected ref count of '$count'. Recommend investigation."
            $hasError = $true
        }
    }

    if ($hasError)
    {
        return $false
    }
    
    return $true
}

Function Rotate-Secrets
{
    param(
        [Parameter(Mandatory=$true)][string]$subscriptionName,
        [Parameter(Mandatory=$true)][string]$resourceGroupName,
        [Parameter(Mandatory=$false)][int]$SecretRotationFrequencyDays = 45)
        
    $ErrorActionPreference = "Stop"
    $valid = Validate-SecretTags -subscriptionName $subscriptionName -resourceGroupName $resourceGroupName

    if ($valid -eq $false)
    {
        Write-Host -ForegroundColor Red "ERR: Unable to rotate secrets while there are tagging errors in the key vault."
        return
    }

    $rg = Get-ResourceGroup -resourceGroupName $resourceGroupName
    $keyVault = Get-TaggedKeyVault -resourceGroupName $resourceGroupName

    
    $secrets = Get-AzKeyVaultSecret -VaultName $keyVault.Name

    foreach ($secret in $secrets)
    {        
        $name = $secret.Name

        if ($secret.ContentType -eq "application/x-pkcs12")
        {
            Write-Host -ForegroundColor Green "Secret $name is a certificate; not rotating."
            continue
        }

        if ($secret.Tags -eq $null)
        {
            throw "Secret $name has no tags. Unable to rotate keys."
        }

        $resourceType = [string]$secret.Tags["ResourceType"]
        if ([System.String]::IsNullOrWhiteSpace($resourceType))
        {
            throw "Secret $name 'ResourceType' tag is not specified."
        }
        $resourceType = $resourceType.Trim().ToLowerInvariant()

        if ($resourceType -eq "external")
        {
            Write-Host -ForegroundColor Yellow "Secret $name's ResourceType is External; not rotating."
            continue
        }

        $resourceName = [string]$secret.Tags["ResourceName"]
        if ([System.String]::IsNullOrWhiteSpace($resourceName))
        {
            throw "Secret $name 'ResourceName' tag is not specified."
        }
        $resourceName = $resourceName.Trim().ToLowerInvariant()

        if (([System.DateTimeOffset]::UtcNow - $secret.Created).TotalDays -lt $SecretRotationFrequencyDays)
        {
            Write-Host -ForegroundColor Yellow "Secret $name is not yet old enough to be rotated. Secrets are rotated after $SecretRotationFrequencyDays days.";
            continue
        }

        Write-Host "Rotating $resourceType $name..."

        $secret = Get-AzKeyVaultSecret -VaultName $keyVault.Name -Name $secret.Name
        $version = $secret.Version
    
        Write-Host  -ForegroundColor Green "Current version for $resourceType $name is: $version"

        switch ($resourceType)
        {
            "cosmosdb" { Rotate-CosmosDb -secret $secret -cosmosDbName $resourceName -resourceGroupName $rg.ResourceGroupName; break }
            "eventhub" { Rotate-EventHubKey -secret $secret -eventHubNamespace $resourceName -resourceGroupName $rg.ResourceGroupName; break }
            "storageaccount"  { Rotate-StorageAccount -secret $secret -storageAccountName $resourceName -resourceGroupName $rg.ResourceGroupName; break }
            default {  throw "Secret $name has unknown resource type: $resouceType" }
        }
    
        Write-Host -ForegroundColor Green "Rotated $resourceType $name!"
    }
}

# Reads all secrets from the asset group into memory, and attempts to match them with secrets in the key vault that are not tagged.
Function AutoTag-Secrets
{
    param(
        [Parameter(Mandatory=$true)][string]$subscriptionName,
        [Parameter(Mandatory=$true)][string]$resourceGroupName)

    $ErrorActionPreference = "Stop"

    Ensure-AzureAccountLoggedIn -subscriptionName $subscriptionName
    $rg = Get-ResourceGroup -resourceGroupName $resourceGroupName
    $keyVault = Get-TaggedKeyVault -resourceGroupName $resourceGroupName

    Write-Host -ForegroundColor Green "Reading keyvault secret metadata..."
    $untaggedSecrets = Get-AzKeyVaultSecret -VaultName $keyVault.Name | Where-Object { ($_.Tags -eq $null) -or (-not $_.Tags.ContainsKey("ResourceType")) } | Where-Object { $_.ContentType -ne "application/x-pkcs12" }

    Write-Host -ForegroundColor Green "Found $($untaggedSecrets.Count) untagged secrets"
    if ($untaggedSecrets.Count -eq 0)
    {
        Write-Host -ForegroundColor Green "No secrets left to tag!"
        return
    }

    # Prefetch the list of resources so we can verify that tags point at the right resources.
    Write-Host -ForegroundColor Green "Querying resource group assets..."
    $eventHubNamespaces = Get-AzResource -ResourceGroupName $rg.ResourceGroupName -ResourceType Microsoft.EventHub/namespaces
    $cosmosDbs = Get-AzResource -ResourceGroupName $rg.ResourceGroupName -ResourceType Microsoft.DocumentDb/databaseAccounts
    $storageAccounts = Get-AzResource -ResourceGroupName $rg.ResourceGroupName -ResourceType Microsoft.Storage/storageAccounts
    
    # Stores a list of keys, where each item has the following format:
    # {
    #    ResouceType = "cosmosdb/storageaccount/etc"
    #    ResourceName = "name"
    #    Keys =
    #    [
    #       { Type = "connectionstring/key/etc", Value = "the key" },
    #       { Type = "connectionstring/key/etc", Value = "anotherkey" },
    #    ],
    #    ExtraTags = @{}
    # }
    $resourceList = @()

    # Fetch the keys for each type of account.
    Write-Host -ForegroundColor Green "Reading storage account keys..."
    foreach ($sa in $storageAccounts)
    {
        $keys = Get-AzStorageAccountKey -ResourceGroupName $rg.ResourceGroupName -Name $sa.Name

        $body = @{}

        $body.ResourceType = "storageaccount"
        $body.ResourceName = $sa.Name
        
        # Map
        $body.Keys = @()

        foreach ($key in $keys)
        {
            $body.Keys += @{ "Type" = "key"; "Value" = $key.Value }
            $body.Keys += @{ "Type" = "connectionstring"; "Value" = "DefaultEndpointsProtocol=https;AccountName=$($sa.Name);AccountKey=$($key.Value);EndpointSuffix=core.windows.net" }
        }

        $resourceList += $body
    }

    Write-Host -ForegroundColor Green "Reading cosmosdb keys..."
    foreach ($cdb in $cosmosDbs)
    {    
        $keys = Invoke-AzResourceAction -Action listKeys `
                                        -ResourceType "Microsoft.DocumentDb/databaseAccounts" `
                                        -ResourceGroupName $rg.ResourceGroupName `
                                        -Name $cdb.Name `
                                        -Force

        $body = @{}

        $body.ResourceType = "cosmosdb"
        $body.ResourceName = $cdb.Name
        $body.Keys = @(
            @{ "Type" = "key"; Value = $keys.primaryMasterKey },
            @{ "Type" = "key"; Value = $keys.secondaryMasterKey },
            @{ "Type" = "connectionstring"; Value = "AccountEndpoint=https://$($cdb.Name).documents.azure.com:443/;AccountKey=$($keys.primaryMasterKey);"},
            @{ "Type" = "connectionstring"; Value = "AccountEndpoint=https://$($cdb.Name).documents.azure.com:443/;AccountKey=$($keys.secondaryMasterKey);"}
        )

        $resourceList += $body
    }

    Write-Host -ForegroundColor Green "Reading event hub keys..."
    foreach ($ehn in $eventHubNamespaces)
    {        
        # Event hubs are trickiest. There are multiple keys at the root of the namespace, and then keys within each constituent event hub.
        $eventHubs = Get-AzEventHub -ResourceGroupName $rg.ResourceGroupName -Namespace $ehn.Name

        # Look for keys on individual event hub instances within the namespace
        foreach ($eh in $eventHubs)
        {
            $rules = Get-AzEventHubAuthorizationRule -ResourceGroupName $rg.ResourceGroupName -Namespace $ehn.Name -Eventhub $eh.Name
            foreach ($rule in $rules)
            {        
                $keys = Invoke-AzResourceAction -ResourceGroupName $rg.ResourceGroupName `
                                                -ResourceType "Microsoft.EventHub/Namespaces/EventHubs/AuthorizationRules" `
                                                -ResourceName "$($ehn.Name)/$($eh.Name)/$($rule.Name)" `
                                                -Action listKeys `
                                                -Force

                $body = @{
                    "ResourceName" = $ehn.Name;
                    "ResourceType" = "eventhub";
                    "Keys" = @(
                        @{ "Type" = "connectionstring"; "Value" = $keys.primaryConnectionString },
                        @{ "Type" = "connectionstring"; "Value" = $keys.secondaryConnectionString },
                        @{ "Type" = "key"; "Value" = $keys.primaryKey },
                        @{ "Type" = "key"; "Value" = $keys.secondaryKey }
                    );
                    ExtraTags = @{ "EventHubName" = $eh.Name; "SharedAccessKeyName" = $rule.Name }
                }

                $resourceList += $body
            }
        }

        # Look for keys at the namespace level
        $rules = Get-AzEventHubAuthorizationRule -ResourceGroupName $rg.ResourceGroupName -Namespace $ehn.Name
        foreach ($rule in $rules)
        {
            $keys = Invoke-AzResourceAction -ResourceGroupName $rg.ResourceGroupName `
                                            -ResourceType "Microsoft.EventHub/Namespaces/AuthorizationRules" `
                                            -ResourceName "$($ehn.Name)/$($rule.Name)" `
                                            -Action listKeys `
                                            -Force

            $body = @{
                "ResourceName" = $ehn.Name;
                "ResourceType" = "eventhub";
                "Keys" = @(
                    @{ "Type" = "connectionstring"; "Value" = $keys.primaryConnectionString },
                    @{ "Type" = "connectionstring"; "Value" = $keys.secondaryConnectionString },
                    @{ "Type" = "key"; "Value" = $keys.primaryKey },
                    @{ "Type" = "key"; "Value" = $keys.secondaryKey }
                );
                ExtraTags = @{ "SharedAccessKeyName" = $rule.Name }
            }

            $resourceList += $body
        }
    }

    # Great, we know all the secrets for all the stuff now. Now, we look through untagged secrets in the 
    # key vault and see if we can find a hit. If we do, then we can update the tags.
    :OuterLoop foreach ($secret in $untaggedSecrets)
    {
        Write-Host -ForegroundColor Green "Examining secret '$($secret.Name)'..."

        $fullSecret = Get-AzKeyVaultSecret -VaultName $keyVault.Name -Name $secret.Name
        
        foreach ($resource in $resourceList)
        {
            foreach ($key in $resource.Keys)
            {
                if ($key.Value -eq $fullSecret.SecretValueText)
                {
                    Write-Host -ForegroundColor Green "Found a match for secret '$($secret.Name)' in '$($resource.ResourceType)' '$($resource.ResourceName)'"
                    $suggestedTags = @{}
                    $suggestedTags.ResourceName = $resource.ResourceName
                    $suggestedTags.ResourceType = $resource.ResourceType

                    if ($resource.ExtraTags -ne $null)
                    {
                        foreach ($extraTag in $resource.ExtraTags.Keys)
                        {
                            $suggestedTags[$extraTag] = $resource.ExtraTags[$extraTag]
                        }   
                    }

                    Write-Host -ForegroundColor Green "Proposed set of tags for secret '$($secret.Name)':"

                    $choices = New-Object Collections.ObjectModel.Collection[Management.Automation.Host.ChoiceDescription]
                    $choices.Add((New-Object Management.Automation.Host.ChoiceDescription -ArgumentList '&Yes'))
                    $choices.Add((New-Object Management.Automation.Host.ChoiceDescription -ArgumentList '&No'))

                    if (PromptForYesNoChoice -title "Update tags on Secret '$($secret.Name)'" -message "New tags:`r`n$($suggestedTags | Out-String)`r`n`r`nDo you want to continue?")
                    {
                        Update-AzKeyVaultSecret -VaultName $keyVault.Name -Name $secret.Name -Tag $suggestedTags
                        Write-Host -ForegroundColor Green "Updated tags on Secret $($secret.Name)"
                    }
                    else 
                    {
                        Write-Host -ForegroundColor Yellow 'Cancelled by user; add tags manually.'
                    }

                    continue OuterLoop
                }
            }
        }

        if (PromptForYesNoChoice -title "Unable to find matching key for secret $($secret.Name)" -message "Is this a secret that is external to this resource group?")
        {
            Update-AzKeyVaultSecret -VaultName $keyVault.Name -Name $secret.Name -Tag @{ResourceType="external"}
            Write-Host -ForegroundColor Green "Updated tags on Secret $($secret.Name) to indicate it is an external resource"
        }
        else
        {
            Write-Host -ForegroundColor Yellow "No tagging options available for secret $($secret.Name)"
        }
    }
}