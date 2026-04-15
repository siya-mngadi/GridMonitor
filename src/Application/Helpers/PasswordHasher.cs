using System.Security.Cryptography;

namespace GridMonitor.Application.Helpers;

public static class PasswordHasher
{
	public static string HashPassword(string password)
	{
		byte[] salt = RandomNumberGenerator.GetBytes(16);
		byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password.AsSpan(), salt, 100_000, HashAlgorithmName.SHA256, 32);

		// store: salt + hash (combine them)
		return Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
	}

	public static bool VerifyPassword(string stored, string password)
	{
		var parts = stored.Split('.');
		var salt = Convert.FromBase64String(parts[0]);
		var hash = Convert.FromBase64String(parts[1]);

		byte[] testHash = Rfc2898DeriveBytes.Pbkdf2(password.AsSpan(), salt, 100_000, HashAlgorithmName.SHA256, 32);

		return CryptographicOperations.FixedTimeEquals(hash, testHash);
	}
}
