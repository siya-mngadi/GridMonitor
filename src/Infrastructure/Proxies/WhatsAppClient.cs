namespace GridMonitor.Infrastructure.Proxies;

// TODO: add whatsapp client proxy implementation here.
// This will be used to send notifications to users via whatsapp when they have an active subscription and an alert is triggered.
public class WhatsAppClient
{
	private readonly HttpClient httpClient;
	public WhatsAppClient(HttpClient httpClient)
	{
		this.httpClient = httpClient;
	}

	public ValueTask SendAsync(object payload, CancellationToken ct)
	{
		return ValueTask.CompletedTask;
	}
}
