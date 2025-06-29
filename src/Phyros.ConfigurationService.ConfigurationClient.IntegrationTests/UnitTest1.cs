namespace Phyros.ConfigurationService.ConfigurationClient.IntegrationTests;

public class UnitTest1 : IClassFixture<StartupFixture>
{
	private readonly StartupFixture _startupFixture;

	public UnitTest1(StartupFixture startupFixture)
	{
		_startupFixture = startupFixture;
	}
	//[Fact]
	//public async Task Ping_should_connect_Async()
	//{
	//	//var apiClient = new ConfigurationApiClient(_startupFixture.NetFrameworkHttpClientFactory, "configuration.dev.phyros.net");
	//	//var result = await apiClient.PingAsync();
	//	//result.Should().BeTrue();
	//}

}
