<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Web.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Configuration.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.DirectoryServices.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.EnterpriseServices.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Web.RegularExpressions.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Design.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Web.ApplicationServices.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.ComponentModel.DataAnnotations.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.DirectoryServices.Protocols.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Security.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.ServiceProcess.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Web.Services.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\Microsoft.Build.Utilities.v4.0.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\Microsoft.Build.Framework.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\Microsoft.Build.Tasks.v4.0.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Windows.Forms.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Runtime.Caching.dll</Reference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>System.Security.Cryptography</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Converters</Namespace>
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.IO.Compression</Namespace>
  <Namespace>System.Web</Namespace>
</Query>

void Main()
{
	MainAsync().GetAwaiter().GetResult();
}

async Task MainAsync()
{
	await LogSearchAsync();
	//await SllSearchAsync();
	//await GetConfig();
}

async Task SllSearchAsync()
{
	var query = new LensQuery()
		.WithEnvironments(Environments.PxsProd)
		.WithMachineFunctions(MachineFunctions.PxsView)
		.WithFilePattern(FilePatterns.PxsSllLogs)
		.WithNoCache()
		.WithStartAgo(TimeSpan.FromHours(6))
		.WithIgnoreCase()
		.WithSearches("getdatapolicyoperation")
		//.WithExcludes("$nextLink")
		//.WithSearches("\"dependencyOperationName\":\"SendEvent\"")
		;

	var results = await Lens.ExecuteAsync<PxsSllEntry>(query, PxsSllEntry.Transform);
	results.Dump(2);
}

async Task LogSearchAsync()
{
	var query = new LensQuery()
		.WithEnvironments(Environments.PxsProd)
		.WithMachineFunctions(MachineFunctions.PxsViewWatchdog)
		.WithFilePattern(FilePatterns.PxsCertificateInstaller)
		.WithNoCache()
		.WithStartAgo(TimeSpan.FromDays(1))
		.WithIgnoreCase()
		.WithSearches("Certificate installation")
		//.WithExcludes("[DeleteRequestArchiver]")
		//.WithExcludes("[WatermarkWriter]")
		//.WithExcludes("NullReferenceException")
		;

	var results = await Lens.ExecuteAsync<PxsLogEntry>(query, PxsLogEntry.Transform);
	results.Dump(2);
}

async Task GetConfig()
{
	var query = new LensQuery()
		.WithEnvironments(Environments.PxsProd)
		.WithMachineFunctions(MachineFunctions.PxsViewWatchdog)
		.WithFilePattern(FilePatterns.PxsLogs)
		.WithNoCache()
		.WithStartAgo(TimeSpan.FromDays(3))
		.WithSearches("Parsing raw configuration data: ")
		;

	var results = await Lens.ExecuteAsync<PxsLogEntry>(query, PxsLogEntry.Transform);
	results
		.GroupBy(e => e.Machine)
		.Select(g => new { Machine = g.Key, Timestamp = g.Max(e => e.Timestamp), LatestConfig = ParseConfig(g.OrderByDescending(e => e.Timestamp).First().Description) })
		.GroupBy(e => e.LatestConfig)
		.OrderByDescending(e => e.Max(e2 => e2.Timestamp))
		.Select(e => new { Config = e.Key, Machines = e.Select(e2 => new { e2.Machine, e2.Timestamp }).OrderBy(e2 => e2.Machine) })
		//.Select(e => new { Config = FilterConfigByDataType(e.Key, "ContentConsumption"), Machines = e.Select(e2 => new { e2.Machine, e2.Timestamp }).OrderBy(e2 => e2.Machine) })
		.Dump();
}

string FilterConfigByDataType(string json, string dataType)
{
	if (dataType == null)
		return json;

	var config = JsonConvert.DeserializeObject<JObject>(json);
	var tokens = config.SelectTokens($"$..[?(@.dataType != '{dataType}')]").ToList();
	foreach (var element in tokens)
	{
		element.Remove();
	}
	return JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
}

private DateTimeOffset BucketTime(DateTimeOffset timestamp, TimeSpan bucketSize)
{
	long ts = timestamp.ToUnixTimeMilliseconds();
	ts -= ts % (long)bucketSize.TotalMilliseconds;
	return DateTimeOffset.FromUnixTimeMilliseconds(ts);
}

