using FluentAssertions;
using NSubstitute;
using Phyros.ConfigurationService.ConfigurationClient.ApiClient;
using Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement;
using Phyros.ConfigurationService.ConfigurationClient.Models;
using Phyros.OrganizationalUnits;
using Xunit.Abstractions;

namespace Phyros.ConfigurationService.ConfigurationClient.Tests.ConfigurationManagement.PhyrosConfigurationProviderTests;

public class LoadTests
{
	private readonly ITestOutputHelper _outputHelper;

	public LoadTests(ITestOutputHelper outputHelper)
	{
		_outputHelper = outputHelper;
	}
	public List<IConfigurationSetting> TestSettingsData =>
	[
		new PhyrosConfigurationSetting()
		{
			Key = "HierarchicalKey",
			Locked = false,
			Value = "Root Value",
			ValueType = Guid.NewGuid().ToString(),
			OrganizationalUnit = ""
		},
		new PhyrosConfigurationSetting()
		{
			Key = "HierarchicalKey",
			Locked = false,
			Value = "Child 1 Value",
			ValueType = Guid.NewGuid().ToString(),
			OrganizationalUnit = "child1"
		},
		new PhyrosConfigurationSetting()
		{
			Key = "HierarchicalKey",
			Locked = false,
			Value = "Child 2 Value",
			ValueType = Guid.NewGuid().ToString(),
			OrganizationalUnit = "child1.child2"
		},
		new PhyrosConfigurationSetting()
		{
			Key = "HierarchicalKey",
			Locked = false,
			Value = "Child 3 Value",
			ValueType = Guid.NewGuid().ToString(),
			OrganizationalUnit = "child1.child2.child3"
		},
		new PhyrosConfigurationSetting()
		{
			Key = "SkippingHierarchicalKey",
			Locked = false,
			Value = "Root Skipping Value",
			ValueType = Guid.NewGuid().ToString(),
			OrganizationalUnit = ""
		},
		new PhyrosConfigurationSetting()
		{
			Key = "SkippingHierarchicalKey",
			Locked = false,
			Value = "Child 3 Skipping Value",
			ValueType = Guid.NewGuid().ToString(),
			OrganizationalUnit = "child1.child2.child3"
		}
	];


	[Theory]
	[InlineData("", "HierarchicalKey", "Root Value", null, 0)]
	[InlineData("Phyros", "HierarchicalKey", "Root Value", null, 0)]
	[InlineData("child1", "child1|HierarchicalKey", "Child 1 Value", null, 0)]
	[InlineData("child1.child2", "child1.child2|HierarchicalKey", "Child 2 Value", null, 0)]
	[InlineData("child1.child2.child3", "child1.child2.child3|HierarchicalKey", "Child 3 Value", null, 0)]
	[InlineData("", "SkippingHierarchicalKey", "Root Skipping Value", null, 0)]
	[InlineData("child1", "child1|SkippingHierarchicalKey", "Root Skipping Value", "child1", 1)]
	[InlineData("child1.child2", "child1.child2|SkippingHierarchicalKey", "Root Skipping Value", "child1, child1.child2", 2)]
	[InlineData("child1.child2.child3", "child1.child2.child3|SkippingHierarchicalKey", "Child 3 Skipping Value", "child1, child1.child2", 2)]
	public async Task Load_should_read_settings_correctly_for_root_phyros_node(string organizationalUnit, string key, string expectedValue, string? orgUnitsWithNullSettingValues, int shouldCallAddAdditionalConfigurationSettingAsyncCount)
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
		settingReader.AddAdditionalConfigurationSettingAsync(Arg.Any<OrganizationalUnit>(), mockClientData.ConfigurationGroupName, Arg.Any<string>()).Returns(callInfo =>
		{
			var configurationKey = new ConfigurationKey(callInfo.ArgAt<string>(2), mockClientData.OrganizationalUnit);
			return new NullConfigurationSetting()
			{
				OrganizationalUnit = configurationKey.OrganizationalUnit,
				Key = configurationKey.Name
			};
		});
		var settingEditor = Substitute.For<IConfigurationApiValueWriter>();
		var dataStore = new MultitenantConfigurationDataStore();
		var classUnderTest = new PhyrosConfigurationProvider(settingReader, settingEditor, mockClientData, dataStore);
		var targetManagedKey = new ConfigurationKey(key, mockClientData.OrganizationalUnit);

		// act
		classUnderTest.Load();
		var found = classUnderTest.TryGet(targetManagedKey, out var foundValue);

		// assert
		await settingReader.Received(1).LoadConfigurationGroupAsync(mockClientData.OrganizationalUnit, mockClientData.ConfigurationGroupName);
		await settingReader.Received(shouldCallAddAdditionalConfigurationSettingAsyncCount).AddAdditionalConfigurationSettingAsync(Arg.Any<OrganizationalUnit>(), Arg.Any<string>(), Arg.Any<string>());

		var data = classUnderTest.GetDataSnapshot();
		data.Count.Should().Be(TestSettingsData.Count + shouldCallAddAdditionalConfigurationSettingAsyncCount + 1); // we add one for connection strings
		if (!String.IsNullOrWhiteSpace(orgUnitsWithNullSettingValues))
		{
			foreach (var orgUnitWithNullSettingValue in orgUnitsWithNullSettingValues.Split(','))
			{
				var settingKey = new ConfigurationKey(targetManagedKey.Name, orgUnitWithNullSettingValue.Trim());
				_outputHelper.WriteLine($"Identified that a {nameof(NullConfigurationSetting)} record should exist for Organizational Unit '{settingKey.OrganizationalUnit}' with key '{settingKey}'");
				data[settingKey].Should().BeOfType<NullConfigurationSetting>();
			}
		}
		found.Should().BeTrue();
		foundValue.Should().BeEquivalentTo(expectedValue);
	}
}
