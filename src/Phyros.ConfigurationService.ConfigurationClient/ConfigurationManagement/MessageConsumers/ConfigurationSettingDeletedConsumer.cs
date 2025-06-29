using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Phyros.ConfigurationService.ConfigurationClient.ApiClient;
using Phyros.ConfigurationService.ConfigurationClient.Models;
using Phyros.DomainEventModels.ConfigurationEvents.Configuration;

namespace Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement.MessageConsumers;

internal class ConfigurationSettingDeletedConsumer : IConsumer<ConfigurationSettingDeleted>
{
	private readonly IConfiguration _configuration;
	private readonly IConfigurationApiReader _client;
	private readonly IConfigurationState _configurationState;
	private readonly IConfigurationMetadataProvider _configurationMetadataProvider;

	public ConfigurationSettingDeletedConsumer(IConfiguration configuration, IConfigurationApiReader client, IConfigurationState configurationState, IConfigurationMetadataProvider configurationMetadataProvider)
	{
		_configuration = configuration;
		_client = client;
		_configurationState = configurationState;
		_configurationMetadataProvider = configurationMetadataProvider;
	}
	public Task Consume(ConsumeContext<ConfigurationSettingDeleted> context)
	{
		var targetKey = new ConfigurationKey(context.Message.Key, context.Message.OrganizationalUnit);
		if (!_configurationMetadataProvider.HasManagedKey(targetKey))
		{
			// if we aren't using the key in this application, we don't need to delete it from the list we're watching.  On the other hand, it doesn't harm us and in fact,
			// it would help to keep watching it in case someone is deleting and then re-adding it.  Apart from that, we may be just deleting a "child" organizational unit's
			// value, and now we're going to need the parent.  There's probably some cleanup that needs to happen, but I don't have a use case for making it a priority.
			return Task.CompletedTask;
		}
		// clear the key
		_configuration[targetKey] = null;
		_configurationState.TriggerDeleteHandling(context.Message.OrganizationalUnit, targetKey);

		// if we actually receive a value for this configuration setting, it's probably that we deleted a child organizational unit's setting, and now we're inheriting the base.  Instead
		// of a delete event, we'll internally trigger a change event, because to this system, it's just a change.
		var connectionStrings = JsonSerializer.Deserialize<Dictionary<string, string>>(_configuration[Constants.CONNECTION_STRINGS_CONFIG_GROUP] ?? "{}", new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })!;
		if (connectionStrings.ContainsKey(targetKey))
		{
			connectionStrings.Remove(targetKey);
			_configuration[Constants.CONNECTION_STRINGS_CONFIG_GROUP] = JsonSerializer.Serialize(connectionStrings);
			_configurationState.TriggerDeleteHandling(context.Message.OrganizationalUnit, $"{Constants.CONNECTION_STRINGS_CONFIG_GROUP}:{targetKey}");
		}

		if (string.IsNullOrWhiteSpace(_configuration[targetKey]))
		{
			return Task.CompletedTask;
		}

		// only change and throw the change event in the event of an actual data change.
		_configuration[targetKey] = null;
		_configurationState.TriggerDeleteHandling(context.Message.OrganizationalUnit, targetKey);
		return Task.CompletedTask;
	}
}
