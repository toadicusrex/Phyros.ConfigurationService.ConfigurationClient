using Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement.EventHandlers;
using Phyros.OrganizationalUnits;

namespace Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement;

public interface IConfigurationState
{
	event ConfigurationSettingChangedEventHandler? ConfigurationSettingChanged;
	event ConfigurationSettingDeletedEventHandler? ConfigurationSettingDeleted;
	OrganizationalUnit ConfigurationOrganizationalUnit { get; }
	string TenancyConnectionStringName { get; }
	EventChangeHandler? OnChange { get; set; }
	void SetOrganizationalUnit(string organizationalUnitName);
	void SetOptions(ConfigurationClientOptions options);
	void TriggerChangeHandling(string key);
	void TriggerDeleteHandling(OrganizationalUnit organizationalUnit, string key);
	void SetBaseConnectionStringName(string? baseConnectionStringName);
	void TriggerIgnoredChangeHandling(string key);
}
