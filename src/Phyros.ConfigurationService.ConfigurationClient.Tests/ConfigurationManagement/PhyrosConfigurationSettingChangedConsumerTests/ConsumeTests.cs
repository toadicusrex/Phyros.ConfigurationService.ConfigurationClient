using MassTransit;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Phyros.ConfigurationService.ConfigurationClient.ApiClient;
using Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement;
using Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement.MessageConsumers;
using Phyros.ConfigurationService.ConfigurationClient.Models;
using Phyros.DomainEventModels.ConfigurationEvents.Configuration;
using Phyros.OrganizationalUnits;
using Serilog;

namespace Phyros.ConfigurationService.ConfigurationClient.Tests.ConfigurationManagement.PhyrosConfigurationSettingChangedConsumerTests;
public class ConsumeTests
{
	/// <summary>
	/// Shoulds the handle correctly when the key isnt watched by the client.
	/// This just means that the client, as it is configured, doesn't care about the particular value that it received the event about.
	/// </summary>
	[Fact]
	public async Task Should_handle_correctly_when_the_key_is_not_watched_by_the_client()
	{
		// arrange
		var key = Guid.NewGuid().ToString();
		var organizationalUnit = OrganizationalUnit.Parse(Guid.NewGuid().ToString());
		var configuration = Substitute.For<IConfiguration>();
		var configurationReader = Substitute.For<IConfigurationApiReader>();
		var configurationState = Substitute.For<IConfigurationState>();
		configurationState.ConfigurationOrganizationalUnit.Returns(organizationalUnit);
		var consumeContext = Substitute.For<ConsumeContext<ConfigurationSettingChanged>>();
		var logger = Substitute.For<ILogger>();
		var metadataProvider = Substitute.For<IConfigurationMetadataProvider>();
		metadataProvider.HasManagedKey(Arg.Any<string>()).Returns(false);

		consumeContext.Message.Returns(new ConfigurationSettingChanged()
		{
			Key = key, OrganizationalUnit = organizationalUnit,
		});
		
		// act
		var classUnderTest = new ConfigurationSettingChangedConsumer(configuration, configurationReader, configurationState, logger, metadataProvider);
		await classUnderTest.Consume(consumeContext);

		// assert
		metadataProvider.Received(1).HasManagedKey(new ConfigurationKey(key, organizationalUnit));
	}

	/// <summary>Should handle correctly when the key is watched by the client but is not an phyros setting.  Eventually there will very, very likely be other types of configuration values.</summary>
	[Fact]
	public async Task Should_handle_correctly_when_the_key_is_watched_by_the_client_but_is_not_an_Phyros_setting()
	{
		// arrange
		var key = Guid.NewGuid().ToString();
		var organizationalUnit = OrganizationalUnit.Parse(Guid.NewGuid().ToString());
		var configuration = Substitute.For<IConfiguration>();
		var configurationReader = Substitute.For<IConfigurationApiReader>();
		var configurationState = Substitute.For<IConfigurationState>();
		configurationState.ConfigurationOrganizationalUnit.Returns(organizationalUnit);
		var consumeContext = Substitute.For<ConsumeContext<ConfigurationSettingChanged>>();
		var logger = Substitute.For<ILogger>();
		consumeContext.Message.Returns(new ConfigurationSettingChanged()
		{
			Key = key,
			OrganizationalUnit = organizationalUnit,
		});
		var metadataProvider = Substitute.For<IConfigurationMetadataProvider>();
		metadataProvider.HasManagedKey(Arg.Any<string>()).Returns(true);

		// act
		var classUnderTest = new ConfigurationSettingChangedConsumer(configuration, configurationReader, configurationState, logger, metadataProvider);
		await classUnderTest.Consume(consumeContext);

		// assert
		await configurationReader.Received(1).GetConfigurationSettingAsync(Arg.Any<OrganizationalUnit>(), key);
	}

	[Fact]
	public async Task Should_handle_correctly_when_the_key_is_watched_by_the_client()
	{
		// arrange
		var key = Guid.NewGuid().ToString();
		var organizationalUnit = OrganizationalUnit.Parse(Guid.NewGuid().ToString());
		var configuration = Substitute.For<IConfiguration>();
		var configurationReader = Substitute.For<IConfigurationApiReader>();
		var logWriter = Substitute.For<ILogger>();
		configurationReader.GetConfigurationSettingAsync(organizationalUnit, key).Returns(callInfo =>
			new PhyrosConfigurationSetting()
			{
				Key = key,
				OrganizationalUnit = organizationalUnit,
				ValueType = Guid.NewGuid().ToString(),
				Locked = true,
				Value = Guid.NewGuid().ToString()
			});
		var configurationState = Substitute.For<IConfigurationState>();
		configurationState.ConfigurationOrganizationalUnit.Returns(organizationalUnit);
		var consumeContext = Substitute.For<ConsumeContext<ConfigurationSettingChanged>>();
		consumeContext.Message.Returns(new ConfigurationSettingChanged()
		{
			Key = key,
			OrganizationalUnit = organizationalUnit,
		});
		var metadataProvider = Substitute.For<IConfigurationMetadataProvider>();
		metadataProvider.HasManagedKey(Arg.Any<string>()).Returns(true);

		// act
		var classUnderTest = new ConfigurationSettingChangedConsumer(configuration, configurationReader, configurationState, logWriter, metadataProvider);
		await classUnderTest.Consume(consumeContext);

		// assert
		await configurationReader.Received(1).GetConfigurationSettingAsync(Arg.Any<OrganizationalUnit>(), key);
	}
}
