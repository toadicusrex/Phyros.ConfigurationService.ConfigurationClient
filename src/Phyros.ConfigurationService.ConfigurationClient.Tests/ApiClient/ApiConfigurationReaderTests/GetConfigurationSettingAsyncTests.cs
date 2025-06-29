namespace Phyros.ConfigurationService.ConfigurationClient.Tests.ApiClient.ApiConfigurationReaderTests;
public class GetConfigurationSettingAsyncTests
{
	//[Fact]
	//public async Task Should_get_a_config_setting()
	//{
	//	// arrange
	//	var callbackWasCalled = false;
	//	var mockFactory = new MockHttpClientFactoryGenerator().SetCallback((message, CancellationToken) =>
	//	{
	//		callbackWasCalled = true;
	//	}).Build(HttpStatusCode.OK, (await File.ReadAllTextAsync("ApiClient/ApiConfigurationReaderTests/AddAdditionalConfigurationSettingAsyncTests.json"))!);

	//	var mockConfiguration = Substitute.For<IConfiguration>();
	//	mockConfiguration["ConfigurationApiHostName"].Returns("configuration.phyros.nonexistent");
	//	var startupOrganizationalUnitName = Guid.NewGuid().ToString();
	//	var key = Guid.NewGuid().ToString();

	//	// act
	//	var classUnderTest = new ConfigurationApiClient(mockFactory, mockConfiguration);
	//	var result = await classUnderTest.GetConfigurationSettingAsync(startupOrganizationalUnitName, key);

	//	// assert
	//	callbackWasCalled.Should().BeTrue();
	//	var setting1 = result as PhyrosConfigurationSetting;
	//	setting1.Should().NotBeNull();
	//	setting1!.Name.Should().BeEquivalentTo("Key1");
	//	setting1.Locked.Should().BeTrue();
	//	setting1.OrganizationalUnit.Should().BeEquivalentTo("org.one");
	//	setting1.Value.Should().BeEquivalentTo("Value 1");
	//	setting1.ValueType.Should().BeEquivalentTo("ConnectionString");
	//}
}
