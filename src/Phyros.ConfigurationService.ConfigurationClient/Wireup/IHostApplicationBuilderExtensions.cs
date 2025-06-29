using Phyros.OrganizationalUnits;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Hosting;

// ReSharper disable once InconsistentNaming
public static class IHostApplicationBuilderExtensions
{
	// .NET CORE base web application builder
	public static IHostApplicationBuilder AddPhyrosConfiguration(this IHostApplicationBuilder appBuilder, string configurationGroupName, OrganizationalUnit organizationalUnit, string baseConnectionStringName = "TenancyDatabaseConnection")
	{
		var configurationDataStore = new MultitenantConfigurationDataStore();
		appBuilder.Services.AddPhyrosConfigurationComponentRegistrations(appBuilder.Configuration, configurationGroupName, organizationalUnit, baseConnectionStringName, configurationDataStore, out var configurationState);
		configurationState.SetOrganizationalUnit(organizationalUnit);
		appBuilder.Configuration.AddPhyrosConfigurationProvider(configurationGroupName, organizationalUnit, baseConnectionStringName, configurationState, configurationDataStore);
		return appBuilder;
	}
}
