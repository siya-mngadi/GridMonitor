using GridMonitor.Domain.Shared;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GridMonitor.Infrastructure.Proxies;

// TODO: Add retry policy
public class WebhookClient
{
	private readonly HttpClient httpClient;
	public WebhookClient(HttpClient httpClient)
	{
		this.httpClient = httpClient;
	}

	public async ValueTask SendAsync(string url, object payload ,CancellationToken ct)
	{
		var json = JsonSerializer.Serialize(payload);

		var signature = GenerateSignature(json);

		using var request = new HttpRequestMessage(HttpMethod.Post, url)
		{
			Content = new StringContent(json, Encoding.UTF8, "application/json")
		};

		request.Headers.Add("X-Signature", signature);

		var response = await httpClient.SendAsync(request, ct);
		response.EnsureSuccessStatusCode();
	}

	private static string GenerateSignature(string payload)
	{
		var signedPayload = $"{DateTime.UnixEpoch.Date.Ticks}.{payload}";

		var keyBytes = Encoding.UTF8.GetBytes(WebhookConfig.Secret);
		var payloadBytes = Encoding.UTF8.GetBytes(signedPayload);

		using var hmac = new HMACSHA256(keyBytes);
		var hash = hmac.ComputeHash(payloadBytes);

		return Convert.ToHexString(hash).ToLower();
	}
}
