<Query Kind="Program">
  <NuGetReference>WindowsAzure.Storage</NuGetReference>
  <Namespace>Microsoft.WindowsAzure.Storage.Blob</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Microsoft.WindowsAzure.Storage</Namespace>
</Query>

void Main()
{
	MainAsync().GetAwaiter().GetResult();
}

public Uri GetContainerUri(string connectionString, string containerName)
{
	CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
	CloudBlobContainer container = account.CreateCloudBlobClient().GetContainerReference(containerName);
	container.CreateIfNotExists();

	var policy = new SharedAccessBlobPolicy
	{
		Permissions = SharedAccessBlobPermissions.Add | SharedAccessBlobPermissions.Create | SharedAccessBlobPermissions.Write,
		SharedAccessExpiryTime = DateTimeOffset.UtcNow + TimeSpan.FromDays(60)
	};
	string token = container.GetSharedAccessSignature(policy);

	return new Uri(container.Uri, token);
}

async Task MainAsync()
{
	var storageConnectionString = "<add azure blob connection string>";
	var desiredContainerName = "test";
	var uri = GetContainerUri(storageConnectionString, desiredContainerName);
	uri.Dump();

	try
	{
		// Do some rudimentary validation of the storage destination, that it is actually a container,
		// and the Uri itself is enough to successfully write to it (It has a write permission SAS token)
		var testContainer = new CloudBlobContainer(uri);
		CloudAppendBlob blob = testContainer.GetAppendBlobReference("foo/bar/requestid.txt");
		await blob.UploadTextAsync(Guid.NewGuid().ToString()).ConfigureAwait(false);
	}
	catch (Exception ex)
	{
		ex.ToString().Dump();
	}

}

// Define other methods and classes here