public string ParseConfig(string description)
{
	string prefix = "ComponentName: [DataManagementConfigLoader]..Parsing raw configuration data: ";
	var data = description.Substring(prefix.Length);
	data = data.Substring(0, data.Length - 2);
	using (var memStream = new MemoryStream(Convert.FromBase64String(data)))
	using (var gzStream = new GZipStream(memStream, CompressionMode.Decompress))
	using (var reader = new StreamReader(gzStream))
	{
		var jsonObject = JsonConvert.DeserializeObject<JObject>(reader.ReadToEnd());
		return JsonConvert.SerializeObject(jsonObject, Newtonsoft.Json.Formatting.Indented, new StringEnumConverter());
	}
}

public static class Environments
{
	public static readonly string[] UxProd = {
		"PortalEAP-Prod-BN2.BN2",
		"PortalEAP-Prod-CY2.CY2",
		"PortalEAP-Prod-DB5.DB5",
		"PortalEAP-Prod-HK2.HK2"
	};

	public static readonly string[] PxsProd = {
		"PXS-Prod-BN3P.BN3P",
		"PXS-Prod-BY3P.BY3P",
		"PXS-Prod-DB5P.DB5P",
		"PXS-Prod-HK2P.HK2P"
	};

	public static readonly string[] PxsProdWorker = {
		"PXS-Prod-BN3P.BN3P",
		"PXS-Prod-BY3P.BY3P"
	};

	public static readonly string[] PxsPpe = {
		"PXS-PPE-MW1P.MW1P",
		"PXS-PPE-SN3P.SN3P"
	};

	public static readonly string[] PxsInt = {
		"PXS-Sandbox-MW1P.MW1P",
		"PXS-Sandbox-SN3P.SN3P"
	};
}

public static class MachineFunctions
{
	public static readonly string[] PxsView = {
		"PrivacyViewMF"
	};

	public static readonly string[] PxsWorker = {
		"PrivacyWorkerMF"
	};

	public static readonly string[] PcfApi =
	{
		"PCFApiMF"
	};
	
	public static readonly string[] PxsViewWatchdog = {
		"PrivacyViewWatchdogMF"
	};
	
	public static readonly string[] Ux = {
		"MeePortalFunction"
	};
}

public static class FilePatterns
{
	public static string RunService(string serviceLogPattern)
	{
		return $"RunService_{serviceLogPattern}";
	}
	
	public static readonly string PxsCertificateInstaller = "PrivacyAzureKeyVaultCertificateInstaller_*.log";
	
	public static readonly string PxsSllLogs = "MemberViewServiceSll_*.log";

	public static readonly string UxSllLogs = "slllogs_*.log";

	public static readonly string PxsLogs = "PrivacyExperienceService_*.log";
	
	public static readonly string PxsViewWatchdogLogs = "PrivacyExperienceServiceWD_*.log";

	public static readonly string PxsAqsWorkerLogs = "PrivacyAqsWorker_*.log";

	public static readonly string PxsVortexDeviceDeleteWorkerLogs = "VortexDeviceDeleteWorker_*.log";

	public static readonly string PxsAadAccountCloseLogs = "AadAccountCloseWorker_*.log";

	public static readonly string PcfSllLogs = "PCF/*SllLogs*.log";
}

public interface ITimestamped
{
	DateTimeOffset? Timestamp { get; }
}

public class PxsSllEntry : ITimestamped
{
	public object Json { get; }
	public DateTimeOffset? Timestamp { get; }
	public string UserId { get; }
	public int? ProtocolStatusCode { get; }
	public int? LatencyMs { get; }
	public string TargetUri { get; }
	public string Machine { get; }

	private PxsSllEntry(JObject obj)
	{
		this.Json = new { Json = JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented, new StringEnumConverter()) };
		var time = obj.SelectToken("$..time")?.Value<DateTime?>();
		if (time != null)
			this.Timestamp = new DateTimeOffset(time.Value).ToLocalTime();
		this.UserId = obj.SelectTokens("$..id")?.FirstOrDefault()?.Value<string>();
		this.ProtocolStatusCode = obj.SelectToken("$..protocolStatusCode")?.Value<int?>();
		this.LatencyMs = obj.SelectToken("$..latencyMs")?.Value<int?>();
		this.TargetUri = obj.SelectToken("$..targetUri")?.Value<string>();
		this.Machine = obj.SelectToken("$..roleInstance")?.Value<string>();
	}

	public static PxsSllEntry Transform(string line, PxsSllEntry lastEntry)
	{
		string json = line.Substring(line.IndexOf(',') + 1);
		try
		{
			return new PxsSllEntry((JObject)JsonConvert.DeserializeObject(json));
		}
		catch (Exception ex)
		{
			json.Dump();
			ex.Dump();
			return null;
		}
	}
}

