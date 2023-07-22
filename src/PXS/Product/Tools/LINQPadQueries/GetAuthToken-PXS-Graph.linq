<Query Kind="Statements">
  <Reference>C:\Users\krsolem\.nuget\packages\microsoft.azure.keyvault.core\3.0.5\lib\net461\Microsoft.Azure.KeyVault.Core.dll</Reference>
  <Reference>C:\Users\krsolem\.nuget\packages\microsoft.azure.keyvault\3.0.5\lib\net461\Microsoft.Azure.KeyVault.dll</Reference>
  <Reference>C:\Users\krsolem\.nuget\packages\microsoft.azure.services.appauthentication\1.6.1\lib\net472\Microsoft.Azure.Services.AppAuthentication.dll</Reference>
  <Reference>C:\Users\krsolem\.nuget\packages\microsoft.identity.client\4.33.0\lib\net461\Microsoft.Identity.Client.dll</Reference>
  <Reference>C:\Users\krsolem\.nuget\packages\Microsoft.IdentityModel.Clients.ActiveDirectory\5.2.9\lib\net45\Microsoft.IdentityModel.Clients.ActiveDirectory.dll</Reference>
  <Reference>C:\Users\krsolem\.nuget\packages\microsoft.identitymodel.jsonwebtokens\6.11.1\lib\net461\Microsoft.IdentityModel.JsonWebTokens.dll</Reference>
  <Reference>C:\Users\krsolem\.nuget\packages\microsoft.identitymodel.logging\6.11.1\lib\net461\Microsoft.IdentityModel.Logging.dll</Reference>
  <Reference>C:\Users\krsolem\.nuget\packages\microsoft.identitymodel.s2s.tokens\2.2.0\lib\net461\Microsoft.IdentityModel.S2S.Tokens.dll</Reference>
  <Reference>C:\Users\krsolem\.nuget\packages\microsoft.identitymodel.tokens\6.11.1\lib\net461\Microsoft.IdentityModel.Tokens.dll</Reference>
  <Reference>C:\Users\krsolem\.nuget\packages\microsoft.rest.clientruntime.azure\3.3.18\lib\net452\Microsoft.Rest.ClientRuntime.Azure.dll</Reference>
  <Reference>C:\Users\krsolem\.nuget\packages\microsoft.rest.clientruntime\2.3.23\lib\net461\Microsoft.Rest.ClientRuntime.dll</Reference>
  <Reference>C:\Users\krsolem\.nuget\packages\newtonsoft.json\13.0.1\lib\net45\Newtonsoft.Json.dll</Reference>
  <Reference>C:\Users\krsolem\.nuget\packages\system.identitymodel.tokens.jwt\6.11.1\lib\net461\System.IdentityModel.Tokens.Jwt.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Linq.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Net.Http.dll</Reference>
  <Namespace>Microsoft.Azure.KeyVault</Namespace>
  <Namespace>Microsoft.Azure.KeyVault.Models</Namespace>
  <Namespace>Microsoft.Azure.Services.AppAuthentication</Namespace>
  <Namespace>Microsoft.Identity.Client</Namespace>
  <Namespace>Microsoft.IdentityModel.S2S.Tokens</Namespace>
  <Namespace>Microsoft.Rest.ClientRuntime.Azure</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>System.IdentityModel.Tokens.Jwt</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Security</Namespace>
  <Namespace>System.Security.Cryptography.X509Certificates</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

// This can be used to create the Authentication Header Value for calls to the pxs graph apis
// Set the tokenConfig value below to either resourceConfig or homeConfig depending on which 
// tenant you want to target.
// In addition to setting this header value, you must add the x-ms-gateway-serviceRoot header.
// You can set this value to "https://localhost/"

const string AuthorityBase = "https://login.microsoftonline.com/";

const string PXS_ClientId_PPE = "705363a0-5817-47fb-ba32-59f47ce80bb7";  /* PXS PPE */
const string PXS_ClientId_Prod = "877310d5-c81c-45d8-ba2d-bf935665a43a";
const string Graph_ResourceId = "00000003-0000-0000-c000-000000000000"; /* Graph Prod */

const string S2SAppId_Home = "feb76379-5080-4b88-86d0-7bef3558d507"; // meepxstest
const string S2SAppId_Resource = "31e2ae73-1a3f-4104-9868-4007cc2ee6ce"; // meepxstest2

const string keyVaultBaseUrl="https://adgcs-ame-kv.vault.azure.net/"; // for cloudtest-privacy-int cert
const string certificateName="cloudtest-privacy-int";

