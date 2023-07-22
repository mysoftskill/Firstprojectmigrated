<Query Kind="Statements">
  <NuGetReference>Microsoft.Identity.Client</NuGetReference>
  <Namespace>Microsoft.Identity.Client</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

const string Authority = "https://login.microsoftonline.com/microsoft.onmicrosoft.com";
const string RedirectUri = "https://management.privacy.microsoft.com";
//For Prod: b1b98629-ed2f-450c-9095-b895d292ccf1
//For Non Prod: ff3a41f1-6748-48fa-ba46-d19a123ae965
var scopes = new[] { $"ff3a41f1-6748-48fa-ba46-d19a123ae965/.default" };
var app = PublicClientApplicationBuilder.Create("25862df9-0e4d-4fb7-a5c8-dfe8ac261930")
.WithAuthority(Authority)
.WithRedirectUri(RedirectUri)
.Build();
var result = await app.AcquireTokenInteractive(scopes).ExecuteAsync();
result.AccessToken.Dump();