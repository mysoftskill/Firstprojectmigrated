<Query Kind="Program">
  <Reference Relative="..\..\..\Bin\Debug\x64\PrivacyOperationClient\Microsoft.IdentityModel.Clients.ActiveDirectory.dll">C:\Users\minnguye\source\repos\MEE.Privacy.Experience.Svc\Bin\Debug\x64\PrivacyOperationClient\Microsoft.IdentityModel.Clients.ActiveDirectory.dll</Reference>
  <Reference Relative="..\..\..\Bin\Debug\x64\PrivacyOperationClient\Microsoft.IdentityModel.dll">C:\Users\minnguye\source\repos\MEE.Privacy.Experience.Svc\Bin\Debug\x64\PrivacyOperationClient\Microsoft.IdentityModel.dll</Reference>
  <Reference Relative="..\..\..\Bin\Debug\x64\PrivacyOperationClient\Microsoft.OSGS.HttpClientCommon.dll">C:\Users\minnguye\source\repos\MEE.Privacy.Experience.Svc\Bin\Debug\x64\PrivacyOperationClient\Microsoft.OSGS.HttpClientCommon.dll</Reference>
  <Reference Relative="..\..\..\Bin\Debug\x64\PrivacyOperationClient\Microsoft.PrivacyServices.CommandFeed.Contracts.dll">C:\Users\minnguye\source\repos\MEE.Privacy.Experience.Svc\Bin\Debug\x64\PrivacyOperationClient\Microsoft.PrivacyServices.CommandFeed.Contracts.dll</Reference>
  <Reference Relative="..\..\..\Bin\Debug\x64\PrivacyOperationClient\Microsoft.PrivacyServices.PrivacyOperation.Client.dll">C:\Users\minnguye\source\repos\MEE.Privacy.Experience.Svc\Bin\Debug\x64\PrivacyOperationClient\Microsoft.PrivacyServices.PrivacyOperation.Client.dll</Reference>
  <Reference Relative="..\..\..\Bin\Debug\x64\PrivacyOperationClient\Microsoft.PrivacyServices.PrivacyOperation.Contracts.dll">C:\Users\minnguye\source\repos\MEE.Privacy.Experience.Svc\Bin\Debug\x64\PrivacyOperationClient\Microsoft.PrivacyServices.PrivacyOperation.Contracts.dll</Reference>
  <Reference Relative="..\..\..\Bin\Debug\x64\PrivacyOperationClient\System.Net.Http.Formatting.dll">C:\Users\minnguye\source\repos\MEE.Privacy.Experience.Svc\Bin\Debug\x64\PrivacyOperationClient\System.Net.Http.Formatting.dll</Reference>
  <Reference Relative="..\..\..\Bin\Debug\x64\PrivacyOperationClient\System.Web.Http.dll">C:\Users\minnguye\source\repos\MEE.Privacy.Experience.Svc\Bin\Debug\x64\PrivacyOperationClient\System.Web.Http.dll</Reference>
  <Reference Relative="..\..\..\Bin\Debug\x64\PrivacyOperationClient\System.Web.Http.WebHost.dll">C:\Users\minnguye\source\repos\MEE.Privacy.Experience.Svc\Bin\Debug\x64\PrivacyOperationClient\System.Web.Http.WebHost.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Net.Http.dll</Reference>
  <Namespace>Microsoft.PrivacyServices.PrivacyOperation.Client</Namespace>
  <Namespace>System.Security.Cryptography.X509Certificates</Namespace>
  <Namespace>Microsoft.PrivacyServices.PrivacyOperation.Client.Clients.Implementations</Namespace>
  <Namespace>Microsoft.IdentityModel.Clients.ActiveDirectory</Namespace>
  <Namespace>Microsoft.PrivacyServices.PrivacyOperation.Client.Models</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Microsoft.PrivacyServices.PrivacyOperation.Client.Clients.Interfaces</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>Microsoft.PrivacyServices.PrivacyOperation.Contracts</Namespace>
</Query>

void Main()
{
	//GetAadTokenForMsGraph();
	//GetAadTokenForPcd(false);
	MainAsync().GetAwaiter().GetResult();
}

async Task MainAsync()
{
	ServicePointManager.ServerCertificateValidationCallback +=
		(sender, cert, chain, sslPolicyErrors) => true;
		
	var httpClient = new Microsoft.OSGS.HttpClientCommon.HttpClient();
	httpClient.BaseAddress = new Uri("https://localhost");
	
	var authClient = new NativeAuthClient();

	var client = new PrivacyOperationClient(httpClient, authClient);

	var result = await client.ListRequestsAsync(new ListOperationArgs()
	{
		CorrelationVector = Guid.NewGuid().ToString(),
		UserAssertion = new UserAssertion("Not Used With NativeAuthClient"),
	});

	result.Dump("Results:");
}

void GetAadTokenForMsGraph()
{
	// At the login prompt, please login with a global admin within meepxs.onmicrosoft.com tenant
	string adContext = "https://login.microsoftonline.com/meepxs.onmicrosoft.com";
    string appId = "eb11f721-7c77-4115-8662-ef671a4fb226";
    Uri appUri = new Uri("https://minhnativeapp.meepxs.onmicrosoft.com");

    var authContext = new AuthenticationContext(adContext);
    var token = authContext.AcquireTokenAsync(
        "00000003-0000-0000-c000-000000000000",
        appId,
        appUri,
        new PlatformParameters(PromptBehavior.Always),
        UserIdentifier.AnyUser).Result;
	token.Dump("Token: ");
}

void GetAadTokenForPcd(bool forProd)
{
	// At the login prompt, please log in with your work account after joining NGP – Access to Alt-Subject Test Page (PCD) in idweb. Please note that you need to wait for 24 hours after joining.
	string adContext = "https://login.microsoftonline.com/microsoft.onmicrosoft.com";
    string appId = "eb11f721-7c77-4115-8662-ef671a4fb226";
    Uri appUri = new Uri("https://minhnativeapp.meepxs.onmicrosoft.com");

    var authContext = new AuthenticationContext(adContext);
    var token = authContext.AcquireTokenAsync(
        forProd ? "877310d5-c81c-45d8-ba2d-bf935665a43a" : "705363a0-5817-47fb-ba32-59f47ce80bb7",
        appId,
        appUri,
        new PlatformParameters(PromptBehavior.Always),
        UserIdentifier.AnyUser).Result;
	token.Dump("Token: ");
}

public class NativeAuthClient : IPrivacyOperationAuthClient
{
	public async Task<AuthenticationHeaderValue> GetAadAuthToken(CancellationToken cancellationToken, UserAssertion userAssertion = null)
	{
		string adContext = "https://login.microsoftonline.com/microsoft.onmicrosoft.com";
		string appId = "48ccf375-d4b9-4b63-a956-163a89d04978";
		Uri appUri = new Uri("https://microsoft.com/keithjac-privacy2");

		var authContext = new AuthenticationContext(adContext);
		var token = await authContext.AcquireTokenAsync(
			appId,
			appId,
			appUri,
			new PlatformParameters(PromptBehavior.Auto),
			UserIdentifier.AnyUser);
		token.Dump("Auth Token:");
		
		return new AuthenticationHeaderValue("Bearer", token.AccessToken);
	}
}

// Define other methods and classes here