param (
$AgentId,
$AssetGroupId,
$CommandPage
)

# Summary
# -------
# A wrapper function providing a Cmdlet like interface to submit test command insertion requests.
function Send-TestCommandInsertionRequest
{
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$true)]
        [string] $AgentId,
        [Parameter(Mandatory=$true)]
        [string] $AssetGroupId,
        [Parameter(Mandatory=$true)]
        [string] $CommandPage
    )

    Process
    {
       $BatchReplayRequester = [TestCommandInsertionRequester]::new()
       $BatchReplayRequester.SendRequest($AgentId, $AssetGroupId, $CommandPage)
    }
}

# Summary
# -------
# Get's the token given the environment.
function Get-Token
{
    import-module Microsoft.Identity.Client

    $environmentConfig = Get-EnvironmentConfiguration
    $authority = "https://login.microsoftonline.com/microsoft.onmicrosoft.com"
    $redirect = "https://management.privacy.microsoft.com"
    $pcaConfig = [Microsoft.Identity.Client.PublicClientApplicationBuilder]::Create($environmentConfig.ClientId).WithAuthority($authority).WithRedirectUri($redirect) 
    $TokenResult = $pcaConfig.Build().AcquireTokenInteractive($environmentConfig.Scopes).ExecuteAsync().Result; 
    $token = $TokenResult.AccessToken;
    return $token
}

# Summary
# -------
# A helper function that returns all environment parameters needed to submit a test command insertion requests
function Get-EnvironmentConfiguration
{

    [hashtable]$return = @{}
    $clientId = "25862df9-0e4d-4fb7-a5c8-dfe8ac261930"

    $Scopes = New-Object System.Collections.Generic.List[string]
    $Scopes.Add("api://3c7702e1-cde9-430b-b3ec-9ae4211c3acb/access_as_user")

    $return.Uri = "pcfv2-ppe.privacy.microsoft-ppe.com"
    $return.ClientId = $clientId
    $return.Scopes = $Scopes
    return $return
}

# Summary
# -------
# The class responsible for submitting Test Command Insertion requests.
Class TestCommandInsertionRequester
{
    # Summary
    # -------
    # Submit's test command insertion requests given Agent Id, Asset Group Id and Command Page. 
    # 
    # Parameters
    # ----------
    # agentId: A string specifying the Agent Id
    # assetGroupId: A string specifying the Asset Group Id
    # commandPage: The test command page to be inserted
    [void] SendRequest([string] $AgentId, [string] $AssetGroupId, [string] $CommandPage)
    {
        $token= Get-Token
        $environmentConfig = Get-EnvironmentConfiguration
        $headers = @{ ‘Authorization’ = "Bearer $($token)" } 
        $rawJson = Get-Content $CommandPage | ConvertFrom-Json
        $Body = ($rawJson|ConvertTo-Json -Depth 20)
        $Uri = "https://$($environmentConfig.Uri)/dsr/testcommandinsertion?agentId=$($AgentId)&assetGroupId=$($AssetGroupId)" 
            
        try 
        {
            Invoke-RestMethod -Uri $Uri -Method Post -Headers $headers -Body $Body -ContentType "application/json" -ErrorVariable RespErr 
            Write-Host "Successful Test Command Insertion"
        }
        catch [System.Net.WebException] 
        {
            $this.LogResponseToConsole()    
            Write-Host "`nSubmission Body:`n$Body `n"
            throw $_.Exception
        }
    }

    # Summary
    # -------
    # A helper functions that performs null checking and logs exceptions that contain a
    # response body.
    [void] LogResponseToConsole()
    {
        if($_.Exception.Response -ne $null)
        {
            $respStream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($respStream)
            $respBody = $reader.ReadToEnd()

            Write-Host "`nResponse Body:`n$respBody `n"
        }
    }
}

Send-TestCommandInsertionRequest -AgentId $AgentId -AssetGroupId $AssetGroupId -CommandPage $CommandPage