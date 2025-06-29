using Phyros.OrganizationalUnits;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Phyros.ConfigurationService.ConfigurationClient;
using Phyros.ConfigurationService.ConfigurationClient.ApiClient;
using Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement;
using Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement.MessageConsumers;
using Phyros.ConfigurationService.ConfigurationClient.Wireup;
using Phyros.DomainEventModels.ConfigurationEvents.Configuration;
using Polly;
using Polly.Extensions.Http;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;
public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPhyrosConfigurationComponentRegistrations(this IServiceCollection serviceCollection, IConfiguration apiConfiguration, string configurationGroupName, OrganizationalUnit organizationalUnit, string baseConnectionStringName, MultitenantConfigurationDataStore configurationDataStore, out IConfigurationState configurationState, bool useNetFrameworkHttpClientFactory = false)
	{
		configurationState = new ConfigurationState();
		// we have to add the single datastore; we also have to add it to the temporary service collection we'll be adding.
		serviceCollection.AddSingleton(configurationDataStore);

		// my original implementation.  Remove after the nuget has been deployed and older implementation is not used.
		serviceCollection.AddSingleton(new ConfigurationClientData()
		{
			OrganizationalUnit = organizationalUnit,
			BaseConnectionStringName = baseConnectionStringName,
			ConfigurationGroupName = configurationGroupName
		});
		serviceCollection.AddTransient<IConsumer<ConfigurationSettingChanged>, ConfigurationSettingChangedConsumer>();
		serviceCollection.AddTransient<IConfigurationApiReader, ConfigurationApiClient>();
		serviceCollection.AddTransient<IConfigurationApiValueWriter, ConfigurationApiClient>();
		serviceCollection.AddTransient<IClientStatusProvider, ConfigurationApiClient>();
		serviceCollection.AddTransient<IClientInformationProvider, ConfigurationApiClient>();

		serviceCollection.AddSingleton(configurationState);
		if (!useNetFrameworkHttpClientFactory)
		{
			serviceCollection.AddHttpClient<IConfigurationApiReader, ConfigurationApiClient>((services, client) =>
				{
					var configuration = services.GetService<IConfiguration>()!;
					client.BaseAddress = new Uri($"https://{configuration["ConfigurationApiHostName"]!}");
				})
				.SetHandlerLifetime(TimeSpan.FromMinutes(1)) //Set lifetime to five minutes
				.AddPolicyHandler(GetRetryPolicy());

			serviceCollection.AddHttpClient<IConfigurationApiValueWriter, ConfigurationApiClient>((services, client) =>
				{
					var configuration = services.GetService<IConfiguration>()!;
					client.BaseAddress = new Uri($"https://{configuration["ConfigurationApiHostName"]!}");
				})
				.SetHandlerLifetime(TimeSpan.FromMinutes(1)) //Set lifetime to five minutes
				.AddPolicyHandler(GetRetryPolicy());

			serviceCollection.AddHttpClient<IClientStatusProvider, ConfigurationApiClient>((services, client) =>
				{
					var configuration = services.GetService<IConfiguration>()!;
					client.BaseAddress = new Uri($"https://{configuration["ConfigurationApiHostName"]!}");
				})
				.SetHandlerLifetime(TimeSpan.FromMinutes(1)) //Set lifetime to five minutes
				.AddPolicyHandler(GetRetryPolicy());

			serviceCollection.AddHttpClient<IClientInformationProvider, ConfigurationApiClient>((services, client) =>
				{
					var configuration = services.GetService<IConfiguration>()!;
					client.BaseAddress = new Uri($"https://{configuration["ConfigurationApiHostName"]!}");
				})
				.SetHandlerLifetime(TimeSpan.FromMinutes(1)) //Set lifetime to five minutes
				.AddPolicyHandler(GetRetryPolicy());
		}
		else
		{
			//serviceCollection.AddHttpClient<ConfigurationApiClient>((services, client) =>
			//{
			//	var configuration = services.GetService<IConfiguration>()!;
			//	client.BaseAddress = new Uri($"https://{configuration["ConfigurationApiHostName"]!}");
			//	client.DefaultRequestHeaders.Add("content-type", "application/json");
			//});
			NetFrameworkHttpClientFactory.BaseUrl = $"https://{apiConfiguration["ConfigurationApiHostName"]!}";
			serviceCollection.AddSingleton<HttpClient>(sp => NetFrameworkHttpClientFactory.Instance);
		}

		serviceCollection.AddTransient<PhyrosConfigurationProvider>();
		serviceCollection.AddTransient<IConfigurationMetadataProvider>(serviceProvider => serviceProvider.GetRequiredService<PhyrosConfigurationProvider>());
		serviceCollection.AddTransient<PhyrosConfigurationSource>();
		serviceCollection.AddSingleton<IConfiguration>(apiConfiguration);
		return serviceCollection;
	}

	public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
	{
		return HttpPolicyExtensions
			.HandleTransientHttpError()
			.OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
			.WaitAndRetryAsync(0, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
				retryAttempt)));
	}

	public static class NetFrameworkHttpClientFactory
	{
		public static string BaseUrl { get; set; } = String.Empty;

		private static readonly Lazy<HttpClient> lazyHttpClient = new Lazy<HttpClient>(() =>
		{
			var client =new HttpClient() { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(30) };
			return client;
		});
		public static HttpClient Instance
		{
			get
			{
				return lazyHttpClient.Value;
			}
		}
	}
}
