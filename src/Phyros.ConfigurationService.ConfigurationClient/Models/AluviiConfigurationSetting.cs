namespace Phyros.ConfigurationService.ConfigurationClient.Models;

public class PhyrosConfigurationSetting : IConfigurationSetting
{
	public string Key { get; set; } = string.Empty;
	
	public string? Value { get; set; }

	public string? ValueType { get; set; }

	public string OrganizationalUnit { get; set; } = string.Empty;

	public bool Locked { get; set; }
}
