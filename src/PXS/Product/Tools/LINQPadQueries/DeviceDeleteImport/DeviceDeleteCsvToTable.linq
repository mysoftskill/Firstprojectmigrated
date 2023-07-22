<Query Kind="Program">
  <NuGetReference>WindowsAzure.Storage</NuGetReference>
  <Namespace>Microsoft.WindowsAzure.Storage</Namespace>
  <Namespace>Microsoft.WindowsAzure.Storage.Queue</Namespace>
  <Namespace>Microsoft.WindowsAzure.Storage.Table</Namespace>
</Query>

/* Instructions for script:
	1. Update the input file path to a path on disk that contains the device ids. Each row of data is a different global device id.
	2. Optional: Update the starting line number (in case resuming an import). Default: leave at 0
	3. Update the value for 'ConnectionStringForTable' to be a SAS URI for table. 
		a. Access required: 
			Allowed services: Table
			Allowed resource types: Service, Container, Object
			Allowed permissions: All
	4. Run script.
	5. Optional: Abort if needed. See step 2 to resume.
*/

void Main()
{
	// Input file must contain device ids that need to be processed.
	const string InputFilePath = @"C:\deviceids.txt";
	
	// Table will be where device ids are stored.
	const string TableName = "devicedeleteforshell";
	
	// 'Connection String' from SAS goes here
	const string ConnectionStringForTable = "";

	// Update this in case something failed so resume can pick up at whatever line number is needed.
	const int StartingLineNumber = 0;
	
	var storageTarget = CloudStorageAccount.Parse(ConnectionStringForTable);
	CloudTable table = storageTarget.CreateCloudTableClient().GetTableReference(TableName);
	table.CreateIfNotExistsAsync().GetAwaiter().GetResult();
	
	int counter = 0;
	string line;
	int storedDeviceIdCount = 0;
	int existingDeviceIdCount = 0;
	using (StreamReader file = new StreamReader(InputFilePath))
	{
		while ((line = file.ReadLine()) != null)
		{
			counter++;
			
			if (counter < StartingLineNumber)
			{
				continue;
			}
			
			// Validate input...
			if (line.Split(',').Count() > 1)
			{
				Console.WriteLine($"Invalid input: {line}");
				return;
			}

			if (!line.StartsWith("g:"))
			{
				Console.WriteLine($"Expected format is 'g:' prefix. Invalid input was: {line}");
				return;
			}

			// Write to ATS
			var deviceEntity = new DeviceEntity(line);
			TableOperation insertOperation = TableOperation.Insert(deviceEntity);
			try
			{
				table.ExecuteAsync(insertOperation).GetAwaiter().GetResult();
				storedDeviceIdCount++;
			}
			catch (StorageException storageException)
			{
				if (storageException.RequestInformation.HttpStatusCode == 409)
				{
					existingDeviceIdCount++;
				}
				else
				{
					Console.WriteLine($"Failed on Line#{counter}. Value: {line}");
					throw;
				}
			}
			
			// Periodic update so we know processing is happening.
			if ((counter % 1000) == 0){
				Console.WriteLine(counter);
			}
		}
	}

	Console.WriteLine($"Read {counter} lines from file");
	Console.WriteLine($"New device id's stored: {storedDeviceIdCount}");
	Console.WriteLine($"Existing device id count: {existingDeviceIdCount}");
}

// Define other methods and classes here
public class DeviceEntity : TableEntity
{
	public DeviceEntity(string deviceId)
	{
		this.PartitionKey = deviceId;
		this.RowKey = deviceId;
	}
}