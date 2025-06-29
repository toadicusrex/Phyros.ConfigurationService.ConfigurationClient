using Phyros.OrganizationalUnits;

namespace Phyros.ConfigurationService.ConfigurationClient.Wireup;
internal class ConfigurationClientData
{
	public OrganizationalUnit OrganizationalUnit { get; set; } = null!;
	public string BaseConnectionStringName { get; set; } = null!;
	public string ConfigurationGroupName { get; set; } = null!;
}
