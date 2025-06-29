using Microsoft.Extensions.Configuration;

namespace Phyros.ConfigurationService.ConfigurationClient.Wireup;
internal static class UrlBuilder
{
	public static Uri Build(IConfiguration configuration)
	{
		return new Uri($"https://{configuration["ConfigurationApiHostName"]!}");
		;
	}
}
