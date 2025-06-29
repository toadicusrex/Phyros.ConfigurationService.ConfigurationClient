namespace Phyros.ConfigurationService.ConfigurationClient.Tests.ApiClient.ApiConfigurationReaderTests;
public class LoadConfigurationGroupAsyncTests
{
	//[Fact]
	//public async Task Should_load_configuration_group()
	//{
	//	// arrange
	//	var callbackWasCalled = false;
	//	var mockFactory = new MockHttpClientFactoryGenerator().SetCallback((message, CancellationToken) =>
	//	{
	//		callbackWasCalled = true;
	//	}).Build(HttpStatusCode.OK, (await File.ReadAllTextAsync("ApiClient/ApiConfigurationReaderTests/LoadConfigurationGroupAsyncTests.json"))!);

	//	var mockConfiguration = Substitute.For<IConfiguration>();
	//	mockConfiguration["ConfigurationApiHostName"].Returns("configuration.phyros.nonexistent");
	//	var startupOrganizationalUnitName = Guid.NewGuid().ToString();
	//	var configurationGroupName = Guid.NewGuid().ToString();

	//	// act
	//	var classUnderTest = new ConfigurationApiClient(mockFactory, mockConfiguration);
	//	var result = await classUnderTest.LoadConfigurationGroupAsync(startupOrganizationalUnitName, configurationGroupName);

	//	// assert
	//	callbackWasCalled.Should().BeTrue();
		
	//	result.Count().Should().Be(2);

	//	var setting1 = result.SingleOrDefault(x => x.Name == "Key1") as PhyrosConfigurationSetting;
	//	setting1.Should().NotBeNull();
	//	setting1!.Name.Should().BeEquivalentTo("Key1");
	//	setting1.Locked.Should().BeTrue();
	//	setting1.OrganizationalUnit.Should().BeEquivalentTo("org.one");
	//	setting1.Value.Should().BeEquivalentTo("Value 1");
	//	setting1.ValueType.Should().BeEquivalentTo("ConnectionString");

	//	var setting2 = result.SingleOrDefault(x => x.Name == "Key2") as PhyrosConfigurationSetting;
	//	setting2.Should().NotBeNull();
	//	setting2!.Name.Should().BeEquivalentTo("Key2");
	//	setting2.Locked.Should().BeTrue();
	//	setting2.OrganizationalUnit.Should().BeEquivalentTo("org.two");
	//	setting2.Value.Should().BeEquivalentTo("Value 2");
	//	setting2.ValueType.Should().BeEquivalentTo("string");
	//}
}
