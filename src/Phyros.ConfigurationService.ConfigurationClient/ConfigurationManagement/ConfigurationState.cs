using System.Collections.Concurrent;
using Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement.EventHandlers;
using Phyros.OrganizationalUnits;

namespace Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement;

internal class ConfigurationState : IConfigurationState
{
	public event ConfigurationSettingChangedEventHandler? ConfigurationSettingChanged;
	public event ConfigurationSettingDeletedEventHandler? ConfigurationSettingDeleted;
	public event ConfigurationSettingIgnoredChangeEventHandler? ConfigurationSettingChangeIgnored;

	public OrganizationalUnit ConfigurationOrganizationalUnit { get; private set; } = "";
	public string TenancyConnectionStringName { get; private set; } = "TenancyDatabaseConnection";

	private static readonly ConcurrentBag<string> _managedKeys = [];

	public void SetOrganizationalUnit(string organizationalUnitName)
	{
		ConfigurationOrganizationalUnit = organizationalUnitName ?? throw new ArgumentException("Parameter cannot be null.", nameof(organizationalUnitName));
	}

	public void SetBaseConnectionStringName(string? baseConnectionStringName)
	{
		TenancyConnectionStringName = baseConnectionStringName ?? throw new ArgumentException("Parameter cannot be null.", nameof(baseConnectionStringName));
	}

	public void AddManagedKey(string key)
	{
		var loweredKey = key.ToLower();
		if (!_managedKeys.Contains(loweredKey))
		{
			_managedKeys.Add(loweredKey);
		}

		if (ConfigurationSettingChanged is not null)
		{
			ConfigurationSettingChanged.Invoke(key);
		}
	}

	public void SetManagedKeys(IEnumerable<string> keySet)
	{
		foreach (var key in keySet)
		{
			var loweredKey = key.ToLower();
			if (!_managedKeys.Contains(loweredKey))
			{
				_managedKeys.Add(loweredKey);
			}
		}
	}

	public bool HasManagedKey(string key)
	{
		return _managedKeys.Contains(key.ToLower());
	}

	public EventChangeHandler? OnChange { get; set; } = null;

	public void SetOptions(ConfigurationClientOptions options)
	{
		if (options.OnChanged is not null)
		{
			ConfigurationSettingChanged += options.OnChanged;
		}

		if (options.OnDeleted is not null)
		{
			ConfigurationSettingDeleted += options.OnDeleted;
		}

		if (options.OnChangeIgnored is not null)
		{
			ConfigurationSettingChangeIgnored += options.OnChangeIgnored;
		}
	}

	public void TriggerChangeHandling(string key)
	{
		if (ConfigurationSettingChanged != null)
		{
			ConfigurationSettingChanged.Invoke(key);
		}
	}

	public void TriggerDeleteHandling(OrganizationalUnit organizationalUnit, string key)
	{
		if (ConfigurationSettingDeleted != null)
		{
			ConfigurationSettingDeleted.Invoke(organizationalUnit, key);
		}
	}

	public void TriggerIgnoredChangeHandling(string key)
	{
		if (ConfigurationSettingChangeIgnored != null)
		{
			ConfigurationSettingChangeIgnored.Invoke(key);
		}
	}
}

public delegate void EventChangeHandler(string key);
