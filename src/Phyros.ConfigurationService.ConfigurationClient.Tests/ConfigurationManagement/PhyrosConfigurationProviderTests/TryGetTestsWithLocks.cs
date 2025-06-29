using FluentAssertions;
using NSubstitute;
using Phyros.ConfigurationService.ConfigurationClient.ApiClient;
using Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement;
using Phyros.ConfigurationService.ConfigurationClient.Models;
using Phyros.OrganizationalUnits;
using Xunit.Abstractions;

namespace Phyros.ConfigurationService.ConfigurationClient.Tests.ConfigurationManagement.PhyrosConfigurationProviderTests;

public class TryGetTestsWithLocks
{
	private readonly ITestOutputHelper _outputHelper;

	public TryGetTestsWithLocks(ITestOutputHelper outputHelper)
	{
		_outputHelper = outputHelper;
	}
	public List<IConfigurationSetting> TestSettingsData =>
	[
		new PhyrosConfigurationSetting()
		{
			Key = "LockedAtRoot",
			Locked = true,
			Value = "LockedAtRoot",
			ValueType = Guid.NewGuid().ToString(),
			OrganizationalUnit = ""
		},
		new PhyrosConfigurationSetting()
		{
			Key = "LockedAtRoot",
			Locked = false,
			Value = "Child 1 Value",
			ValueType = Guid.NewGuid().ToString(),
			OrganizationalUnit = "child1"
		},
		new PhyrosConfigurationSetting()
		{
			Key = "LockedAtChild1",
			Locked = false,
			Value = "LockedAtChild1 Root Value",
			ValueType = Guid.NewGuid().ToString(),
			OrganizationalUnit = ""
		},
		new PhyrosConfigurationSetting()
		{
			Key = "LockedAtChild1",
			Locked = true,
			Value = "LockedAtChild1",
			ValueType = Guid.NewGuid().ToString(),
			OrganizationalUnit = "child1"
		},
		new PhyrosConfigurationSetting()
		{
			Key = "LockedAtChild2WithoutRoot",
			Locked = false,
			Value = "LockedAtChild2WithoutRoot Child1 Value",
			ValueType = Guid.NewGuid().ToString(),
			OrganizationalUnit = "child1"
		},
		new PhyrosConfigurationSetting()
		{
			Key = "LockedAtChild2WithoutRoot",
			Locked = true,
			Value = "LockedAtChild2WithoutRoot",
			ValueType = Guid.NewGuid().ToString(),
			OrganizationalUnit = "child1.child2"
		}
	];

	[Theory]
	[InlineData("", "LockedAtRoot", "LockedAtRoot", 0)]
	[InlineData("child1", "child1|LockedAtRoot", "LockedAtRoot", 0)]
	[InlineData( "child1.child2", "child1.child2|LockedAtRoot", "LockedAtRoot", 0)]
	[InlineData( "child1.child2.child3", "child1.child2.child3|LockedAtRoot", "LockedAtRoot", 0)]
	[InlineData("", "LockedAtChild1", "LockedAtChild1 Root Value", 0)]
	[InlineData("child1", "child1|LockedAtChild1", "LockedAtChild1", 0)]
	[InlineData("child1.child2", "child1.child2|LockedAtChild1", "LockedAtChild1", 0)]
	[InlineData("child1.child2.child3", "child1.child2.child3|LockedAtChild1", "LockedAtChild1", 0)]
	[InlineData("", "LockedAtChild2WithoutRoot", null, 1)]
	[InlineData("child1", "child1|LockedAtChild2WithoutRoot", "LockedAtChild2WithoutRoot Child1 Value", 1)]
	[InlineData("child1.child2", "child1.child2|LockedAtChild2WithoutRoot", "LockedAtChild2WithoutRoot", 1)]
	[InlineData("child1.child2.child3", "child1.child2.child3|LockedAtChild2WithoutRoot", "LockedAtChild2WithoutRoot", 1)]
	public async Task Load_should_read_settings_correctly_for_locked_nodes(string organizationalUnit, string key, string? expectedValue, int shouldCallAddAdditionalConfigurationSettingAsyncCount)
	{
		// arrange
		var mockClientData = new Wireup.ConfigurationClientData()
		{
			BaseConnectionStringName = Guid.NewGuid().ToString(),
			ConfigurationGroupName = Guid.NewGuid().ToString(),
			OrganizationalUnit = ""
		};
		var settingReader = Substitute.For<IConfigurationApiReader>();
		settingReader.LoadConfigurationGroupAsync(mockClientData.OrganizationalUnit, mockClientData.ConfigurationGroupName).Returns(callInfo => TestSettingsData);
		settingReader.AddAdditionalConfigurationSettingAsync(mockClientData.OrganizationalUnit, mockClientData.ConfigurationGroupName, Arg.Any<string>()).Returns(callInfo =>
		{
			var configurationKey = new ConfigurationKey(callInfo.ArgAt<string>(2), mockClientData.OrganizationalUnit);
			return new NullConfigurationSetting()
			{
				OrganizationalUnit = configurationKey.OrganizationalUnit,
				Key = configurationKey.Name,
			};
		});
		var settingWriter = Substitute.For<IConfigurationApiValueWriter>();
		var dataStore = new MultitenantConfigurationDataStore();
		var classUnderTest = new PhyrosConfigurationProvider(settingReader, settingWriter, mockClientData, dataStore);
		var targetManagedKey = new ConfigurationKey(key, mockClientData.OrganizationalUnit);
		classUnderTest.Load();

		// act
		var found = classUnderTest.TryGet(targetManagedKey, out var foundValue);

		// assert
		await settingReader.Received(1).LoadConfigurationGroupAsync(mockClientData.OrganizationalUnit, mockClientData.ConfigurationGroupName);
		await settingReader.Received(shouldCallAddAdditionalConfigurationSettingAsyncCount).AddAdditionalConfigurationSettingAsync(Arg.Any<OrganizationalUnit>(), Arg.Any<string>(), Arg.Any<string>());

		var data = classUnderTest.GetDataSnapshot();
		
		found.Should().Be(expectedValue != null);
		if (found)
		{
			foundValue.Should().BeEquivalentTo(expectedValue);
		}
		
	}
}
