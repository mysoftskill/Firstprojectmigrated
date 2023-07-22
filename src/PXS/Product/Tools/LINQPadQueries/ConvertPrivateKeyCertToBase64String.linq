<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.dll</Reference>
  <Namespace>System.Security.Cryptography.X509Certificates</Namespace>
  <Namespace>System.Security.Cryptography</Namespace>
</Query>

void Main()
{
	const string Thumbprint = "756A050B741031C8E14FCBF2859CA2F3F0E5AF87";
	const string Password = "INPUT.YOUR.VALUE.HERE";

	X509Certificate2 cert;
	X509Store certStore = new X509Store(StoreName.My, StoreLocation.LocalMachine);
	string certText;
	byte[] certBytes;

	certStore.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);

	try
	{
		cert = certStore.Certificates.Find(X509FindType.FindByThumbprint, Thumbprint, false)[0];
		
		if (!cert.HasPrivateKey)
		{
			Console.WriteLine($"Cert does not have private key. Cannot continue. Install it with private key and retry.");
			return;
		}
	}
	finally
	{
		certStore.Close();
	}
	
	if (cert == null)
	{
		throw new ArgumentNullException(nameof(cert));
	}

	try
	{
		certBytes = cert.Export(X509ContentType.Pfx, Password);
		certText = Convert.ToBase64String(certBytes);
		Console.Write(certText);
	}
	catch(CryptographicException e)
	{
		if (e.Message.StartsWith("Key not valid for use in specified state."))
		{
			Console.Write("Key is not exportable. Re-install private key and mark it as exportable.");
		}
	
		throw;
	}
}