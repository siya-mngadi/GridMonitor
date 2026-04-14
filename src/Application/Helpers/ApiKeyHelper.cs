using System.Security.Cryptography;
using System.Text;

namespace GridMonitor.Application.Helpers;

public static class ApiKeyHelper
{
	public static (string plainKey, string hash, string prefix) Generate()
	{
		var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
						 .Replace("+", "")
						 .Replace("/", "")
						 .Replace("=", "")[..40];

		var plain = $"ls_{raw}";
		var hash = Hash(plain);
		var prefix = $"ls_{raw[..8]}...";

		return (plain, hash, prefix);
	}

	public static string Hash(string key)
	{
		var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
		return Convert.ToHexString(bytes).ToLower();
	}
}
