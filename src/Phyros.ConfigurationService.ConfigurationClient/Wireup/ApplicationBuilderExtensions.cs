using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder;

public static class ApplicationBuilderExtensions
{
	public static IApplicationBuilder UsePhyrosConfigurationEvents(this IApplicationBuilder application, Action<ConfigurationClientOptions> configure)
	{
		var logger = application.ApplicationServices.GetRequiredService<ILogger>();
		var configurationState = application.ApplicationServices.GetRequiredService<IConfigurationState>();
		try
		{
			var options = new ConfigurationClientOptions();
			configure(options);
			configurationState.SetOptions(options);
		}
		catch (Exception ex)
		{
			logger.Fatal("Unable to load configuration mutation events.", ex);
		}
		return application;
	}
}