public class PxsLogEntry : ITimestamped
{
	public DateTimeOffset? Timestamp { get; }
	public string Machine { get; }
	public string LogLevel { get; }
	public string Component { get; }
	public string Title { get; }
	public string Description { get; private set; }

	private PxsLogEntry(DateTimeOffset timestamp, string machine, string logLevel, string component, string title, string description)
	{
		this.Timestamp = timestamp;
		this.Machine = machine;
		this.LogLevel = logLevel;
		this.Component = component;
		this.Title = title;
		this.Description = description;
	}

	public static PxsLogEntry Transform(string line, PxsLogEntry lastEntry)
	{
		// 2017/11/14 10:30:04.208,MW1PEPF000001F8,i,Common,DefaultTag,ComponentName: [ExportDequeuer]..nothing from ExportStatusQueue, started 11/14/2017 6:12:51 PM +00:00 up 
		if (line.StartsWith(" "))
		{
			if (lastEntry == null)
				throw new Exception("Unexpected line: " + line);
			lastEntry.Description += Environment.NewLine + line;
			return null;
		}
		var parts = line.Split(new[] { ',' }, 6); // TODO: How are commas in data handled?
		try
		{
			return new PxsLogEntry(
				DateTimeOffset.ParseExact(parts[0], "yyyy/MM/dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
				parts[1],
				parts[2],
				parts[3],
				parts[4],
				parts[5]);

		}
		catch (Exception)
		{
			parts.Length.Dump();
			line.Dump();
			throw;
		}
	}
}

public class LensQuery
{
	private string args;
	private DateTimeOffset startTime;
	private DateTimeOffset endTime;
	private bool noCache;

	public string Args
	{
		get { return this.args; }
	}

	public DateTimeOffset StartTime
	{
		get { return this.startTime; }
	}

	public DateTimeOffset EndTime
	{
		get { return this.endTime; }
	}

	public bool NoCache
	{
		get { return this.noCache; }
	}

	public LensQuery()
	{
		this.args = "-cp -qt 120 -rt 120";
		this.startTime = DateTimeOffset.MinValue.ToLocalTime();
		this.endTime = DateTimeOffset.MaxValue.ToLocalTime();
	}

	private LensQuery(string args, DateTimeOffset startTime, DateTimeOffset endTime, bool noCache)
	{
		this.args = args;
		this.startTime = startTime;
		this.endTime = endTime;
		this.noCache = noCache;
	}

	public LensQuery WithRaw(string args, DateTimeOffset? startTime = null, DateTimeOffset? endTime = null, bool? noCache = null)
	{
		if (string.IsNullOrWhiteSpace(args))
			return this;
		return new LensQuery(
			((this.args.Length > 0) ? (this.args + " ") : string.Empty) + args,
			startTime?.ToLocalTime() ?? this.startTime,
			endTime?.ToLocalTime() ?? this.endTime,
			noCache ?? this.noCache);
	}

	public LensQuery WithMachineFunctions(params string[] machineFunctions)
	{
		if (machineFunctions.Length <= 0)
			return this;
		return this.WithRaw("-mf " + string.Join(",", machineFunctions));
	}

	public LensQuery WithMachines(params string[] machines)
	{
		if (machines.Length <= 0)
			return this;
		return this.WithRaw("-m " + string.Join(",", machines));
	}

	public LensQuery WithEnvironments(params string[] environments)
	{
		if (environments.Length <= 0)
			return this;
		return this.WithRaw("-env " + string.Join(",", environments));
	}

	public LensQuery WithSearches(params string[] searches)
	{
		if (searches.Length <= 0)
			return this;
		return this.WithRaw("-s " + string.Join("-s ", searches.Select(r => "\"" + r.Replace("\"", "\\\"") + "\"")));
	}

	public LensQuery WithExcludes(params string[] excludes)
	{
		if (excludes.Length <= 0)
			return this;
		return this.WithRaw("-xs " + string.Join("-xs ", excludes.Select(r => "\"" + r.Replace("\"", "\\\"") + "\"")));
	}

