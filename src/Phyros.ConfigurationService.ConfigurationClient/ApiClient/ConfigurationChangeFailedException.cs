namespace Phyros.ConfigurationService.ConfigurationClient.ApiClient;

public class ConfigurationChangeFailedException : Exception
{
	public ConfigurationChangeFailedException(HttpResponseMessage result) : base(
		$"Failure to update configuration through the Configuration API, HttpStatusCode: {result.StatusCode}, Content: {result.Content.ReadAsStringAsync()}")
	{

	}
}
