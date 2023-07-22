<Query Kind="Program">
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.Security.Cryptography</Namespace>
</Query>

void Main()
{
	PuidToQuickExportId(985153942753932).Dump();
}

string PuidToQuickExportId(long puid)
{
	var hexStr = puid.ToString("X", CultureInfo.InvariantCulture).PadLeft(16, '0');

	var derivedBytes = new PasswordDeriveBytes(new byte[] { 0x58, 0x41, 0x0F, 0x3B, 0x3F, 0xBE, 0x34, 0x64, 0xA4, 0xAA, 0x7B, 0x5D, 0xD2, 0xD8, 0xCE, 0x1B }, null);
	var derivedKey = derivedBytes.CryptDeriveKey("RC2", "MD5", 128, new byte[8]);
	HMACMD5 hmac = new HMACMD5(derivedKey);

	const int PuidLength = 32;

	byte[] bytes = new byte[sizeof(char) * (PuidLength + 1)];
	Encoding.Unicode.GetBytes(hexStr).CopyTo(bytes, 0);

	byte[] hash = hmac.ComputeHash(bytes);

	StringBuilder hex = new StringBuilder(16);
	for (int i = 0; i < hash.Length - 4; i++)
	{
		hex.Append(hash[i].ToString("X", CultureInfo.InvariantCulture).PadLeft(2, '0'));
	}

	hex.Append('F', 8);

	return hex.ToString();
}

// Define other methods and classes here