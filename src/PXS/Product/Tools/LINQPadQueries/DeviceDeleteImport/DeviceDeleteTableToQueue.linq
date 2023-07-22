<Query Kind="Program">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>WindowsAzure.Storage</NuGetReference>
  <Namespace>Microsoft.WindowsAzure.Storage</Namespace>
  <Namespace>Microsoft.WindowsAzure.Storage.Queue</Namespace>
  <Namespace>Microsoft.WindowsAzure.Storage.Table</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
</Query>

/* Instructions for script:
	1. Modify MaxRPS value.
	2. Update the connection string for 'storageTable' to be the location where device ids are stored. 
		a. Access required: 
			Allowed services: Table
			Allowed resource types: Service, Container, Object
			Allowed permissions: All
	3. Update the connection string for 'storageTarget1' to be the queue where device ids should be sent to.
		a. Access required: 
			Allowed services: Queue
			Allowed resource types: Service, Container, Object
			Allowed permissions: All
	4. Run script.
	5. Optional: Abort if needed. No extra steps to worry about to resume - state is all tracked in ATS.
*/

void Main()
{
	const int MaxRPS = 1;
	Console.WriteLine($"Script is configured to do {MaxRPS} insertion into queue per second.");

	var storageTable = CloudStorageAccount.Parse("");
	var storageTarget1 = CloudStorageAccount.Parse("");

	string tableName = "devicedeleteforshell";
	string queueName = "devicedeleterequest";

	CloudQueue queue1 = storageTarget1.CreateCloudQueueClient().GetQueueReference(queueName);
	
	CloudTable table = storageTable.CreateCloudTableClient().GetTableReference(tableName);
	
	List<ITableEntity> data = new List<ITableEntity>();
	int rowCount = 0;
	while (true)
	{
		var segment = table.ExecuteQuerySegmented(new TableQuery(), null);
		foreach (var element in segment)
		{
			data.Add(element);
		}
		
		Console.WriteLine($"Read {data.Count} from table storage.");

		// Write to Queue
		int count = 0;
		Stopwatch timer = Stopwatch.StartNew();
		foreach (var item in data)
		{
			try
			{
				WriteToQueue(queue1, item.RowKey); // RowKey == Device Id in format of g:1234, where 1234 is the global device id in decimal format.
			}
			catch(Exception e)
			{
				Console.WriteLine($"Exception occurred while writing to queue. Exiting. Exception: {e}");
				throw;
			}
			
			try
			{
				// Delete from ATS
				table.ExecuteAsync(TableOperation.Delete(item)).GetAwaiter().GetResult();
			}
			catch (Exception e)
			{
				Console.WriteLine($"Exception occurred while deleting from table. Exiting. Exception: {e}");
				throw;
			}
			
			rowCount++;
			count++;
			
			// Keeps traffic throttled at max rps
			if (count > (timer.Elapsed.TotalSeconds * MaxRPS))
				Thread.Sleep(1000);

			// Output to show processing is happening
			if ((rowCount % 100) == 0)
			{
				Console.WriteLine($"Finished processing: {rowCount}");
			}
		}
		
		// Clear data after loop iteration finished
		data = new List<ITableEntity>();

		// If nothing is left in ATS, we are done
		if (segment.ContinuationToken == null)
			break;
	}

	Console.WriteLine($"Total finished processing: {rowCount}");
	
	return;
}

void WriteToQueue(CloudQueue queue, string deviceId)
{
	var extensions = new VortexEvent.Extensions();
	
	var vortexEvent = new VortexEvent
	{
		Ext = new VortexEvent.Extensions { Device = new VortexEvent.Device { Id = deviceId }}
	};
	
	var deleteRequest = new DeviceDeleteRequest
	{
		Data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(vortexEvent)),
		RequestInformation = new VortexRequestInformation { WasCompressed = false, HadServerName = false, IsWatchdogRequest = false, ServedBy = string.Empty, HadUserAgent = false }
	};
	
	var serializedQueueItem = JsonConvert.SerializeObject(deleteRequest);
	
	queue.AddMessage(new CloudQueueMessage(JsonConvert.SerializeObject(deleteRequest)), TimeSpan.FromSeconds(-1));
}