	public LensQuery WithRegexes(params string[] regexes)
	{
		if (regexes.Length <= 0)
			return this;
		return this.WithRaw("-r " + string.Join("-r ", regexes.Select(r => "\"" + r.Replace("\"", "\\\"") + "\"")));
	}

	public LensQuery WithExcludeRegexes(params string[] regexes)
	{
		if (regexes.Length <= 0)
			return this;
		return this.WithRaw("-xr " + string.Join("-xr ", regexes.Select(r => "\"" + r.Replace("\"", "\\\"") + "\"")));
	}

	public LensQuery WithIgnoreCase()
	{
		return this.WithRaw("-i");
	}

	public LensQuery WithStartTime(DateTimeOffset startTime, bool exact = false)
	{
		if (!exact)
		{
			// Fix to 15 minute intervals, to take advantage of caching with 'now' based queries.
			// Always wider than the range asked for
			startTime = new DateTimeOffset(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, (startTime.Minute / 15) * 15, 0, startTime.Offset);
		}

		startTime = startTime.ToLocalTime();
		return this.WithRaw($"-sd \"{startTime:yyyy/MM/dd HH:mm:ss}\"", startTime, null);
	}

	public LensQuery WithStartAgo(TimeSpan ago, bool exact = false)
	{
		return this.WithStartTime(DateTimeOffset.UtcNow - ago, exact);
	}

	public LensQuery WithEndTime(DateTimeOffset endTime, bool exact = false)
	{
		if (!exact)
		{
			// Fix to 15 minute intervals, to take advantage of caching with 'now' based queries.
			// Always wider than the range asked for
			endTime += TimeSpan.FromMinutes(15);
			endTime = new DateTimeOffset(endTime.Year, endTime.Month, endTime.Day, endTime.Hour, (endTime.Minute / 15) * 15, 0, endTime.Offset);
		}

		endTime = endTime.ToLocalTime();
		return this.WithRaw($"-ed \"{endTime:yyyy/MM/dd HH:mm:ss}\"", null, endTime);
	}

	public LensQuery WithEndAgo(TimeSpan ago, bool exact = false)
	{
		return this.WithEndTime(DateTimeOffset.UtcNow - ago, exact);
	}

	public LensQuery WithTail()
	{
		return this.WithRaw("-tailf");
	}

	public LensQuery WithNoTimeParsing()
	{
		return this.WithRaw("-notp");
	}

	public LensQuery ConfirmLargeScale(string code)
	{
		return this.WithRaw("-confirmlargescale \"" + code + "\"");
	}

	public LensQuery WithFilePattern(string pattern)
	{
		var query = this.WithRaw("-f \"" + pattern + "\"");
		if (pattern == FilePatterns.UxSllLogs || pattern == FilePatterns.PxsSllLogs)
			return query.WithNoTimeParsing();
		return query;
	}

	public LensQuery WithNoCache()
	{
		return new LensQuery(this.args, this.startTime, this.endTime, true);
	}
}

public static class Lens
{
	private const string LensSource = @"\\REDDOG\public\AutoPilot\lens\stable\latest";
	private static readonly string LensDestination = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LensQuery");

	public static void DeleteCache(LensQuery query)
	{
		CachedProcessStreamer.DeleteCache(
			Path.Combine(LensDestination, "lens.exe"),
			query.Args);
	}

	public static async Task<IEnumerable<T>> ExecuteAsync<T>(LensQuery query, Func<string, T, T> transform) where T : class, ITimestamped
	{
		EnsureInstalled();

		if (query.EndTime == DateTimeOffset.MaxValue)
			query = query.WithEndTime(DateTimeOffset.UtcNow + TimeSpan.FromHours(1));

		if (query.NoCache)
			Lens.DeleteCache(query);

		string largeScale = null;
		for (int i = 0; i < 2; i++)
		{
			string largeScaleErrString = null;
			List<T> items = new List<T>();
			var dc = new DumpContainer();
			dc.Dump("Lens Status");
			int lines = 0;
			long bytes = 0;
			T lastData = null;
			await CachedProcessStreamer.ExecuteAsync(
				Path.Combine(LensDestination, "lens.exe"),
				largeScale == null ? query.Args : query.WithRaw(largeScale).Args,
				line =>
				{
					lines++;
					bytes += Encoding.UTF8.GetByteCount(line) + 2; // Two for the newline
					var data = transform(line, lastData);
					if (data != null)
					{
						if (!data.Timestamp.HasValue || (data.Timestamp?.ToLocalTime() >= query.StartTime && data.Timestamp?.ToLocalTime() <= query.EndTime))
						{
							lastData = data;
							items.Add(data);
						}
					}

					dc.Content = $"{lines} lines, {((float)bytes / 1024 / 1024):0.000} MB";
					dc.Refresh();
				},
				err =>
				{
					if (err.StartsWith("-confirmlargescale "))
						largeScaleErrString = err;
					err.Dump();
				},
				query.Args);
			dc.Content = $"Complete: {lines} lines, {((float)bytes / 1024 / 1024):0.000} MB";
			dc.Refresh();

			if (largeScaleErrString != null)
			{
				largeScale = largeScaleErrString;
				continue;
			}

			return items.OrderByDescending(e => e.Timestamp?.ToLocalTime() ?? DateTimeOffset.MinValue.ToLocalTime()).ToList();
		}

		throw new Exception("Too many tries");
	}

