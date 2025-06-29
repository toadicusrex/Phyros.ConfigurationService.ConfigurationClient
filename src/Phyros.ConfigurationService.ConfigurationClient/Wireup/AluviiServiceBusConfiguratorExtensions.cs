using Microsoft.Extensions.Logging.Abstractions;
using Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement.MessageConsumers;
using Phyros.DomainEventModels.ConfigurationEvents.Configuration;


// ReSharper disable once CheckNamespace
namespace Phyros.ServiceBus.Wireup;

public static class PhyrosServiceBusConfiguratorExtensions
{
	//public static PhyrosServiceBusConfigurator AddPhyrosConfigurationSubscriptions(
	//	this PhyrosServiceBusConfigurator configurator, string applicationName, TimeSpan idleTimeoutBeforeSubscriptionDeletion, string? forwardedTopicSuffix = null, string? forwardedTopicSubscription = null)
	//{

	//	configurator.AddTopicSubscriptionConfiguration<ConfigurationSettingChanged, ConfigurationSettingChangedConsumer>
	//		(config =>
	//		{
	//			if (!String.IsNullOrWhiteSpace(forwardedTopicSuffix))
	//			{
	//				config.SetTopicForwarding(forwardedTopicSuffix, forwardedTopicSubscription ?? forwardedTopicSuffix);
	//			}
	//			config.AddApplicationName(applicationName).AddMachineName().AddIdleTimeoutBeforeDeletion(idleTimeoutBeforeSubscriptionDeletion);
	//			//logWriter?.Log(LogEntry<>.Information("Attempting to create configuration change event subscription named {subscriptionName}", config.ToString()));
	//		});
	//	return configurator;
	//}
}