// Define other methods and classes here
public class DeviceDeleteRequest
{
	[JsonProperty("vortexEventData", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public byte[] Data { get; set; }

	[JsonProperty("requestInformation", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public VortexRequestInformation RequestInformation { get; set; }

	[JsonProperty("requestId")]
	public Guid RequestId { get; set; } = Guid.NewGuid();
}

/// <summary>
///     Contains information for logging as well as if request is for watchdog or other factors
/// </summary>
public class VortexRequestInformation
{
	/// <summary>
	///     Gets or sets a value indicating whether headers had vortex server name.
	/// </summary>
	[JsonProperty("hadServerName", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public bool HadServerName { get; set; }

	/// <summary>
	///     Gets or sets a value indicating whether had user agent information.
	/// </summary>
	[JsonProperty("hadUserAgent", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public bool HadUserAgent { get; set; }

	/// <summary>
	///     Gets or sets a value indicating whether this instance is watchdog request.
	///     Watch dog requests come from the watch dog machine to check if we're up and
	///     running. Based on our response it will try to restart us. We should process
	///     everything up to sending the requests to PCF and Delete Feed.
	/// </summary>
	[JsonProperty("isWatchdogRequest", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public bool IsWatchdogRequest { get; set; }

	/// <summary>
	///     Gets or sets the name of the vortex machine serving us the event.
	/// </summary>
	[JsonProperty("servedBy", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public string ServedBy { get; set; }

	/// <summary>
	///     Gets or sets the user agent.
	/// </summary>
	[JsonProperty("userAgent", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public string UserAgent { get; set; }

	/// <summary>
	///     Gets or sets a value indicating whether payload was compressed. Compression
	///     is expected to be marked in the request headers coming from Vortex.
	/// </summary>
	[JsonProperty("wasCompressed", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public bool WasCompressed { get; set; }

	/// <summary>
	///     Gets the information as a message string
	/// </summary>
	/// <returns>A string of the information</returns>
	public string ToMessage()
	{
		return
			$"WasCompressed: {this.WasCompressed}, IsWatchDog: {this.IsWatchdogRequest}, ServedBy: {this.ServedBy}, UserAgent: {this.UserAgent}, HadServerName: {this.HadServerName}, HadUserAgent: {this.HadUserAgent}";
	}
}

public class VortexEvents
{
	[JsonProperty("Events", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public VortexEvent[] Events { get; set; }
}

public partial class VortexEvent
{
	/// <summary>
	///     Gets or sets the correlation vector. If null, check <see cref="VortexTags.CorrelationVector"/> for legacy location
	/// </summary>
	[JsonProperty("cV", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public string CorrelationVector { get; set; }

	[JsonProperty("ext", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public Extensions Ext { get; set; }

	/// <summary>
	///     Gets or sets the user identifier in the legacy location
	/// </summary>
	[JsonProperty("userId", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public string LegacyUserId { get; set; }

	/// <summary>
	/// Gets or sets the device identifier in the legacy location
	/// </summary>
	[JsonProperty("deviceId", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public string LegacyDeviceId { get; set; }

	[JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public string Name { get; set; }

	[JsonProperty("tags", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public VortexTags Tags { get; set; }

	[JsonProperty("time", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public DateTimeOffset Time { get; set; }

	[JsonProperty("ver", DefaultValueHandling = DefaultValueHandling.Ignore)]
	public float Version { get; set; }
}

public partial class VortexEvent
{
	public class Extensions
	{
		[JsonProperty("device")]
		public Device Device { get; set; }

		[JsonProperty("user")]
		public User User { get; set; }
	}
}

public partial class VortexEvent
{
	public class Device
	{
		[JsonProperty("orgId", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string OrganizationId;

		[JsonProperty("id")]
		public string Id { get; set; }
	}
}

public partial class VortexEvent
{
	public class User
	{
		[JsonProperty("id")]
		public string Id { get; set; }
	}
}

public partial class VortexEvent
{
	public class VortexTags
	{
		/// <summary>
		///     Gets or sets the correlation vector in the legacy location
		/// </summary>
		[JsonProperty("cV", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string CorrelationVector { get; set; }
	}
}