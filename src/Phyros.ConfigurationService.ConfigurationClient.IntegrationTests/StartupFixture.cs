using Microsoft.Extensions.DependencyInjection;

namespace Phyros.ConfigurationService.ConfigurationClient.IntegrationTests;
public class StartupFixture : IDisposable
{
	public IHttpClientFactory HttpClientFactory { get; }

	public StartupFixture()
	{
		var services = new ServiceCollection();
		services.AddHttpClient(); // Register IHttpClientFactory
		// Add other services if needed

		var serviceProvider = services.BuildServiceProvider();
		HttpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
	}

	public void Dispose()
	{
		// Clean up if necessary
	}
}
