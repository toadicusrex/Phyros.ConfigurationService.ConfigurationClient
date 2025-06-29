using System.Net;

namespace Phyros.ConfigurationService.ConfigurationClient.Models;
internal class NullConfigurationSetting : IConfigurationSetting
{
	public string Key { get; set; } = string.Empty;
	public string? Content { get; set; }
	public HttpStatusCode? Status { get; set; }
	public string OrganizationalUnit { get; set; }
}
