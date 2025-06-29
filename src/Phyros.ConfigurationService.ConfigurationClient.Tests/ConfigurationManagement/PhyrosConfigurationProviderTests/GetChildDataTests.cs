using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using Phyros.ConfigurationService.ConfigurationClient.ApiClient;
using Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement;
using Phyros.ConfigurationService.ConfigurationClient.Models;
using Phyros.OrganizationalUnits;

namespace Phyros.ConfigurationService.ConfigurationClient.Tests.ConfigurationManagement.PhyrosConfigurationProviderTests;
public class GetChildDataTests
{
	[Theory]
	[InlineData("key1", "key1value", true)]
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
		var settingsWriter = Substitute.For<IConfigurationApiValueWriter>();
		var dataStore = new MultitenantConfigurationDataStore();
		var classUnderTest = new PhyrosConfigurationProvider(settingReader, settingsWriter, mockClientData, dataStore);
		var testObject = new { One = 1, Two = "Two", Three = new { ThreeHasChildren = true } };
		classUnderTest.Set("parentPath", JsonSerializer.Serialize(testObject));

		// act
		var result = classUnderTest.GetChildKeys(new List<string>(), "parentPath").ToList();

		// assert
		result.Count.Should().Be(4);
		result.Should().Contain("parentPath:One");
		result.Should().Contain("parentPath:Two");
		result.Should().Contain("parentPath:Three");
		result.Should().Contain("parentPath:Three:ThreeHasChildren");
	}
}
