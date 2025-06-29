using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Phyros.ConfigurationService.ConfigurationClient.ApiClient;
using Phyros.ConfigurationService.ConfigurationClient.Models;
using Phyros.DomainEventModels.ConfigurationEvents.Configuration;
using Serilog;
using Constants = Phyros.ConfigurationService.ConfigurationClient.Models.Constants;

namespace Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement.MessageConsumers;
internal class ConfigurationSettingChangedConsumer : IConsumer<ConfigurationSettingChanged>
{
	private readonly IConfiguration _configuration;
	private readonly IConfigurationApiReader _client;
	private readonly IConfigurationState _configurationState;
	private readonly ILogger _logger;
	private readonly IConfigurationMetadataProvider _configurationMetadataProvider;

	public ConfigurationSettingChangedConsumer(IConfiguration configuration, IConfigurationApiReader client,
		IConfigurationState configurationState, ILogger logger,
		IConfigurationMetadataProvider configurationMetadataProvider)
	{
		_configuration = configuration;
		_client = client;
		_configurationState = configurationState;
		_logger = logger;
		_configurationMetadataProvider = configurationMetadataProvider;
	}
	public Task Consume(ConsumeContext<ConfigurationSettingChanged> context)
	{
		var targetKey = new ConfigurationKey(context.Message.Key, context.Message.OrganizationalUnit);
		if (!_configurationMetadataProvider.HasManagedKey(targetKey))
		{
			// if we aren't using the key in this application, we don't need to update it.
			_configurationState.TriggerIgnoredChangeHandling(targetKey);
			return Task.CompletedTask;
		}
		var newSetting = Task.Run(async () => await _client.GetConfigurationSettingAsync(_configurationState.ConfigurationOrganizationalUnit, context.Message.Key))
			.ConfigureAwait(false)
			.GetAwaiter()
			.GetResult();
		if (newSetting is not PhyrosConfigurationSetting setting)
		{
			_logger
				.ForContext("setting", newSetting)
				.Information("No valid configuration setting with key of '{key}' was found. Setting was of type '{type}'.", context.Message.Key, newSetting?.GetType()?.FullName ?? "unknown");
			return Task.CompletedTask;
		}

		if (setting.ValueType == Constants.CONNECTION_STRING_VALUE_TYPE)
		{
			var connectionStrings = JsonSerializer.Deserialize<Dictionary<string, string>>(_configuration[Constants.CONNECTION_STRINGS_CONFIG_GROUP] ?? "{}", new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })!;
			if (!connectionStrings.ContainsKey(targetKey) || connectionStrings[targetKey] != setting.Value)
			{
				connectionStrings[targetKey] = setting.Value!;
			}
			_configuration[Constants.CONNECTION_STRINGS_CONFIG_GROUP] = JsonSerializer.Serialize(connectionStrings);
			_configurationState.TriggerChangeHandling($"{Constants.CONNECTION_STRINGS_CONFIG_GROUP}:{targetKey}");
			//_logWriter.Log(LogEntry.Information("Triggered change handling for connection string '{key}'.", context.Message.Name, context.Message.OrganizationalUnit.ToString()));
		}
		if (_configuration[targetKey] != setting.Value)
		{
			// only change and throw the change event in the event of an actual data change.
			_configuration[targetKey] = setting.Value;
			_configurationState.TriggerChangeHandling(targetKey);
		}
		return Task.CompletedTask;
	}
}