const string testKeyVaultBaseUrl = "https://pxs-test-ame.vault.azure.net/"; // for test secrets
const string TargetAadAudience = "https://pxs.api.account.microsoft-int.com";

// constants for home tenant (meepxs)
IDictionary<string, string> homeConfig = new Dictionary<string, string>() {
	{ "tenantId", "7BDB2545-6702-490D-8D07-5CC0A5376DD9" }, // meepxs
	{ "clientId", "90b23419-a7ce-4459-95e1-8f251ea7f606" }, // meepxsfunctionaltest
	{ "s2sAppId", S2SAppId_Home },
	{ "password_secret", "user1-password" },
	{ "upn_secret", "user1-upn" }
};

// constants for resource tenant (meepxsresource)
IDictionary<string, string> resourceConfig = new Dictionary<string, string>() {
	{ "tenantId", "49B410FF-4D15-4BDF-B82D-4687AC464753" }, // meepxsresource
	{ "clientId", "f107c8c7-500f-406f-84b1-5b90576a8297" }, // meepxsfunctionaltest2
	{ "s2sAppId", S2SAppId_Resource },
	{ "password_secret", "user2-password" },
	{ "upn_secret", "user2-upn" }
};

// NOTE: Set this to the appropriate configuration for the scenario you want

IDictionary<string, string> tokenConfig = resourceConfig;

Microsoft.Identity.Client.AuthenticationResult result;
	
AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
KeyVaultClient keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
	
CancellationToken cancellationToken = default(CancellationToken);
	
// Get App Token
var appToken = await GetAppToken(tokenConfig);
//appToken.Dump();

// Get Access Token
var accessToken = await GetAccessToken(tokenConfig);
//accessToken.Dump();
	
// Create Authentication Header Value
string authHeaderValue = TokenCreator.CreateMSAuth1_0PFATHeader(accessToken, appToken);
authHeaderValue.Dump();

async Task<string> GetAppToken(IDictionary<string, string> config)
{
	SecretBundle secretBundle = await keyVaultClient.GetSecretAsync(keyVaultBaseUrl, certificateName, cancellationToken)
	    .ConfigureAwait(false);
	
	X509Certificate2 cert = new X509Certificate2(
	    Convert.FromBase64String(secretBundle.Value),
	    (SecureString)null,
	    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
					
	string[] scopes = new string[] { $"{TargetAadAudience}/.default" };
	var app = ConfidentialClientApplicationBuilder.Create(config["s2sAppId"])
	                .WithCertificate(cert)
	                .WithAuthority(new Uri(AuthorityBase + config["tenantId"]))
	                .Build();
	result = await app.AcquireTokenForClient(scopes).WithSendX5C(true).ExecuteAsync().ConfigureAwait(false);
	return result?.AccessToken;
}

async Task<string> GetAccessToken(IDictionary<string, string> config)
{
    // Get functional test admin name and password
	SecretBundle adminPassword = await keyVaultClient.GetSecretAsync(testKeyVaultBaseUrl, config["password_secret"], cancellationToken)
	    .ConfigureAwait(false);
		
	var password = adminPassword.Value;
	
	SecretBundle adminUpn = await keyVaultClient.GetSecretAsync(testKeyVaultBaseUrl, config["upn_secret"], cancellationToken)
	    .ConfigureAwait(false);
	
	var upn = adminUpn.Value;
	
	Dictionary<string, string> values = new Dictionary<string, string>
	{
	    { "client_id", config["clientId"] },
	    { "password", password },
	    { "grant_type", "password" },
	    { "username", upn },
	    { "scope", "Directory.AccessAsUser.All" }
	};
	
	var content = new FormUrlEncodedContent(values);
	
	// Get tenant access token
	HttpClient client = new HttpClient();
	string accessTokenUri = string.Format("https://login.microsoftonline.com/{0}/oauth2/v2.0/token", upn.Substring(upn.LastIndexOf('@') + 1));
	
	HttpResponseMessage getTokenResponse = await client.PostAsync(
	    new Uri(accessTokenUri),
	    content).ConfigureAwait(false);
	
	// If successful, build the auth header value (which is a combination of the AppToken and the AccessToken)
	if (getTokenResponse.IsSuccessStatusCode)
	{
	    string rawEvoResponse = await getTokenResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
	    dynamic evoResponse = JsonConvert.DeserializeObject(rawEvoResponse);
		string access_token = evoResponse.access_token;
	
		return TokenCreator.TransformProtectedForwardedToken(access_token);
	}
	
	return null;
}
