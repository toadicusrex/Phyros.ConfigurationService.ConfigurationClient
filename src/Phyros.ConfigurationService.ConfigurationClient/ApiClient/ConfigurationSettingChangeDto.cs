namespace Phyros.ConfigurationService.ConfigurationClient.ApiClient;

public class ConfigurationSettingChangeDto
{
	public ConfigurationSettingChangeDto(string? value, string? valueType, bool locked)
	{
		Value = value;
		ValueType = valueType ?? "string";
		Locked = locked;
	}

	public string? Value { get; private set; }
	public string ValueType { get; private set; }
	public bool Locked { get; private set; } = false;
}
