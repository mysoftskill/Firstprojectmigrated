<Query Kind="Program">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>System.IO.Compression</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Converters</Namespace>
</Query>

void Main()
{
	string key = "H4sIAAAAAAAEAKtWylayqlbKVLJSSrTSNTIzNrQwNTA2NjI3szQ0NbOwNFDSUSoBShoZGJrrGpjrGhuGGBhYgZGeAQRog3lAdalAdUC-oS4Y4VaXCFTnl5-XCmQmKVnllebk1EIscSwoCC1OTE91TixKUaoFAGUC1QWbAAAA";
	DecodeTimelineKey(key);
}

void DecodeTimelineKey(string input)
{
	input = input.Replace('-', '+').Replace('_', '/');
	switch (input.Length % 4)
	{
		case 0:
			break;
		case 2:
			input += "==";
			break;
		case 3:
			input += "=";
			break;
		default:
			throw new FormatException($"Unable to decode: '{input}' as Base64Url encoded string.");
	}

	using (var inStream = new MemoryStream(Convert.FromBase64String(input)))
	using (var gzStream = new GZipStream(inStream, CompressionMode.Decompress, true))
	using (var outStream = new MemoryStream())
	{
		gzStream.CopyTo(outStream);

		string json = Encoding.UTF8.GetString(outStream.ToArray());
		var jobject = JsonConvert.DeserializeObject(json);
		JsonConvert.SerializeObject(jobject, Newtonsoft.Json.Formatting.Indented, new StringEnumConverter()).Dump("Key");
	}
}