	private static void EnsureInstalled()
	{
		if (!Directory.Exists(LensDestination) || !File.Exists(Path.Combine(LensDestination, "Lens.exe")))
		{
			var dc = new DumpContainer();
			dc.Dump($"Installing Lens to {LensDestination}");
			Directory.CreateDirectory(LensDestination);
			foreach (var fullPath in Directory.EnumerateFiles(LensSource, "*.*", SearchOption.AllDirectories))
			{
				var dir = Path.GetDirectoryName(fullPath);
				var file = Path.GetFileName(fullPath);
				dir = dir.Substring(LensSource.Length);
				dc.Content = $"Copying {Path.Combine(dir, file)}";
				dc.Refresh();
				File.Copy(Path.Combine(LensSource, dir, file), Path.Combine(LensDestination, dir, file), true);
			}
			dc.Content = "Finished.";
			dc.Refresh();
		}
	}
}

public static class CachedProcessStreamer
{
	private static string GetCacheFile(string cmd, string args)
	{
		using (var hash = SHA256.Create())
		{
			return Path.Combine(
				Path.GetTempPath(),
				"LensQueryCache-" + string.Join(
					string.Empty,
					hash.ComputeHash(Encoding.UTF8.GetBytes(cmd + args)).Select(c => c.ToString("x2"))) + ".txt");
		}
	}

	public static void DeleteCache(string cmd, string args)
	{
		File.Delete(GetCacheFile(cmd, args));
	}

	public static async Task ExecuteAsync(string cmd, string args, Action<string> lineAction, Action<string> errorAction, string cacheArgs = null)
	{
		var cacheFileName = GetCacheFile(cmd, cacheArgs ?? args);
		if (File.Exists(cacheFileName))
		{
			("Loading cached '" + cmd + " " + args + "'").Dump();
			using (var reader = new StreamReader(cacheFileName))
			{
				string line;
				while ((line = await reader.ReadLineAsync()) != null)
					lineAction(line);
			}

			return;
		}

		bool saveCache = true;
		string tmpFile = Path.GetTempFileName().Dump();
		using (var writer = new StreamWriter(tmpFile))
		{
			("Executing '" + cmd + " " + args + "'").Dump();
			ProcessStartInfo startInfo = new ProcessStartInfo(cmd, args);
			startInfo.CreateNoWindow = false;
			startInfo.RedirectStandardError = true;
			startInfo.RedirectStandardOutput = true;
			startInfo.UseShellExecute = false;

			Process proc = new Process();
			proc.StartInfo = startInfo;

			proc.OutputDataReceived += (sender, evt) =>
			{
				if (evt.Data != null)
				{
					writer.WriteLine(evt.Data);
					lineAction(evt.Data);
				}
			};

			proc.ErrorDataReceived += (sender, evt) =>
			{
				if (evt.Data != null)
					errorAction(evt.Data);
			};

			proc.Start();
			proc.BeginErrorReadLine();
			proc.BeginOutputReadLine();

			while (!proc.HasExited)
			{
				await Task.Delay(10);
			}

			if (proc.ExitCode != 0)
			{
				saveCache = false;
				$"Not caching, failed run ({proc.ExitCode})".Dump();
			}
		}

		if (saveCache)
		{
			File.Move(tmpFile, cacheFileName);
		}
		else
		{
			File.Delete(tmpFile);
		}
	}
}