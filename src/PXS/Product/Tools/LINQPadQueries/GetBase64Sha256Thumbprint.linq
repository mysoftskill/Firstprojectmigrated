<Query Kind="Program">
  <Namespace>System.Security.Cryptography.X509Certificates</Namespace>
  <Namespace>System.Security.Cryptography</Namespace>
</Query>

void Main()
{
    var certificate = GetCertFromStoreByThumprint("8bdc5326f30e734e11694b400f3b0856e88a1a2b");

    using (var sha256Hash = SHA256.Create())
    {
        var certBytes = certificate.GetRawCertData();
        var certHash = sha256Hash.ComputeHash(certBytes);
        var certThumbprint = Convert.ToBase64String(certHash);
        certThumbprint.Dump();
    }
}

public X509Certificate2 GetCertFromStoreByThumprint(string thumbprint)
{
    X509Store certStore = new X509Store(StoreName.My, StoreLocation.LocalMachine);

    certStore.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);

    try
    {
        return certStore.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false)[0];
    }
    finally
    {
        certStore.Close();
    }
}
