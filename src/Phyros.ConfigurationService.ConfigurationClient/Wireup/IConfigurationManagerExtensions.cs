using Microsoft.Extensions.DependencyInjection;
using Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Configuration;

// ReSharper disable once InconsistentNaming
public static class IConfigurationManagerExtensions
{
	public static IConfigurationManager AddPhyrosConfigurationProvider(this IConfigurationManager configurationManager, string configurationGroupName, string organizationalUnitName, string baseConnectionStringName, IConfigurationState configurationState, MultitenantConfigurationDataStore configurationDataStore)
	{
		// my original implementation.  Remove after the nuget has been deployed and older implementation is not used.
		if (string.IsNullOrWhiteSpace(organizationalUnitName))
		{
			organizationalUnitName = configurationManager["StartupOrganizationalUnitName"] ?? string.Empty;
		}
		if (!string.IsNullOrWhiteSpace(baseConnectionStringName))
		{
			configurationState.SetBaseConnectionStringName(baseConnectionStringName);
		}

		// we need a temporary service collection, wired up with the same stuff in order to create our config provider at startup (we need IOC resolution prior to the application IOC's resolution phase)
		var serviceCollection = new ServiceCollection();
		// add all necessary registrations to our temporary IOC, using the same wireup
		serviceCollection.AddPhyrosConfigurationComponentRegistrations(configurationManager, configurationGroupName, organizationalUnitName, baseConnectionStringName, configurationDataStore, out var c);
		// create the service provider (i.e. move our temporary IOC into the resolution phase)
		var serviceProvider = serviceCollection.BuildServiceProvider();
		// cast the configurationManager to a configurationBuilder, and add the config source to the list of sources for the host application's configuration
		IConfigurationBuilder configBuilder = configurationManager;
		configBuilder.Add(serviceProvider.GetRequiredService<PhyrosConfigurationSource>());

		return configurationManager;
	}
}
