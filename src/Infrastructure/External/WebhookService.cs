using GridMonitor.Infrastructure.Proxies;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace GridMonitor.Infrastructure.ExternalServices;

public class WebhookService
{
	private readonly WebhookClient httpClient;
	private readonly ILogger<WebhookService> logger;

	public WebhookService(WebhookClient httpClient, ILogger<WebhookService> logger)
	{
		this.httpClient = httpClient;
		this.logger = logger;
	}

	public async Task SendWebhookAsync(string url, string eventType, object payload)
	{
		var body = new
		{
			eventType,
			timestamp = DateTime.UtcNow,
			data = payload
		};

		var json = JsonSerializer.Serialize(body);

		var request = new HttpRequestMessage(HttpMethod.Post, url)
		{
			Content = new StringContent(json, Encoding.UTF8, "application/json")
		};

		request.Headers.Add("X-Webhook-Signature", GenerateSignature(json));

		try
		{
			var response = await httpClient.SendAsync(request);

			// optional: log failures
			if (!response.IsSuccessStatusCode)
			{
				// log or retry queue
			}
		}
		catch (Exception)
		{
			// log error / retry queue
		}
	}

	private string GenerateSignature(string payload)
	{
		// Simple placeholder (use HMAC in production)
		return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
	}
}
