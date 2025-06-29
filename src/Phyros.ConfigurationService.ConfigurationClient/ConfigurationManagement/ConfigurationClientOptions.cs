using Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement.EventHandlers;

namespace Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement;
public class ConfigurationClientOptions
{
	public ConfigurationSettingChangedEventHandler? OnChanged { get; private set; }
	public ConfigurationSettingDeletedEventHandler? OnDeleted { get; private set; }
	public ConfigurationSettingIgnoredChangeEventHandler? OnChangeIgnored { get; private set; }

	public void SetChangeHandler(ConfigurationSettingChangedEventHandler onChanged)
	{
		OnChanged = onChanged;
	}

	public void SetDeletedHandler(ConfigurationSettingDeletedEventHandler onDeleted)
	{
		OnDeleted = onDeleted;
	}

	public void SetIgnoredChangeHandler(ConfigurationSettingIgnoredChangeEventHandler onIgnoredChange)
	{
		OnChangeIgnored = onIgnoredChange;
	}
}
