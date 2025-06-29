using Phyros.ConfigurationService.ConfigurationClient.Models;
using Phyros.OrganizationalUnits;

namespace Phyros.ConfigurationService.ConfigurationClient.ApiClient;
public interface IConfigurationApiReader
{
	Task<bool> PingAsync();
	Task<IEnumerable<IConfigurationSetting>?> LoadConfigurationGroupAsync(OrganizationalUnit organizationalUnit, string configurationGroupName);
	Task<IEnumerable<IConfigurationSetting>?> LoadUngroupedSettingsAsync(OrganizationalUnit organizationalUnit, IEnumerable<string> keys);
	Task<IConfigurationSetting?> AddAdditionalConfigurationSettingAsync(OrganizationalUnit organizationalUnit, string configurationGroupName, string key);
	Task<IConfigurationSetting> GetConfigurationSettingAsync(OrganizationalUnit organizationalUnit, string key);
	Task<string> GetClientConnectionString(OrganizationalUnit organizationalUnit);
	
}
