using FluentAssertions;
using NSubstitute;
using Phyros.ConfigurationService.ConfigurationClient.ApiClient;
using Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement;
using Phyros.ConfigurationService.ConfigurationClient.Models;
using Phyros.OrganizationalUnits;

namespace Phyros.ConfigurationService.ConfigurationClient.Tests.ConfigurationManagement.PhyrosConfigurationProviderTests;
public class TryGetTests
{
	[Theory]
	[InlineData("key1", "key1value", true)]
	[InlineData("key2", null, false)]
	public void TryGet_should_return_key_appropriately(string targetKey, string expectedValue, bool expectedSuccess)
	{
		// arrange
		var mockClientData = new Wireup.ConfigurationClientData()
		{
			BaseConnectionStringName = Guid.NewGuid().ToString(),
			ConfigurationGroupName = Guid.NewGuid().ToString(),
			OrganizationalUnit = OrganizationalUnit.Parse(Guid.NewGuid().ToString())
		};
		var settingReader = Substitute.For<IConfigurationApiReader>();
		settingReader.AddAdditionalConfigurationSettingAsync(mockClientData.OrganizationalUnit, mockClientData.ConfigurationGroupName, targetKey)
			.Returns(callInfo =>
			{
				if (!String.IsNullOrWhiteSpace(expectedValue))
				{
					return new PhyrosConfigurationSetting()
					{
						Key = expectedValue,
						Locked = true,
						Value = expectedValue,
						ValueType = Guid.NewGuid().ToString(),
						OrganizationalUnit = mockClientData.OrganizationalUnit
					};
				}
				return new NullConfigurationSetting();
			});
		var settingWriter = Substitute.For<IConfigurationApiValueWriter>();
		var dataStore = new MultitenantConfigurationDataStore();

		// act
		var classUnderTest = new PhyrosConfigurationProvider(settingReader, settingWriter, mockClientData, dataStore);
		var result = classUnderTest.TryGet(targetKey, out string? settingValue);

		// assert
		settingReader
			.Received(1)
			.AddAdditionalConfigurationSettingAsync(mockClientData.OrganizationalUnit, mockClientData.ConfigurationGroupName, targetKey);
		result.Should().Be(expectedSuccess);
		if (expectedSuccess)
		{
			settingValue.Should().BeEquivalentTo(expectedValue);
		}
		else
		{
			settingValue.Should().BeNull();
		}
	}
}
