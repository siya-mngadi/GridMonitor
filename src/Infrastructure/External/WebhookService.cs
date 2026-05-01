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

	public async ValueTask SendWebhookAsync(string url, object payload, CancellationToken ct)
	{
		try
		{
			await httpClient.SendAsync(url, payload, ct);
		}
		catch (Exception)
		{
			// log error / retry queue
		}
	}
}
